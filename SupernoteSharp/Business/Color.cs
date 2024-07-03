using SupernoteSharp.Common;
using System;

namespace SupernoteSharp.Business
{
    public class ColorPalette
    {
        public string Mode { get; private set; }
        public int Black { get; private set; }
        public int DarkGray { get; private set; }
        public int Gray { get; private set; }
        public int White { get; private set; }
        public int Transparent { get; private set; }
        public int DarkGray_Compatible { get; private set; }
        public int Gray_Compatible { get; private set; }

        public ColorPalette(string mode = Constants.MODE_GRAYSCALE, int[] colors = null)
        {
            if (mode != Constants.MODE_GRAYSCALE && mode != Constants.MODE_RGB)
                throw new ArgumentException("mode must be MODE_GRAYSCALE or MODE_RGB");

            if (colors == null || colors.Length != 6)
                throw new ArgumentException("colors must have 6 color values (black, darkgray, gray, white, darkgray_compatible, gray_compatible)");

            Mode = mode;
            Black = colors[0];
            DarkGray = colors[1];
            Gray = colors[2];
            White = colors[3];
            Transparent = (mode == Constants.MODE_GRAYSCALE) ? Constants.GRAYSCALE_TRANSPARENT : Constants.RGB_TRANSPARENT;
            DarkGray_Compatible = colors[4];
            Gray_Compatible = colors[5];
        }
    }

    public static class DefaultColorPalette
    {
        public static ColorPalette Grayscale { get; }
            = new ColorPalette(Constants.MODE_GRAYSCALE, new int[] {
                Constants.GRAYSCALE_BLACK,
                Constants.GRAYSCALE_DARK_GRAY,
                Constants.GRAYSCALE_GRAY,
                Constants.GRAYSCALE_WHITE,
                Constants.GRAYSCALE_DARK_GRAY_COMPATIBLE,
                Constants.GRAYSCALE_GRAY_COMPATIBLE});

        public static ColorPalette Rgb { get; }
            = new ColorPalette(Constants.MODE_RGB, new int[] {
                Constants.RGB_BLACK,
                Constants.RGB_DARK_GRAY,
                Constants.RGB_GRAY,
                Constants.RGB_WHITE,
                Constants.RGB_RGB_DARK_GRAY_COMPATIBLE,
                Constants.RGB_RGB_GRAY_COMPATIBLE});
    }

    internal static class ColorUtilities
    {
        internal static (byte r, byte g, byte b) GetRgb(int value)
        {
            byte r = (byte)((value & 0xff0000) >> 16);
            byte g = (byte)((value & 0x00ff00) >> 8);
            byte b = (byte)(value & 0x0000ff);

            return (r, g, b);
        }

        internal static string WebString(int value, string mode = Constants.MODE_RGB)
        {
            if (mode == Constants.MODE_GRAYSCALE)
                return "#" + (value & 0xff).ToString("x2") + (value & 0xff).ToString("x2") + (value & 0xff).ToString("x2");
            else
            {
                (int r, int g, int b) = GetRgb(value);
                return "#" + r.ToString("x2") + g.ToString("x2") + b.ToString("x2");
            }
        }
    }
}