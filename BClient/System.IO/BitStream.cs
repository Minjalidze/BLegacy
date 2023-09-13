using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace System.IO
{
    public class BitStream
    {
        public virtual Stream BaseStream { get; private set; }
        public Boolean Serialization;
        private long writerPosition;
        private long readerPosition;

        public BitStream(Boolean serialize = true)
        {
            this.BaseStream = new MemoryStream();
            this.Serialization = serialize;
        }

        public BitStream(Byte[] bytes, Boolean serialize = true)
        {
            this.BaseStream = new MemoryStream(bytes);
            this.Serialization = serialize;
        }
        
        public BitStream(Stream input, Boolean serialize = true)
        {
            this.BaseStream = input;
            this.Serialization = serialize;
        }

        public void RegisterType<T>(BitStreamCodec.SerializerHandler serializer, BitStreamCodec.DeserializerHandler deserializer)
        {
            BitStreamCodec.RegisterCodec<T>(serializer, deserializer);
        }

        public long Length
        {
            get { return this.BaseStream.Length; }
        }

        public void SetLength(long value)
        {
            this.BaseStream.SetLength(value);
        }

        public long ReadPosition
        {
            get { return this.readerPosition; }
            set { this.readerPosition = value; }
        }

        public long WritePosition
        {
            get { return this.writerPosition; }
            set { this.writerPosition = value; }
        }

        public long Seek(long offset, SeekOrigin origin)
        { 
            return this.BaseStream.Seek(offset, origin);
        }

        public Byte[] GetBuffer(int offset = 0)
        { 
            var bytes = new Byte[this.BaseStream.Length];
            this.BaseStream.Seek(offset, SeekOrigin.Begin);
            this.BaseStream.Read(bytes, 0, bytes.Length);
            this.BaseStream.Position = this.readerPosition; 
            return bytes;
        }

        public void SetBuffer(Byte[] bytes, int offset = 0)
        {
            this.BaseStream = new MemoryStream(bytes, offset, bytes.Length);
            this.writerPosition = this.BaseStream.Position;
            this.readerPosition = 0; 
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            this.BaseStream.Position = this.writerPosition;
            this.BaseStream.Write(buffer, offset, count);
            this.writerPosition = this.BaseStream.Position;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            this.BaseStream.Position = this.readerPosition;
            return this.BaseStream.Read(buffer, offset, count);
        }

        #region [public] WriteBytes(Byte[])
        public void WriteBytes(Byte[] bytes, int offset, int length)
        {
            this.BaseStream.Position = this.writerPosition;
            this.BaseStream.Write(bytes, offset, length);
            this.writerPosition = this.BaseStream.Position;
        }
        #endregion
        #region [public] ReadBytes(length)
        public Byte[] ReadBytes(long length = 0)
        {
            if (length <= 0) { length = this.BaseStream.Length - this.readerPosition; }
            var bytes = new Byte[length];
            this.BaseStream.Position = this.readerPosition;
            this.BaseStream.Read(bytes, 0, bytes.Length);
            this.readerPosition = this.BaseStream.Position;
            return bytes;
        }
        #endregion

        #region [public] Write System Types
        public void Write<T>(object value)
        {
            var Codec = BitStreamCodec.GetCodec<T>(); 
            Codec.Serializer(this, value, ref this.writerPosition);
        }
        public void WriteBoolean(Boolean value) { this.Write<Boolean>(value); }
        public void WriteChar(Char value) { this.Write<Char>(value); }
        public void WriteByte(Byte value) { this.Write<Byte>(value); }
        public void WriteSByte(SByte value) { this.Write<SByte>(value); }
        public void WriteInt16(Int16 value) { this.Write<Int16>(value); }
        public void WriteUInt16(UInt16 value) { this.Write<UInt16>(value); }
        public void WriteInt32(Int32 value) { this.Write<Int32>(value); }
        public void WriteUInt32(UInt32 value) { this.Write<UInt32>(value); }
        public void WriteInt64(Int64 value) { this.Write<Int64>(value); }
        public void WriteUInt64(UInt64 value) { this.Write<UInt64>(value); }
        public void WriteSingle(Single value) { this.Write<Single>(value); }
        public void WriteDouble(Double value) { this.Write<Double>(value); }
        public void WriteDecimal(Decimal value) { this.Write<Decimal>(value); }
        public void WriteTimeSpan(TimeSpan value) { this.Write<TimeSpan>(value); }
        public void WriteDateTime(DateTime value) { this.Write<DateTime>(value); }
        public void WriteString(String value) { this.Write<String>(value); }
        #endregion        
        #region [public] Read System Types
        public T Read<T>()
        {
            var Codec = BitStreamCodec.GetCodec<T>();
            return (T)Codec.Deserializer(this, ref this.readerPosition);
        }
        public Boolean ReadBoolean() { return this.Read<Boolean>(); }        
        public Char ReadChar() { return this.Read<Char>(); }
        public Byte ReadByte() { return this.Read<Byte>(); }        
        public SByte ReadSByte() { return this.Read<SByte>(); }
        public Int16 ReadInt16() { return this.Read<Int16>(); }
        public UInt16 ReadUInt16() { return this.Read<UInt16>(); }
        public Int32 ReadInt32() { return this.Read<Int32>(); }
        public UInt32 ReadUInt32() { return this.Read<UInt32>(); }
        public Int64 ReadInt64() { return this.Read<Int64>(); }
        public UInt64 ReadUInt64() { return this.Read<UInt64>(); }
        public Single ReadSingle() { return this.Read<Single>(); }
        public Double ReadDouble() { return this.Read<Double>(); }
        public Decimal ReadDecimal() { return this.Read<Decimal>(); }
        public TimeSpan ReadTimeSpan() { return this.Read<TimeSpan>(); }
        public DateTime ReadDateTime() { return this.Read<DateTime>(); }
        public String ReadString() { return this.Read<String>(); }
        #endregion
    }
}