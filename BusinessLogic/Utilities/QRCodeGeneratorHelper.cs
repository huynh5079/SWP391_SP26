using QRCoder;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace BusinessLogic.Utilities
{
    public static class QRCodeGeneratorHelper
    {
        public static string GenerateQRCodeBase64(string payload)
        {
            if (string.IsNullOrEmpty(payload))
                return string.Empty;

            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeBytes = qrCode.GetGraphic(20);
                    return Convert.ToBase64String(qrCodeBytes);
                }
            }
        }
    }
}
