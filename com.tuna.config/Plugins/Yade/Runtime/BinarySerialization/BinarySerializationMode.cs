//  Copyright (c) 2022-present amlovey
//  
namespace Yade.Runtime.BinarySerialization
{
    /// <summary>
    /// Mode of binary serialization
    /// </summary>
    public enum BinarySerializationMode
    {
        /// <summary>
        /// Serialize full data to binaries.
        /// </summary>
        Full = 1,

        /// <summary>
        /// Serialize the changed items to binaries. This is the default mode.
        /// </summary>
        Incremental,
    }
}