using System.Configuration;

namespace MFDLabs.Configuration.Elements.ConsulKV
{
    public class ConsulKVGroupConfigurationElements : ConfigurationElementCollection
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

        public ConsulKVGroupConfigurationElement this[int idx]
        {
            get { return (ConsulKVGroupConfigurationElement)BaseGet(idx); }
            set
            {
                if (BaseGet(idx) != null) BaseRemoveAt(idx);
                BaseAdd(idx, value);
            }
        }

        new public ConsulKVGroupConfigurationElement this[string name]
        {
            get { return (ConsulKVGroupConfigurationElement)BaseGet(name); }
        }

        public void Add(ConsulKVGroupConfigurationElement item)
        {
            BaseAdd(item);
        }

        public void Remove(ConsulKVGroupConfigurationElement item)
        {
            BaseRemove(item);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ConsulKVGroupConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as ConsulKVGroupConfigurationElement).GroupName;
        }
    }
}
