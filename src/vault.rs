use std::collections::HashMap;
use std::sync::Arc;
use std::time::{Duration, Instant};

use anyhow::{bail, Result};
use tokio::sync::Mutex;
use vaultrs::client::{VaultClient, VaultClientSettingsBuilder};
use vaultrs::kv2;

pub struct Provider {
    client: VaultClient,
    data: Arc<Mutex<HashMap<String, String>>>,
    expiration: Arc<Mutex<Instant>>,
    mount: String,
    path: String,
    interval: Duration,
    changed: Arc<Mutex<bool>>,
}

impl Provider {
    pub async fn new(addr: &str, token: &str, mount: &str, path: &str, interval: Duration) -> Self {
        let client = if token.contains(":") {
            todo!();
        } else {
            VaultClient::new(
                VaultClientSettingsBuilder::default()
                    .address(addr)
                    .token(token)
                    .build()
                    .unwrap(),
            )
            .unwrap()
        };

        let data = Arc::new(Mutex::new(HashMap::new()));

        debug!(
            "Initialized vault config provider with url: {}, mount: {}, path: {}",
            addr, mount, path
        );

        Self {
            client,
            data,
            expiration: Arc::new(Mutex::new(Instant::now())),
            mount: mount.to_owned(),
            path: path.to_owned(),
            interval,
            changed: Arc::new(Mutex::new(false)),
        }
    }

    async fn fetch_config(&self) -> HashMap<String, String> {
        kv2::read::<HashMap<String, String>>(&self.client, self.mount.as_str(), self.path.as_str())
            .await
            .unwrap()
    }

    async fn write_to_vault_if_changed(&self) -> bool {
        let mut changed = self.changed.lock().await;

        if !*changed {
            return false;
        }

        debug!(
            "Writing new configuration to vault at: {}/{}",
            self.mount, self.path
        );

        let data = self.data.lock().await.clone();

        kv2::set(&self.client, &self.mount, &self.path, &data)
            .await
            .unwrap();

        *changed = false;

        true
    }

    async fn refresh_if_needed(&self) {
        let mut expiration = self.expiration.lock().await;

        if *expiration < Instant::now() {
            debug!(
                "The configuration is expired, fetching new configuration from vault at: {}/{}",
                self.mount, self.path
            );

            if self.write_to_vault_if_changed().await {
                *expiration = Instant::now() + self.interval;
                return;
            }

            let data = self.fetch_config().await;
            let mut data_lock = self.data.lock().await;
            *data_lock = data;

            *expiration = Instant::now() + self.interval;
            debug!("Configuration refreshed, next refresh at: {:?}", expiration);
        }
    }

    pub async fn get<T>(&self, key: &str) -> Result<T>
    where
        T: std::str::FromStr,
        <T as std::str::FromStr>::Err: std::fmt::Debug,
    {
        self.refresh_if_needed().await;

        let data = self.data.lock().await;
        let value = data.get(key);

        match value {
            None => bail!("key not found: {}", key),
            Some(value) => Ok(value.parse().unwrap()),
        }
    }
}
