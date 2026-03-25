using AuraEcho.ExternalTools.Models;
using AuraEcho.ExternalTools.Utils;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace AuraEcho.ExternalTools.Converters;


public class ExternalToolIconConverter : MarkupExtension, IMultiValueConverter
{
    private static ExternalToolIconConverter _instance;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not ExternalToolType type) return null;

        var command = values[1] as string ?? throw new ArgumentException("values[1] is not a string");

        return type switch
        {
            ExternalToolType.None
            => new BitmapImage(new Uri("pack://application:,,,/AuraEcho.ExternalTools;component/Assets/Images/external_tool.png")),

            ExternalToolType.Website
            => new BitmapImage(new Uri($"{new UriBuilder(command).Uri.Scheme}://{new UriBuilder(command).Uri.Host}/favicon.ico")),
            //=> new BitmapImage(new Uri("pack://application:,,,/AuraEcho.ExternalTools;component/Assets/Images/web.png")),

            ExternalToolType.PathCommand
            => new BitmapImage(new Uri("pack://application:,,,/AuraEcho.ExternalTools;component/Assets/Images/command.png")),

            ExternalToolType.File
            => Win32Helper.GetIcon(command),
            _ => throw new NotImplementedException(),
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
            => _instance ??= new ExternalToolIconConverter();


}
