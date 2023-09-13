using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System
{
    public static class BitConverterHelper
    {
        public static Byte[] GetBytes(this Decimal value)
        {
            var bytes = new List<byte>();
            foreach (var i in Decimal.GetBits(value)) bytes.AddRange(BitConverter.GetBytes(i));
            return bytes.ToArray();
        }
    }
    
    public static class ByteCollectionHelper
    {
        public static decimal ToDecimal(this byte[] bytes)
        {
            if (bytes.Count() != 16) throw new Exception("A decimal must be created from exactly 16 bytes");
            var bits = new Int32[4]; for (var i = 0; i <= 15; i += 4) { bits[i / 4] = BitConverter.ToInt32(bytes, i); }
            return new decimal(bits);
        }
    }
}

namespace System.IO
{
    public enum BitStreamTypeCode : byte
    {
        Undefined                   = 0,
        Boolean                     = 0x01,
        Char                        = 0x02,
        SByte                       = 0x03,
        Byte                        = 0x04,
        Int16                       = 0x05,
        UInt16                      = 0x06,
        Int32                       = 0x07,
        UInt32                      = 0x08,
        Int64                       = 0x09,
        UInt64                      = 0x0a,
        Single                      = 0x0b,
        Double                      = 0x0c,
        Decimal                     = 0x0d,
        TimeSpan                    = 0x0e,
        DateTime                    = 0x0f,
        String                      = 0x10,
        // Array Values //
        ArrayType                   = 0x80,
        ArrayTypeMax                = 0xff,        
        MaxValue                    = 0xff
    }

    public class BitStreamCodec
    {
        // Dictionary(Type, BitStreamCodec) //
        private static readonly Dictionary<Type, BitStreamCodec> Codecs = new Dictionary<Type, BitStreamCodec>();                
        
        // Last Custom Code for Codecs //
        private static Byte LastCode = 0x10;
        
        // Serialize/Deserialize Handlers //
        public delegate void SerializerHandler(BitStream stream, object value, ref long position);
        public delegate object DeserializerHandler(BitStream stream, ref long position);

        // Personal //
        public Type Type { get; private set; } 
        public Byte Code { get; private set; }        
        public SerializerHandler Serializer;        
        public DeserializerHandler Deserializer;
        
        // Register a Codecs //
        static BitStreamCodec()
        {            
            BitStreamCodec.RegisterCodec<Object>(BitStreamTypeCode.Undefined, new SerializerHandler(SerializeObject), new DeserializerHandler(DeserializeObject));
            BitStreamCodec.RegisterCodec<Boolean>(BitStreamTypeCode.Boolean, new SerializerHandler(SerializeBoolean), new DeserializerHandler(DeserializeBoolean));
            BitStreamCodec.RegisterCodec<Char>(BitStreamTypeCode.Char, new SerializerHandler(SerializeChar), new DeserializerHandler(DeserializeChar));
            BitStreamCodec.RegisterCodec<Byte>(BitStreamTypeCode.Byte, new SerializerHandler(SerializeByte), new DeserializerHandler(DeserializeByte));
            BitStreamCodec.RegisterCodec<SByte>(BitStreamTypeCode.SByte, new SerializerHandler(SerializeSByte), new DeserializerHandler(DeserializeSByte));
            BitStreamCodec.RegisterCodec<Int16>(BitStreamTypeCode.Int16, new SerializerHandler(SerializeInt16), new DeserializerHandler(DeserializeInt16));
            BitStreamCodec.RegisterCodec<UInt16>(BitStreamTypeCode.UInt16, new SerializerHandler(SerializeUInt16), new DeserializerHandler(DeserializeUInt16));
            BitStreamCodec.RegisterCodec<Int32>(BitStreamTypeCode.Int32, new SerializerHandler(SerializeInt32), new DeserializerHandler(DeserializeInt32));
            BitStreamCodec.RegisterCodec<UInt32>(BitStreamTypeCode.UInt32, new SerializerHandler(SerializeUInt32), new DeserializerHandler(DeserializeUInt32));
            BitStreamCodec.RegisterCodec<Int64>(BitStreamTypeCode.Int64, new SerializerHandler(SerializeInt64), new DeserializerHandler(DeserializeInt64));
            BitStreamCodec.RegisterCodec<UInt64>(BitStreamTypeCode.UInt64, new SerializerHandler(SerializeUInt64), new DeserializerHandler(DeserializeUInt64));
            BitStreamCodec.RegisterCodec<Single>(BitStreamTypeCode.Single, new SerializerHandler(SerializeSingle), new DeserializerHandler(DeserializeSingle));
            BitStreamCodec.RegisterCodec<Double>(BitStreamTypeCode.Double, new SerializerHandler(SerializeDouble), new DeserializerHandler(DeserializeDouble));
            BitStreamCodec.RegisterCodec<Decimal>(BitStreamTypeCode.Decimal, new SerializerHandler(SerializeDecimal), new DeserializerHandler(DeserializeDecimal));
            BitStreamCodec.RegisterCodec<TimeSpan>(BitStreamTypeCode.TimeSpan, new SerializerHandler(SerializeTimeSpan), new DeserializerHandler(DeserializeTimeSpan));
            BitStreamCodec.RegisterCodec<DateTime>(BitStreamTypeCode.DateTime, new SerializerHandler(SerializeDateTime), new DeserializerHandler(DeserializeDateTime));
            BitStreamCodec.RegisterCodec<String>(BitStreamTypeCode.String, new SerializerHandler(SerializeString), new DeserializerHandler(DeserializeString));
        }

        public BitStreamCodec(Type type, Byte bytecode, SerializerHandler serializer, DeserializerHandler deserializer)
        {
            this.Type = type; 
            this.Code = bytecode;            
            this.Serializer = serializer;       
            this.Deserializer = deserializer;
        }
        
        #region GetCodec(type)
        public static BitStreamCodec GetCodec<T>()
        {
            return BitStreamCodec.GetCodec(typeof(T));
        }
        public static BitStreamCodec GetCodec(Type type)
        {
            BitStreamCodec codec;
            if (!BitStreamCodec.Codecs.TryGetValue(type, out codec))
            {
                return BitStreamCodec.Codecs[typeof(Object)];
            }
            return codec;
        }
        #endregion

        #region RegisterCodec<T>(type, code, serializer, deserializer)
        public static void RegisterCodec<T>(BitStreamTypeCode typeCode, SerializerHandler serializer, DeserializerHandler deserializer)
        {
            RegisterCodec<T>((Byte)typeCode, serializer, deserializer);
        }
        public static void RegisterCodec<T>(Byte code, SerializerHandler serializer, DeserializerHandler deserializer)
        {
            RegisterCodec(typeof(T), code, serializer, deserializer);
        }
        public static void RegisterCodec<T>(SerializerHandler serializer, DeserializerHandler deserializer)
        {
            if (BitStreamCodec.Codecs.ContainsKey(typeof(T)) || LastCode >= 0x80) return;
            RegisterCodec(typeof(T), ++LastCode, serializer, deserializer);
        }
        public static void RegisterCodec(Type type, Byte code, SerializerHandler serializer, DeserializerHandler deserializer)
        {
            if (BitStreamCodec.Codecs.ContainsKey(type)) return;
            BitStreamCodec.Codecs[type] = new BitStreamCodec(type, code, serializer, deserializer);
        }
        #endregion
        
        #region System Types: Serialize\Deserialize Handlers
        private static void SerializeObject(BitStream stream, object value, ref long position)
        {            
            GetCodec<Object>().Serialize(stream, value, ref position);
        }
        private static object DeserializeObject(BitStream stream, ref long position)
        {
            return GetCodec<Object>().Deserialize(stream, ref position);
        }

        private static void SerializeBoolean(BitStream stream, object value, ref long position)
        {            
            GetCodec<Boolean>().Serialize(stream, value, ref position);
        }
        private static object DeserializeBoolean(BitStream stream, ref long position)
        {
            return GetCodec<Boolean>().Deserialize(stream, ref position);
        }

        private static void SerializeChar(BitStream stream, object value, ref long position)
        {            
            GetCodec<Char>().Serialize(stream, value, ref position);
        }
        private static object DeserializeChar(BitStream stream, ref long position)
        {
            return GetCodec<Char>().Deserialize(stream, ref position);
        }

        private static void SerializeByte(BitStream stream, object value, ref long position)
        {            
            GetCodec<Byte>().Serialize(stream, value, ref position);
        }
        private static object DeserializeByte(BitStream stream, ref long position)
        {
            return GetCodec<Byte>().Deserialize(stream, ref position);
        }

        private static void SerializeSByte(BitStream stream, object value, ref long position)
        {            
            GetCodec<SByte>().Serialize(stream, value, ref position);
        }
        private static object DeserializeSByte(BitStream stream, ref long position)
        {
            return GetCodec<SByte>().Deserialize(stream, ref position);
        }

        private static void SerializeInt16(BitStream stream, object value, ref long position)
        {            
            GetCodec<Int16>().Serialize(stream, value, ref position);
        }
        private static object DeserializeInt16(BitStream stream, ref long position)
        {
            return GetCodec<Int16>().Deserialize(stream, ref position);
        }

        private static void SerializeUInt16(BitStream stream, object value, ref long position)
        {            
            GetCodec<UInt16>().Serialize(stream, value, ref position);
        }
        private static object DeserializeUInt16(BitStream stream, ref long position)
        {
            return GetCodec<UInt16>().Deserialize(stream, ref position);
        }

        private static void SerializeInt32(BitStream stream, object value, ref long position)
        {            
            GetCodec<Int32>().Serialize(stream, value, ref position);
        }
        private static object DeserializeInt32(BitStream stream, ref long position)
        {
            return GetCodec<Int32>().Deserialize(stream, ref position);
        }

        private static void SerializeUInt32(BitStream stream, object value, ref long position)
        {            
            GetCodec<UInt32>().Serialize(stream, value, ref position);
        }
        private static object DeserializeUInt32(BitStream stream, ref long position)
        {
            return GetCodec<UInt32>().Deserialize(stream, ref position);
        }

        private static void SerializeInt64(BitStream stream, object value, ref long position)
        {            
            GetCodec<Int64>().Serialize(stream, value, ref position);
        }
        private static object DeserializeInt64(BitStream stream, ref long position)
        {
            return GetCodec<Int64>().Deserialize(stream, ref position);
        }

        private static void SerializeUInt64(BitStream stream, object value, ref long position)
        {            
            GetCodec<UInt64>().Serialize(stream, value, ref position);
        }
        private static object DeserializeUInt64(BitStream stream, ref long position)
        {
            return GetCodec<UInt64>().Deserialize(stream, ref position);
        }

        private static void SerializeSingle(BitStream stream, object value, ref long position)
        {            
            GetCodec<Single>().Serialize(stream, value, ref position);
        }
        private static object DeserializeSingle(BitStream stream, ref long position)
        {
            return GetCodec<Single>().Deserialize(stream, ref position);
        }

        private static void SerializeDouble(BitStream stream, object value, ref long position)
        {            
            GetCodec<Double>().Serialize(stream, value, ref position);
        }
        private static object DeserializeDouble(BitStream stream, ref long position)
        {
            return GetCodec<Double>().Deserialize(stream, ref position);
        }

        private static void SerializeDecimal(BitStream stream, object value, ref long position)
        {            
            GetCodec<Decimal>().Serialize(stream, value, ref position);
        }
        private static object DeserializeDecimal(BitStream stream, ref long position)
        {
            return GetCodec<Decimal>().Deserialize(stream, ref position);
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
            GetCodec<String>().Serialize(stream, value, ref position);
        }
        private static object DeserializeString(BitStream stream, ref long position)
        {
            return GetCodec<String>().Deserialize(stream, ref position);
        }        
        #endregion

        #region System Types: Serialize
        private void Serialize(BitStream stream, object value, ref long position)        
        {
            stream.BaseStream.Position = position; Byte[] bytes;           
            if (stream.Serialization) stream.BaseStream.WriteByte(this.Code);
            
            switch ((BitStreamTypeCode)this.Code)
            {
                case BitStreamTypeCode.Boolean:                    
                    bytes = BitConverter.GetBytes(Convert.ToBoolean(value));
                break;
                case BitStreamTypeCode.Char:
                    bytes = new Byte[1] { Convert.ToByte(value) };
                break;
                case BitStreamTypeCode.Byte: case BitStreamTypeCode.SByte:
                    bytes = new Byte[1] { Convert.ToByte(value) };
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
                    bytes = Encoding.UTF8.GetBytes((String)value);
                    var size = BitConverter.GetBytes(bytes.Length);                    
                    stream.BaseStream.Write(size, 0, size.Length);                    
                break;
                default:
                    bytes = new Byte[0];
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
            Byte[] bytes = null; var result = new System.Object();
            var byteCode = this.Code;

            if (stream.Serialization)
            {
                byteCode = (Byte)stream.BaseStream.ReadByte();
                if (byteCode != this.Code) return new object();
            }

            switch ((BitStreamTypeCode)this.Code)
            {                
                case BitStreamTypeCode.Boolean:
                    bytes = new Byte[sizeof(Boolean)];
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
                    bytes = new Byte[sizeof(Int16)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToInt16(bytes, 0);                
                break;
                case BitStreamTypeCode.UInt16:
                    bytes = new Byte[sizeof(UInt16)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToUInt16(bytes, 0);
                break;
                case BitStreamTypeCode.Int32:                    
                    bytes = new Byte[sizeof(Int32)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToInt32(bytes, 0);                
                break;
                case BitStreamTypeCode.UInt32:
                    bytes = new Byte[sizeof(UInt32)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToUInt32(bytes, 0);
                break;
                case BitStreamTypeCode.Int64:                    
                    bytes = new Byte[sizeof(Int64)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToInt64(bytes, 0);                
                break;
                case BitStreamTypeCode.UInt64:
                    bytes = new Byte[sizeof(UInt64)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToUInt64(bytes, 0);
                break;
                case BitStreamTypeCode.Single:                    
                    bytes = new Byte[sizeof(Single)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToSingle(bytes, 0);                
                break;
                case BitStreamTypeCode.Double:
                    bytes = new Byte[sizeof(Double)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = BitConverter.ToDouble(bytes, 0);
                break;
                case BitStreamTypeCode.Decimal:
                    bytes = new Byte[sizeof(Decimal)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = bytes.ToDecimal();
                break;
                case BitStreamTypeCode.TimeSpan:
                    bytes = new Byte[sizeof(Int64)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = TimeSpan.FromTicks(BitConverter.ToInt64(bytes, 0));
                break;
                case BitStreamTypeCode.DateTime:
                    bytes = new Byte[sizeof(Int64)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = DateTime.FromBinary(BitConverter.ToInt64(bytes, 0));
                break;
                case BitStreamTypeCode.String:                    
                    bytes = new Byte[sizeof(Int32)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    bytes = new Byte[BitConverter.ToInt32(bytes, 0)];
                    stream.BaseStream.Read(bytes, 0, bytes.Length);
                    result = Encoding.UTF8.GetString(bytes);
                break;
            }
            position = stream.BaseStream.Position; 
            return result;
        }
        #endregion
    }
}
