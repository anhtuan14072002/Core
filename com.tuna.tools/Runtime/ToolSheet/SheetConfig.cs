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

        // Khai báo cột trong sheet được ánh xạ vào field config.
        public DataSheetAttribute(int column)
        {
            if (column < 0)
                throw new ArgumentOutOfRangeException(nameof(column));

            Column = column;
        }
    }

    public sealed class SheetTable<T>
        where T : class, new()
    {
        private readonly List<T> rows;
        private readonly Dictionary<string, T> rowsById;

        // Lưu danh sách config và bảng tra cứu nhanh theo Id.
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

        // Lấy config theo thứ tự dòng, bắt đầu từ 1.
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

        // Lấy config theo Id dạng chuỗi.
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

        // Lấy config theo Id dạng số.
        public T GetById(int id)
        {
            return GetById(id.ToString(CultureInfo.InvariantCulture));
        }

        // Thử lấy config theo Id chuỗi mà không phát sinh exception.
        public bool TryGetById(string id, out T row)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                row = default;
                return false;
            }

            return rowsById.TryGetValue(id, out row);
        }

        // Thử lấy config theo Id số mà không phát sinh exception.
        public bool TryGetById(int id, out T row)
        {
            return TryGetById(
                id.ToString(CultureInfo.InvariantCulture),
                out row);
        }
    }

    public static partial class SheetConfig
    {
        private const string ResourcesFolder = "Config/";

        // Tải config từ Resources, ánh xạ dữ liệu và cache kết quả để tái sử dụng.
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

        // Chuyển chuỗi trong ô sheet sang kiểu dữ liệu được yêu cầu.
        internal static T ConvertValue<T>(string value)
        {
            return (T)ConvertValue(value, typeof(T));
        }

        // Xử lý chuyển đổi giá trị đơn, enum, bool, nullable và mảng phân cách bằng '|'.
        private static object ConvertValue(string value, Type requestedType)
        {
            Type targetType =
                Nullable.GetUnderlyingType(requestedType) ?? requestedType;

            if (targetType == typeof(string))
                return value ?? string.Empty;

            if (targetType.IsArray)
            {
                Type elementType = targetType.GetElementType();
                string[] values = value.Split('|');
                Array result = Array.CreateInstance(elementType, values.Length);

                for (int i = 0; i < values.Length; i++)
                    result.SetValue(ConvertValue(values[i].Trim(), elementType), i);

                return result;
            }

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

        // Đọc các field có DataSheetAttribute và tạo thông tin ánh xạ được cache.
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

            bindings.Sort(
                (left, right) => left.Column.CompareTo(right.Column));
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

            // Lưu field đích và chỉ số cột nguồn tương ứng.
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

            // Lưu toàn bộ binding của một loại config và field Id của nó.
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
