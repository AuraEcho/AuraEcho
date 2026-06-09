using QRCoder;
using System.Windows.Media.Imaging;

namespace AuraEcho.Tools;

public static class QRCodeUtil
{
    public static BitmapSource GenerateQRCode(string url)
    {
        using QRCodeGenerator qrGenerator = new QRCodeGenerator();
        using QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20, false);
        return ToBitmapImage(qrCodeAsPngByteArr);
    }

    public static BitmapImage ToBitmapImage(byte[] data)
    {
        var bitmap = new BitmapImage();
        using (var ms = new System.IO.MemoryStream(data))
        {
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // 立即将流加载到内存
            bitmap.EndInit();
            bitmap.Freeze(); // 冻结对象，以便跨线程使用（UI 性能优化）
        }
        return bitmap;
    }
}
