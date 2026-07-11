using System;
using System.Collections.Generic;
using System.Linq;

using Il2CppSLZ.Marrow.Warehouse;

namespace ItemReplacer.Helpers
{
    public static class WarehouseHelper
    {
        public static string[] GetBarcodes<T>(this List<T> scannables) where T : Scannable
        {
            if (scannables == null || scannables.Count == 0)
                return [];

            List<string> barcodes = [];
            scannables.ForEach(c => barcodes.Add(c.Barcode.ID));
            return [.. barcodes];
        }

        public static string[] GetBarcodes<T>(this Il2CppSystem.Collections.Generic.List<T> scannables) where T : Scannable
            => scannables.ToArray().ToList().GetBarcodes();

        public static string[] GetBarcodes<T, TScannable>(this List<T> references) where T : ScannableReference<TScannable> where TScannable : Scannable
            => (references ?? []).ConvertAll(x => x.Scannable).GetBarcodes();

        public static string[] GetBarcodes<T, TScannable>(this Il2CppSystem.Collections.Generic.List<T> references) where T : ScannableReference<TScannable> where TScannable : Scannable
            => (references ?? new()).ToArray().ToList().GetBarcodes<T, TScannable>();
    }
}