using QRCoder;

namespace Service.DInspect.Helpers
{
    public class QRCodeHelper
    {
        QRCodeGenerator qrGenerator;

        public QRCodeHelper()
        {
            qrGenerator = new QRCodeGenerator();
        }

        public string GenerateStringQRCode(string text)
        {

            QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            Base64QRCode qrCode = new Base64QRCode(qrCodeData);

            return qrCode.GetGraphic(20);
        }
    }
}
