#nullable enable
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Beacon.Excel.Data
{
    internal static class ResourceBitmaps
    {
        private static readonly ConcurrentDictionary<string, Bitmap> _bitmaps = new ConcurrentDictionary<string, Bitmap>();
        private static readonly string _uriPrefix = $"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Images/";

        public static Bitmap GetBitmap(string name) => ResourceBitmaps._bitmaps.GetOrAdd(name, ResourceBitmaps.CreateBitmap);

        private static Bitmap CreateBitmap(string name)
        {
            BitmapSource source = new BitmapImage(new Uri(String.Concat(ResourceBitmaps._uriPrefix, name)));
            using MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new PngBitmapEncoder
            {
                Frames =
                {
                    BitmapFrame.Create(source)
                }
            };
            encoder.Save(stream);
            return new Bitmap(stream);
        }
    }
}