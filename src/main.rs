#[macro_use]
extern crate tracing;

mod vault;

use std::env;
use std::time::Duration;

use anyhow::{Error, Result};
use poise::serenity_prelude::*;
use poise::{Framework, FrameworkOptions};

use crate::vault::Provider;

pub type Context<'a> = poise::Context<'a, Data, Error>;
pub type ApplicationContext<'a> = poise::ApplicationContext<'a, Data, Error>;

pub struct Data {}

#[poise::command(slash_command, prefix_command)]
async fn debug(ctx: Context<'_>) -> Result<()> {
    ctx.say("Hello, world!").await?;

    Ok(())
}

#[tokio::main]
async fn main() {
    tracing_subscriber::fmt::init();

    info!("Hello, world!");

    let vault_addr = env::var("VAULT_ADDR").expect("env variable `VAULT_ADDR` is not set");
    let vault_token = env::var("VAULT_TOKEN").expect("env variable `VAULT_TOKEN` is not set");
    let vault_mount = env::var("VAULT_MOUNT").expect("env variable `VAULT_MOUNT` is not set");
    let vault_path = env::var("VAULT_PATH").expect("env variable `VAULT_PATH` is not set");

    let provider = Provider::new(
        &vault_addr,
        &vault_token,
        &vault_mount,
        &vault_path,
        Duration::from_secs(60),
    )
    .await;

    let framework = Framework::builder()
        .options(FrameworkOptions {
            commands: vec![debug()],
            pre_command: |ctx| {
                Box::pin(async move {
                    debug!("{} -> {}", ctx.author().tag(), ctx.invoked_command_name());
                })
            },
            post_command: |ctx| {
                Box::pin(async move {
                    debug!("{} <- {}", ctx.author().tag(), ctx.invoked_command_name());
                })
            },
            ..Default::default()
        })
        .token(provider.get::<String>("token").await.unwrap())
        .intents(GatewayIntents::all())
        .setup(|ctx, _ready, framework| {
            Box::pin(async move {
                poise::builtins::register_globally(ctx, &framework.options().commands).await?;

                Ok(Data {})
            })
        });

    framework.run().await.unwrap();
}
