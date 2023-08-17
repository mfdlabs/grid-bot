using System.Configuration;
using Configuration.Elements.Vault;

namespace Configuration.Sections.Vault
{
    public class VaultProviderConfigurationSection : ConfigurationSection
    {
        static VaultProviderConfigurationSection()
        {
            _groups = new ConfigurationProperty("vaultConfigurationGroups",
                typeof(VaultGroupConfigurationElements),
                null,
                ConfigurationPropertyOptions.IsRequired);
            _props.Add(_groups);
        }

        public VaultGroupConfigurationElements Groups => (VaultGroupConfigurationElements)base[_groups];
        protected override ConfigurationPropertyCollection Properties => _props;

        private static readonly ConfigurationPropertyCollection _props = new ConfigurationPropertyCollection();
        private static readonly ConfigurationProperty _groups;
    }
}
