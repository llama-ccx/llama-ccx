using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.GUI.Canvas;
using Lama.UI;

namespace Lama.Gh
{
	public class LamaInfo : GH_AssemblyInfo
	{
		public override string Name => "Lama";
		public override Bitmap Icon => LoadIcon();
		public override string Description => "Lama Grasshopper plugin";
		public override Guid Id => new Guid("c8a9b2f8-1b2f-4c5a-8a45-6c0a4b0b8c12");
		public override string AuthorName => "Marco Pellegrino";
		public override string AuthorContact => "lama.calculix@gmail.com";

		private static Bitmap LoadIcon()
		{
			var icon = IconLoader.LoadDefaultIcon();
			return ThemeHelper.IsDarkMode() ? IconLoader.InvertColors(icon) : icon;
		}
	}

	public class LamaCategoryIcon : GH_AssemblyPriority
	{
		public override GH_LoadingInstruction PriorityLoad()
		{
			Instances.CanvasCreated += MenuLoad.OnStartup;

			Bitmap icon = LoadCategoryIcon();
			if (icon != null)
			{
				Instances.ComponentServer.AddCategoryIcon("Lama", icon);
			}
			Instances.ComponentServer.AddCategorySymbolName("Lama", '\u03BB');
			return GH_LoadingInstruction.Proceed;
		}

		private static Bitmap LoadCategoryIcon()
		{
			var icon = IconLoader.LoadDefaultIcon();
			return ThemeHelper.IsDarkMode() ? IconLoader.InvertColors(icon) : icon;
		}
	}

	internal static class ThemeHelper
	{
		internal static bool IsDarkMode()
		{
			try
			{
				var fill = GH_Skin.palette_white_standard.Fill;
				return fill.R < 128 && fill.G < 128 && fill.B < 128;
			}
			catch
			{
				return false;
			}
		}
	}
}
