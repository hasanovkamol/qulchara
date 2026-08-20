using System;
using QRCoder;

namespace OpenBudget.Application.Services;

public class QrCodeService : IQrCodeService
{
    public byte[] GenerateQrCode(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20); // 20 pixels per module
    }
}
