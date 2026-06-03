//  Copyright (c) 2022-present amlovey
//  
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Yade.Runtime.BinarySerialization;

namespace Yade.Runtime
{
    [Serializable]
    public class YadeWorkSheets : SerializableDictionary<string, YadeSheetData> { }

    /// <summary>
    /// Yade database
    /// </summary>
    [CreateAssetMenu(order = 121, menuName = "Yade Database", fileName = "YadeDB.asset")]
    [HelpURL("https://www.amlovey.com/YadeDocs/#/YadeDatabase?id=yadedatabase")]
    [Serializable]
    public class YadeDatabase : ScriptableObject
    {
        /// <summary>
        /// Sheets in this database 
        /// </summary>
        public YadeWorkSheets Sheets;

        private bool binarySerializerEnabled;

        /// <summary>
        /// Turn binary serialization on or off
        /// </summary>
        /// <value></value>
        public bool BinarySerializerEnabled
        {
            get 
            {
                return binarySerializerEnabled;
            }
            set
            {
                binarySerializerEnabled = value;
                
                if (Sheets == null)
                {
                    return;
                }

                foreach (var item in Sheets.Values)
                {
                    item.BinarySerializerEnabled = value;
                }
            }
        }

        private BinarySerializer binarySerializer;

        /// <summary>
        /// Binary Serializer for Yade sheet
        /// </summary>
        internal BinarySerializer BinarySerializer
        {
            get
            {
                if (binarySerializer == null)
                {
                    binarySerializer = new YadeDatabaseBinarySerializer(this);
                }

                return binarySerializer;
            }
        }

        private Dictionary<string, object> Cache = new Dictionary<string, object>();

        private void OnEnable() 
        {
            if (Sheets == null)
            {
                Sheets = new YadeWorkSheets();
            }
        }

        /// <summary>
        /// Load database from binary data. By following below rules:
        ///     1. If there are already same sheet name exits in database, it will be merged. 
        ///     2. If there are no sheet name exists in database, a new sheet will be created
        /// 
        /// This method works when BinarySerializerEnabled is set to true.
        /// </summary>
        /// <param name="bytes">Binary data of database</param>
        public void Deserialize(byte[] bytes)
        {
            if (!this.BinarySerializerEnabled)
            {
                return;
            }

            BinarySerializer.Deserialize(bytes);
        }

        /// <summary>
        /// Serialize database to binaries with incremental mode. This method works when 
        /// BinarySerializerEnabled is set to true.
        /// </summary>
        /// <returns>Binary data of database</returns>
        public byte[] Serialize()
        {
            return Serialize(BinarySerializationSettings.Default);
        }

        /// <summary>
        /// Serialize database to binaries. This method works when BinarySerializerEnabled is set to true.
        /// </summary>
        /// <param name="settings">Settings for Binary serialization</param>
        /// <returns>Binary data of database</returns>
        public byte[] Serialize(BinarySerializationSettings settings)
        {
            if (!BinarySerializerEnabled)
            {
                return new byte[0];
            }

            return this.BinarySerializer.Serialize(settings);
        }

        /// <summary>
        /// Query sheet to get a cell by alpha index
        /// </summary>
        /// <param name="sheetName">Name of sheet</param>
        /// <param name="alphaIndex">Alpha based index</param>
        /// <returns>Cell</returns>
        public Cell Query(string sheetName, string alphaIndex)
        {
            if (string.IsNullOrEmpty(sheetName) || string.IsNullOrEmpty(alphaIndex) || Sheets == null || !Sheets.ContainsKey(sheetName))
            {
                return null;
            }

            var sheet = GetSheetByName(sheetName);
            if (!sheet)
            {
                return null;
            }

            return sheet.GetCell(alphaIndex);
        }

        /// <summary>
        /// Query sheet to get a cell by row and column index
        /// </summary>
        /// <param name="sheetName">Name of sheet</param>
        /// <param name="row">Index of row</param>
        /// <param name="column">Index of column</param>
        /// <returns>Cell</returns>
        public Cell Query(string sheetName, int row, int column)
        {
            if (string.IsNullOrEmpty(sheetName) || Sheets == null || !Sheets.ContainsKey(sheetName))
            {
                return null;
            }

            var sheet = GetSheetByName(sheetName);
            if (!sheet)
            {
                return null;
            }

            return sheet.GetCell(row, column);
        }

        /// <summary>
        /// Query sheet to get a collection of typed data
        /// </summary>
        /// <typeparam name="T">Type class of a row</typeparam>
        public IEnumerable<T> Query<T>(string sheetName, Func<T, bool> predicate = null) where T : class
        {
            if (predicate == null)
            {
                return TryGetSheetAsList<T>(sheetName);
            }
            else
            {
                return TryGetSheetAsList<T>(sheetName).Where(predicate);
            }
        }

        /// <summary>
        /// Query sheets to get a collection of typed data
        /// </summary>
        /// <typeparam name="T">Type class of a row</typeparam>
        public IEnumerable<T> Query<T>(string[] sheetNames, Func<T, bool> predicate = null) where T : class
        {
            if (sheetNames == null)
            {
                return null;
            }

            List<T> list = new List<T>();
            HashSet<string> checkSet = new HashSet<string>();

            foreach (var name in sheetNames)
            {
                if (checkSet.Contains(name))
                {
                    continue;
                }

                checkSet.Add(name);

                IEnumerable<T> results;

                if (predicate == null)
                {
                    results = TryGetSheetAsList<T>(name);
                }
                else
                {
                    results = TryGetSheetAsList<T>(name).Where(predicate);
                }

                if (results != null)
                {
                    list.AddRange(results);
                }
            }

            return list;
        }

        /// <summary>
        /// Query sheet to get a dictionary with the first column of sheet as Keys of the dictionary. 
        /// Rows with null or empty value of first column will be ignored.
        /// </summary>
        /// <typeparam name="string">Name of sheet</typeparam>
        /// <typeparam name="T">Type class of a row</typeparam>
        public Dictionary<string, T> MapQuery<T>(string sheetName, Predicate<KeyValuePair<string, T>> predicate = null) where T : class
        {
            var dict = TryGetSheetAsDictionary<T>(sheetName);
            if (predicate == null)
            {
                return dict;
            }
            else
            {
                var finalDict = new Dictionary<string, T>();
                foreach (var kvp in dict)
                {
                    if (predicate(kvp))
                    {
                        finalDict.Add(kvp.Key, kvp.Value);
                    }
                }

                return finalDict;
            }
        }

        /// <summary>
        /// Get a typed class row of sheet by key. NOTE: The first column of a row is the key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public T QueryByKey<T>(string sheetName, string key) where T : class
        {
            var dict = TryGetSheetAsDictionary<T>(sheetName);
            if (dict == null || !dict.ContainsKey(key))
            {
                return null;
            }

            return dict[key];
        }

        /// <summary>
        /// Set raw value of a cell in sheet. NOTE: ASSET formula does not support in build.
        /// </summary>
        /// <param name="sheetName">Name of yade sheet</param>
        /// <param name="rowIndex">Index of row</param>
        /// <param name="columnIndex">Index of column</param>
        /// <param name="rawText">Value of raw data</param>
        public void SetRawValue(string sheetName, int rowIndex, int columnIndex, string rawText)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return;
            }

            if (this.Sheets.ContainsKey(sheetName))
            {
                var sheet = GetSheetByName(sheetName);
                sheet.SetRawValue(rowIndex, columnIndex, rawText);
            }
        }

        /// <summary>
        /// Set raw value of a cell in sheet. NOTE: ASSET formula does not support in build.
        /// </summary>
        /// <param name="sheetName">Name of yade sheet</param>
        /// <param name="alphaIndex">Alpha index of cell</param>
        /// <param name="rawText">Value of raw data</param>
        public void SetRawValue(string sheetName, string alphaIndex, string rawText)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return;
            }

            if (this.Sheets.ContainsKey(sheetName))
            {
                var sheet = GetSheetByName(sheetName);
                sheet.SetRawValue(alphaIndex, rawText);
            }
        }

        private Dictionary<string, ValueT> TryGetSheetAsDictionary<ValueT>(string sheetName) where ValueT : class
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return null;
            }

            if (!Sheets.ContainsKey(sheetName))
            {
                return new Dictionary<string, ValueT>();
            }

            var sheet = GetSheetByName(sheetName);

            var key = GetSheetDictKey(sheetName, typeof(ValueT));
            if (Cache.ContainsKey(key) && !sheet.HasToRebuildCache)
            {
                return Cache[key] as Dictionary<string, ValueT>;
            }
            else
            {
                if (sheet.HasToRebuildCache)
                {
                    sheet.HasToRebuildCache = false;
                }

                var dict = AsDictionary<string, ValueT>(sheetName, s => s);

                if (Cache.ContainsKey(key))
                {
                    Cache[key] = dict;
                }
                else
                {
                    Cache.Add(key, dict);
                }
                
                return dict;
            }
        }

        private Dictionary<KeyT, ValueT> AsDictionary<KeyT, ValueT>(
            string sheetName,
            Func<string, KeyT> KeyParser
        ) where ValueT : class
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return null;
            }

            if (KeyParser == null)
            {
                throw new Exception("Key parser cannot be null");
            }

            if (!Sheets.ContainsKey(sheetName))
            {
                return null;
            }

            var list = TryGetSheetAsList<ValueT>(sheetName);
            var sheet = GetSheetByName(sheetName);
            if (!sheet)
            {
                return null;
            }

            Dictionary<KeyT, ValueT> map = new Dictionary<KeyT, ValueT>();
            for (int i = 0; i < list.Count; i++)
            {
                var cell = sheet.GetCell(i, 0);
                if (cell == null)
                {
                    continue;
                }

                var keyString = cell.GetValue();
                if (string.IsNullOrEmpty(keyString))
                {
                    continue;
                }

                var key = KeyParser(keyString);
                if (map.ContainsKey(key))
                {
                    throw new Exception(string.Format("Key '{0}'is alreay Exist!"));
                }
                else
                {
                    map.Add(key, list[i]);
                }
            }

            return map;
        }

        public YadeSheetData GetSheetByName(string sheetName)
        {
            if (!Sheets.ContainsKey(sheetName))
            {
                return null;
            }

            var sheet = Sheets[sheetName];

#if UNITY_EDITOR
            // Fix access
            if (!sheet)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(sheet);
                if (!string.IsNullOrEmpty(path))
                {
                    sheet = UnityEditor.AssetDatabase.LoadAssetAtPath<YadeSheetData>(path);
                    
                    if (sheet)
                    {
                        Sheets[sheetName] = sheet;
                        sheet.HasToRebuildCache = true;
                    }
                }
            }
#endif
            return sheet;
        }

        private List<T> TryGetSheetAsList<T>(string sheetName) where T : class
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return new List<T>();
            }

            if (!Sheets.ContainsKey(sheetName))
            {
                return new List<T>();
            }

            var sheet = GetSheetByName(sheetName);
            var key = GetSheetListKey(sheetName, typeof(T));
            if (Cache.ContainsKey(key) && !sheet.HasToRebuildCache)
            {
                return Cache[key] as List<T>;
            }
            else
            {
                if (sheet.HasToRebuildCache)
                {
                    sheet.HasToRebuildCache = false;
                }

                var list = sheet.AsList<T>();
                
                if (Cache.ContainsKey(key))
                {
                    Cache[key] = list;
                }
                else
                {
                    Cache.Add(key, list);
                }

                return list;
            }
        }

        private string GetSheetDictKey(string sheetName, Type t)
        {
            return string.Format("$D_{0}_{1}_D$", sheetName, t);
        }

        private string GetSheetListKey(string sheetName, Type t)
        {
            return string.Format("$L_{0}_{1}_L$", sheetName, t);
        }

        internal void RemoveFromCache(string sheetName, Type t)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return;
            }

            var listKey = GetSheetListKey(sheetName, t);
            if (Cache.ContainsKey(listKey))
            {
                Cache.Remove(listKey);
            }

            var dictKey = GetSheetDictKey(sheetName, t);
            if (Cache.ContainsKey(dictKey))
            {
                Cache.Remove(dictKey);
            }
        }

        internal void ClearCache()
        {
            if (Cache != null)
            {
                Cache.Clear();
            }
        }
    }
}