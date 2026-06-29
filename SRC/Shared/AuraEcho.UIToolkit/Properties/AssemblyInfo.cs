using System.Windows;
using System.Windows.Markup;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]
[assembly: XmlnsDefinition("http://ui.auraecho.com/", "AuraEcho.UIToolkit.Converters")]
[assembly: XmlnsDefinition("http://ui.auraecho.com/", "AuraEcho.UIToolkit.Behaviors")]
[assembly: XmlnsDefinition("http://ui.auraecho.com/", "AuraEcho.UIToolkit.EntranceEffects")]
[assembly: XmlnsDefinition("http://ui.auraecho.com/", "AuraEcho.UIToolkit.Tools")]
[assembly: XmlnsDefinition("http://ui.auraecho.com/", "AuraEcho.UIToolkit.MarkupExtensions")]