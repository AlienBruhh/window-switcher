using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;

namespace WindowToggleLauncher.Services;

public class QrCodeService
{
    public static BitmapImage GenerateQrCodeImage(string text, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);

        var image = new BitmapImage();
        using var stream = new MemoryStream(qrCodeBytes);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze(); // Freezing allows WPF to share the bitmap across threads safely

        return image;
    }

    public static string GenerateQrCodeBase64(string text, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
        return "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
    }
}
