using System;
using System.Linq;

namespace Neo4j.Driver
{
    /// <summary>
    ///     Converts from/to big endian (target) to platform endian.
    /// </summary>
    public class BigEndianTargetBitConverter : BitConverterBase
    {
        /// <summary>
        ///     Converts the bytes to big endian.
        /// </summary>
        /// <param name="bytes">The bytes to convert.</param>
        /// <returns>The bytes converted to big endian.</returns>
        protected override byte[] ToTargetEndian(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                return bytes.Reverse().ToArray();
            }
            return bytes;
        }

        /// <summary>
        ///     Converts the bytes to the platform endian type.
        /// </summary>
        /// <param name="bytes">The bytes to convert.</param>
        /// <returns>The bytes converted to the platform endian type.</returns>
        protected override byte[] ToPlatformEndian(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                return bytes.Reverse().ToArray();
            }
            return bytes;
        }
    }
}