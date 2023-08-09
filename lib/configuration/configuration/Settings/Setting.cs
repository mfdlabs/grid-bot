using System;

namespace Configuration.Settings
{
    public interface ISetting
    {
        string Name { get; }
        string Value { get; }
        DateTime Updated { get; }
    }

    [Serializable]
    internal class Setting : ISetting
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTime Updated { get; set; }
    }
}
