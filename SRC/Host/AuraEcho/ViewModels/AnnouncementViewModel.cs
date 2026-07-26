using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.PluginContracts.Constants;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace AuraEcho.ViewModels;

public class AnnouncementViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
{
    private readonly IRegionManager _regionManager;
    private readonly IAnnouncementService _announcementService;

    public ReadOnlyObservableCollection<AnnouncementEntry> Announcements => _announcementService.Announcements;

    /// <summary>
    /// 当前选中的公告。选中即视为阅读，红点随之消失。
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
    }

    public bool KeepAlive => false;

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        ((INotifyCollectionChanged)Announcements).CollectionChanged -= OnAnnouncementsCollectionChanged;
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 打开即选中第一条（最新一条），该条随即被标记为已读。
        if (TrySelectFirst()) return;

        // 列表尚未拉取到数据时，等首批公告到达后再补选。
        ((INotifyCollectionChanged)Announcements).CollectionChanged += OnAnnouncementsCollectionChanged;
    }

    private void OnAnnouncementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (TrySelectFirst())
            ((INotifyCollectionChanged)Announcements).CollectionChanged -= OnAnnouncementsCollectionChanged;
    }

    private bool TrySelectFirst()
    {
        if (SelectedAnnouncement is not null) return true;
        if (Announcements.Count == 0) return false;

        SelectedAnnouncement = Announcements[0];
        return true;
    }
}
