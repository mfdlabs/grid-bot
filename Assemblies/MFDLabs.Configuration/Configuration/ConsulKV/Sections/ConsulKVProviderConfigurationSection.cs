using System.Configuration;
using MFDLabs.Configuration.Elements.ConsulKV;

namespace MFDLabs.Configuration.Sections.ConsulKV
{
    public class ConsulKVProviderConfigurationSection : ConfigurationSection
    {
        static ConsulKVProviderConfigurationSection()
        {
            _groups = new ConfigurationProperty("vaultConfigurationGroups", typeof(ConsulKVGroupConfigurationElements), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_groups);
        }

        public ConsulKVGroupConfigurationElements Groups
        {
            get { return (ConsulKVGroupConfigurationElements)base[_groups]; }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                return _props;
            }
        }

        private static readonly ConfigurationPropertyCollection _props = new ConfigurationPropertyCollection();
        private static readonly ConfigurationProperty _groups;
    }
}
