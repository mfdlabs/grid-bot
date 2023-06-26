use std::time::Duration;

use serenity::framework::standard::macros::hook;
use serenity::model::user::OnlineStatus;
use vault_config::VaultConfigProvider;

use serenity::async_trait;
use serenity::framework::standard::StandardFramework;
use serenity::model::channel::Message;
use serenity::model::prelude::{Activity, Ready};
use serenity::prelude::*;

#[macro_use]
extern crate log;

// tokio::main

struct Handler {
    config: VaultConfigProvider,
}

#[async_trait]
impl EventHandler for Handler {
    // For now just read prefix from config
    async fn message(&self, ctx: Context, msg: Message) {
        let prefix = self.config.get::<String>("prefix").unwrap();
        let notice = self.config.get::<String>("notice").unwrap_or("".to_string());

        if msg.content.starts_with(&prefix) {
            // Get the command name
            let command_name = msg.content.split_whitespace().next().unwrap();

            if command_name.len() > prefix.len()
                && command_name
                    .chars()
                    .nth(prefix.len())
                    .unwrap()
                    .is_alphanumeric()
            {
                info!("Received command: {}", command_name);

                msg.reply_mention(ctx.http, notice).await.unwrap();
            }
        }
    }

    // On ready, print some information to standard out
    async fn ready(&self, ctx: Context, ready: Ready) {
        info!("{} is connected!", ready.user.name);

        // Set the activity
        let activity = self.config.get::<String>("activity").unwrap_or("".to_string());

        ctx.set_presence(
            Some(Activity::playing(activity)),
            OnlineStatus::DoNotDisturb,
        )
        .await;
    }
}

#[hook]
async fn before(_: &Context, msg: &Message, command_name: &str) -> bool {
    info!(
        "Got command '{}' by user '{}'",
        command_name, msg.author.name
    );

    true
}

#[tokio::main]
async fn main() {
    let vault_addr =
        std::env::var("VAULT_ADDR").unwrap_or_else(|_| "http://localhost:8200".to_string());
    let vault_token = std::env::var("VAULT_TOKEN").unwrap();
    let vault_mount = std::env::var("VAULT_MOUNT").unwrap_or_else(|_| "kv".to_string());
    let vault_path = std::env::var("VAULT_PATH").unwrap_or_else(|_| "config".to_string());

    // Refresh interval
    let refresh_interval = std::env::var("REFRESH_INTERVAL")
        .unwrap_or_else(|_| "60".to_string())
        .parse::<u64>()
        .unwrap();

    let provider = VaultConfigProvider::new(
        &vault_addr,
        &vault_token,
        &vault_mount,
        &vault_path,
        Duration::from_secs(refresh_interval),
    );

    // If the RUST_LOG environment variable is not set, use the logging config from vault

    if std::env::var("RUST_LOG").is_err() {
        let logging_config = provider
            .get::<String>("logging")
            .unwrap_or(format!("{}=info", env!("CARGO_CRATE_NAME")));

        std::env::set_var("RUST_LOG", logging_config);
    }

    env_logger::init();

    let prefix = provider.get::<String>("prefix").unwrap();

    let framework = StandardFramework::new()
        .configure(|c| c.prefix(prefix))
        .before(before);

    // Login with a bot token from the environment
    let token = provider.get::<String>("token").unwrap();

    let intents = GatewayIntents::non_privileged() | GatewayIntents::MESSAGE_CONTENT;
    let mut client = Client::builder(token, intents)
        .event_handler(Handler { config: provider })
        .framework(framework)
        .await
        .expect("Error creating client");

    let shard_manager = client.shard_manager.clone();

    tokio::spawn(async move {
        tokio::signal::ctrl_c()
            .await
            .expect("Could not register ctrl+c handler");
        shard_manager.lock().await.shutdown_all().await;
    });

    if let Err(why) = client.start_autosharded().await {
        error!("An error occurred while running the client: {:?}", why);
    }
}
