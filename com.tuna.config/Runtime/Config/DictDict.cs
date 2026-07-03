using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class DictDict<K, I, V> : IDictionary<K, Dictionary<I, V>>
    {
        private Dictionary<K, Dictionary<I, V>> Dict { get; } = new Dictionary<K, Dictionary<I, V>>();
        public bool IsReadOnly => true;
        public int Count => Dict.Count;
        public int ItemCount(K key) => Dict.ContainsKey(key) ? Dict[key].Count : 0;
        public ICollection<K> Keys => Dict.Keys;
        public ICollection<Dictionary<I, V>> Values => Dict.Values;
        public ICollection<I> ItemKeys(K key) => Dict[key].Keys;
        public ICollection<V> ItemValues(K key) => Dict[key].Values;
        public Dictionary<I, V> this[K key]
        {
            get => Dict[key];
            set => Dict[key] = value;
        }
        public V this[K key, I index]
        {
            get => !Dict.ContainsKey(key) ? default : !Dict[key].ContainsKey(index) ? default : Dict[key][index];
            set => Add(key, index, value);
        }
        public IEnumerator<KeyValuePair<K, Dictionary<I, V>>> GetEnumerator()
        {
            return Dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<I, V>> GetEnumerator(K key)
        {
            return Dict[key].GetEnumerator();
        }

        public void Add(KeyValuePair<K, Dictionary<I, V>> item)
        {
            Dict.Add(item.Key, item.Value);
        }
        
        public void Add(K key, Dictionary<I, V> value)
        {
            Dict.Add(key, value);
        }
        
        public void Add(K key, I index, V value)
        {
            if(!Dict.ContainsKey(key))
                Dict.Add(key, new Dictionary<I, V>());
            if(!Dict[key].ContainsKey(index))
                Dict[key].Add(index, value);
            else
                Dict[key][index] = value;
        }

        public bool ContainsKey(K key)
        {
            return Dict.ContainsKey(key);
        }

        public bool ContainsKey(K key, I index)
        {
            return Dict.ContainsKey(key) && Dict[key].ContainsKey(index);
        }

        public bool Contains(KeyValuePair<K, Dictionary<I, V>> item)
        {
            return Dict.Contains(item);
        }

        public bool Contains(I index)
        {
            foreach (var dict in Dict)
            foreach (var item in dict.Value)
                if (item.Key.Equals(index))
                    return true;
            return false;
        }

        public bool Contains(V value)
        {
            foreach (var dict in Dict)
            foreach (var item in dict.Value)
                if (item.Value.Equals(value))
                    return true;
            return false;
        }

        public bool TryGetValue(K key, out Dictionary<I, V> value)
        {
            return Dict.TryGetValue(key, out value);
        }

        public bool TryGetValue(K key, I index, out V value)
        {
            if (Dict.TryGetValue(key, out Dictionary<I, V> dict))
                return dict.TryGetValue(index, out value);
            value = default;
            return false;
        }

        public bool Remove(KeyValuePair<K, Dictionary<I, V>> item)
        {
            return Dict.Remove(item.Key);
        }

        public bool Remove(K key)
        {
            return Dict.Remove(key);
        }

        public bool Remove(I index)
        {
            if (!FindKey(index, out K key)) 
                return false;
            return Dict.Remove(key);
        }

        public bool Remove(V value)
        {
            if (!FindValue(value, out K key, out I index))
                return false;
            return Dict[key].Remove(index);
        }

        public bool FindKey(I index, out K key)
        {
            foreach (var dict in Dict)
                foreach (var item in dict.Value)
                    if (item.Key.Equals(index))
                    {
                        key = dict.Key;
                        return true;
                    }
            key = default;
            return false;
        }
        
        public bool FindValue(V value, out K key, out I index)
        {
            foreach (var dict in Dict)
            foreach (var item in dict.Value)
                if (item.Value.Equals(value))
                {
                    key = dict.Key;
                    index = item.Key;
                    return true;
                }
            key = default;
            index = default;
            return false;
        }

        public void CopyTo(KeyValuePair<K, Dictionary<I, V>>[] array, int arrayIndex)
        {
            ((IDictionary<K, Dictionary<I, V>>)Dict).CopyTo(array, arrayIndex);
        }

        public void Clear()
        {
            Dict.Clear();
        }
    }
}