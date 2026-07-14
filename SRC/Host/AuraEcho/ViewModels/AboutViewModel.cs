using Prism.Mvvm;
using System;
using System.Reflection;

namespace AuraEcho.ViewModels;

public class AboutViewModel : BindableBase
{
    public Version CurrentVersion
    {
        get;
        set => SetProperty(ref field, value);
    }

    public AboutViewModel()
    {

        CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
    }
}
