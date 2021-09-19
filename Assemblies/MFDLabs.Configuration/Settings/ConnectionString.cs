using System;

namespace MFDLabs.Configuration.Settings
{
    public interface IConnectionString
    {
        string GroupName { get; }
        string Name { get; }
        string Value { get; }
        DateTime Updated { get; }
    }

    internal class ConnectionString : IConnectionString
    {
        public string GroupName { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTime Updated { get; set; }
    }
}
