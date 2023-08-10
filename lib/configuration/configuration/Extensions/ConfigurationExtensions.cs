using System;
using System.ComponentModel;
using System.Linq.Expressions;

using Configuration.Logging;

namespace Configuration
{
    public static class Utilities
    {
        public static void MonitorChanges<T, TVal>(this T settings, Expression<Func<T, TVal>> exp, Action propertyChangeHandler)
            where T : INotifyPropertyChanged
            => settings.MonitorChanges(exp, propertyChangeHandler, false);

        public static ISingleSetting<TVal> ToSingleSetting<T, TVal>(this T settings, Expression<Func<T, TVal>> exp)
            where T : class, INotifyPropertyChanged
            => new SingleSetting<T, TVal>(settings, exp.Compile(), GetPropertyName(exp));

        public static void ReadValueAndMonitorChanges<T, TVal>(this T settings, Expression<Func<T, TVal>> exp, Action propertyChangeHandler)
            where T : INotifyPropertyChanged
            => settings.MonitorChanges(exp, propertyChangeHandler, true);

        private static void MonitorChanges<T, TVal>(this T settings, Expression<Func<T, TVal>> exp, Action propertyChangeHandler, bool readValueFirst)
            where T : INotifyPropertyChanged
        {
            var getter = exp.Compile();
            var value = getter(settings);

            if (readValueFirst) SafelyInvokeAction(propertyChangeHandler);

            var name = GetPropertyName(exp);
            settings.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == name)
                {
                    var newValue = getter(settings);

                    if (newValue == null || !newValue.Equals(value))
                    {
                        SafelyInvokeAction(propertyChangeHandler);

                        value = newValue;
                    }
                }
            };
        }
        public static void MonitorChanges<T, TVal>(this T settings, Expression<Func<T, TVal>> exp, Action<TVal> propertyChangeHandler)
            where T : INotifyPropertyChanged
            => settings.MonitorChanges(exp, propertyChangeHandler, false);

        public static void ReadValueAndMonitorChanges<T, TVal>(this T settings, Expression<Func<T, TVal>> exp, Action<TVal> propertyChangeHandler)
            where T : INotifyPropertyChanged
            => settings.MonitorChanges(exp, propertyChangeHandler, true);

        private static void MonitorChanges<T, TVal>(this T settings, Expression<Func<T, TVal>> exp, Action<TVal> propertyChangeHandler, bool readValueFirst)
            where T : INotifyPropertyChanged
        {
            var getter = exp.Compile();
            var value = getter(settings);

            if (readValueFirst) SafelyInvokeAction(propertyChangeHandler, value);

            var name = GetPropertyName(exp);
            settings.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == name)
                {
                    var newValue = getter(settings);

                    if (newValue == null || !newValue.Equals(value))
                    {
                        SafelyInvokeAction(propertyChangeHandler, newValue);

                        value = newValue;
                    }
                }
            };
        }

        private static void SafelyInvokeAction(Action propertyChangeHandler)
        {
            try
            {
                propertyChangeHandler();
            }
            catch (Exception ex)
            {
                ConfigurationLogging.Error("{0}", ex.ToString());
            }
        }

        private static void SafelyInvokeAction<TVal>(Action<TVal> propertyChangeHandler, TVal propertyValue)
        {
            try
            {
                propertyChangeHandler(propertyValue);
            }
            catch (Exception ex)
            {
                ConfigurationLogging.Error("{0}", ex.ToString());
            }
        }

        private static string GetPropertyName<T, TVal>(Expression<Func<T, TVal>> exp)
            where T : INotifyPropertyChanged
            => ((MemberExpression)exp.Body).Member.Name;
    }
}
