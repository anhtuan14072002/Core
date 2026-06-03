//  Copyright (c) 2022-present amlovey
//  
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Yade.Runtime.BinarySerialization
{
    internal static class BinaryHelper
    {
        internal static byte[] GetLogBytes(int idx, string rawText, byte mode)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(BitConverter.GetBytes(idx), 0, 4);
                var rawBytes = rawText.GetBytes(mode);
                ms.Write(BitConverter.GetBytes(rawBytes.Length), 0, 4);
                ms.Write(rawBytes, 0, rawBytes.Length);

                return ms.ToArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ExtractRow(int idx)
        {
            return idx >> BinarySerializationSettings.COLUMN_BIT_COUNT;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ExtractColumn(int idx)
        {
            return idx & BinarySerializationSettings.COLUMN_MASK;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ToIdx(int row, int column)
        {
            return (row << BinarySerializationSettings.COLUMN_BIT_COUNT)
                 | (BinarySerializationSettings.COLUMN_MASK & column);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte GetModeByte(BinarySerializationMode mode)
        {
            return (byte)(~mode);   
        }

        internal static string GetString(this byte[] bytes, byte mode)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= mode;
            }

            return Encoding.UTF8.GetString(bytes);
        }

        internal static byte[] GetBytes(this string s, byte mode)
        {
            if (string.IsNullOrEmpty(s))
            {
                return new byte[0];
            }

            var bytes = Encoding.UTF8.GetBytes(s);

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= mode;
            }

            return bytes;
        }

        internal static byte[] GetRange(this byte[] bytes, int start, int length)
        {
            byte[] results = new byte[length];

            for (int i = 0; i < length; i++)
            {
                results[i] = bytes[i + start];
            }
            
            return results;
        }

        internal static byte[] GetRange(this byte[] bytes, int start)
        {
            var length = bytes.Length - start;
            byte[] results = new byte[length];
            for (int i = 0; i < length; i++)
            {
                results[i] = bytes[i + start];
            }
            return results;
        }
    }
}