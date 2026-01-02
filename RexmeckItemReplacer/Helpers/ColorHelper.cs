using System;

using UnityEngine;

namespace RexmeckItemReplacer.Helpers
{
    public static class ColorHelper
    {
        public static string ToHEX(this Color color)
            => $"{(int)(color.r * 255):X2}{(int)(color.g * 255):X2}{(int)(color.b * 255):X2}";

        public static Color FromHEX(this string hex)
        {
            if (hex.StartsWith('#'))
                hex = hex[1..];

            var r = Convert.ToInt32(hex[..2], 16) / 255f;
            var g = Convert.ToInt32(hex[2..4], 16) / 255f;
            var b = Convert.ToInt32(hex[4..6], 16) / 255f;
            var a = 1f;
            if (hex.Length == 8)
                a = Convert.ToInt32(hex[6..8], 16) / 255f;

            return new Color(r, g, b, a);
        }
    }
}
