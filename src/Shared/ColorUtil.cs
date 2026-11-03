// ============================================================================
//  ConsoleSplash —— 颜色工具 / color helpers
// ============================================================================
using System;
using System.Drawing;
using System.Globalization;

namespace ConsoleSplash.Shared
{
    public static class ColorUtil
    {
        /// <summary>解析 #RGB / #RRGGBB / #AARRGGBB 或颜色名 / parse CSS-like colors.</summary>
        public static Color Parse(string value, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            string text = value.Trim();

            if (text.StartsWith("#", StringComparison.Ordinal))
            {
                string hex = text.Substring(1);
                try
                {
                    if (hex.Length == 3)
                    {
                        int r = int.Parse(new string(hex[0], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        int g = int.Parse(new string(hex[1], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        int b = int.Parse(new string(hex[2], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        return Color.FromArgb(r, g, b);
                    }

                    if (hex.Length == 6)
                    {
                        int rgb = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        return Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
                    }

                    if (hex.Length == 8)
                    {
                        uint argb = uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        return Color.FromArgb((int)((argb >> 24) & 255), (int)((argb >> 16) & 255),
                            (int)((argb >> 8) & 255), (int)(argb & 255));
                    }
                }
                catch
                {
                    return fallback;
                }
            }

            try
            {
                Color named = Color.FromName(text);
                if (named.IsKnownColor || named.A != 0) return named;
            }
            catch
            {
            }

            return fallback;
        }

        /// <summary>线性混合两色 / linear blend.</summary>
        public static Color Blend(Color a, Color b, double amount)
        {
            if (amount < 0) amount = 0;
            if (amount > 1) amount = 1;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * amount),
                (int)(a.R + (b.R - a.R) * amount),
                (int)(a.G + (b.G - a.G) * amount),
                (int)(a.B + (b.B - a.B) * amount));
        }

        /// <summary>转为 CSS 十六进制 / CSS hex string.</summary>
        public static string ToCss(Color color)
        {
            return "#" + color.R.ToString("X2", CultureInfo.InvariantCulture)
                + color.G.ToString("X2", CultureInfo.InvariantCulture)
                + color.B.ToString("X2", CultureInfo.InvariantCulture);
        }

        /// <summary>COLORREF / Win32 COLORREF value.</summary>
        public static uint ToColorRef(Color color)
        {
            return (uint)(color.R | (color.G << 8) | (color.B << 16));
        }
    }
}
