using System;
using System.Configuration;

namespace MFDLabs.Configuration.Elements.Vault
{
    public class VaultGroupConfigurationElement : ConfigurationElement
    {
        static VaultGroupConfigurationElement()
        {
            _updateInterval = new ConfigurationProperty("updateInterval", typeof(TimeSpan), TimeSpan.FromSeconds(1.5), ConfigurationPropertyOptions.None);
            _props.Add(_updateInterval);
            _groupName = new ConfigurationProperty("groupName", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_groupName);
            _vaultAddress = new ConfigurationProperty("address", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_vaultAddress);
            _vaultRoleId = new ConfigurationProperty("roleId", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_vaultRoleId);
            _vaultSecretId = new ConfigurationProperty("secretId", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_vaultSecretId);
            _overrideFileName = new ConfigurationProperty("overrideFileName", typeof(string), null, ConfigurationPropertyOptions.None);
            _props.Add(_overrideFileName);
        }

        public string GroupName
        {
            get => (string)base[_groupName];
            set => base[_groupName] = value;
        }

        public TimeSpan UpdateInterval
        {
            get => (TimeSpan)base[_updateInterval];
            set => base[_updateInterval] = value;
        }

        public string Address
        {
            get => (string)base[_vaultAddress];
            set => base[_vaultAddress] = value;
        }

        public string RoleId
        {
            get => (string)base[_vaultRoleId];
            set => base[_vaultRoleId] = value;
        }

        public string SecretId
        {
            get => (string)base[_vaultSecretId];
            set => base[_vaultSecretId] = value;
        }

        public string OverrideFileName
        {
            get => (string)base[_overrideFileName];
            set => base[_overrideFileName] = value;
        }

        protected override ConfigurationPropertyCollection Properties => _props;

        private static readonly ConfigurationPropertyCollection _props = new ConfigurationPropertyCollection();

        private static readonly ConfigurationProperty _updateInterval;
        private static readonly ConfigurationProperty _groupName;
        private static readonly ConfigurationProperty _vaultAddress;
        private static readonly ConfigurationProperty _vaultRoleId;
        private static readonly ConfigurationProperty _vaultSecretId;
        private static readonly ConfigurationProperty _overrideFileName;
    }
}
