using System;
using UnityEngine;
using Yade.Runtime;

namespace Core
{
    [TypeKey(1001)]
    public class EnumValue<K> : ICellParser where K : struct, Enum
    {
        public K Key { get; private set; }
        public float Value { get; private set; }
        public const char Separator = ';';

        public void ParseFrom(string s)
        {
            try
            {
                var temp = s.Split(Separator);
                if (temp.Length != 2) return;
                Key = (K)Enum.Parse(typeof(K), temp[0]);
                Value = float.Parse(temp[1]);
            }
            catch (Exception e)
            {
                Debug.LogError("Mising enum type: " + typeof(K) + " " + s.Split(Separator)[0]);
            }
        }
    }
    
    [TypeKey(1002)]
    public class IntValue : ICellParser
    {
        public int Key { get; private set; }
        public float Value { get; private set; }
        public const char Separator = ';';

        public void ParseFrom(string s)
        {
            var temp = s.Split(Separator);
            if (temp.Length != 2) return;
            Key = int.Parse(temp[0]);
            Value = float.Parse(temp[1]);
        }
    }
}