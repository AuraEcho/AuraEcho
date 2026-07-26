using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AuraEcho.Cloud.V1;
using AuraEcho.Cloud.V1.Models.Announcement;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Events;
using AuraEcho.Interfaces;
using AuraEcho.Models;
using AuraEcho.Persistence.Contracts;
using AuraEcho.Persistence.Entities;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Mvvm;

namespace AuraEcho.Services;

/// <inheritdoc cref="IAnnouncementService"/>
public class AnnouncementService : BindableBase, IAnnouncementService
{
    private readonly ApiClient _apiClient;
    private readonly IUserAnnouncementRepository _repository;
    private readonly IClientSession _clientSession;
    private readonly ILogger<AnnouncementService> _logger;

    /// <summary>
    /// 公告操作锁
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    private readonly ObservableCollection<AnnouncementEntry> _announcements = [];

    public AnnouncementService(
        ApiClient apiClient,
        IUserAnnouncementRepository repository,
        IClientSession clientSession,
        IEventAggregator eventAggregator,
        ILogger<AnnouncementService> logger)
    {
        _apiClient = apiClient;
        _repository = repository;
        _clientSession = clientSession;
        _logger = logger;

        Announcements = new ReadOnlyObservableCollection<AnnouncementEntry>(_announcements);

        eventAggregator.GetEvent<AnnouncementsChangedEvent>().Subscribe(() => _ = RefreshAsync());
        eventAggregator.GetEvent<SignedInEvent>().Subscribe(() => _ = RefreshAsync());
        eventAggregator.GetEvent<SignedOutEvent>().Subscribe(() => _ = RefreshAsync());
    }

    /// <inheritdoc />
    public ReadOnlyObservableCollection<AnnouncementEntry> Announcements { get; }

    /// <inheritdoc />
    public bool HasUnread
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <inheritdoc />
    public async Task RefreshAsync()
    {
        await _gate.WaitAsync();
        try
        {
            List<AnnouncementItem>? items = await FetchActiveAsync();

            if (items is null) return;

            Guid? userId = _clientSession.CurrentUser?.Id;

            // 未登录时没一律按已读处理。
            Dictionary<Guid, DateTime> readVersions = userId is null
                ? []
                : await LoadReadVersionsAsync(userId.Value, items);

            await OnUIThreadAsync(() => Apply(items, readVersions, isSignedIn: userId is not null));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "拉取公告失败");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task MarkReadAsync(AnnouncementEntry entry)
    {
        if (entry is null || !entry.IsUnread) return;

        Guid? userId = _clientSession.CurrentUser?.Id;
        if (userId is null) return;

        await _gate.WaitAsync();
        try
        {
            await _repository.MarkReadAsync(userId.Value, entry.Item.Id, entry.Item.UpdatedAt);

            await OnUIThreadAsync(() =>
            {
                entry.IsUnread = false;
                RecalculateHasUnread();
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "标记公告已读失败：{AnnouncementId}", entry.Item.Id);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 拉取当前生效的公告，按更新时间倒序。
    /// </summary>
    private async Task<List<AnnouncementItem>?> FetchActiveAsync()
    {
        GetActiveAnnouncementsResponse? response = await _apiClient.Announcement.GetActiveAsync();
        if (response is null) return null;

        return response.Announcements
                       .OrderByDescending(a => a.UpdatedAt)
                       .ToList();
    }

    /// <summary>
    /// 读取指定用户的已读记录，并清理已失效公告的残留记录。
    /// </summary>
    private async Task<Dictionary<Guid, DateTime>> LoadReadVersionsAsync(
        Guid userId,
        List<AnnouncementItem> items)
    {
        // 公告过期或被删除后其已读记录不再有意义，避免该表无限增长。
        await _repository.PruneAsync(userId, items.Select(a => a.Id));

        List<UserReadAnnouncement> records = await _repository.GetReadRecordsAsync(userId);

        // 理论上 (UserId, AnnouncementId) 有唯一索引，此处按 Max 归并只为容错。
        return records
            .GroupBy(r => r.AnnouncementId)
            .ToDictionary(g => g.Key, g => g.Max(r => r.ReadVersion));
    }

    /// <summary>
    /// 用最新公告与已读记录重建列表。
    /// </summary>
    /// <param name="isSignedIn">
    /// 未登录时无用户上下文，无从判断未读，全部按已读处理，保证红点不显示。
    /// </param>
    private void Apply(
        List<AnnouncementItem> items,
        Dictionary<Guid, DateTime> readVersions,
        bool isSignedIn)
    {
        _announcements.Clear();
        foreach (AnnouncementItem item in items)
        {
            bool isUnread =
                isSignedIn
                && (!readVersions.TryGetValue(item.Id, out DateTime readVersion)
                    || readVersion < item.UpdatedAt);

            _announcements.Add(new AnnouncementEntry(item, isUnread));
        }

        RecalculateHasUnread();
    }

    private void RecalculateHasUnread()
    {
        HasUnread = _announcements.Any(entry => entry.IsUnread);
    }

    /// <summary>
    /// 在 UI 线程上执行函数
    /// </summary>
    private static async Task OnUIThreadAsync(Action action)
    {
        if (Application.Current?.Dispatcher is not { } dispatcher || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        await dispatcher.InvokeAsync(action);
    }
}
