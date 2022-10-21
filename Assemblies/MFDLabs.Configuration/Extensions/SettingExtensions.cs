using System;
using MFDLabs.Text.Extensions;
using MFDLabs.Reflection.Extensions;

#nullable enable

namespace MFDLabs.Configuration.Extensions
{
    public static class SettingExtensions
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

            // Get the env var value
            var env = Environment.GetEnvironmentVariable(envVarName);

            // Check if the env var value is empty, if so, return the original string
            if (env.IsNullOrEmpty()) return str.To<TResult>();

            // Replace the env var value with the env var name
            return env.To<TResult>();
        }
    }
}
