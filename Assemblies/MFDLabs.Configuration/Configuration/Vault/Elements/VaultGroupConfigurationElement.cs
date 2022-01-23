using System;
using System.Configuration;

namespace MFDLabs.Configuration.Elements.Vault
{
    public enum VaultAuthenticationType
    {
        Token,
        AppRole,
        LDAP
    }

    public class VaultGroupConfigurationElement : ConfigurationElement
    {
        static VaultGroupConfigurationElement()
        {
            _updateInterval = new ConfigurationProperty("updateInterval", typeof(TimeSpan), TimeSpan.FromMinutes(30), ConfigurationPropertyOptions.None);
            _props.Add(_updateInterval);
            _groupName = new ConfigurationProperty("groupName", typeof(string), "*", ConfigurationPropertyOptions.None);
            _props.Add(_groupName);
            _vaultAddress = new ConfigurationProperty("address", typeof(string), null, ConfigurationPropertyOptions.None);
            _props.Add(_vaultAddress);
            _authType = new ConfigurationProperty("authType", typeof(VaultAuthenticationType), VaultAuthenticationType.Token, ConfigurationPropertyOptions.None);
            _props.Add(_authType);
            _vaultCredential = new ConfigurationProperty("credential", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_vaultCredential);
            _useFileBasedConfig = new ConfigurationProperty("useFileBasedConfig", typeof(bool), false, ConfigurationPropertyOptions.None);
            _props.Add(_useFileBasedConfig);
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

        public VaultAuthenticationType AuthenticationType
        {
            get => (VaultAuthenticationType)base[_authType];
            set => base[_authType] = value;
        }

        public string Address
        {
            get => (string)base[_vaultAddress];
            set => base[_vaultAddress] = value;
        }

        public string Credential
        {
            get => (string)base[_vaultCredential];
            set => base[_vaultCredential] = value;
        }

        public bool UseFileBasedConfig
        {
            get => (bool)base[_useFileBasedConfig];
            set => base[_useFileBasedConfig] = value;
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
        private static readonly ConfigurationProperty _authType;
        private static readonly ConfigurationProperty _vaultAddress;
        private static readonly ConfigurationProperty _vaultCredential;
        private static readonly ConfigurationProperty _useFileBasedConfig;
        private static readonly ConfigurationProperty _overrideFileName;
    }
}
