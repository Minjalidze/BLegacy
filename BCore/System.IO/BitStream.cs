namespace System.IO;

public class BitStream
{
    private long readerPosition;
    public bool Serialization;
    private long writerPosition;

    public BitStream(bool serialize = true)
    {
        BaseStream = new MemoryStream();
        Serialization = serialize;
    }

    public BitStream(byte[] bytes, bool serialize = true)
    {
        BaseStream = new MemoryStream(bytes);
        Serialization = serialize;
    }

    public BitStream(Stream input, bool serialize = true)
    {
        BaseStream = input;
        Serialization = serialize;
    }

    public virtual Stream BaseStream { get; private set; }

    public long Length => BaseStream.Length;

    public long ReadPosition
    {
        get => readerPosition;
        set => readerPosition = value;
    }

    public long WritePosition
    {
        get => writerPosition;
        set => writerPosition = value;
    }

    public void RegisterType<T>(BitStreamCodec.SerializerHandler serializer,
        BitStreamCodec.DeserializerHandler deserializer)
    {
        BitStreamCodec.RegisterCodec<T>(serializer, deserializer);
    }

    public void SetLength(long value)
    {
        BaseStream.SetLength(value);
    }

    public long Seek(long offset, SeekOrigin origin)
    {
        return BaseStream.Seek(offset, origin);
    }

    public byte[] GetBuffer(int offset = 0)
    {
        var bytes = new byte[BaseStream.Length];
        BaseStream.Seek(offset, SeekOrigin.Begin);
        BaseStream.Read(bytes, 0, bytes.Length);
        BaseStream.Position = readerPosition;
        return bytes;
    }

    public void SetBuffer(byte[] bytes, int offset = 0)
    {
        BaseStream = new MemoryStream(bytes, offset, bytes.Length);
        writerPosition = BaseStream.Position;
        readerPosition = 0;
    }

    public void Write(byte[] buffer, int offset, int count)
    {
        BaseStream.Position = writerPosition;
        BaseStream.Write(buffer, offset, count);
        writerPosition = BaseStream.Position;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        BaseStream.Position = readerPosition;
        return BaseStream.Read(buffer, offset, count);
    }

    #region [public] WriteBytes(Byte[])

    public void WriteBytes(byte[] bytes, int offset, int length)
    {
        BaseStream.Position = writerPosition;
        BaseStream.Write(bytes, offset, length);
        writerPosition = BaseStream.Position;
    }

    #endregion

    #region [public] ReadBytes(length)

    public byte[] ReadBytes(long length = 0)
    {
        if (length <= 0) length = BaseStream.Length - readerPosition;
        var bytes = new byte[length];
        BaseStream.Position = readerPosition;
        BaseStream.Read(bytes, 0, bytes.Length);
        readerPosition = BaseStream.Position;
        return bytes;
    }

    #endregion

    #region [public] Write System Types

    public void Write<T>(object value)
    {
        var Codec = BitStreamCodec.GetCodec<T>();
        Codec.Serializer(this, value, ref writerPosition);
    }

    public void WriteBoolean(bool value)
    {
        Write<bool>(value);
    }

    public void WriteChar(char value)
    {
        Write<char>(value);
    }

    public void WriteByte(byte value)
    {
        Write<byte>(value);
    }

    public void WriteSByte(sbyte value)
    {
        Write<sbyte>(value);
    }

    public void WriteInt16(short value)
    {
        Write<short>(value);
    }

    public void WriteUInt16(ushort value)
    {
        Write<ushort>(value);
    }

    public void WriteInt32(int value)
    {
        Write<int>(value);
    }

    public void WriteUInt32(uint value)
    {
        Write<uint>(value);
    }

    public void WriteInt64(long value)
    {
        Write<long>(value);
    }

    public void WriteUInt64(ulong value)
    {
        Write<ulong>(value);
    }

    public void WriteSingle(float value)
    {
        Write<float>(value);
    }

    public void WriteDouble(double value)
    {
        Write<double>(value);
    }

    public void WriteDecimal(decimal value)
    {
        Write<decimal>(value);
    }

    public void WriteTimeSpan(TimeSpan value)
    {
        Write<TimeSpan>(value);
    }

    public void WriteDateTime(DateTime value)
    {
        Write<DateTime>(value);
    }

    public void WriteString(string value)
    {
        Write<string>(value);
    }

    #endregion

    #region [public] Read System Types

    public T Read<T>()
    {
        var Codec = BitStreamCodec.GetCodec<T>();
        return (T)Codec.Deserializer(this, ref readerPosition);
    }

    public bool ReadBoolean()
    {
        return Read<bool>();
    }

    public char ReadChar()
    {
        return Read<char>();
    }

    public byte ReadByte()
    {
        return Read<byte>();
    }

    public sbyte ReadSByte()
    {
        return Read<sbyte>();
    }

    public short ReadInt16()
    {
        return Read<short>();
    }

    public ushort ReadUInt16()
    {
        return Read<ushort>();
    }

    public int ReadInt32()
    {
        return Read<int>();
    }

    public uint ReadUInt32()
    {
        return Read<uint>();
    }

    public long ReadInt64()
    {
        return Read<long>();
    }

    public ulong ReadUInt64()
    {
        return Read<ulong>();
    }

    public float ReadSingle()
    {
        return Read<float>();
    }

    public double ReadDouble()
    {
        return Read<double>();
    }

    public decimal ReadDecimal()
    {
        return Read<decimal>();
    }

    public TimeSpan ReadTimeSpan()
    {
        return Read<TimeSpan>();
    }

    public DateTime ReadDateTime()
    {
        return Read<DateTime>();
    }

    public string ReadString()
    {
        return Read<string>();
    }

    #endregion
}