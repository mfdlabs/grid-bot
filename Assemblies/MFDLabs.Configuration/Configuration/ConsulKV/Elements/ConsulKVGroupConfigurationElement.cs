using System;
using System.Configuration;

namespace MFDLabs.Configuration.Elements.ConsulKv
{
    public class ConsulKvGroupConfigurationElement : ConfigurationElement
    {
        static ConsulKvGroupConfigurationElement()
        {
            _updateInterval = new ConfigurationProperty("updateInterval", typeof(TimeSpan), TimeSpan.FromSeconds(1.5), ConfigurationPropertyOptions.None);
            _props.Add(_updateInterval);
            _groupName = new ConfigurationProperty("groupName", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_groupName);
            _consulAddress = new ConfigurationProperty("address", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_consulAddress);
            _consulAclToken = new ConfigurationProperty("token", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_consulAclToken);
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
            get => (string)base[_consulAddress];
            set => base[_consulAddress] = value;
        }

        public string AclToken
        {
            get => (string)base[_consulAclToken];
            set => base[_consulAclToken] = value;
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
        private static readonly ConfigurationProperty _consulAddress;
        private static readonly ConfigurationProperty _consulAclToken;
        private static readonly ConfigurationProperty _overrideFileName;
    }
}
