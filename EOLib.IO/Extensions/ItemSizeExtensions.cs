using System;

namespace EOLib.IO.Extensions
{
    public static class ItemSizeExtensions
    {
        public static (int Width, int Height) GetDimensions(this ItemSize itemSize)
        {
            var sizeStr = Enum.GetName(typeof(ItemSize), itemSize);
            if (sizeStr == null || sizeStr.Length != 7)
            {
                // Invalid / unknown size (e.g. placeholder item) — treat as 1x1 so
                // inventory slot math never uses 0 and SetSize never goes negative.
                return (1, 1);
            }

            var width = Convert.ToInt32(sizeStr.Substring(4, 1));
            var height = Convert.ToInt32(sizeStr.Substring(6, 1));

            return (Math.Max(1, width), Math.Max(1, height));
        }
    }
}
