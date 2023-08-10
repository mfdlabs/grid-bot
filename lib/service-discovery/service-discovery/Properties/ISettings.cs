namespace ServiceDiscovery.Properties;

using System;
using System.ComponentModel;

internal interface ISettings : INotifyPropertyChanged
{
    string ConsulAddress { get; }
    TimeSpan ConsulBackoffBase { get; }
    TimeSpan ConsulLongPollingMaxWaitTime { get; }
    TimeSpan MaximumConsulBackoff { get; }
}
