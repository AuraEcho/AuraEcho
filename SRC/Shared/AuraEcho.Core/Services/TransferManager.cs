using System.Collections.ObjectModel;
using AuraEcho.PluginContracts.Interfaces;
using Prism.Mvvm;

namespace AuraEcho.Core.Services;

public class TransferManager : BindableBase, ITransferManager
{
    public ObservableCollection<ITransferTask> AllTasks { get; } = [];

    public async void AddTask(ITransferTask task)
    {
        if (AllTasks.Any(t => t.Id == task.Id)) return;

        AllTasks.Add(task);
        await task.Start();
        AllTasks.Remove(task);
    }

    public ITransferTask GetTaskById(string id)
        => AllTasks.FirstOrDefault(t => t.Id == id);
}
