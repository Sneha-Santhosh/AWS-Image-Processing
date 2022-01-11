using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using GrapeCity.Documents.Drawing;
using GrapeCity.Documents.Text;
using GrapeCity.Documents.Imaging;

namespace SnehaSanthosh_Lab4_Serverless
{
    class GCImagingOperations
    {
        public static string GetConvertedImage(byte[] stream)
        {
            using (var bmp = new GcBitmap())
            {
                bmp.Load(stream);
                // Add watermark 
                var newImg = new GcBitmap();
                newImg.Load(stream);
                using (var g = bmp.CreateGraphics(Color.White))
                {
                    g.DrawImage(
                       newImg,
                       new RectangleF(0, 0, bmp.Width, bmp.Height),
                       null,
                       ImageAlign.Default
                       );
                }
                //  Convert to grayscale 
                bmp.ApplyEffect(GrayscaleEffect.Get(GrayscaleStandard.BT601));
                //  Resize to thumbnail 
                var resizedImage = bmp.Resize(100, 100, InterpolationMode.NearestNeighbor);
                return GetBase64(resizedImage);
            }
        }
        #region helper 
        private static string GetBase64(GcBitmap bmp)
        {
            using (MemoryStream m = new MemoryStream())
            {
                bmp.SaveAsPng(m);
                return Convert.ToBase64String(m.ToArray());
            }
        }
        #endregion
    }
}
