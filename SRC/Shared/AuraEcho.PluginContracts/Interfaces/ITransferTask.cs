using System.Threading.Tasks;
using AuraEcho.PluginContracts.Models;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface ITransferTask
    {
        string Id { get; }
        TransferType Type { get; }
        double Progress { get; }

        Task Start();
        void Cancel();
    }
}