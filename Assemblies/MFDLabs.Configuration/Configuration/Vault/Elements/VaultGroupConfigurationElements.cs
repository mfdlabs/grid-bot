using System.Configuration;

namespace MFDLabs.Configuration.Elements.Vault
{
    public class VaultGroupConfigurationElements : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        protected override string ElementName => "vaultConfigurationGroup";

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                return new ConfigurationPropertyCollection();
            }
        }

        public VaultGroupConfigurationElement this[int idx]
        {
            get { return (VaultGroupConfigurationElement)BaseGet(idx); }
            set
            {
                if (BaseGet(idx) != null) BaseRemoveAt(idx);
                BaseAdd(idx, value);
            }
        }

        new public VaultGroupConfigurationElement this[string name]
        {
            get { return (VaultGroupConfigurationElement)BaseGet(name); }
        }

        public void Add(VaultGroupConfigurationElement item)
        {
            BaseAdd(item);
        }

        public void Remove(VaultGroupConfigurationElement item)
        {
            BaseRemove(item);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new VaultGroupConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as VaultGroupConfigurationElement).GroupName;
        }
    }
}
