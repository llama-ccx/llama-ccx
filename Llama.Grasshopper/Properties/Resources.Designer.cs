using System.Drawing;

namespace Llama.Gh.Properties
{
    // Lightweight strongly-typed resource access for component icons.
    internal static class Resources
    {
        private static Bitmap _llama24x24;
        private static Bitmap _llama;

        internal static Bitmap Llama_24x24 =>
            _llama24x24 ?? (_llama24x24 = IconLoader.Load("Llama_24x24"));

        internal static Bitmap Llama =>
            _llama ?? (_llama = IconLoader.Load("Llama"));
    }
}
