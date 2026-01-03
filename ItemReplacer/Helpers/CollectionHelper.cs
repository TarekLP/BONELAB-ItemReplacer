using System;
using System.Collections.Generic;

namespace ItemReplacer.Helpers
{
    public static class CollectionHelper
    {
        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var pair in dictionary)
                action?.Invoke(pair);
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
                action?.Invoke(item);
        }

        public static void ForEach<T>(this Il2CppSystem.Collections.Generic.HashSet<T> hashSet, Action<T> action)
        {
            foreach (var item in hashSet)
                action?.Invoke(item);
        }
    }
}
