using System.Collections.ObjectModel;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface ITransferManager
    {
        ObservableCollection<ITransferTask> AllTasks { get; }
        void AddTask(ITransferTask task);
        ITransferTask GetTaskById(string id);
    }
}
