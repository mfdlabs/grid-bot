using System.Configuration;

namespace MFDLabs.Configuration.Elements.ConsulKv
{
    public class ConsulKvGroupConfigurationElements : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;
        protected override string ElementName => "consulKvConfigurationGroup";
        protected override ConfigurationPropertyCollection Properties => new ConfigurationPropertyCollection();

        public ConsulKvGroupConfigurationElement this[int idx]
        {
            get => (ConsulKvGroupConfigurationElement)BaseGet(idx);
            set
            {
                if (BaseGet(idx) != null) BaseRemoveAt(idx);
                BaseAdd(idx, value);
            }
        }
        public new ConsulKvGroupConfigurationElement this[string name] => (ConsulKvGroupConfigurationElement)BaseGet(name);

        public void Add(ConsulKvGroupConfigurationElement item) => BaseAdd(item);
        public void Remove(ConsulKvGroupConfigurationElement item) => BaseRemove(item);
        public void RemoveAt(int index) => BaseRemoveAt(index);
        protected override ConfigurationElement CreateNewElement() => new ConsulKvGroupConfigurationElement();
        protected override object GetElementKey(ConfigurationElement element) => (element as ConsulKvGroupConfigurationElement)?.GroupName;
    }
}
