using System;
using System.ComponentModel;

using Configuration.Logging;

namespace Configuration
{
    internal class SingleSetting<T, TVal> : ISingleSetting<TVal>, INotifyPropertyChanged, IDisposable
        where T : class, INotifyPropertyChanged
    {
        private readonly T _Settings;
        private readonly Func<T, TVal> _ValueGetter;
        private readonly string _SettingName;

        public TVal Value { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SingleSetting(T settings, Func<T, TVal> valueGetter, string settingName)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _ValueGetter = valueGetter ?? throw new ArgumentNullException(nameof(valueGetter));
            _SettingName = settingName ?? throw new ArgumentNullException(nameof(settingName));

            settings.PropertyChanged += SettingsOnPropertyChanged;

            Value = valueGetter(settings);
        }

        private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var value = default(TVal);

            try { value = _ValueGetter(_Settings); }
            catch (Exception ex)
            {
                ConfigurationLogging.Error(ex.ToString());
                return;
            }

            if (e.PropertyName == _SettingName && !Equals(Value, value))
            {
                Value = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
        public void Dispose()
        {
            _Settings.PropertyChanged -= SettingsOnPropertyChanged;
        }
    }
}
