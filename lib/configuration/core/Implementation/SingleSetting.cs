namespace Configuration;

using System;
using System.ComponentModel;

using Logging;

internal class SingleSetting<T, TVal> : ISingleSetting<TVal>, INotifyPropertyChanged, IDisposable
    where T : class, INotifyPropertyChanged
{
    private readonly T _settings;
    private readonly string _settingName;
    private readonly Func<T, TVal> _valueGetter;

    private readonly ILogger _logger;

    public TVal Value { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;

    public SingleSetting(T settings, Func<T, TVal> valueGetter, string settingName, ILogger logger = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _valueGetter = valueGetter ?? throw new ArgumentNullException(nameof(valueGetter));
        _settingName = settingName ?? throw new ArgumentNullException(nameof(settingName));

        _logger = logger;

        settings.PropertyChanged += SettingsOnPropertyChanged;

        Value = valueGetter(settings);
    }

    private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var value = default(TVal);

        try { value = _valueGetter(_settings); }
        catch (Exception ex)
        {
            _logger?.Error(ex);

            return;
        }

        if (e.PropertyName == _settingName && !Equals(Value, value))
        {
            Value = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public void Dispose()
    {
        _settings.PropertyChanged -= SettingsOnPropertyChanged;
    }
}
