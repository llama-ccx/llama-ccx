using Grasshopper.Kernel;
using System.Drawing;

namespace Llama.Gh.Widgets
{
    public class StandardFont
    {
        public static Font font()
        {
            return GH_FontServer.StandardAdjusted;
        }

        public static Font largeFont()
        {
            return GH_FontServer.LargeAdjusted;
        }
    }
}
