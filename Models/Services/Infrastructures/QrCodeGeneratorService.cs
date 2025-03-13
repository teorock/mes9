using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.Rendering;

#nullable enable

namespace mes.Models.Services.Infrastructures
{
    /// <summary>
    /// Service for generating QR codes using ZXing.Net library.
    /// </summary>
    public class QrCodeGeneratorService
    {
        /// <summary>
        /// Generates a QR code as a Bitmap from the provided input string.
        /// </summary>
        /// <param name="inputString">The content to encode in the QR code</param>
        /// <param name="dimension">The size (width and height) of the generated QR code in pixels</param>
        /// <returns>A Bitmap containing the QR code image, or null if the input is empty</returns>
        public Bitmap? GenerateQrCode(string? inputString, int dimension)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return null;
            }

            var barcodeWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = dimension,
                    Height = dimension,
                    ErrorCorrection = ErrorCorrectionLevel.Q,
                    Margin = 1
                }
            };

            var pixelData = barcodeWriter.Write(inputString);

            Bitmap bitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                ImageLockMode.WriteOnly,
                bitmap.PixelFormat);

            try
            {
                System.Runtime.InteropServices.Marshal.Copy(
                    pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        /// <summary>
        /// Converts a bitmap image to grayscale.
        /// </summary>
        /// <param name="bmp">The bitmap to convert</param>
        /// <returns>The grayscale bitmap</returns>
        public Bitmap ToGrayScale(Bitmap bmp)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    int rgb = (int)Math.Round(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
                    bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
                }
            }
            return bmp;
        }
    }
}

//using System;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.IO;
//using ZXing;
//using ZXing.QrCode;
//using ZXing.QrCode.Internal;
//using ZXing.Rendering;
//
//#nullable enable
//
//namespace mes.Models.Services.Infrastructures
//{
//    /// <summary>
//    /// Service for generating QR codes using ZXing.Net library.
//    /// </summary>
//    public class QrCodeGeneratorService
//    {
//        /// <summary>
//        /// Generates a QR code as a byte array from the provided input string.
//        /// </summary>
//        /// <param name="inputString">The content to encode in the QR code</param>
//        /// <param name="dimension">The size (width and height) of the generated QR code in pixels</param>
//        /// <returns>A byte array containing the QR code image in PNG format, or null if the input is empty</returns>
//        public byte[]? GenerateQrCode(string? inputString, int dimension)
//        {
//            if (string.IsNullOrEmpty(inputString))
//            {
//                return null;
//            }
//
//            var barcodeWriter = new BarcodeWriterPixelData
//            {
//                Format = BarcodeFormat.QR_CODE,
//                Options = new QrCodeEncodingOptions
//                {
//                    Width = dimension,
//                    Height = dimension,
//                    ErrorCorrection = ErrorCorrectionLevel.Q,
//                    Margin = 1
//                }
//            };
//
//            var pixelData = barcodeWriter.Write(inputString);
//            
//            using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
//            using var ms = new MemoryStream();
//            
//            var bitmapData = bitmap.LockBits(
//                new Rectangle(0, 0, pixelData.Width, pixelData.Height),
//                ImageLockMode.WriteOnly,
//                bitmap.PixelFormat);
//            
//            try
//            {
//                System.Runtime.InteropServices.Marshal.Copy(
//                    pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
//            }
//            finally
//            {
//                bitmap.UnlockBits(bitmapData);
//            }
//            
//            bitmap.Save(ms, ImageFormat.Png);
//            return ms.ToArray();
//        }
//
//        /// <summary>
//        /// Converts a bitmap image to grayscale.
//        /// </summary>
//        /// <param name="bmp">The bitmap to convert</param>
//        /// <returns>The grayscale bitmap</returns>
//        public Bitmap ToGrayScale(Bitmap bmp)
//        {
//            for (int y = 0; y < bmp.Height; y++)
//            {
//                for (int x = 0; x < bmp.Width; x++)
//                {
//                    Color c = bmp.GetPixel(x, y);
//                    int rgb = (int)Math.Round(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
//                    bmp.SetPixel(x, y, Color.FromArgb(rgb, rgb, rgb));
//                }
//            }
//            return bmp;
//        }
//    }
//}
