namespace Redis;

using System;
using System.Globalization;
using System.ComponentModel;

/// <summary>
/// Converter class from converting <see cref="RedisEndpoints"/> to strings.
/// </summary>
public class RedisEndpointsConverter : TypeConverter
{
    /// <inheritdoc cref="TypeConverter.CanConvertFrom(ITypeDescriptorContext, Type)"/>
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) 
        => sourceType == typeof(string);

    /// <inheritdoc cref="TypeConverter.ConvertFrom(ITypeDescriptorContext, CultureInfo, object)"/>
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string endpoints) return new RedisEndpoints(endpoints);

        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc cref="TypeConverter.ConvertTo(ITypeDescriptorContext, CultureInfo, object, Type)"/>
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string)) return (value as RedisEndpoints).Source;

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
