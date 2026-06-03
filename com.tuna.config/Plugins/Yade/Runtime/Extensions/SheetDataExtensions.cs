//  Copyright (c) 2020-present amlovey
//  
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using Yade.Runtime.BinarySerialization;
using System.Linq;
using System.Collections;

namespace Yade.Runtime
{
    /// <summary>
    /// Cell Parser interface
    /// </summary>
    public interface ICellParser
    {
        void ParseFrom(string s);
    }

    /// <summary>
    /// Data filed definition attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class DataFieldAttribute : Attribute
    {
        public int Index { get; private set; }

        public DataFieldAttribute(string alphaBasedIndex)
        {
            this.Index = IndexHelper.AlphaToIntIndex(alphaBasedIndex);
        }

        public DataFieldAttribute(int index)
        {
            this.Index = index;
        }
    }

    internal abstract class ParseMethodWrapper
    {
        public object Output { get; protected set; }
        public abstract void Execute(string s);
    }

    internal class ParseMethodGeneric<T> : ParseMethodWrapper
    {
        private Func<string, T> parse;

        public ParseMethodGeneric(Func<string, T> parse)
        {
            this.parse = parse;
        }

        public override void Execute(string s)
        {
            Output = this.parse(s);
        }
    }

    public static class SheetDataExtensions
    {
        private static readonly Dictionary<Type, ParseMethodWrapper> PARSER_MAP = new Dictionary<Type, ParseMethodWrapper>();

        static SheetDataExtensions()
        {
            RegisterParser<int>(int.Parse);
            RegisterParser<uint>(uint.Parse);
            RegisterParser<long>(long.Parse);
            RegisterParser<ulong>(ulong.Parse);
            RegisterParser<float>(float.Parse);
            RegisterParser<double>(double.Parse);
            RegisterParser<bool>(bool.Parse);
            RegisterParser<Int64>(Int64.Parse);
            RegisterParser<Int16>(Int16.Parse);
            RegisterParser<byte>(byte.Parse);
            RegisterParser<Vector2>(Vector2Parse);
            RegisterParser<Vector3>(Vector3Parse);
            RegisterParser<Vector4>(Vector4Parse);
        }

        internal static void RegisterParser<T>(Func<string, T> Parse)
        {
            var t = typeof(T);
            if (!PARSER_MAP.ContainsKey(t))
            {
                PARSER_MAP.Add(typeof(T), new ParseMethodGeneric<T>(Parse));
            }
        }

        /// <summary>
        /// Convert yade data sheet to list of objects.
        /// </summary>
        public static List<T> AsList<T>(this YadeSheetData sheetData) where T : class
        {
            // Get set properties and fileds value action map
            //
            var flags = BindingFlags.Public | BindingFlags.Instance;
            Dictionary<int, Action<T, int>> SetPropertyActions = new Dictionary<int, Action<T, int>>();

            var type = typeof(T);

            // Process properties
            GetAndUpdateSetValueActions(
                SetPropertyActions,
                type.GetProperties(flags),
                sheetData,
                (member) => member.PropertyType,
                (member, instance, value) =>
                {
                    if (member == null || instance == null)
                    {
                        return;
                    }

                    member.SetValue(instance, value);
                }
            );

            // Process fields
            GetAndUpdateSetValueActions(
                SetPropertyActions,
                type.GetFields(flags),
                sheetData,
                (member) => member.FieldType,
                (member, instance, value) =>
                {
                    if (member == null || instance == null)
                    {
                        return;
                    }

                    member.SetValue(instance, value);
                }
            );

            // Set values
            var rowCount = sheetData.GetRowCount();
            List<T> list = new List<T>();
            for (int i = 0; i < rowCount; i++)
            {
                T instance = Activator.CreateInstance<T>();

                foreach (KeyValuePair<int, Action<T, int>> kvp in SetPropertyActions)
                {
                    kvp.Value(instance, i);
                }

                list.Add(instance);
            }

            return list;
        }

        private static void GetAndUpdateSetValueActions<T, MT>(
            Dictionary<int, Action<T, int>> actions,
            MT[] members,
            YadeSheetData sheetData,
            Func<MT, Type> memberTypeGetter,
            Action<MT, object, object> Setter) where MT : MemberInfo
        {
            foreach (var member in members)
            {
                var dataFieldIndex = member.GetCustomAttribute(typeof(DataFieldAttribute));
                if (dataFieldIndex == null)
                {
                    continue;
                }

                var index = (dataFieldIndex as DataFieldAttribute).Index;

                if (actions.ContainsKey(index))
                {
                    throw new Exception(string.Format("Key '{0}' is alreay exists", index));
                }

                Action<T, int> action;
                Type dataType = memberTypeGetter(member);

                // Process custom type
                if (typeof(ICellParser).IsAssignableFrom(dataType))
                {
                    action = (T instance, int row) =>
                    {
                        var cell = sheetData.GetCell(row, index);
                        string value = string.Empty;
                        if (cell != null)
                        {
                            value = cell.GetValue();
                        }

                        if (!string.IsNullOrEmpty(value))
                        {
                            ICellParser valueObject;
                            if (dataType.IsSubclassOf(typeof(UnityEngine.ScriptableObject)))
                            {
                                valueObject = UnityEngine.ScriptableObject.CreateInstance(dataType) as ICellParser;
                            }
                            else
                            {
                                valueObject = Activator.CreateInstance(dataType) as ICellParser;
                            }

                            valueObject.ParseFrom(value);
                            Setter(member, instance, valueObject);
                        }
                    };
                }

                // Process Unity Object
                else if (dataType.IsSubclassOf(typeof(UnityEngine.Object)))
                {
                    action = (T instance, int row) =>
                    {
                        var cell = sheetData.GetCell(row, index);
                        object value = null;
                        if (cell != null)
                        {
                            value = cell.GetUnityObject();
                        }
                        Setter(member, instance, value);
                    };
                }
                else if (dataType.IsArray)
                {
                    action = (T instance, int row) =>
                    {
                        var cell = sheetData.GetCell(row, index);
                        object value = null;
                        if (cell != null)
                        {
                            var elementType = dataType.GetElementType();
                            var unityObjects = cell.GetUnityObjects();

                            var array = Array.CreateInstance(elementType, unityObjects.Length);
                            for (int i = 0; i < array.Length; i++)
                            {
                                array.SetValue(unityObjects[i], i);
                            }

                            value = array;
                        }

                        Setter(member, instance, value);
                    };
                }
                else if (IsEnumerableButNotArray(dataType))
                {
                    action = (T instance, int row) =>
                    {
                        var cell = sheetData.GetCell(row, index);
                        object value = null;
                        if (cell != null)
                        {
                            var elementType = dataType.GetGenericArguments()[0];
                            var listType = typeof(List<>).MakeGenericType(elementType);
                            var list = (IList)Activator.CreateInstance(listType);
                            var unityObjects = cell.GetUnityObjects();
                            foreach (var item in unityObjects)
                            {
                                list.Add(item);
                            }

                            value = list;
                        }

                        Setter(member, instance, value);
                    };
                }

                // Proces other types including enum
                else
                {
                    action = (T instance, int row) =>
                    {
                        var cell = sheetData.GetCell(row, index);
                        object value = GetCellValue(dataType, cell);
                        Setter(member, instance, value);
                    };
                }

                actions.Add(index, action);
            }
        }

        public static bool IsEnumerableButNotArray(Type type)
        {
            bool isGeneric = type.IsGenericType && type.GetInterfaces().Any(ti => (ti == typeof(IEnumerable<>) || ti.Name == "IEnumerable"));
            return isGeneric;
        }

        private static object GetCellValue(Type dataType, Cell cell)
        {
            if (dataType == typeof(string))
            {
                var value = cell == null ? string.Empty : cell.GetValue();
                return value;
            }
            else if (typeof(System.Enum).IsAssignableFrom(dataType))
            {
                if (cell == null)
                {
                    return Activator.CreateInstance(dataType);
                }

                return EnumParse(dataType, cell.GetValue());
            }
            else
            {
                if (PARSER_MAP.ContainsKey(dataType))
                {
                    var parser = PARSER_MAP[dataType];
                    if (cell != null)
                    {
                        parser.Execute(cell.GetValue());
                        return parser.Output;
                    }
                }
            }

            return null;
        }

        private static object EnumParse(Type t, string s)
        {
            return Enum.Parse(t, s);
        }

        private static Vector4 Vector4Parse(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return default(Vector4);
            }

            var temp = s.Split(new char[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length < 4)
            {
                return default(Vector4);
            }

            Vector4 vec;
            float.TryParse(temp[0].Trim(), out vec.x);
            float.TryParse(temp[1].Trim(), out vec.y);
            float.TryParse(temp[2].Trim(), out vec.z);
            float.TryParse(temp[3].Trim(), out vec.w);

            return vec;
        }

        private static Vector3 Vector3Parse(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return default(Vector3);
            }

            var temp = s.Split(new char[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length < 3)
            {
                return default(Vector3);
            }

            Vector3 vec;
            float.TryParse(temp[0].Trim(), out vec.x);
            float.TryParse(temp[1].Trim(), out vec.y);
            float.TryParse(temp[2].Trim(), out vec.z);

            return vec;
        }

        private static Vector2 Vector2Parse(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return default(Vector2);
            }

            var temp = s.Split(new char[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length < 2)
            {
                return default(Vector2);
            }

            Vector2 vec;
            float.TryParse(temp[0].Trim(), out vec.x);
            float.TryParse(temp[1].Trim(), out vec.y);

            return vec;
        }

        /// <summary>
        /// Convert yade sheet data to a dictionary
        /// </summary>
        /// <param name="sheetData">Yade data sheet</param>
        /// <param name="keySelector">Key Selector</param>
        /// <param name="throwIfKeyConflict">Whether throw exeption if there are key conflict</param>
        /// <returns></returns>
        public static Dictionary<KeyT, ValueT> AsDictionary<KeyT, ValueT>(
            this YadeSheetData sheetData,
            Func<ValueT, KeyT> keySelector,
            bool throwIfKeyConflict = true
        ) where ValueT : class
        {
            FixSheetMissingIfNeeds(sheetData);

            var list = sheetData.AsList<ValueT>();
            Dictionary<KeyT, ValueT> map = new Dictionary<KeyT, ValueT>();

            list.ForEach(item =>
            {
                KeyT key = keySelector(item);

                if (key == null)
                {
                    return;
                }

                if (throwIfKeyConflict)
                {
                    map.Add(key, item);
                }
                else
                {
                    if (!map.ContainsKey(key))
                    {
                        map.Add(key, item);
                    }
                }
            });

            return map;
        }

        /// <summary>
        /// Set the raw value of cell at specific position. NOTE: ASSET formula does not support in build.
        /// </summary>
        /// <param name="sheet">YadeSheetData Instance</param>
        /// <param name="rowIndex">Index of row, starts from zero</param>
        /// <param name="columnIndex">Index of column, starts form zero</param>
        /// <param name="rawText">Raw text for the cell</param>
        public static void SetRawValue(this YadeSheetData sheet, int rowIndex, int columnIndex, string rawText)
        {
            FixSheetMissingIfNeeds(sheet);

            if (sheet == null)
            {
                return;
            }

            sheet.BuildFullRefTreeIfNeeds();
            sheet.SetRawValueInternal(rowIndex, columnIndex, rawText);
            sheet.HasToRebuildCache = true;
            if (rawText.StartsWith("="))
            {
                sheet.UpdateToRefTreeIfNeeds(rowIndex, columnIndex, rawText);
            }

            sheet.UpdateReferencedCells(rowIndex, columnIndex);

            if (sheet.BinarySerializerEnabled)
            {
                var serializer = sheet.BinarySerializer as YadeSheetBinarySerializer;
                if (serializer != null)
                {
                    serializer.AddLog(rowIndex, columnIndex, rawText);
                }
            }

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(sheet);
            #endif
        }

        /// <summary>
        /// Set the raw value of cell at specific position. NOTE: ASSET formula does not support in build.
        /// </summary>
        /// <param name="sheet">YadeSheetData Instance</param>
        /// <param name="alphaIndex">Alpha based index</param>
        /// <param name="rawText"></param>
        public static void SetRawValue(this YadeSheetData sheet, string alphaIndex, string rawText)
        {
            FixSheetMissingIfNeeds(sheet);

            if (sheet == null)
            {
                return;
            }

            var cellIndex = IndexHelper.AlphaBasedToCellIndex(alphaIndex);
            SetRawValue(sheet, cellIndex.row, cellIndex.column, rawText);
        }

        /// <summary>
        /// Deserialize binaries to yade sheet. This method works when BinarySerializerEnabled is set to true.
        /// </summary>
        /// <param name="sheet">Yade sheet</param>
        /// <param name="bytes">Binary data of sheet</param>
        public static void Deserialize(this YadeSheetData sheet, byte[] bytes)
        {
            FixSheetMissingIfNeeds(sheet);

            if (sheet == null || !sheet.BinarySerializerEnabled)
            {
                return;
            }

            sheet.BinarySerializer.Deserialize(bytes);
        }

        /// <summary>
        /// Serialize sheet to binaries. This method works when BinarySerializerEnabled is set to true.
        /// </summary>
        /// <param name="sheet">Yade sheet</param>
        /// <param name="settings">Binary serialization settings</param>
        /// <returns>Binary data of sheet</returns>
        public static byte[] Serialize(this YadeSheetData sheet, BinarySerializationSettings settings)
        {
            FixSheetMissingIfNeeds(sheet);

            if (sheet == null || !sheet.BinarySerializerEnabled)
            {
                return new byte[0];
            }

            return sheet.BinarySerializer.Serialize(settings);
        }

        /// <summary>
        /// Serialize sheet to binaries with incremental mode. This method works when 
        /// BinarySerializerEnabled is set to true.
        /// </summary>
        /// <param name="sheet">Yade sheet</param>
        /// <returns>Binary data of sheet</returns>
        public static byte[] Serialize(this YadeSheetData sheet)
        {
            return Serialize(sheet, BinarySerializationSettings.Default);
        }

        /// <summary>
        /// Copy sheet data to the other yade sheet
        /// </summary>
        /// <param name="sheet">Yade sheet</param>
        /// <param name="other">Yade sheet</param>
        public static void CopyTo(this YadeSheetData sheet, YadeSheetData other)
        {
            FixSheetMissingIfNeeds(sheet);
            FixSheetMissingIfNeeds(other);

            if (other == null || sheet == null)
            {
                return;
            }
            
            other.Clear();

            var rowCount = sheet.GetRowCount();
            var columnCount = sheet.GetColumnCount();

            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    var cell = sheet.GetCell(row, column);
                    if (cell != null)
                    {
                        other.SetRawValueInternal(row, column, cell.GetRawValue());
                    }
                }
            }

            other.RecalculateValues();
        }

        /// <summary>
        /// Recalculate the vaules of yade sheet
        /// </summary>
        /// <param name="sheet"></param>
        internal static void RecalculateValues(this YadeSheetData sheet)
        {
            FixSheetMissingIfNeeds(sheet);

            var rowCount = sheet.GetRowCount();
            var columnCount = sheet.GetColumnCount();

            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    var cell = sheet.GetCell(row, column);
                    if (cell != null)
                    {
                        var raw = cell.GetRawValue();
                        if (raw.StartsWith("="))
                        {
                            sheet.SetRawValueInternal(row, column, cell.GetRawValue());
                        }
                    }
                }
            }

            // Need to update status to changed, so that YadeDatabase will get current data
            sheet.HasToRebuildCache = true;
        }

        private static void FixSheetMissingIfNeeds(YadeSheetData sheet)
        {
#if UNITY_EDITOR
            if (!sheet)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(sheet);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                sheet = UnityEditor.AssetDatabase.LoadAssetAtPath<YadeSheetData>(path);
            }
#endif
        }
    }
}
