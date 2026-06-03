//  Copyright (c) 2022-present amlovey
//  
namespace Yade.Runtime.BinarySerialization
{
    /// <summary>
    /// Settings for binary serialization
    /// </summary>
    public class BinarySerializationSettings
    {
        /// <summary>
        /// Default settings for binary serialization
        /// </summary>
        public static BinarySerializationSettings Default = new BinarySerializationSettings();

        /// <summary>
        /// Mode of binary serialization
        /// </summary>
        public BinarySerializationMode Mode { get; set; }

        /// <summary>
        /// Maximum count of column that serialization support. Max rows count = 2147483647 / ( 1 << MAX_COLUMN_COUNT)xxw;
        /// </summary>
        internal static int COLUMN_BIT_COUNT = 10;
        internal static int COLUMN_MASK = ~(~0 << COLUMN_BIT_COUNT);

        public BinarySerializationSettings(BinarySerializationMode mode = BinarySerializationMode.Incremental)
        {
            this.Mode = mode;
        }
    }
}