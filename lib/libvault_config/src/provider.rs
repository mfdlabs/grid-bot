// Class that handles fetching of configuration from vault
//
// Uses the refresh ahead functionality to ensure that the data is always up to date.
// Also ensures thread safety when accessing the data by using a mutex.
//
// Supports KV v1 and v2. As well as Token and AppRole authentication.

use std::{collections::HashMap, sync::Arc};

use tokio::sync::Mutex;

// vaultrs
use vaultrs::{
    auth::approle::*,
    client::{VaultClient, VaultClientSettingsBuilder},
    kv2,
};

// chrono
use chrono::{DateTime, Duration, Utc};

use log::debug;

// Config error
#[derive(Debug)]
pub enum ConfigError {
    KeyNotFound(String),
}

/// Struct that handles fetching of configuration from vault
pub struct VaultConfigProvider {
    data: Arc<Mutex<HashMap<String, String>>>,
    expiration: Arc<Mutex<DateTime<Utc>>>,
    interval: Duration,
    client: VaultClient,
    mount: String,
    path: String,

    // Whether or not the config has changed locally, allow mutation of the config
    changed_locally: Arc<Mutex<bool>>
}

impl VaultConfigProvider {
    /// Creates a new instance of the vault config provider
    ///
    /// # Arguments
    /// * `address` - The address of the vault server
    /// * `token` - The token to use for authentication
    /// * `mount` - The mount point of the KV engine
    /// * `path` - The path to the configuration in vault
    /// * `interval` - The interval to refresh the configuration at
    /// * `is_kv2` - Whether or not the KV engine is v2
    pub async fn new(
        address: &str,
        credential: &str,
        mount: &str,
        path: &str,
        interval: Duration,
    ) -> Self {
        let client = Self::get_client_based_on_credential(address, credential).await;

        let data = Arc::new(Mutex::new(HashMap::new()));

        debug!(
            "Initialized vault config provider with url: {}, mount: {}, path: {}",
            address, mount, path
        );

        Self {
            data,
            client,
            expiration: Arc::new(Mutex::new(Utc::now())),
            interval,
            mount: mount.to_string(),
            path: path.to_string(),
            changed_locally: Arc::new(Mutex::new(false))
        }
    }

    async fn get_client_based_on_credential(address: &str, credential: &str) -> VaultClient {
        // 2 different clients for token and approle
        // Token credential is just the raw token
        // AppRole in the form of role_id:secret_id:{optional: mount}

        if credential.contains(":") {
            let parts: Vec<&str> = credential.split(":").collect();
            let role_id = parts[0];
            let secret_id = parts[1];
            let mount = if parts.len() == 3 {
                parts[2]
            } else {
                "approle"
            };

            let temp = VaultClient::new(
                VaultClientSettingsBuilder::default()
                    .address(address)
                    .build()
                    .unwrap(),
            )
            .unwrap();

            login(&temp, mount, role_id, secret_id).await.unwrap();

            temp
        } else {
            VaultClient::new(
                VaultClientSettingsBuilder::default()
                    .address(address)
                    .token(credential)
                    .build()
                    .unwrap(),
            )
            .unwrap()
        }
    }

    async fn fetch_config(&self) -> HashMap<String, String> {
        kv2::read::<HashMap<String, String>>(&self.client, self.mount.as_str(), self.path.as_str())
            .await
            .unwrap()
    }

    async fn refresh_if_needed(&self) {
        let mut expiration = self.expiration.lock().await;

        if *expiration < Utc::now() {
            debug!(
                "The configuration is expired, fetching new configuration from vault at: {}/{}",
                self.mount, self.path
            );

            if self.write_to_vault_if_changed().await {
                *expiration = Utc::now() + self.interval;
                return;
            }

            let data = self.fetch_config().await;
            let mut data_lock = self.data.lock().await;

            *data_lock = data;

            *expiration = Utc::now() + self.interval;

            debug!("Configuration refreshed, next refresh at: {}", expiration);
        }
    }

    async fn write_to_vault_if_changed(&self) -> bool {
        let mut changed_locally = self.changed_locally.lock().await;

        if !*changed_locally {
            return false;
        }

        debug!(
            "Writing new configuration to vault at: {}/{}",
            self.mount, self.path
        );

        let data = self.data.lock().await.clone();

        kv2::set(&self.client, self.mount.as_str(), self.path.as_str(), &data).await.unwrap();

        *changed_locally = false;

        true
    }

    /// Gets the configuration value for the specified key
    ///
    /// # Type parameters
    /// * `T` - The type to convert to
    ///
    /// # Arguments
    /// * `key` - The key to get the value for
    pub async fn get<T>(&self, key: &str) -> Result<T, ConfigError>
    where
        T: std::str::FromStr,
        <T as std::str::FromStr>::Err: std::fmt::Debug,
    {
        self.refresh_if_needed().await;

        let data = self.data.lock().await;

        let value = data.get(key);

        match value {
            None => Err(ConfigError::KeyNotFound(key.to_string())),
            Some(value) => Ok(value.parse().unwrap()),
        }
    }

    /// Sets the configuration value for the specified key
    /// This will persist the value to vault after the next refresh
    ///
    /// # Type parameters
    /// * `T` - The type to convert to
    ///
    /// # Arguments
    /// * `key` - The key to set the value for
    /// * `value` - The value to set
    pub async fn set<T>(&mut self, key: &str, value: T)
    where
        T: std::string::ToString,
    {
        let mut data = self.data.lock().await;

        data.insert(key.to_string(), value.to_string());

        let mut changed_locally = self.changed_locally.lock().await;

        *changed_locally = true;
    }
}
