using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using AuraEcho.Core.Extensions;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class AnnouncementViewModel : BindableBase, IRegionMemberLifetime
{
    private readonly IRegionManager _regionManager;
    private readonly IAnnouncementService _announcementService;

    public ReadOnlyObservableCollection<AnnouncementEntry> Announcements
    {
        get;
    }

    /// <summary>
    /// 当前选中的公告
    /// </summary>
    public AnnouncementEntry? SelectedAnnouncement
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            if (value is not null)
                _ = _announcementService.MarkReadAsync(value);
        }
    }

    public DelegateCommand CloseCommand { get; }
    private void Close()
    {
        _regionManager.Regions[HostRegionNames.DialogRegion].RemoveAll();
    }

    public DelegateCommand<AnnouncementEntry> SwitchAnnouncementCommand { get; }
    private void SwitchAnnouncement(AnnouncementEntry targetAnnouncement)
    {
        SelectedAnnouncement = targetAnnouncement;
    }

    public AnnouncementViewModel(IRegionManager regionManager, IAnnouncementService announcementService)
    {
        _regionManager = regionManager;
        _announcementService = announcementService;

        CloseCommand = new DelegateCommand(Close);
        SwitchAnnouncementCommand = new DelegateCommand<AnnouncementEntry>(SwitchAnnouncement);
        
        Announcements = new ReadOnlyObservableCollection<AnnouncementEntry>(
            _announcementService.Announcements.ToObservableCollection());

        SelectedAnnouncement = _announcementService.Announcements.FirstOrDefault();
    }

    public bool KeepAlive => false;
}
