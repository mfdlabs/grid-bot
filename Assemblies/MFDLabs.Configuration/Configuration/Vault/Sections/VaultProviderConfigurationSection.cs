using System.Configuration;
using MFDLabs.Configuration.Elements.Vault;

namespace MFDLabs.Configuration.Sections.Vault
{
    public class VaultProviderConfigurationSection : ConfigurationSection
    {
        static VaultProviderConfigurationSection()
        {
            _groups = new ConfigurationProperty("vaultConfigurationGroups", typeof(VaultGroupConfigurationElements), null, ConfigurationPropertyOptions.IsRequired);
            _props.Add(_groups);
        }

        public VaultGroupConfigurationElements Groups
        {
            get { return (VaultGroupConfigurationElements)base[_groups]; }
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
