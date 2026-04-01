using Prism.Mvvm;

namespace PluginPacker.Models;

public class PluginItem : BindableBase
{
    #region private members
    private string _id;
    public string _name;
    private PluginItemType _type;
    #endregion

    public PluginItem()
    {
        Id = Guid.NewGuid().ToString();
    }

    public PluginItem(string name)
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
    }

    public PluginItemType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
