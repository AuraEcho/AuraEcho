using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using AuraEcho.Core.Models;
using AuraEcho.UIToolkit.Converters;

namespace AuraEcho.Converters;

/// <summary>
/// 插件图标转换器
/// </summary>
public class PluginIconConverter : MarkupExtension, IValueConverter
{
    public PluginIconConverter _instance;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AppPlugin pr) return null;

        var iconPath = new PathCombineConverter().Convert([pr.WorkingDirectory, pr.Icon], null, null, null);
        var imageSource = new StringToImageSourceConverter().Convert(iconPath, null, null, null);
        return imageSource;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
        => _instance ??= new PluginIconConverter();
}
