//  Copyright (c) 2020-present amlovey
//  
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Yade.Runtime
{
    public abstract class SerializableDictionary<KeyType, ValueType> : ISerializationCallbackReceiver
    {
        [SerializeField]
        private KeyType[] keys;

        [SerializeField]
        private ValueType[] values;

        public Dictionary<KeyType, ValueType> innerDictionary;

        public int Count
        {
            get
            {
                return innerDictionary == null ? 0 : innerDictionary.Count;
            }
        }

        public SerializableDictionary()
        {
            innerDictionary = new Dictionary<KeyType, ValueType>();
        }

        public void OnAfterDeserialize()
        {
            var length = keys.Length;
            innerDictionary = new Dictionary<KeyType, ValueType>(length);
            for (int i = 0; i < length; i++)
            {
                innerDictionary[keys[i]] = values[i];
            }

            keys = null;
            values = null;
        }

        public void OnBeforeSerialize()
        {
            int length = innerDictionary.Count;
            keys = new KeyType[length];
            values = new ValueType[length];

            int index = 0;
            foreach (KeyValuePair<KeyType, ValueType> pair in innerDictionary)
            {
                keys[index] = pair.Key;
                values[index] = pair.Value;
                index++;
            }
        }

        public KeyType[] Keys
        {
            get
            {
                return innerDictionary.Keys.ToArray();
            }
        }

        public ValueType[] Values
        {
            get
            {
                return innerDictionary.Values.ToArray();
            }
        }

        public bool ContainsKey(KeyType key)
        {
            return innerDictionary.ContainsKey(key);
        }

        public void Add(KeyType key, ValueType value)
        {
            innerDictionary.Add(key, value);
        }

        public void Remove(KeyType key)
        {
            innerDictionary.Remove(key);
        }

        public void Clear()
        {
            innerDictionary.Clear();
        }

        public ValueType this[KeyType key]
        {
            get { return innerDictionary[key]; }
            set { innerDictionary[key] = value; }
        }
    }

    public abstract class IntIndexedSerializableDictionary<T> : SerializableDictionary<int, T> 
    {
        /// <summary>
        /// Change the keys which is larger and equals base key
        /// </summary>
        /// <param name="basedKey">base key</param>
        /// <param name="delta">Delta of change</param>
        internal void MoveBy(int baseKey, int delta)
        {
            var keys = this.Keys;
            var values = this.Values;

            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] >= baseKey)
                {
                    keys[i] += delta;
                }
            }

            this.innerDictionary = new Dictionary<int, T>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                innerDictionary[keys[i]] = values[i];
            }
        }

        /// <summary>
        /// Delete items by indexes and then move the keys of the others.
        /// </summary>
        /// <param name="indexes">Indexes to be removed</param>
        internal void DeleteWithMove(int[] indexes)
        {
            if (indexes == null || indexes.Length == 0)
            {
                return;
            }

            foreach (var index in indexes)
            {
                this.Remove(index);
            }

            var keys = this.Keys;
            var values = this.Values;

            for (int i = 0; i < keys.Length; i++)
            {
                int key = keys[i];
                key -= GetDeleteMoveSteps(indexes, key);
                keys[i] = key;
            }

            this.innerDictionary = new Dictionary<int, T>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                innerDictionary[keys[i]] = values[i];
            }
        }

        private int GetDeleteMoveSteps(int[] indexes, int key)
        {
            int count = 0;
            for (int i = 0; i < indexes.Length; i++)
            {
                if (indexes[i] < key)
                {
                    count++;
                }
            }

            return count;
        }
    }
}