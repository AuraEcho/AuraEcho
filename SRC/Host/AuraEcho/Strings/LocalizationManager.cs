using System;
using System.Globalization;
using System.Threading;
using Prism.Mvvm;

namespace AuraEcho.Strings;

public class LocalizationManager : BindableBase
{
    public static LocalizationManager Current { get; private set; }

    public LocalizationManager()
    {
        Current = this;
        Labels = new Labels();
    }

    public Labels Labels { get; set; }
    private string _language;
    /// <summary>
    /// 获取或设置 Language 的值
    /// </summary>
    public string Language
    {
        get => _language;
        set
        {
            if (_language == value)
                return;

            _language = value;
            ChangeCulture(new CultureInfo(value));
        }
    }

    public static void ChangeCulture(CultureInfo cultureInfo)
    {
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Labels.Culture = cultureInfo;

        Current?.RaisePropertyChanged(String.Empty);
    }

    public static string GetString(string name)
        => Labels.ResourceManager.GetString(name, Labels.Culture);
}
