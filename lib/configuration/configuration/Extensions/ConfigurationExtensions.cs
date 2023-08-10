using System;
using System.ComponentModel;
using System.Linq.Expressions;

using Configuration.Logging;

using Text.Extensions;
using Reflection.Extensions;

#nullable enable

namespace Configuration
{
    public static class Extensions
    {
        // There's a special kind of var here that does like this:
        // ${{ env.VAR_NAME }}, which is a special kind of var that will be replaced with the value of the environment variable VAR_NAME.
        // This is useful when you want to pass the value of an environment variable to a command.
        // We have to parse this out, and replace it with the value of the environment variable.
        public static TResult? FromEnvironmentExpression<TResult>(this object setting)
        {
            if (setting is not string str) return default;
            if (str.IsNullOrEmpty()) return str.To<TResult>();

            // Trim the input
            str = str.Trim();

            // Remove the spaces from the input.
            str = str.Replace(" ", "");

            // Check if the input contains the special var
            if (!str.StartsWith("${{")) return str.To<TResult>();

            // Split the input into parts
            var parts = str.Split(new[] { "${{" }, StringSplitOptions.None);

            // We now need to get the part in the middle of ${{ }}
            var otherPart = parts[1];

            // Split the middle part into parts
            var middleParts = otherPart.Split(new[] { "}}" }, StringSplitOptions.None);

            // Get the name of the environment variable
            var middlePart = middleParts[0];

            // Check if the middle part starts with env.
            if (!middlePart.ToLower().StartsWith("env.")) return str.To<TResult>();

            // Get the env var name
            var envVarName = middlePart.Remove(0, 4);

            // Check if the env var is empty
            if (envVarName.IsNullOrWhiteSpace()) return str.To<TResult>();

            // Replace the env var value with the env var name
            return Environment.GetEnvironmentVariable(envVarName).To<TResult>();
        }

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
