namespace Configuration;

using System;

/// <summary>
/// Attribute to define the setting name for a property.
/// 
/// This is only used for the <see cref="Configuration.VaultProvider"/>,
/// in the case where the call to GetOrDefault is using a key that is different
/// from the property name.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class SettingNameAttribute : Attribute
{
    /// <summary>
    /// The name of the setting.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Construct a new instance of <see cref="SettingNameAttribute"/>
    /// </summary>
    /// <param name="name">The name of the setting</param>
    public SettingNameAttribute(string name)
    {
        Name = name;
    }
}
