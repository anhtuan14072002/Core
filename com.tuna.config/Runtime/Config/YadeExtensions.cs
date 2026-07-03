using System;
using System.Collections.Generic;
using Yade.Runtime;

namespace Core
{
    public static class YadeExtensions
    {
        public static DictList<K, V> AsDictList<K, V>(this YadeSheetData sheetData, Func<V, K> keySelector)
            where V : class
        {
            var list = sheetData.AsList<V>();
            DictList<K, V> map = new DictList<K, V>();
            foreach (var item in list)
            {
                K key = keySelector(item);
                if (key != null)
                    map.Add(key, item);
            }

            return map;
        }

        public static DictDict<K, I, V> AsDictDict<K, I, V>(this YadeSheetData sheetData, Func<V, K> keySelector,
            Func<V, I> indexSelector) where V : class
        {
            var list = sheetData.AsList<V>();
            DictDict<K, I, V> map = new DictDict<K, I, V>();
            foreach (var item in list)
            {
                K key = keySelector(item);
                I index = indexSelector(item);
                if (key != null && index != null)
                    map.Add(key, index, item);
            }

            return map;
        }

        public static DictDict<K, I, List<V>> AsDictDictList<K, I, V>(this YadeSheetData sheetData,
            Func<V, K> keySelector, Func<V, I> indexSelector) where V : class
        {
            var list = sheetData.AsList<V>();
            DictDict<K, I, List<V>> map = new DictDict<K, I, List<V>>();
            foreach (var item in list)
            {
                K key = keySelector(item);
                I index = indexSelector(item);
                if (key != null && index != null)
                {
                    if (!map.ContainsKey(key))
                        map.Add(key,new Dictionary<I, List<V>>());
                    if (!map[key].ContainsKey(index))
                        map[key].Add(index, new List<V>());
                    map[key][index].Add(item);
                }
            }

            return map;
        }
    }
}