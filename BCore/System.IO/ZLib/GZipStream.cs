using System.Text;

namespace System.IO.Compression.Zlib;

public class GZipStream : Stream
{
    internal static readonly DateTime _unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    internal static readonly Encoding iso8859dash1 = Encoding.GetEncoding("iso-8859-1");
    internal ZlibBaseStream _baseStream;
    private string _Comment;
    private bool _disposed;
    private string _FileName;
    private bool _firstReadDone;

    private int _headerByteCount;

    public DateTime? LastModified;

    public GZipStream(Stream stream, CompressionMode mode) : this(stream, mode, CompressionLevel.Default, false)
    {
    }

    public GZipStream(Stream stream, CompressionMode mode, CompressionLevel level) : this(stream, mode, level, false)
    {
    }

    public GZipStream(Stream stream, CompressionMode mode, bool leaveOpen) : this(stream, mode,
        CompressionLevel.Default, leaveOpen)
    {
    }

    public GZipStream(Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
    {
        _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.GZIP, leaveOpen);
    }

    public string Comment
    {
        get => _Comment;
        set
        {
            if (_disposed) throw new ObjectDisposedException("GZipStream");
            _Comment = value;
        }
    }

    public string FileName
    {
        get => _FileName;
        set
        {
            if (_disposed) throw new ObjectDisposedException("GZipStream");
            _FileName = value;
            if (_FileName == null) return;
            if (_FileName.IndexOf("/") != -1) _FileName = _FileName.Replace("/", "\\");
            if (_FileName.EndsWith("\\")) throw new Exception("Illegal filename");
            if (_FileName.IndexOf("\\") != -1) _FileName = Path.GetFileName(_FileName);
        }
    }

    public int Crc32 { get; private set; }

    private int EmitHeader()
    {
        var commentBytes = Comment == null ? null : iso8859dash1.GetBytes(Comment);
        var filenameBytes = FileName == null ? null : iso8859dash1.GetBytes(FileName);

        var cbLength = Comment == null ? 0 : commentBytes.Length + 1;
        var fnLength = FileName == null ? 0 : filenameBytes.Length + 1;

        var bufferLength = 10 + cbLength + fnLength;
        var header = new byte[bufferLength];
        var i = 0;

        header[i++] = 0x1F;
        header[i++] = 0x8B;

        // compression method
        header[i++] = 8;
        byte flag = 0;
        if (Comment != null) flag ^= 0x10;
        if (FileName != null) flag ^= 0x8;

        // flag
        header[i++] = flag;

        // mtime
        if (!LastModified.HasValue) LastModified = DateTime.Now;
        var delta = LastModified.Value - _unixEpoch;
        var timet = (int)delta.TotalSeconds;
        Array.Copy(BitConverter.GetBytes(timet), 0, header, i, 4);
        i += 4;

        header[i++] = 0; // this field is totally useless
        header[i++] = 0xFF; // 0xFF == unspecified

        // extra field length - only if FEXTRA is set, which it is not.
        //header[i++]= 0;
        //header[i++]= 0;

        // filename
        if (fnLength != 0)
        {
            Array.Copy(filenameBytes, 0, header, i, fnLength - 1);
            i += fnLength - 1;
            header[i++] = 0; // terminate
        }

        // comment
        if (cbLength != 0)
        {
            Array.Copy(commentBytes, 0, header, i, cbLength - 1);
            i += cbLength - 1;
            header[i++] = 0; // terminate
        }

        _baseStream._stream.Write(header, 0, header.Length);
        return header.Length; // bytes written
    }

    public static byte[] CompressString(string s)
    {
        using (var ms = new MemoryStream())
        {
            Stream compressor = new GZipStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression);
            ZlibBaseStream.CompressString(s, compressor);
            return ms.ToArray();
        }
    }

    public static byte[] CompressBuffer(byte[] b)
    {
        using (var ms = new MemoryStream())
        {
            Stream compressor = new GZipStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression);
            ZlibBaseStream.CompressBuffer(b, compressor);
            return ms.ToArray();
        }
    }

    public static string UncompressString(byte[] compressed)
    {
        using (var input = new MemoryStream(compressed))
        {
            Stream decompressor = new GZipStream(input, CompressionMode.Decompress);
            return ZlibBaseStream.UncompressString(compressed, decompressor);
        }
    }

    public static byte[] UncompressBuffer(byte[] compressed)
    {
        using (var input = new MemoryStream(compressed))
        {
            Stream decompressor = new GZipStream(input, CompressionMode.Decompress);
            return ZlibBaseStream.UncompressBuffer(compressed, decompressor);
        }
    }

    #region Zlib properties

    public virtual FlushType FlushMode
    {
        get => _baseStream._flushMode;
        set
        {
            if (_disposed) throw new ObjectDisposedException("GZipStream");
            _baseStream._flushMode = value;
        }
    }

    public int BufferSize
    {
        get => _baseStream._bufferSize;
        set
        {
            if (_disposed) throw new ObjectDisposedException("GZipStream");
            if (_baseStream._workingBuffer != null) throw new ZlibException("The working buffer is already set.");
            if (value < ZlibConstants.WorkingBufferSizeMin)
                throw new ZlibException(
                    $"Don't be silly. {value} bytes?? Use a bigger buffer, at least {ZlibConstants.WorkingBufferSizeMin}.");
            _baseStream._bufferSize = value;
        }
    }

    public virtual long TotalIn => _baseStream._z.TotalBytesIn;

    public virtual long TotalOut => _baseStream._z.TotalBytesOut;

    #endregion

    #region Stream methods

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (!_disposed)
            {
                if (disposing && _baseStream != null)
                {
                    _baseStream.Close();
                    Crc32 = _baseStream.Crc32;
                }

                _disposed = true;
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    public override bool CanRead
    {
        get
        {
            if (_disposed) throw new ObjectDisposedException("GZipStream");
            return _baseStream._stream.CanRead;
        }
    }

    public override bool CanSeek => false;

    public override bool CanWrite
    {
        get
        {
            if (_disposed) throw new ObjectDisposedException("GZipStream");
            return _baseStream._stream.CanWrite;
        }
    }

    public override void Flush()
    {
        if (_disposed) throw new ObjectDisposedException("GZipStream");
        _baseStream.Flush();
    }

    public override long Length => throw new NotImplementedException();

    public override long Position
    {
        get
        {
            if (_baseStream._streamMode == ZlibBaseStream.StreamMode.Writer)
                return _baseStream._z.TotalBytesOut + _headerByteCount;
            if (_baseStream._streamMode == ZlibBaseStream.StreamMode.Reader)
                return _baseStream._z.TotalBytesIn + _baseStream._gzipHeaderByteCount;
            return 0;
        }
        set => throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_disposed) throw new ObjectDisposedException("GZipStream");
        var n = _baseStream.Read(buffer, offset, count);

        if (!_firstReadDone)
        {
            _firstReadDone = true;
            FileName = _baseStream._GzipFileName;
            Comment = _baseStream._GzipComment;
        }

        return n;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_disposed) throw new ObjectDisposedException("GZipStream");
        if (_baseStream._streamMode == ZlibBaseStream.StreamMode.Undefined)
        {
            if (_baseStream._wantCompress)
                _headerByteCount = EmitHeader();
            else
                throw new InvalidOperationException();
        }

        _baseStream.Write(buffer, offset, count);
    }

    #endregion
}