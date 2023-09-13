using System;
namespace System.IO.Compression
{
    public class CRC32
    {
        public Int64 TotalBytesRead
        {
            get
            {
                return _TotalBytesRead;
            }
        }

        public Int32 Crc32Result
        {
            get
            {
                return unchecked((Int32)(~_register));
            }
        }

        public Int32 GetCrc32(System.IO.Stream input)
        {
            return GetCrc32AndCopy(input, null);
        }

        public Int32 GetCrc32AndCopy(System.IO.Stream input, System.IO.Stream output)
        {
            if (input == null) throw new Exception("The input stream must not be null.");

            unchecked
            {
                var buffer = new byte[BUFFER_SIZE];
                var readSize = BUFFER_SIZE;

                _TotalBytesRead = 0;
                var count = input.Read(buffer, 0, readSize);
                if (output != null) output.Write(buffer, 0, count);
                _TotalBytesRead += count;
                while (count > 0)
                {
                    SlurpBlock(buffer, 0, count);
                    count = input.Read(buffer, 0, readSize);
                    if (output != null) output.Write(buffer, 0, count);
                    _TotalBytesRead += count;
                }

                return (Int32)(~_register);
            }
        }

        public Int32 ComputeCrc32(Int32 W, byte B)
        {
            return _InternalComputeCrc32((UInt32)W, B);
        }

        internal Int32 _InternalComputeCrc32(UInt32 W, byte B)
        {
            return (Int32)(crc32Table[(W ^ B) & 0xFF] ^ (W >> 8));
        }

        public void SlurpBlock(byte[] block, int offset, int count)
        {
            if (block == null) throw new Exception("The data buffer must not be null.");

            for (var i = 0; i < count; i++)
            {
                var x = offset + i;
                var b = block[x];
                if (this.reverseBits)
                {
                    var temp = (_register >> 24) ^ b;
                    _register = (_register << 8) ^ crc32Table[temp];
                }
                else
                {
                    var temp = (_register & 0x000000FF) ^ b;
                    _register = (_register >> 8) ^ crc32Table[temp];
                }
            }
            _TotalBytesRead += count;
        }

        public void UpdateCRC(byte b)
        {
            if (this.reverseBits)
            {
                var temp = (_register >> 24) ^ b;
                _register = (_register << 8) ^ crc32Table[temp];
            }
            else
            {
                var temp = (_register & 0x000000FF) ^ b;
                _register = (_register >> 8) ^ crc32Table[temp];
            }
        }

        public void UpdateCRC(byte b, int n)
        {
            while (n-- > 0)
            {
                if (this.reverseBits)
                {
                    var temp = (_register >> 24) ^ b;
                    _register = (_register << 8) ^ crc32Table[(temp >= 0)
                                                              ? temp
                                                              : (temp + 256)];
                }
                else
                {
                    var temp = (_register & 0x000000FF) ^ b;
                    _register = (_register >> 8) ^ crc32Table[(temp >= 0)
                                                              ? temp
                                                              : (temp + 256)];

                }
            }
        }

        private static uint ReverseBits(uint data)
        {
            unchecked
            {
                var ret = data;
                ret = (ret & 0x55555555) << 1 | (ret >> 1) & 0x55555555;
                ret = (ret & 0x33333333) << 2 | (ret >> 2) & 0x33333333;
                ret = (ret & 0x0F0F0F0F) << 4 | (ret >> 4) & 0x0F0F0F0F;
                ret = (ret << 24) | ((ret & 0xFF00) << 8) | ((ret >> 8) & 0xFF00) | (ret >> 24);
                return ret;
            }
        }

        private static byte ReverseBits(byte data)
        {
            unchecked
            {
                var u = (uint)data * 0x00020202;
                uint m = 0x01044010;
                var s = u & m;
                var t = (u << 2) & (m << 1);
                return (byte)((0x01001001 * (s + t)) >> 24);
            }
        }

        private void GenerateLookupTable()
        {
            crc32Table = new UInt32[256];
            unchecked
            {
                UInt32 dwCrc;
                byte i = 0;
                do
                {
                    dwCrc = i;
                    for (byte j = 8; j > 0; j--)
                    {
                        if ((dwCrc & 1) == 1)
                        {
                            dwCrc = (dwCrc >> 1) ^ dwPolynomial;
                        }
                        else
                        {
                            dwCrc >>= 1;
                        }
                    }
                    if (reverseBits)
                    {
                        crc32Table[ReverseBits(i)] = ReverseBits(dwCrc);
                    }
                    else
                    {
                        crc32Table[i] = dwCrc;
                    }
                    i++;
                } while (i != 0);
            }
        }

        private uint gf2_matrix_times(uint[] matrix, uint vec)
        {
            uint sum = 0;
            var i = 0;
            while (vec != 0)
            {
                if ((vec & 0x01) == 0x01)
                    sum ^= matrix[i];
                vec >>= 1;
                i++;
            }
            return sum;
        }

        private void gf2_matrix_square(uint[] square, uint[] mat)
        {
            for (var i = 0; i < 32; i++)
                square[i] = gf2_matrix_times(mat, mat[i]);
        }

        public void Combine(int crc, int length)
        {
            var even = new uint[32];
            var odd = new uint[32];
            if (length == 0) return;

            var crc1 = ~_register;
            var crc2 = (uint)crc;

            odd[0] = this.dwPolynomial;
            uint row = 1;
            for (var i = 1; i < 32; i++)
            {
                odd[i] = row;
                row <<= 1;
            }

            gf2_matrix_square(even, odd);
            gf2_matrix_square(odd, even);
            var len2 = (uint)length;

            do
            {
                gf2_matrix_square(even, odd);
                if ((len2 & 1) == 1) crc1 = gf2_matrix_times(even, crc1);
                len2 >>= 1; if (len2 == 0) break;
                gf2_matrix_square(odd, even);
                if ((len2 & 1) == 1) crc1 = gf2_matrix_times(odd, crc1);
                len2 >>= 1;
            } while (len2 != 0);

            crc1 ^= crc2;
            _register = ~crc1;
            return;
        }

        public CRC32() : this(false)
        {
        }

        public CRC32(bool reverseBits) : this(unchecked((int)0xEDB88320), reverseBits)
        {
        }

        public CRC32(int polynomial, bool reverseBits)
        {
            this.reverseBits = reverseBits;
            this.dwPolynomial = (uint)polynomial;
            this.GenerateLookupTable();
        }

        public void Reset()
        {
            _register = 0xFFFFFFFFU;
        }

        private UInt32 dwPolynomial;
        private Int64 _TotalBytesRead;
        private bool reverseBits;
        private UInt32[] crc32Table;
        private const int BUFFER_SIZE = 8192;
        private UInt32 _register = 0xFFFFFFFFU;
    }

    public class CrcCalculatorStream : System.IO.Stream, System.IDisposable
    {
        private static readonly Int64 UnsetLengthLimit = -99;
        
        internal System.IO.Stream _innerStream;
        private CRC32 _Crc32;
        private Int64 _lengthLimit = -99;
        private bool _leaveOpen;

        public CrcCalculatorStream(System.IO.Stream stream) : this(true, CrcCalculatorStream.UnsetLengthLimit, stream, null)
        {
        }

        public CrcCalculatorStream(System.IO.Stream stream, bool leaveOpen) : this(leaveOpen, CrcCalculatorStream.UnsetLengthLimit, stream, null)
        {
        }

        public CrcCalculatorStream(System.IO.Stream stream, Int64 length) : this(true, length, stream, null)
        {
            if (length < 0) throw new ArgumentException("length");
        }

        public CrcCalculatorStream(System.IO.Stream stream, Int64 length, bool leaveOpen) : this(leaveOpen, length, stream, null)
        {
            if (length < 0) throw new ArgumentException("length");
        }

        public CrcCalculatorStream(System.IO.Stream stream, Int64 length, bool leaveOpen, CRC32 crc32) : this(leaveOpen, length, stream, crc32)
        {
            if (length < 0) throw new ArgumentException("length");
        }

        private CrcCalculatorStream(bool leaveOpen, Int64 length, System.IO.Stream stream, CRC32 crc32) : base()
        {
            _innerStream = stream;
            _Crc32 = crc32 ?? new CRC32();
            _lengthLimit = length;
            _leaveOpen = leaveOpen;
        }

        public Int64 TotalBytesSlurped
        {
            get { return _Crc32.TotalBytesRead; }
        }

        public Int32 Crc
        {
            get { return _Crc32.Crc32Result; }
        }

        public bool LeaveOpen
        {
            get { return _leaveOpen; }
            set { _leaveOpen = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesToRead = count;

            if (_lengthLimit != CrcCalculatorStream.UnsetLengthLimit)
            {
                if (_Crc32.TotalBytesRead >= _lengthLimit) return 0; // EOF
                var bytesRemaining = _lengthLimit - _Crc32.TotalBytesRead;
                if (bytesRemaining < count) bytesToRead = (int)bytesRemaining;
            }
            var n = _innerStream.Read(buffer, offset, bytesToRead);
            if (n > 0) _Crc32.SlurpBlock(buffer, offset, n);
            return n;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > 0) _Crc32.SlurpBlock(buffer, offset, count);
            _innerStream.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return _innerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return _innerStream.CanWrite; }
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override long Length
        {
            get
            {
                if (_lengthLimit == CrcCalculatorStream.UnsetLengthLimit)
                    return _innerStream.Length;
                else return _lengthLimit;
            }
        }

        public override long Position
        {
            get { return _Crc32.TotalBytesRead; }
            set { throw new NotSupportedException(); }
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        public override void Close()
        {
            base.Close();
            if (!_leaveOpen)
                _innerStream.Close();
        }
    }
}