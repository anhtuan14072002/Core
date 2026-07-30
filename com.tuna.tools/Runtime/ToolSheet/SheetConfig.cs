using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace Sheet
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DataSheetAttribute : Attribute
    {
        public int Column { get; }

        public DataSheetAttribute(int column)
        {
            if (column < 0)
                throw new ArgumentOutOfRangeException(nameof(column));

            Column = column;
        }
    }

    public interface ISheetConfigPostLoad
    {
        void OnLoaded(SheetRowReader row);
    }

    public readonly struct SheetRowReader
    {
        private readonly string configName;
        private readonly int rowNumber;
        private readonly GoogleSheetDataRow row;
        private readonly Dictionary<string, int> columns;

        internal SheetRowReader(
            string configName,
            int rowNumber,
            GoogleSheetDataRow row,
            Dictionary<string, int> columns)
        {
            this.configName = configName;
            this.rowNumber = rowNumber;
            this.row = row;
            this.columns = columns;
        }

        public T Get<T>(string columnName)
        {
            return SheetConfig.ConvertValue<T>(GetCell(columnName));
        }

        public Dictionary<TKey, TValue> GetMap<TKey, TValue>(
            string keyColumn,
            string valueColumn,
            char separator = '|')
        {
            string[] keys = GetCell(keyColumn).Split(separator);
            string[] values = GetCell(valueColumn).Split(separator);

            if (keys.Length != values.Length)
            {
                throw new InvalidOperationException(
                    $"Columns '{keyColumn}' and '{valueColumn}' " +
                    $"have different item counts at row {rowNumber} " +
                    $"in config '{configName}'.");
            }

            var result = new Dictionary<TKey, TValue>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                TKey key = SheetConfig.ConvertValue<TKey>(keys[i].Trim());
                TValue value = SheetConfig.ConvertValue<TValue>(
                    values[i].Trim());

                if (!result.TryAdd(key, value))
                {
                    throw new InvalidOperationException(
                        $"Duplicate key '{key}' at row {rowNumber} " +
                        $"in config '{configName}'.");
                }
            }

            return result;
        }

        private string GetCell(string columnName)
        {
            if (!columns.TryGetValue(columnName, out int columnIndex))
            {
                throw new KeyNotFoundException(
                    $"Cannot find column '{columnName}' " +
                    $"in config '{configName}'.");
            }

            if (columnIndex >= row.Cells.Count)
            {
                throw new InvalidOperationException(
                    $"Row {rowNumber} in config '{configName}' " +
                    $"does not contain column '{columnName}'.");
            }

            return row.Cells[columnIndex];
        }
    }

    public sealed class SheetTable<T>
        where T : class, new()
    {
        private readonly List<T> rows;
        private readonly Dictionary<string, T> rowsById;

        internal SheetTable(
            List<T> rows,
            Dictionary<string, T> rowsById)
        {
            this.rows = rows;
            this.rowsById = rowsById;
        }

        public int Count => rows.Count;
        public IReadOnlyList<T> Rows => rows;
        public T this[int index] => rows[index];

        public T GetByOrder(int order)
        {
            if (order < 1 || order > rows.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(order),
                    $"Order must be between 1 and {rows.Count}.");
            }

            return rows[order - 1];
        }

        public T GetById(string id)
        {
            if (!string.IsNullOrWhiteSpace(id) &&
                rowsById.TryGetValue(id, out T row))
            {
                return row;
            }

            throw new KeyNotFoundException(
                $"Cannot find Id '{id}' in {typeof(T).Name}.");
        }

        public T GetById(int id)
        {
            return GetById(id.ToString(CultureInfo.InvariantCulture));
        }

        public bool TryGetById(string id, out T row)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                row = default;
                return false;
            }

            return rowsById.TryGetValue(id, out row);
        }

        public bool TryGetById(int id, out T row)
        {
            return TryGetById(
                id.ToString(CultureInfo.InvariantCulture),
                out row);
        }
    }

    public static class SheetConfig
    {
        private const string ResourcesFolder = "Config/";

        public static SheetTable<T> LoadConfig<T>(string configName)
            where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(configName))
                throw new ArgumentException("Config name is required.", nameof(configName));

            if (SheetCache<T>.Tables.TryGetValue(
                    configName,
                    out SheetTable<T> table))
            {
                return table;
            }

            GoogleSheetData data = Resources.Load<GoogleSheetData>(
                ResourcesFolder + configName);
            if (data == null)
            {
                throw new InvalidOperationException(
                    $"Cannot load config " +
                    $"'Resources/{ResourcesFolder}{configName}'.");
            }

            Dictionary<string, int> columns = ReadColumns(data);
            RowBinding binding = BindingCache<T>.Value;
            var rows = new List<T>(Mathf.Max(0, data.Rows.Count - 1));
            var rowsById = new Dictionary<string, T>(
                StringComparer.OrdinalIgnoreCase);

            for (int rowIndex = 1;
                 rowIndex < data.Rows.Count;
                 rowIndex++)
            {
                GoogleSheetDataRow sourceRow = data.Rows[rowIndex];
                if (sourceRow == null)
                    continue;

                var row = new T();
                for (int i = 0; i < binding.Fields.Length; i++)
                {
                    FieldBinding field = binding.Fields[i];
                    if (field.Column >= sourceRow.Cells.Count)
                    {
                        throw new InvalidOperationException(
                            $"Row {rowIndex} in config '{configName}' " +
                            $"does not contain column {field.Column}.");
                    }

                    field.Field.SetValue(
                        row,
                        ConvertValue(
                            sourceRow.Cells[field.Column],
                            field.Field.FieldType));
                }

                var reader = new SheetRowReader(
                    configName,
                    rowIndex,
                    sourceRow,
                    columns);
                if (row is ISheetConfigPostLoad postLoad)
                    postLoad.OnLoaded(reader);

                string sheetId = Convert.ToString(
                    binding.IdField.GetValue(row),
                    CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(sheetId))
                {
                    throw new InvalidOperationException(
                        $"Row {rowIndex} in config '{configName}' " +
                        "does not contain an Id in column 0.");
                }

                if (!rowsById.TryAdd(sheetId, row))
                {
                    throw new InvalidOperationException(
                        $"Config '{configName}' contains duplicate " +
                        $"Id '{sheetId}'.");
                }

                rows.Add(row);
            }

            table = new SheetTable<T>(rows, rowsById);
            SheetCache<T>.Tables.Add(configName, table);
            return table;
        }

        internal static T ConvertValue<T>(string value)
        {
            return (T)ConvertValue(value, typeof(T));
        }

        private static object ConvertValue(string value, Type requestedType)
        {
            Type targetType =
                Nullable.GetUnderlyingType(requestedType) ?? requestedType;

            if (targetType == typeof(string))
                return value ?? string.Empty;

            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, true);

            if (targetType == typeof(bool))
            {
                if (value == "1")
                    return true;

                if (value == "0")
                    return false;
            }

            try
            {
                return Convert.ChangeType(
                    value,
                    targetType,
                    CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
                when (exception is FormatException or
                          InvalidCastException or
                          OverflowException)
            {
                throw new FormatException(
                    $"Cannot convert '{value}' to {targetType.Name}.",
                    exception);
            }
        }

        private static Dictionary<string, int> ReadColumns(
            GoogleSheetData data)
        {
            if (data.Rows.Count == 0 || data.Rows[0] == null)
            {
                throw new InvalidOperationException(
                    $"Config '{data.name}' does not contain a header row.");
            }

            var columns = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);
            List<string> headers = data.Rows[0].Cells;

            for (int column = 0; column < headers.Count; column++)
            {
                string header = headers[column];
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                if (!columns.TryAdd(header, column))
                {
                    throw new InvalidOperationException(
                        $"Config '{data.name}' contains duplicate " +
                        $"column '{header}'.");
                }
            }

            return columns;
        }

        private static RowBinding ReadBinding(Type rowType)
        {
            FieldInfo[] fields = rowType.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            var bindings = new List<FieldBinding>(fields.Length);
            var usedColumns = new HashSet<int>();
            FieldInfo idField = null;

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                DataSheetAttribute attribute =
                    field.GetCustomAttribute<DataSheetAttribute>();
                if (attribute == null)
                    continue;

                if (field.IsInitOnly)
                {
                    throw new InvalidOperationException(
                        $"{rowType.Name}.{field.Name} cannot be readonly.");
                }

                if (!usedColumns.Add(attribute.Column))
                {
                    throw new InvalidOperationException(
                        $"{rowType.Name} maps column " +
                        $"{attribute.Column} more than once.");
                }

                bindings.Add(
                    new FieldBinding(field, attribute.Column));
                if (attribute.Column == 0)
                    idField = field;
            }

            if (idField == null)
            {
                throw new InvalidOperationException(
                    $"{rowType.Name} must declare an Id field " +
                    "with [DataSheet(0)].");
            }

            bindings.Sort((left, right) => left.Column.CompareTo(right.Column));
            return new RowBinding(bindings.ToArray(), idField);
        }

        private static class SheetCache<T>
            where T : class, new()
        {
            public static readonly Dictionary<string, SheetTable<T>> Tables =
                new(StringComparer.OrdinalIgnoreCase);
        }

        private static class BindingCache<T>
            where T : class, new()
        {
            public static readonly RowBinding Value =
                ReadBinding(typeof(T));
        }

        private readonly struct FieldBinding
        {
            public readonly FieldInfo Field;
            public readonly int Column;

            public FieldBinding(FieldInfo field, int column)
            {
                Field = field;
                Column = column;
            }
        }

        private sealed class RowBinding
        {
            public readonly FieldBinding[] Fields;
            public readonly FieldInfo IdField;

            public RowBinding(
                FieldBinding[] fields,
                FieldInfo idField)
            {
                Fields = fields;
                IdField = idField;
            }
        }
    }
}
