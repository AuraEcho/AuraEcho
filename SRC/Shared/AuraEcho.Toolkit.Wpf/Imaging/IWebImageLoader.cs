using System.Windows.Media.Imaging;

namespace AuraEcho.Toolkit.Wpf.Imaging
{
    public interface IWebImageLoader
    {
        Task<BitmapImage> GetImageAsync(string url, int decodeWidth = 0, int decodeHeight = 0);
    }
}
