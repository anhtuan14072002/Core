using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class DictList<K, V> : IDictionary<K, List<V>>
    {
        private Dictionary<K, List<V>> Dict { get; } = new Dictionary<K, List<V>>();
        public bool IsReadOnly => true;
        public int Count => Dict.Count;
        public int ItemCount(K key) => Dict.ContainsKey(key) ? Dict[key].Count : 0;
        public ICollection<K> Keys => Dict.Keys;
        public ICollection<List<V>> Values => Dict.Values;
        public ICollection<V> Items(K key) => Dict[key];
        public List<V> this[K key]
        {
            get => Dict[key];
            set => Dict[key] = value;
        }
        public V this[K key, int index]
        {
            get => !Dict.ContainsKey(key) ? default : Dict[key][index];
            set => Add(key, index, value);
        }

        public IEnumerator<KeyValuePair<K, List<V>>> GetEnumerator()
        {
            return Dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<V> GetEnumerator(K key)
        {
            return Dict[key].GetEnumerator();
        }

        public void Add(KeyValuePair<K, List<V>> item)
        {
            Dict.Add(item.Key, item.Value);
        }
        
        public void Add(K key, List<V> value)
        {
            Dict.Add(key, value);
        }
        
        public void Add(K key, int index, V value)
        {
            if(!Dict.ContainsKey(key))
                Dict.Add(key, new List<V>());
            if (Contains(key, index))
                Dict[key][index] = value;
        }
        
        public void Add(K key, V value)
        {
            if(!Dict.ContainsKey(key))
                Dict.Add(key, new List<V>());
            Dict[key].Add(value);
        }
        
        public bool TryGetValue(K key, out List<V> value)
        {
            return Dict.TryGetValue(key, out value);
        }
        
        public bool TryGetValue(K key, int index, out V value)
        {
            if (Contains(key, index))
            {
                value = Dict[key][index];
                return true;
            }
            value = default;
            return false;
        }

        public bool ContainsKey(K key)
        {
            return Dict.ContainsKey(key);
        }

        public bool Contains(K key, int index)
        {
            return Dict.ContainsKey(key) && index >= 0 && index < Dict[key].Count;
        }
        
        public bool Contains(K key, V value)
        {
            return Dict.ContainsKey(key) && Dict[key].Contains(value);
        }

        public bool Contains(KeyValuePair<K, List<V>> item)
        {
            return Dict.Contains(item);
        }

        public bool Remove(KeyValuePair<K, List<V>> item)
        {
            return Dict.Remove(item.Key);
        }

        public bool Remove(K key)
        {
            return Dict.Remove(key);
        }

        public bool Remove(K key, V value)
        {
            if (!Contains(key, value)) 
                return false;
            Dict[key].Remove(value);
            return true;
        }
        
        public bool RemoveAt(K key, int index)
        {
            if (!Contains(key, index)) 
                return false;
            Dict[key].RemoveAt(index);
            return true;
        }

        public void CopyTo(KeyValuePair<K, List<V>>[] array, int arrayIndex)
        {
            ((IDictionary<K, List<V>>)Dict).CopyTo(array, arrayIndex);
        }
        
        public void Clear()
        {
            Dict.Clear();
        }
    }
}