using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class BitConverterHelper
    {
        public static byte[] GetBytes(this decimal value)
        {
            var bytes = new List<byte>();
            foreach (var i in decimal.GetBits(value)) bytes.AddRange(BitConverter.GetBytes(i));
            return bytes.ToArray();
        }
    }

    public static class ByteCollectionHelper
    {
        public static decimal ToDecimal(this byte[] bytes)
        {
            if (bytes.Count() != 16) throw new Exception("A decimal must be created from exactly 16 bytes");
            var bits = new int[4];
            for (var i = 0; i <= 15; i += 4) bits[i / 4] = BitConverter.ToInt32(bytes, i);
            return new decimal(bits);
        }
    }
}

namespace System.IO
{
    public enum BitStreamTypeCode : byte
    {
        Undefined = 0,
        Boolean = 0x01,
        Char = 0x02,
        SByte = 0x03,
        Byte = 0x04,
        Int16 = 0x05,
        UInt16 = 0x06,
        Int32 = 0x07,
        UInt32 = 0x08,
        Int64 = 0x09,
        UInt64 = 0x0a,
        Single = 0x0b,
        Double = 0x0c,
        Decimal = 0x0d,
        TimeSpan = 0x0e,
        DateTime = 0x0f,
        String = 0x10,

        // Array Values //
        ArrayType = 0x80,
        ArrayTypeMax = 0xff,
        MaxValue = 0xff
    }

    public class BitStreamCodec
    {
        public delegate object DeserializerHandler(BitStream stream, ref long position);

        // Serialize/Deserialize Handlers //
        public delegate void SerializerHandler(BitStream stream, object value, ref long position);

        // Dictionary(Type, BitStreamCodec) //
        private static readonly Dictionary<Type, BitStreamCodec> Codecs = new();

        // Last Custom Code for Codecs //
        private static byte LastCode = 0x10;
        public DeserializerHandler Deserializer;
        public SerializerHandler Serializer;

        // Register a Codecs //
        static BitStreamCodec()
        {
            RegisterCodec<object>(BitStreamTypeCode.Undefined, SerializeObject, DeserializeObject);
            RegisterCodec<bool>(BitStreamTypeCode.Boolean, SerializeBoolean, DeserializeBoolean);
            RegisterCodec<char>(BitStreamTypeCode.Char, SerializeChar, DeserializeChar);
            RegisterCodec<byte>(BitStreamTypeCode.Byte, SerializeByte, DeserializeByte);
            RegisterCodec<sbyte>(BitStreamTypeCode.SByte, SerializeSByte, DeserializeSByte);
            RegisterCodec<short>(BitStreamTypeCode.Int16, SerializeInt16, DeserializeInt16);
            RegisterCodec<ushort>(BitStreamTypeCode.UInt16, SerializeUInt16, DeserializeUInt16);
            RegisterCodec<int>(BitStreamTypeCode.Int32, SerializeInt32, DeserializeInt32);
            RegisterCodec<uint>(BitStreamTypeCode.UInt32, SerializeUInt32, DeserializeUInt32);
            RegisterCodec<long>(BitStreamTypeCode.Int64, SerializeInt64, DeserializeInt64);
            RegisterCodec<ulong>(BitStreamTypeCode.UInt64, SerializeUInt64, DeserializeUInt64);
            RegisterCodec<float>(BitStreamTypeCode.Single, SerializeSingle, DeserializeSingle);
            RegisterCodec<double>(BitStreamTypeCode.Double, SerializeDouble, DeserializeDouble);
            RegisterCodec<decimal>(BitStreamTypeCode.Decimal, SerializeDecimal, DeserializeDecimal);
            RegisterCodec<TimeSpan>(BitStreamTypeCode.TimeSpan, SerializeTimeSpan, DeserializeTimeSpan);
            RegisterCodec<DateTime>(BitStreamTypeCode.DateTime, SerializeDateTime, DeserializeDateTime);
            RegisterCodec<string>(BitStreamTypeCode.String, SerializeString, DeserializeString);
        }

        public BitStreamCodec(Type type, byte bytecode, SerializerHandler serializer, DeserializerHandler deserializer)
        {
            Type = type;
            Code = bytecode;
            Serializer = serializer;
            Deserializer = deserializer;
        }

        // Personal //
        public Type Type { get; }
        public byte Code { get; }

        #region System Types: Serialize

        private void Serialize(BitStream stream, object value, ref long position)
        {
            stream.BaseStream.Position = position;
            byte[] bytes;
            if (stream.Serialization) stream.BaseStream.WriteByte(Code);

            switch ((BitStreamTypeCode)Code)
            {
                case BitStreamTypeCode.Boolean:
                    bytes = BitConverter.GetBytes(Convert.ToBoolean(value));
                    break;
                case BitStreamTypeCode.Char:
                    bytes = new byte[1] { Convert.ToByte(value) };
                    break;
                case BitStreamTypeCode.Byte:
                case BitStreamTypeCode.SByte:
                    bytes = new byte[1] { Convert.ToByte(value) };
                    break;
                case BitStreamTypeCode.Int16:
                    bytes = BitConverter.GetBytes(Convert.ToInt16(value));
                    break;
                case BitStreamTypeCode.UInt16:
                    bytes = BitConverter.GetBytes(Convert.ToUInt16(value));
                    break;
                case BitStreamTypeCode.Int32:
                    bytes = BitConverter.GetBytes(Convert.ToInt32(value));
                    break;
                case BitStreamTypeCode.UInt32:
                    bytes = BitConverter.GetBytes(Convert.ToUInt32(value));
                    break;
                case BitStreamTypeCode.Int64:
                    bytes = BitConverter.GetBytes(Convert.ToInt64(value));
                    break;
                case BitStreamTypeCode.UInt64:
                    bytes = BitConverter.GetBytes(Convert.ToUInt64(value));
                    break;
                case BitStreamTypeCode.Single:
                    bytes = BitConverter.GetBytes(Convert.ToSingle(value));
                    break;
                case BitStreamTypeCode.Double:
                    bytes = BitConverter.GetBytes(Convert.ToDouble(value));
                    break;
                case BitStreamTypeCode.Decimal:
                    bytes = Convert.ToDecimal(value).GetBytes();
                    break;
                case BitStreamTypeCode.TimeSpan:
                    bytes = BitConverter.GetBytes(((TimeSpan)value).Ticks);
                    break;
                case BitStreamTypeCode.DateTime:
                    bytes = BitConverter.GetBytes(((DateTime)value).ToBinary());
                    break;
                case BitStreamTypeCode.String:
                    bytes = Encoding.UTF8.GetBytes((string)value);
                    var size = BitConverter.GetBytes(bytes.Length);
                    stream.BaseStream.Write(size, 0, size.Length);
                    break;
                default:
                    bytes = new byte[0];
                    break;
            }

            stream.BaseStream.Write(bytes, 0, bytes.Length);
            position = stream.BaseStream.Position;
        }

        #endregion

        #region System Types: Deserialize

        private object Deserialize(BitStream stream, ref long position)
        {
            stream.BaseStream.Position = position;
            byte[] bytes = null;
            var result = new object();
            var byteCode = Code;

            if (stream.Serialization)
            {
                byteCode = (byte)stream.BaseStream.ReadByte();
                if (byteCode != Code) return new object();
            }

            switch ((BitStreamTypeCode)Code)
            {
                case BitStreamTypeCode.Boolean:
                    bytes = new byte[sizeof(bool)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToBoolean(bytes, 0);
                    break;
                case BitStreamTypeCode.Char:
                    result = Convert.ToChar(stream.BaseStream.ReadByte());
                    break;

                case BitStreamTypeCode.Byte:
                    result = Convert.ToByte(stream.BaseStream.ReadByte());
                    break;
                case BitStreamTypeCode.SByte:
                    result = Convert.ToSByte(stream.BaseStream.ReadByte());
                    break;
                case BitStreamTypeCode.Int16:
                    bytes = new byte[sizeof(short)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToInt16(bytes, 0);
                    break;
                case BitStreamTypeCode.UInt16:
                    bytes = new byte[sizeof(ushort)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToUInt16(bytes, 0);
                    break;
                case BitStreamTypeCode.Int32:
                    bytes = new byte[sizeof(int)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToInt32(bytes, 0);
                    break;
                case BitStreamTypeCode.UInt32:
                    bytes = new byte[sizeof(uint)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToUInt32(bytes, 0);
                    break;
                case BitStreamTypeCode.Int64:
                    bytes = new byte[sizeof(long)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToInt64(bytes, 0);
                    break;
                case BitStreamTypeCode.UInt64:
                    bytes = new byte[sizeof(ulong)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToUInt64(bytes, 0);
                    break;
                case BitStreamTypeCode.Single:
                    bytes = new byte[sizeof(float)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToSingle(bytes, 0);
                    break;
                case BitStreamTypeCode.Double:
                    bytes = new byte[sizeof(double)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToDouble(bytes, 0);
                    break;
                case BitStreamTypeCode.Decimal:
                    bytes = new byte[sizeof(decimal)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = bytes.ToDecimal();
                    break;
                case BitStreamTypeCode.TimeSpan:
                    bytes = new byte[sizeof(long)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = TimeSpan.FromTicks(BitConverter.ToInt64(bytes, 0));
                    break;
                case BitStreamTypeCode.DateTime:
                    bytes = new byte[sizeof(long)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = DateTime.FromBinary(BitConverter.ToInt64(bytes, 0));
                    break;
                case BitStreamTypeCode.String:
                    bytes = new byte[sizeof(int)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    bytes = new byte[BitConverter.ToInt32(bytes, 0)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = Encoding.UTF8.GetString(bytes);
                    break;
            }

            position = stream.BaseStream.Position;
            return result;
        }

        #endregion

        #region GetCodec(type)

        public static BitStreamCodec GetCodec<T>()
        {
            return GetCodec(typeof(T));
        }

        public static BitStreamCodec GetCodec(Type type)
        {
            BitStreamCodec codec;
            if (!Codecs.TryGetValue(type, out codec)) return Codecs[typeof(object)];
            return codec;
        }

        #endregion

        #region RegisterCodec<T>(type, code, serializer, deserializer)

        public static void RegisterCodec<T>(BitStreamTypeCode typeCode, SerializerHandler serializer,
            DeserializerHandler deserializer)
        {
            RegisterCodec<T>((byte)typeCode, serializer, deserializer);
        }

        public static void RegisterCodec<T>(byte code, SerializerHandler serializer, DeserializerHandler deserializer)
        {
            RegisterCodec(typeof(T), code, serializer, deserializer);
        }

        public static void RegisterCodec<T>(SerializerHandler serializer, DeserializerHandler deserializer)
        {
            if (Codecs.ContainsKey(typeof(T)) || LastCode >= 0x80) return;
            RegisterCodec(typeof(T), ++LastCode, serializer, deserializer);
        }

        public static void RegisterCodec(Type type, byte code, SerializerHandler serializer,
            DeserializerHandler deserializer)
        {
            if (Codecs.ContainsKey(type)) return;
            Codecs[type] = new BitStreamCodec(type, code, serializer, deserializer);
        }

        #endregion

        #region System Types: Serialize\Deserialize Handlers

        private static void SerializeObject(BitStream stream, object value, ref long position)
        {
            GetCodec<object>().Serialize(stream, value, ref position);
        }

        private static object DeserializeObject(BitStream stream, ref long position)
        {
            return GetCodec<object>().Deserialize(stream, ref position);
        }

        private static void SerializeBoolean(BitStream stream, object value, ref long position)
        {
            GetCodec<bool>().Serialize(stream, value, ref position);
        }

        private static object DeserializeBoolean(BitStream stream, ref long position)
        {
            return GetCodec<bool>().Deserialize(stream, ref position);
        }

        private static void SerializeChar(BitStream stream, object value, ref long position)
        {
            GetCodec<char>().Serialize(stream, value, ref position);
        }

        private static object DeserializeChar(BitStream stream, ref long position)
        {
            return GetCodec<char>().Deserialize(stream, ref position);
        }

        private static void SerializeByte(BitStream stream, object value, ref long position)
        {
            GetCodec<byte>().Serialize(stream, value, ref position);
        }

        private static object DeserializeByte(BitStream stream, ref long position)
        {
            return GetCodec<byte>().Deserialize(stream, ref position);
        }

        private static void SerializeSByte(BitStream stream, object value, ref long position)
        {
            GetCodec<sbyte>().Serialize(stream, value, ref position);
        }

        private static object DeserializeSByte(BitStream stream, ref long position)
        {
            return GetCodec<sbyte>().Deserialize(stream, ref position);
        }

        private static void SerializeInt16(BitStream stream, object value, ref long position)
        {
            GetCodec<short>().Serialize(stream, value, ref position);
        }

        private static object DeserializeInt16(BitStream stream, ref long position)
        {
            return GetCodec<short>().Deserialize(stream, ref position);
        }

        private static void SerializeUInt16(BitStream stream, object value, ref long position)
        {
            GetCodec<ushort>().Serialize(stream, value, ref position);
        }

        private static object DeserializeUInt16(BitStream stream, ref long position)
        {
            return GetCodec<ushort>().Deserialize(stream, ref position);
        }

        private static void SerializeInt32(BitStream stream, object value, ref long position)
        {
            GetCodec<int>().Serialize(stream, value, ref position);
        }

        private static object DeserializeInt32(BitStream stream, ref long position)
        {
            return GetCodec<int>().Deserialize(stream, ref position);
        }

        private static void SerializeUInt32(BitStream stream, object value, ref long position)
        {
            GetCodec<uint>().Serialize(stream, value, ref position);
        }

        private static object DeserializeUInt32(BitStream stream, ref long position)
        {
            return GetCodec<uint>().Deserialize(stream, ref position);
        }

        private static void SerializeInt64(BitStream stream, object value, ref long position)
        {
            GetCodec<long>().Serialize(stream, value, ref position);
        }

        private static object DeserializeInt64(BitStream stream, ref long position)
        {
            return GetCodec<long>().Deserialize(stream, ref position);
        }

        private static void SerializeUInt64(BitStream stream, object value, ref long position)
        {
            GetCodec<ulong>().Serialize(stream, value, ref position);
        }

        private static object DeserializeUInt64(BitStream stream, ref long position)
        {
            return GetCodec<ulong>().Deserialize(stream, ref position);
        }

        private static void SerializeSingle(BitStream stream, object value, ref long position)
        {
            GetCodec<float>().Serialize(stream, value, ref position);
        }

        private static object DeserializeSingle(BitStream stream, ref long position)
        {
            return GetCodec<float>().Deserialize(stream, ref position);
        }

        private static void SerializeDouble(BitStream stream, object value, ref long position)
        {
            GetCodec<double>().Serialize(stream, value, ref position);
        }

        private static object DeserializeDouble(BitStream stream, ref long position)
        {
            return GetCodec<double>().Deserialize(stream, ref position);
        }

        private static void SerializeDecimal(BitStream stream, object value, ref long position)
        {
            GetCodec<decimal>().Serialize(stream, value, ref position);
        }

        private static object DeserializeDecimal(BitStream stream, ref long position)
        {
            return GetCodec<decimal>().Deserialize(stream, ref position);
        }

        private static void SerializeTimeSpan(BitStream stream, object value, ref long position)
        {
            GetCodec<TimeSpan>().Serialize(stream, value, ref position);
        }

        private static object DeserializeTimeSpan(BitStream stream, ref long position)
        {
            return GetCodec<TimeSpan>().Deserialize(stream, ref position);
        }

        private static void SerializeDateTime(BitStream stream, object value, ref long position)
        {
            GetCodec<DateTime>().Serialize(stream, value, ref position);
        }

        private static object DeserializeDateTime(BitStream stream, ref long position)
        {
            return GetCodec<DateTime>().Deserialize(stream, ref position);
        }

        private static void SerializeString(BitStream stream, object value, ref long position)
        {
            GetCodec<string>().Serialize(stream, value, ref position);
        }

        private static object DeserializeString(BitStream stream, ref long position)
        {
            return GetCodec<string>().Deserialize(stream, ref position);
        }

        #endregion
    }
}