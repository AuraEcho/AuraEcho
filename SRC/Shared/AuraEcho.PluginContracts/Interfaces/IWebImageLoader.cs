using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AuraEcho.PluginContracts.Interfaces
{
    public interface IWebImageLoader
    {
        Task<BitmapImage> GetImageAsync(string url, int decodeWidth = 0, int decodeHeight = 0);
    }
}
