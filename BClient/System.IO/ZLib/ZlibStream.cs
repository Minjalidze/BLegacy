using System;
using System.IO;

namespace System.IO.Compression.Zlib
{
    public class ZlibStream : System.IO.Stream
    {
        internal ZlibBaseStream _baseStream;
        bool _disposed;

        public ZlibStream(System.IO.Stream stream, CompressionMode mode) : this(stream, mode, CompressionLevel.Default, false)
        {
        }

        public ZlibStream(System.IO.Stream stream, CompressionMode mode, CompressionLevel level) : this(stream, mode, level, false)
        {
        }

        public ZlibStream(System.IO.Stream stream, CompressionMode mode, bool leaveOpen) : this(stream, mode, CompressionLevel.Default, leaveOpen)
        {
        }

        public ZlibStream(System.IO.Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
        {
            _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.ZLIB, leaveOpen);
        }

        #region Zlib properties
        virtual public FlushType FlushMode
        {
            get 
            { 
                return (this._baseStream._flushMode); 
            }
            set
            {
                if (_disposed) throw new ObjectDisposedException("ZlibStream");
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
                if (_disposed) throw new ObjectDisposedException("ZlibStream");
                if (this._baseStream._workingBuffer != null) throw new ZlibException("The working buffer is already set.");
                if (value < ZlibConstants.WorkingBufferSizeMin) throw new ZlibException(
                    $"Don't be silly. {value} bytes?? Use a bigger buffer, at least {ZlibConstants.WorkingBufferSizeMin}.");
                this._baseStream._bufferSize = value;
            }
        }

        virtual public long TotalIn
        {
            get { return this._baseStream._z.TotalBytesIn; }
        }

        virtual public long TotalOut
        {
            get { return this._baseStream._z.TotalBytesOut; }
        }
        #endregion

        #region System.IO.Stream methods
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    if (disposing && (this._baseStream != null)) this._baseStream.Close();
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
                if (_disposed) throw new ObjectDisposedException("ZlibStream");
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
                if (_disposed) throw new ObjectDisposedException("ZlibStream");
                return _baseStream._stream.CanWrite;
            }
        }

        public override void Flush()
        {
            if (_disposed) throw new ObjectDisposedException("ZlibStream");
            _baseStream.Flush();
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                if (this._baseStream._streamMode == System.IO.Compression.Zlib.ZlibBaseStream.StreamMode.Writer) return this._baseStream._z.TotalBytesOut;
                if (this._baseStream._streamMode == System.IO.Compression.Zlib.ZlibBaseStream.StreamMode.Reader) return this._baseStream._z.TotalBytesIn;
                return 0;
            }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed) throw new ObjectDisposedException("ZlibStream");
            return _baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed) throw new ObjectDisposedException("ZlibStream");
            _baseStream.Write(buffer, offset, count);
        }
        #endregion

        public static byte[] CompressString(String s)
        {
            using (var ms = new MemoryStream())
            {
                Stream compressor = new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression);
                ZlibBaseStream.CompressString(s, compressor);
                return ms.ToArray();
            }
        }

        public static byte[] CompressBuffer(byte[] b)
        {
            using (var ms = new MemoryStream())
            {
                Stream compressor = new ZlibStream( ms, CompressionMode.Compress, CompressionLevel.BestCompression );
                ZlibBaseStream.CompressBuffer(b, compressor);
                return ms.ToArray();
            }
        }

        public static String UncompressString(byte[] compressed)
        {
            using (var input = new MemoryStream(compressed))
            {
                Stream decompressor = new ZlibStream(input, CompressionMode.Decompress);
                return ZlibBaseStream.UncompressString(compressed, decompressor);
            }
        }

        public static byte[] UncompressBuffer(byte[] compressed)
        {
            using (var input = new MemoryStream(compressed))
            {
                Stream decompressor = new ZlibStream( input, CompressionMode.Decompress );
                return ZlibBaseStream.UncompressBuffer(compressed, decompressor);
            }
        }
    }
}