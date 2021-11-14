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
            _vaultRoleID = new ConfigurationProperty("roleId", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_vaultRoleID);
            _vaultSecretID = new ConfigurationProperty("secretId", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_vaultSecretID);
            _overrideFileName = new ConfigurationProperty("overrideFileName", typeof(string), null, ConfigurationPropertyOptions.None);
            _props.Add(_overrideFileName);
        }

        public string GroupName
        {
            get { return (string)base[_groupName]; }
            set { base[_groupName] = value; }
        }

        public TimeSpan UpdateInterval
        {
            get { return (TimeSpan)base[_updateInterval]; }
            set { base[_updateInterval] = value; }
        }

        public string Address
        {
            get { return (string)base[_vaultAddress]; }
            set { base[_vaultAddress] = value; }
        }

        public string RoleID
        {
            get { return (string)base[_vaultRoleID]; }
            set { base[_vaultRoleID] = value; }
        }

        public string SecretID
        {
            get { return (string)base[_vaultSecretID]; }
            set { base[_vaultSecretID] = value; }
        }

        public string OverrideFileName
        {
            get { return (string)base[_overrideFileName]; }
            set { base[_overrideFileName] = value; }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                return _props;
            }
        }

        private static readonly ConfigurationPropertyCollection _props = new ConfigurationPropertyCollection();

        private static readonly ConfigurationProperty _updateInterval;
        private static readonly ConfigurationProperty _groupName;
        private static readonly ConfigurationProperty _vaultAddress;
        private static readonly ConfigurationProperty _vaultRoleID;
        private static readonly ConfigurationProperty _vaultSecretID;
        private static readonly ConfigurationProperty _overrideFileName;
    }
}
