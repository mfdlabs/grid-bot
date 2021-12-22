using System.Configuration;
using MFDLabs.Configuration.Elements.ConsulKv;

namespace MFDLabs.Configuration.Sections.ConsulKv
{
    public class ConsulKvProviderConfigurationSection : ConfigurationSection
    {
        static ConsulKvProviderConfigurationSection()
        {
            _groups = new ConfigurationProperty("vaultConfigurationGroups",
                typeof(ConsulKvGroupConfigurationElements),
                null,
                ConfigurationPropertyOptions.IsRequired);
            _props.Add(_groups);
        }

        public ConsulKvGroupConfigurationElements Groups => (ConsulKvGroupConfigurationElements)base[_groups];
        protected override ConfigurationPropertyCollection Properties => _props;

        private static readonly ConfigurationPropertyCollection _props = new ConfigurationPropertyCollection();
        private static readonly ConfigurationProperty _groups;
    }
}
