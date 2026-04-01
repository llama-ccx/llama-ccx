using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Resources;

namespace Llama.Gh
{
    internal static class IconLoader
    {
        internal const string ResourceBaseName = "Llama.Properties.Resources";
        internal const string DefaultIconKey = "Llama_24x24";
        internal const string DefaultIconResource = "Llama.Resources.Llama_24x24.png";

        public static Bitmap Load(string key, string fallbackResourceName = null)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                try
                {
                    var resourceManager = new ResourceManager(ResourceBaseName, Assembly.GetExecutingAssembly());
                    if (resourceManager.GetObject(key) is Bitmap bitmap)
                        return CloneToArgb(bitmap);
                }
                catch
                {
                    // fallback to manifest resource
                }
            }

            return LoadFromAssemblyResource(fallbackResourceName ?? DefaultIconResource);
        }

        public static Bitmap LoadDefaultIcon() => Load(DefaultIconKey, DefaultIconResource);

        public static Bitmap LoadFromAssemblyResource(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                return null;

            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                using (var source = new Bitmap(stream, false))
                    return CloneToArgb(source);
            }
        }

        private static Bitmap CloneToArgb(Bitmap source)
        {
            // Clone to a stable 32bpp ARGB bitmap detached from stream/palette.
            var icon = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(icon))
                g.DrawImage(source, 0, 0, source.Width, source.Height);

            return icon;
        }

        public static Bitmap InvertColors(Bitmap source)
        {
            var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var pixel = source.GetPixel(x, y);
                    result.SetPixel(x, y, Color.FromArgb(
                        pixel.A,
                        255 - pixel.R,
                        255 - pixel.G,
                        255 - pixel.B));
                }
            }
            return result;
        }
    }
}
