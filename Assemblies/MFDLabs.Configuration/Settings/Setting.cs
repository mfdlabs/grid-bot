using System;

namespace MFDLabs.Configuration.Settings
{
    public interface ISetting
    {
        string GroupName { get; }
        string Name { get; }
        string Type { get; }
        string Value { get; }
        DateTime Updated { get; }
    }

    internal class Setting : ISetting
    {
        public string GroupName { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public DateTime Updated { get; set; }
    }
}
