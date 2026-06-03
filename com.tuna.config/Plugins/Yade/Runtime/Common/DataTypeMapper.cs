//  Copyright (c) 2021-present amlovey
//  
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yade.Runtime
{
    /// <summary>
    /// Set key of the type in DateTypeMapper, and please set the value larger than 100 and unique
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    public class TypeKey : Attribute
    {
        /// <summary>
        /// Order of the type
        /// </summary>
        public int Key { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The order of date type in DataTypeMapper, please set the value larger than 100</param>
        public TypeKey(int key)
        {
            this.Key = key;
        }
    }

    /// <summary>
    /// Info about date type in mapper 
    /// </summary>
    public class DataTypeMeta
    {
        /// <summary>
        /// Name of Date type
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Actual type
        /// </summary>
        public Type Type { get; set; }

        public Func<object, object> ParseFunction;

        public DataTypeMeta()
        {

        }

        public DataTypeMeta(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
    
    /// <summary>
    /// Store the data type that yade can parse
    /// </summary>
    public class DataTypeMapper
    {
        private static Dictionary<int, DataTypeMeta> map = new Dictionary<int, DataTypeMeta>();

        static DataTypeMapper()
        {
            RegisterType<string>(0, "string");
            RegisterType<float>(1, "float");
            RegisterType<int>(2, "int");
            RegisterType<uint>(3, "uint");
            RegisterType<Int16>(4, "Int16");
            RegisterType<Int64>(5, "Int64");
            RegisterType<double>(6, "double");
            RegisterType<short>(7, "short");
            RegisterType<ushort>(8, "ushort");
            RegisterType<byte>(9, "byte");
            RegisterType<bool>(10, "bool");
            RegisterType<long>(11, "long");
            RegisterType<ulong>(12, "ulong");
            RegisterType<object>(13, "object");

            RegisterType<TextAsset>(14, "TextAsset");
            RegisterType<Texture2D>(15, "Texture2D");
            RegisterType<GameObject>(16, "GameObject");
            RegisterType<Sprite>(17, "Sprite");
            RegisterType<Vector2>(18, "Vector2");
            RegisterType<Vector3>(19, "Vector3");
            RegisterType<Vector4>(20, "Vector4");
            RegisterType<UnityEngine.Object>(21, "UnityObject");
        }

        public static void RegisterType(int key, Type type, string alias = "")
        {
            foreach (var item in map.Values)
            {
                if (item.Type == type)
                {
                    return;
                }
            }

            var fullName = type.ToString();
            if (!string.IsNullOrEmpty(alias))
            {
                fullName = alias;
            }

            if (map.ContainsKey(key))
            {
                Debug.LogError(string.Format("Register {0} failed, because the key {1} is already assigned to type {2}", type, key, map[key].Type));
                return;
            }

            map.Add(key, new DataTypeMeta()
            {
                Name = fullName,
                Type = type
            });
        }

        public static void RegisterType<T>(int key, string alias = "")
        {
            Type t = typeof(T);
            RegisterType(key, t, alias);
        }

        public static Type KeyToType(int key)
        {
            if (map.ContainsKey(key))
            {
                return map[key].Type;
            }

            return null;
        }

        public static Type NameToType(string name)
        {
            foreach (KeyValuePair<int, DataTypeMeta> item in map)
            {
                if (item.Value.Name == name)
                {
                    return item.Value.Type;
                }
            }

            return null;
        }

        public static int NameToKey(string name)
        {
            foreach (KeyValuePair<int, DataTypeMeta> item in map)
            {
                if (item.Value.Name == name)
                {
                    return item.Key;
                }
            }

            return -1;
        }

        public static void ForEach(Action<int, DataTypeMeta> callback)
        {
            if (callback == null)
            {
                return;
            }

            foreach (KeyValuePair<int, DataTypeMeta> item in map)
            {
                callback(item.Key, item.Value);
            }
        }

        public static string KeyToName(int key)
        {
            if (map.ContainsKey(key))
            {
                return map[key].Name;
            }

            return string.Empty;
        }
    }
}