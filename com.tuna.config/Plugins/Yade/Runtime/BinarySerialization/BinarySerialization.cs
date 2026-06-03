//  Copyright (c) 2022-present amlovey
//  
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Yade.Runtime.BinarySerialization
{
    internal abstract class BinarySerializer
    {
        public abstract byte[] Serialize(BinarySerializationSettings settings);
        public abstract void Deserialize(byte[] bytes);
    }

    internal class YadeDatabaseBinarySerializer : BinarySerializer
    {
        private YadeDatabase database;

        private const byte YADE_DATABASE_FLAG = 255;

        public YadeDatabaseBinarySerializer(YadeDatabase database)
        {
            this.database = database;
        }

        public override byte[] Serialize(BinarySerializationSettings settings)
        {
            var sheetNames = this.database.Sheets.Keys;
            List<string> infos = new List<string>();
            List<byte[]> bytesList = new List<byte[]>();
            int sheetBytesTotalLength = 0;

            foreach (var name in sheetNames)
            {
                var sheetBytes = this.database.Sheets[name].Serialize(settings);
                infos.Add(string.Format("{0},{1}", name, sheetBytes.Length));
                bytesList.Add(sheetBytes);
                sheetBytesTotalLength += sheetBytes.Length;
            }

            var infoString = string.Join(";", infos);
            var infoBytes = infoString.GetBytes(YADE_DATABASE_FLAG);

            var results = new byte[sheetBytesTotalLength + infoBytes.Length + 5];
            results[0] = YADE_DATABASE_FLAG;

            var infoBytesLengthBytes = BitConverter.GetBytes(infoBytes.Length);
            Buffer.BlockCopy(infoBytesLengthBytes, 0, results, 1, infoBytesLengthBytes.Length);
            Buffer.BlockCopy(infoBytes, 0, results, 1 + infoBytesLengthBytes.Length, infoBytes.Length);

            int offset = 1 + infoBytesLengthBytes.Length + infoBytes.Length;
            foreach (var item in bytesList)
            {
                Buffer.BlockCopy(item, 0, results, offset, item.Length);
                offset += item.Length;
            }

            return results;
        }

        public override void Deserialize(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return;
            }

            if (bytes[0] != YADE_DATABASE_FLAG)
            {
                throw new Exception("Loaded bytes is invalid format");
            }

            var infoLegnth = BitConverter.ToInt32(bytes.GetRange(1, 4), 0);
            var infoBytes = bytes.GetRange(5, infoLegnth);
            var info = infoBytes.GetString(bytes[0]);
            var sheetInfos = info.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var start = infoLegnth + 5;
            foreach (var sheetInfo in sheetInfos)
            {
                var temp = sheetInfo.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var name = temp[0];
                var length = int.Parse(temp[1]);

                var sheetBytes = bytes.GetRange(start, length);

                if (this.database.Sheets == null)
                {
                    this.database.Sheets = new YadeWorkSheets();
                }

                if (this.database.Sheets.ContainsKey(name))
                {
                    this.database.Sheets[name].Deserialize(sheetBytes);
                }
                else
                {
                    YadeSheetData sheet = ScriptableObject.CreateInstance<YadeSheetData>();
                    sheet.BinarySerializerEnabled = true;
                    
                    sheet.name = name;
                    sheet.Deserialize(sheetBytes);

                    this.database.Sheets.Add(name, sheet);
                }

                start += length;
            }
        }
    }

    internal class YadeSheetBinarySerializer : BinarySerializer
    {
        private YadeSheetData sheet;

        internal CellLogCollection Logs { get; private set; }

        public YadeSheetBinarySerializer(YadeSheetData sheet)
        {
            this.sheet = sheet;
            Logs = new CellLogCollection();
        }

        public void AddLog(int rowIndex, int columnIndex, string rawText)
        {
            Logs.AddLog(rowIndex, columnIndex, rawText);
        }

        public override byte[] Serialize(BinarySerializationSettings settings)
        {
            switch (settings.Mode)
            {
                case BinarySerializationMode.Incremental:
                    return SerializeSheetDataIncremental();
                case BinarySerializationMode.Full:
                    return SerializeSheetDataFully();
            }

            return new byte[0];
        }

        public override void Deserialize(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return;
            }

            var mode = bytes[0];
            switch (mode)
            {
                case (byte)BinarySerializationMode.Incremental:
                    DeserializeSheetDataIncremental(bytes);
                    break;
                case (byte)BinarySerializationMode.Full:
                    DeserializeSheetDataFully(bytes);
                    break;
                default:
                    throw new Exception("Loaded bytes is invalid format");
            }
        }

        private byte[] SerializeSheetDataIncremental()
        {
            HashSet<int> checkMap = new HashSet<int>();
            int bytesLength = 0;

            var node = Logs.Last;
            LinkedListNode<CellLog> tempNode = null;

            while (node != null)
            {
                if (checkMap.Contains(node.Value.Idx))
                {
                    tempNode = node.Previous;
                    Logs.Remove(node);
                    node = tempNode;
                    continue;
                }

                checkMap.Add(node.Value.Idx);

                if (node.Value == null)
                {
                    tempNode = node.Previous;
                    Logs.Remove(node);
                    node = tempNode;
                    continue;
                }

                bytesLength += node.Value.Bytes.Length;
                node = node.Previous;
            }

            byte[] bytes = new byte[bytesLength + 1];
            bytes[0] = (byte)BinarySerializationMode.Incremental;
            int offset = 1;

            foreach (var item in Logs)
            {
                Buffer.BlockCopy(item.Bytes, 0, bytes, offset, item.Bytes.Length);
                offset += item.Bytes.Length;
            }

            return bytes;
        }

        private void DeserializeSheetDataIncremental(byte[] bytes)
        {
            if (bytes.Length < 8)
            {
                return;
            }

            Logs.Clear();

            var mode = BinaryHelper.GetModeByte(BinarySerializationMode.Incremental);
            int offset = 1;
            while (offset < bytes.Length)
            {
                var rawBytesLength = BitConverter.ToInt32(bytes.GetRange(offset + 4, 4), 0);
                var cellBytesLength = rawBytesLength + 8;
                var cellBytes = new byte[cellBytesLength];
                Buffer.BlockCopy(bytes, offset, cellBytes, 0, cellBytesLength);

                var idx = BitConverter.ToInt32(cellBytes.GetRange(0, 4), 0);
                var rowIndx = BinaryHelper.ExtractRow(idx);
                var columnIndex = BinaryHelper.ExtractColumn(idx);

                byte[] rawBytes = cellBytes.GetRange(8);
                var rawText = rawBytes.GetString(mode);
                
                this.sheet.SetRawValueInternal(rowIndx, columnIndex, rawText);

                CellLog item = new CellLog();
                item.Idx = idx;
                item.Bytes = cellBytes;
                Logs.AddLast(new LinkedListNode<CellLog>(item));

                offset += cellBytesLength;
            }
        }

        private void DeserializeSheetDataFully(byte[] bytes)
        {
            if (bytes.Length < 5)
            {
                return;
            }

            this.sheet.Clear();

            // Skip first 5 bytes
            int index = 5;
            byte mode = BinaryHelper.GetModeByte(BinarySerializationMode.Full);

            while (index < bytes.Length)
            {
                var idx = BitConverter.ToInt32(bytes.GetRange(index, 4), 0);
                var columnIndex = BinaryHelper.ExtractColumn(idx);
                var rowIndex = BinaryHelper.ExtractRow(idx);

                int rawBytesLength = BitConverter.ToInt32(bytes.GetRange(index + 4, 4), 0);
                var rawText = bytes.GetRange(index + 8, rawBytesLength).GetString(mode);
                this.sheet.SetRawValueInternal(rowIndex, columnIndex, rawText);

                index += 8 + rawBytesLength;
            }
        }

        private byte[] SerializeSheetDataFully()
        {
            byte mode = BinaryHelper.GetModeByte(BinarySerializationMode.Full);

            using (MemoryStream ms = new MemoryStream())
            {
                // Write header
                ms.WriteByte((byte)BinarySerializationMode.Full);

                // 4 bytes Header Length, it's zero by now
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);

                int rowCount = this.sheet.GetRowCount();
                int columnCount = this.sheet.GetColumnCount();
                for (int row = 0; row < rowCount; row++)
                {
                    for (int column = 0; column < columnCount; column++)
                    {
                        var cell = this.sheet.GetCell(row, column);
                        if (cell == null)
                        {
                            continue;
                        }

                        var raw = cell.GetRawValue();
                        if (string.IsNullOrEmpty(raw))
                        {
                            continue;
                        }

                        ms.Write(BitConverter.GetBytes(BinaryHelper.ToIdx(row, column)), 0, 4);
                        var rawBytes = raw.GetBytes(mode);
                        ms.Write(BitConverter.GetBytes(rawBytes.Length), 0, 4);
                        ms.Write(rawBytes, 0, rawBytes.Length);
                    }
                }

                return ms.ToArray();
            }
        }
    }

    internal class CellLogCollection : LinkedList<CellLog>
    {
        private static byte mode = BinaryHelper.GetModeByte(BinarySerializationMode.Incremental);

        public void AddLog(int rowIndex, int columnIndex, string rawText)
        {
            int idx = BinaryHelper.ToIdx(rowIndex, columnIndex);

            var log = new CellLog()
            {
                Idx = idx,
                Bytes = BinaryHelper.GetLogBytes(idx, rawText, mode)
            };

            this.AddLast(new LinkedListNode<CellLog>(log));
        }
    }

    internal class CellLog
    {
        public int Idx;
        public byte[] Bytes;
    }
}