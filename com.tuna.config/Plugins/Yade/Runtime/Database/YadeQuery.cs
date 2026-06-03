//  Copyright (c) 2022-present amlovey
//  
using System.Collections.Generic;
using UnityEngine;
using System;
using Yade.Runtime.BinarySerialization;

namespace Yade.Runtime
{
    internal class YadeQueryManager
    {
        private static object lockObj = new object();

        private static YadeQueryManager instance;
        internal static YadeQueryManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObj)
                    {
                        if (instance == null)
                        {
                            instance = new YadeQueryManager();
                        }
                    }
                }

                return instance;
            }
        }

        internal void Clear()
        {
            if (DBCache != null)
            {
                var dbs = DBCache.Values;

                foreach (var db in dbs)
                {
                    Resources.UnloadAsset(db);
                }

                DBCache.Clear();
            }
        }

        private Dictionary<string, YadeDatabase> DBCache;

        private YadeQueryManager()
        {
            DBCache = new Dictionary<string, YadeDatabase>();
        }

        internal YadeDatabase GetDB(string database)
        {
            if (string.IsNullOrEmpty(database))
            {
                return null;
            }

            if (DBCache.ContainsKey(database))
            {
                return DBCache[database];
            }

            var db = Resources.Load<YadeDatabase>(database);
            if (db == null)
            {
                return null;
            }

            DBCache.Add(database, db);
            return db;
        }
    }

    /// <summary>
    /// Yade Database Utilities. Note: we need place the database asset under Resources folder
    /// </summary>
    public class YadeDB
    {
        /// <summary>
        /// Deafult databse in Resources folder 
        /// </summary>
        public const string DefaultDB = "YadeDB";

        /// <summary>
        /// Get database by name 
        /// </summary>
        /// <param name="database">Name of database</param>
        /// <returns></returns>
        public static YadeDatabase GetDatabase(string database)
        {
            return YadeQueryManager.Instance.GetDB(database);
        }

        /// <summary>
        /// Query sheet to get a cell by alpha index
        /// </summary>
        /// <param name="sheetName">Name of sheet</param>
        /// <param name="alphaIndex">Alpha based index</param>
        /// <param name="database">Database used</param> 
        /// <returns>Cell</returns>
        public static Cell Q(string sheetName, string alphaIndex, string database = DefaultDB)
        {
            return GetDatabase(database).Query(sheetName, alphaIndex);
        }

        /// <summary>
        /// Query sheet to get a cell by row and column index
        /// </summary>
        /// <param name="sheetName">Name of sheet</param>
        /// <param name="row">Index of row</param>
        /// <param name="column">Index of column</param>
        /// <param name="database">Database used</param>
        /// <returns>Cell</returns>
        public static Cell Q(string sheetName, int row, int column, string database = DefaultDB)
        {
            return GetDatabase(database).Query(sheetName, row, column);
        }

        /// <summary>
        /// Query sheet to get a collection of typed data
        /// </summary>
        /// <typeparam name="T">Type class of a row</typeparam>
        /// 
        public static IEnumerable<T> Q<T>(string sheetName, Func<T, bool> predicate = null, string database = DefaultDB) where T : class
        {
            return GetDatabase(database).Query<T>(sheetName, predicate);
        }

        /// <summary>
        /// Query sheet to get a dictionary with the first column of sheet as Keys of the dictionary. 
        /// Rows with null value of first column will be ignored.
        /// </summary>
        /// <typeparam name="string">Name of sheet</typeparam>
        /// <typeparam name="T">Type class of a row</typeparam>
        public static Dictionary<string, T> MapQ<T>(string sheetName, Predicate<KeyValuePair<string, T>> predicate = null, string database = DefaultDB) where T : class
        {
            return GetDatabase(database).MapQuery<T>(sheetName, predicate);
        }

        /// <summary>
        /// Get a typed class row of sheet by key. NOTE: The first column of a row is the key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static T QByKey<T>(string sheetName, string key, string database = DefaultDB) where T : class
        {
            return GetDatabase(database).QueryByKey<T>(sheetName, key);
        }

        /// <summary>
        /// Set raw value of a cell in sheet. NOTE: ASSET formula only support in Unity Editor
        /// </summary>
        /// <param name="sheetName">Name of yade sheet</param>
        /// <param name="rowIndex">Index of row</param>
        /// <param name="columnIndex">Index of column</param>
        /// <param name="rawText">Value of raw data</param>
        public static void SetRawValue(string sheetName, int rowIndex, int columnIndex, string rawText, string database = DefaultDB)
        {
            GetDatabase(database).SetRawValue(sheetName, rowIndex, columnIndex, rawText);
        }

        /// <summary>
        /// Set raw value of a cell in sheet. NOTE: ASSET formula only support in Unity Editor
        /// </summary>
        /// <param name="sheetName">Name of yade sheet</param>
        /// <param name="alphaIndex">Alpha index of cell</param>
        /// <param name="rawText">Value of raw data</param>
        public static void SetRawValue(string sheetName, string alphaIndex, string rawText, string database = DefaultDB)
        {
            GetDatabase(database).SetRawValue(sheetName, alphaIndex, rawText);
        }

        /// <summary>
        /// Serialize database to binaries. This method works when BinarySerializerEnabled is set to true.  
        /// </summary>
        /// <param name="settings">Settings for Binary serialization</param>
        /// <param name="database">Database name</param>
        /// <returns>Binary data of database</returns>
        public static byte[] Serialize(BinarySerializationSettings settings, string database = DefaultDB)
        {
            return GetDatabase(database).Serialize(settings);
        }

        /// <summary>
        /// Serialize database to binaries with incremental mode. This method works when 
        /// BinarySerializerEnabled is set to true.  
        /// </summary>
        /// <param name="database">Database name</param>
        /// <returns>Binary data of database</returns>
        public static byte[] Serialize(string database = DefaultDB)
        {
            return GetDatabase(database).Serialize();
        }

        /// <summary>
        /// Load database from binary data. By following below rules:
        ///     1. If there are already same sheet name exits in database, it will be merged. 
        ///     2. If there are no sheet name exists in database, a new sheet will be created
        /// 
        /// This method works when BinarySerializerEnabled is set to true.
        /// </summary>
        /// <param name="bytes">Binary data of database</param>
        public static void Deserialize(byte[] bytes, string database = DefaultDB)
        {
            GetDatabase(database).Deserialize(bytes);
        }

        /// <summary>
        /// Turn binary serialization on or off
        /// </summary>
        /// <param name="enabled">On or off</param>
        /// <param name="database">Name of database</param>
        public static void SetBinarySerializerEnabled(bool enabled, string database = DefaultDB)
        {
            GetDatabase(database).BinarySerializerEnabled = enabled;
        }
    }
}

