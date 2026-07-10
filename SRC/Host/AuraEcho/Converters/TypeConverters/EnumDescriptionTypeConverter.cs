using System;
using AuraEcho.Strings;
using System.ComponentModel;
using System.Reflection;

namespace AuraEcho.Converters.TypeConverters;

public class EnumDescriptionTypeConverter : EnumConverter
{
    public EnumDescriptionTypeConverter(Type type)
        : base(type)
    {
    }
    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            if (value != null)
            {
                FieldInfo fi = value.GetType().GetField(value.ToString());
                if (fi != null)
                {
                    var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
#if DEBUG
                    var appName = Labels.ResourceManager.GetString(nameof(Labels.AppName), Labels.Culture);
#endif
                    return attributes.Length > 0 && !String.IsNullOrEmpty(attributes[0].Description)
                           ? Labels.ResourceManager.GetString(attributes[0].Description)
                           : value.ToString();
                }
            }

            return string.Empty;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
