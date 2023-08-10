using System.ComponentModel;

namespace Configuration
{
    public interface ISingleSetting<T> : INotifyPropertyChanged
    {
        T Value { get; }
    }
}
