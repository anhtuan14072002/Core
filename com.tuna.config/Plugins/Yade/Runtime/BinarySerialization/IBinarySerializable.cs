//  Copyright (c) 2022-present amlovey
//  
namespace Yade.Runtime.BinarySerialization
{
    internal interface IBinarySerializable
    {
        byte[] GetBytes();
        void FromBytes(byte[] bytes, int offset = 0);
    }
}