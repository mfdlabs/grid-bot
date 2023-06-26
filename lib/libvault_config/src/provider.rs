// Class that handles fetching of configuration from vault
//
// Uses the refresh ahead functionality to ensure that the data is always up to date.
// Also ensures thread safety when accessing the data by using a mutex.
//
// Supports KV v1 and v2. As well as Token and AppRole authentication.

use crate::refresh_ahead::RefreshAhead;

use std::{collections::HashMap, time::Duration};

// vaultrs
use vaultrs::{
    client::{VaultClient, VaultClientSettingsBuilder},
    kv2,
};

// futures_executor
use futures_executor::block_on;

use log::debug;

// Config error
#[derive(Debug)]
pub enum ConfigError {
    KeyNotFound(String),
}

/// Struct that handles fetching of configuration from vault
pub struct VaultConfigProvider {
    data: RefreshAhead<HashMap<String, String>>,
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
    pub fn new(address: &str, token: &str, mount: &str, path: &str, interval: Duration) -> Self {
        let client = VaultClient::new(
            VaultClientSettingsBuilder::default()
                .address(address)
                .token(token)
                .build()
                .unwrap(),
        )
        .unwrap();

        let mount_copy = mount.to_string();
        let path_copy = path.to_string();

        let data = RefreshAhead::new(
            Self::fetch_config(&client, mount, path),
            move || Self::fetch_config(&client, mount_copy.as_str(), path_copy.as_str()),
            interval,
        );

        debug!(
            "Initialized vault config provider with url: {}, mount: {}, path: {}",
            address, mount, path
        );

        Self { data }
    }

    fn fetch_config(client: &VaultClient, mount: &str, path: &str) -> HashMap<String, String> {
        block_on(async {
            kv2::read::<HashMap<String, String>>(client, mount, path)
                .await
                .unwrap()
        })
    }

    fn convert_value<T>(value: &str) -> T
    where
        T: std::str::FromStr,
        <T as std::str::FromStr>::Err: std::fmt::Debug,
    {
        value.parse::<T>().unwrap()
    }

    /// Gets the configuration value for the specified key
    ///
    /// # Type parameters
    /// * `T` - The type to convert to
    ///
    /// # Arguments
    /// * `key` - The key to get the value for
    pub fn get<T>(&self, key: &str) -> Result<T, ConfigError>
    where
        T: std::str::FromStr,
        <T as std::str::FromStr>::Err: std::fmt::Debug,
    {
        let data = self.data.get();

        let value = data.get(key);

        match value {
            None => Err(ConfigError::KeyNotFound(key.to_string())),
            Some(value) => Ok(Self::convert_value(value)),
        }
    }
}
