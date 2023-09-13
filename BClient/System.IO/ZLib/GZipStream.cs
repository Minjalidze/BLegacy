using System;
using System.IO;

namespace System.IO.Compression.Zlib
{
    public class GZipStream : System.IO.Stream
    {
        public String Comment
        {
            get
            {
                return _Comment;
            }
            set
            {
                if (_disposed) throw new ObjectDisposedException("GZipStream");
                _Comment = value;
            }
        }

        public String FileName
        {
            get { return _FileName; }
            set
            {
                if (_disposed) throw new ObjectDisposedException("GZipStream");
                _FileName = value; if (_FileName == null) return;
                if (_FileName.IndexOf("/") != -1) { _FileName = _FileName.Replace("/", "\\"); }
                if (_FileName.EndsWith("\\")) throw new Exception("Illegal filename");
                if (_FileName.IndexOf("\\") != -1) { _FileName = Path.GetFileName(_FileName); }
            }
        }

        public DateTime? LastModified;
        public int Crc32 { get { return _Crc32; } }

        private int _headerByteCount;
        internal ZlibBaseStream _baseStream;
        bool _disposed;
        bool _firstReadDone;
        string _FileName;
        string _Comment;
        int _Crc32;

        public GZipStream(Stream stream, CompressionMode mode) : this(stream, mode, CompressionLevel.Default, false)
        {
        }

        public GZipStream(Stream stream, CompressionMode mode, CompressionLevel level) : this(stream, mode, level, false)
        {
        }

        public GZipStream(Stream stream, CompressionMode mode, bool leaveOpen) : this(stream, mode, CompressionLevel.Default, leaveOpen)
        {
        }

        public GZipStream(Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen) 
        {
            _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.GZIP, leaveOpen);
        }

        #region Zlib properties
        virtual public FlushType FlushMode
        {
            get { return (this._baseStream._flushMode); }
            set 
            {
                if (_disposed) throw new ObjectDisposedException("GZipStream");
                this._baseStream._flushMode = value;
            }
        }

        public int BufferSize
        {
            get
            {
                return this._baseStream._bufferSize;
            }
            set
            {
                if (_disposed) throw new ObjectDisposedException("GZipStream");
                if (this._baseStream._workingBuffer != null) throw new ZlibException("The working buffer is already set.");
                if (value < ZlibConstants.WorkingBufferSizeMin) throw new ZlibException(
                    $"Don't be silly. {value} bytes?? Use a bigger buffer, at least {ZlibConstants.WorkingBufferSizeMin}.");
                this._baseStream._bufferSize = value;
            }
        }

        virtual public long TotalIn
        {
            get
            {
                return this._baseStream._z.TotalBytesIn;
            }
        }

        virtual public long TotalOut
        {
            get
            {
                return this._baseStream._z.TotalBytesOut;
            }
        }
        #endregion

        #region Stream methods
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    if (disposing && (this._baseStream != null))
                    {
                        this._baseStream.Close();
                        this._Crc32 = _baseStream.Crc32;
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

        public override bool CanSeek
        {
            get { return false; }
        }

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

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                if (this._baseStream._streamMode == System.IO.Compression.Zlib.ZlibBaseStream.StreamMode.Writer) return this._baseStream._z.TotalBytesOut + _headerByteCount;
                if (this._baseStream._streamMode == System.IO.Compression.Zlib.ZlibBaseStream.StreamMode.Reader) return this._baseStream._z.TotalBytesIn + this._baseStream._gzipHeaderByteCount;
                return 0;
            }
            set { throw new NotImplementedException(); }
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
            if (_baseStream._streamMode == System.IO.Compression.Zlib.ZlibBaseStream.StreamMode.Undefined)
            {
                if (_baseStream._wantCompress)
                {
                    _headerByteCount = EmitHeader();
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            _baseStream.Write(buffer, offset, count);
        }
        #endregion

        internal static readonly System.DateTime _unixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal static readonly System.Text.Encoding iso8859dash1 = System.Text.Encoding.GetEncoding("iso-8859-1");

        private int EmitHeader()
        {
            var commentBytes = (Comment == null) ? null : iso8859dash1.GetBytes(Comment);
            var filenameBytes = (FileName == null) ? null : iso8859dash1.GetBytes(FileName);

            var cbLength = (Comment == null) ? 0 : commentBytes.Length + 1;
            var fnLength = (FileName == null) ? 0 : filenameBytes.Length + 1;

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
            var timet = (Int32)delta.TotalSeconds;
            Array.Copy(BitConverter.GetBytes(timet), 0, header, i, 4);
            i += 4;

            header[i++] = 0;    // this field is totally useless
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
                i += cbLength - 1; header[i++] = 0; // terminate
            }

            _baseStream._stream.Write(header, 0, header.Length);
            return header.Length; // bytes written
        }

        public static byte[] CompressString(String s)
        {
            using (var ms = new MemoryStream())
            {
                System.IO.Stream compressor = new GZipStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression);
                ZlibBaseStream.CompressString(s, compressor);
                return ms.ToArray();
            }
        }

        public static byte[] CompressBuffer(byte[] b)
        {
            using (var ms = new MemoryStream())
            {
                System.IO.Stream compressor = new GZipStream( ms, CompressionMode.Compress, CompressionLevel.BestCompression );
                ZlibBaseStream.CompressBuffer(b, compressor);
                return ms.ToArray();
            }
        }

        public static String UncompressString(byte[] compressed)
        {
            using (var input = new MemoryStream(compressed))
            {
                System.IO.Stream decompressor = new GZipStream(input, CompressionMode.Decompress);
                return ZlibBaseStream.UncompressString(compressed, decompressor);
            }
        }

        public static byte[] UncompressBuffer(byte[] compressed)
        {
            using (var input = new System.IO.MemoryStream(compressed))
            {
                System.IO.Stream decompressor = new GZipStream( input, CompressionMode.Decompress );
                return ZlibBaseStream.UncompressBuffer(compressed, decompressor);
            }
        }
    }
}
