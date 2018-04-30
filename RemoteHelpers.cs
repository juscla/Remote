namespace Remote
{
    using System.Collections.Generic;
    using System.Linq;

    using MessagePack;

    /// <summary>
    /// The remote helpers.
    /// </summary>
    public static class RemoteHelpers
    {
        /// <summary>
        /// The to byte array.
        /// </summary>
        /// <param name="source">The source object to serialize as a byte array.</param>
        /// <returns>
        /// the object as a byte array.
        /// </returns>
        public static byte[] ToByteArray(this object source)
        {
            return MessagePackSerializer.Typeless.Serialize(source);
        }

        /// <summary>
        /// The to object.
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="raw">The raw.</param>
        /// <returns>
        /// The <see cref="T" />.
        /// Converts the byte[] to a Serializable Type
        /// </returns>
        public static T ToObject<T>(this byte[] raw)
        {
            if (raw == null || raw.Length < 1)
            {
                return default(T);
            }

            return (T)MessagePackSerializer.Typeless.Deserialize(raw);
        }

        /// <summary>
        /// The to object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="raw">The raw.</param>
        /// <returns>
        /// The <see cref="T" />.
        /// </returns>
        public static T ToObject<T>(this IEnumerable<byte> raw)
        {
            return raw.ToArray().ToObject<T>();
        }

        /// <summary>
        /// The to object.
        /// </summary>
        /// <param name="raw">
        /// The raw.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public static object ToObject(this IEnumerable<byte> raw)
        {
            var temp = raw.ToArray();

            return temp.ToObject<object>();
        }

        /// <summary>
        /// The to object.
        /// </summary>
        /// <param name="raw">
        /// The raw.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public static object ToObject(this byte[] raw)
        {
            return raw.ToObject<object>();
        }
    }
}