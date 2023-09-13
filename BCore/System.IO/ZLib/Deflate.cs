namespace System.IO.Compression.Zlib;

internal enum BlockState
{
    NeedMore = 0, // block not completed, need more input or more output
    BlockDone, // block flush performed
    FinishStarted, // finish started, need only more output at next deflate
    FinishDone // finish done, accept no more input or output
}

internal enum DeflateFlavor
{
    Store,
    Fast,
    Slow
}

internal sealed class DeflateManager
{
    private static readonly int MEM_LEVEL_MAX = 9;
    private static readonly int MEM_LEVEL_DEFAULT = 8;

    private static readonly string[] _ErrorMessage =
    {
        "need dictionary",
        "stream end",
        "",
        "file error",
        "stream error",
        "data error",
        "insufficient memory",
        "buffer error",
        "incompatible version",
        ""
    };

    // preset dictionary flag in zlib header
    private static readonly int PRESET_DICT = 0x20;

    private static readonly int INIT_STATE = 42;
    private static readonly int BUSY_STATE = 113;
    private static readonly int FINISH_STATE = 666;

    // The deflate compression method
    private static readonly int Z_DEFLATED = 8;

    private static readonly int STORED_BLOCK = 0;
    private static readonly int STATIC_TREES = 1;
    private static readonly int DYN_TREES = 2;

    // The three kinds of block type
    private static readonly int Z_BINARY = 0;
    private static readonly int Z_ASCII = 1;
    private static readonly int Z_UNKNOWN = 2;

    private static readonly int Buf_size = 8 * 2;

    private static readonly int MIN_MATCH = 3;
    private static readonly int MAX_MATCH = 258;

    private static readonly int MIN_LOOKAHEAD = MAX_MATCH + MIN_MATCH + 1;

    private static readonly int HEAP_SIZE = 2 * InternalConstants.L_CODES + 1;

    private static readonly int END_BLOCK = 256;

    internal ZlibCodec _codec; // the zlib encoder/decoder

    internal int _distanceOffset; // index into pending; points to distance data??

    internal int _lengthOffset; // index for literals or lengths

    internal short bi_buf;
    internal int bi_valid;

    // number of codes at each bit length for an optimal tree
    internal short[] bl_count = new short[InternalConstants.MAX_BITS + 1];
    internal short[] bl_tree; // Huffman tree for bit lengths

    internal int block_start;

    internal CompressionLevel compressionLevel; // compression level (1..9)
    internal CompressionStrategy compressionStrategy; // favor or force Huffman coding

    private Config config;

    internal sbyte data_type; // UNKNOWN, BINARY or ASCII


    private CompressFunc DeflateFunction;

    // Depth of each subtree used as tie breaker for trees of equal frequency
    internal sbyte[] depth = new sbyte[2 * InternalConstants.L_CODES + 1];
    internal short[] dyn_dtree; // distance tree

    internal short[] dyn_ltree; // literal and length tree
    internal int hash_bits; // log2(hash_size)
    internal int hash_mask; // hash_size-1

    internal int hash_shift;
    internal int hash_size; // number of elements in hash table
    internal short[] head; // Heads of the hash chains or NIL.

    // heap used to build the Huffman trees
    internal int[] heap = new int[2 * InternalConstants.L_CODES + 1];

    internal int heap_len; // number of elements in the heap
    internal int heap_max; // element of largest frequency

    internal int ins_h; // hash index of string to be inserted
    internal int last_eob_len; // bit length of EOB code for last block
    internal int last_flush; // value of flush param for previous deflate call

    internal int last_lit; // running index in l_buf

    internal int lit_bufsize;
    internal int lookahead; // number of valid bytes ahead in window
    internal int match_available; // set if previous match exists
    internal int match_length; // length of best match
    internal int match_start; // start of matching string
    internal int matches; // number of string matches in current block
    internal int nextPending; // index of next pending byte to output to the stream

    internal int opt_len; // bit length of current block with optimal trees
    internal byte[] pending; // output still pending - waiting to be compressed
    internal int pendingCount; // number of bytes in the pending buffer
    internal short[] prev;

    internal int prev_length;
    internal int prev_match; // previous match

    private bool Rfc1950BytesEmitted;
    internal int static_len; // bit length of current block with static trees
    internal int status; // as the name implies
    internal int strstart; // start of string to insert into.....????
    internal Tree treeBitLengths = new(); // desc for bit length tree
    internal Tree treeDistances = new(); // desc for distance tree

    internal Tree treeLiterals = new(); // desc for literal tree
    internal int w_bits; // log2(w_size)  (8..16)
    internal int w_mask; // w_size - 1

    internal int w_size; // LZ77 window size (32K by default)

    //internal byte[] dictionary;
    internal byte[] window;
    internal int window_size;

    internal DeflateManager()
    {
        dyn_ltree = new short[HEAP_SIZE * 2];
        dyn_dtree = new short[(2 * InternalConstants.D_CODES + 1) * 2]; // distance tree
        bl_tree = new short[(2 * InternalConstants.BL_CODES + 1) * 2]; // Huffman tree for bit lengths
    }

    internal bool WantRfc1950HeaderBytes { get; set; } = true;

    private void _InitializeLazyMatch()
    {
        window_size = 2 * w_size;

        // clear the hash - workitem 9063
        Array.Clear(head, 0, hash_size);
        //for (int i = 0; i < hash_size; i++) head[i] = 0;

        config = Config.Lookup(compressionLevel);
        SetDeflater();

        strstart = 0;
        block_start = 0;
        lookahead = 0;
        match_length = prev_length = MIN_MATCH - 1;
        match_available = 0;
        ins_h = 0;
    }

    // Initialize the tree data structures for a new zlib stream.
    private void _InitializeTreeData()
    {
        treeLiterals.dyn_tree = dyn_ltree;
        treeLiterals.staticTree = StaticTree.Literals;

        treeDistances.dyn_tree = dyn_dtree;
        treeDistances.staticTree = StaticTree.Distances;

        treeBitLengths.dyn_tree = bl_tree;
        treeBitLengths.staticTree = StaticTree.BitLengths;

        bi_buf = 0;
        bi_valid = 0;
        last_eob_len = 8; // enough lookahead for inflate

        // Initialize the first block of the first file:
        _InitializeBlocks();
    }

    internal void _InitializeBlocks()
    {
        // Initialize the trees.
        for (var i = 0; i < InternalConstants.L_CODES; i++)
            dyn_ltree[i * 2] = 0;
        for (var i = 0; i < InternalConstants.D_CODES; i++)
            dyn_dtree[i * 2] = 0;
        for (var i = 0; i < InternalConstants.BL_CODES; i++)
            bl_tree[i * 2] = 0;

        dyn_ltree[END_BLOCK * 2] = 1;
        opt_len = static_len = 0;
        last_lit = matches = 0;
    }

    internal void pqdownheap(short[] tree, int k)
    {
        var v = heap[k];
        var j = k << 1; // left son of k
        while (j <= heap_len)
        {
            // Set j to the smallest of the two sons:
            if (j < heap_len && _IsSmaller(tree, heap[j + 1], heap[j], depth)) j++;
            // Exit if v is smaller than both sons
            if (_IsSmaller(tree, v, heap[j], depth))
                break;

            // Exchange v with the smallest son
            heap[k] = heap[j];
            k = j;
            // And continue down the tree, setting j to the left son of k
            j <<= 1;
        }

        heap[k] = v;
    }

    internal static bool _IsSmaller(short[] tree, int n, int m, sbyte[] depth)
    {
        var tn2 = tree[n * 2];
        var tm2 = tree[m * 2];
        return tn2 < tm2 || (tn2 == tm2 && depth[n] <= depth[m]);
    }

    internal void scan_tree(short[] tree, int max_code)
    {
        int n; // iterates over all tree elements
        var prevlen = -1; // last emitted length
        int curlen; // length of current code
        var nextlen = (int)tree[0 * 2 + 1]; // length of next code
        var count = 0; // repeat count of the current code
        var max_count = 7; // max repeat count
        var min_count = 4; // min repeat count

        if (nextlen == 0)
        {
            max_count = 138;
            min_count = 3;
        }

        tree[(max_code + 1) * 2 + 1] = 0x7fff; // guard //??

        for (n = 0; n <= max_code; n++)
        {
            curlen = nextlen;
            nextlen = tree[(n + 1) * 2 + 1];
            if (++count < max_count && curlen == nextlen) continue;

            if (count < min_count)
            {
                bl_tree[curlen * 2] = (short)(bl_tree[curlen * 2] + count);
            }
            else if (curlen != 0)
            {
                if (curlen != prevlen)
                    bl_tree[curlen * 2]++;
                bl_tree[InternalConstants.REP_3_6 * 2]++;
            }
            else if (count <= 10)
            {
                bl_tree[InternalConstants.REPZ_3_10 * 2]++;
            }
            else
            {
                bl_tree[InternalConstants.REPZ_11_138 * 2]++;
            }

            count = 0;
            prevlen = curlen;
            if (nextlen == 0)
            {
                max_count = 138;
                min_count = 3;
            }
            else if (curlen == nextlen)
            {
                max_count = 6;
                min_count = 3;
            }
            else
            {
                max_count = 7;
                min_count = 4;
            }
        }
    }

    internal int build_bl_tree()
    {
        int max_blindex; // index of last bit length code of non zero freq
        scan_tree(dyn_ltree, treeLiterals.max_code);
        scan_tree(dyn_dtree, treeDistances.max_code);
        treeBitLengths.build_tree(this);
        for (max_blindex = InternalConstants.BL_CODES - 1; max_blindex >= 3; max_blindex--)
            if (bl_tree[Tree.bl_order[max_blindex] * 2 + 1] != 0)
                break;
        opt_len += 3 * (max_blindex + 1) + 5 + 5 + 4;
        return max_blindex;
    }

    internal void send_all_trees(int lcodes, int dcodes, int blcodes)
    {
        int rank; // index in bl_order
        send_bits(lcodes - 257, 5); // not +255 as stated in appnote.txt
        send_bits(dcodes - 1, 5);
        send_bits(blcodes - 4, 4); // not -3 as stated in appnote.txt
        for (rank = 0; rank < blcodes; rank++) send_bits(bl_tree[Tree.bl_order[rank] * 2 + 1], 3);
        send_tree(dyn_ltree, lcodes - 1); // literal tree
        send_tree(dyn_dtree, dcodes - 1); // distance tree
    }

    internal void send_tree(short[] tree, int max_code)
    {
        int n; // iterates over all tree elements
        var prevlen = -1; // last emitted length
        int curlen; // length of current code
        int nextlen = tree[0 * 2 + 1]; // length of next code
        var count = 0; // repeat count of the current code
        var max_count = 7; // max repeat count
        var min_count = 4; // min repeat count

        if (nextlen == 0)
        {
            max_count = 138;
            min_count = 3;
        }

        for (n = 0; n <= max_code; n++)
        {
            curlen = nextlen;
            nextlen = tree[(n + 1) * 2 + 1];
            if (++count < max_count && curlen == nextlen) continue;

            if (count < min_count)
            {
                do
                {
                    send_code(curlen, bl_tree);
                } while (--count != 0);
            }
            else if (curlen != 0)
            {
                if (curlen != prevlen)
                {
                    send_code(curlen, bl_tree);
                    count--;
                }

                send_code(InternalConstants.REP_3_6, bl_tree);
                send_bits(count - 3, 2);
            }
            else if (count <= 10)
            {
                send_code(InternalConstants.REPZ_3_10, bl_tree);
                send_bits(count - 3, 3);
            }
            else
            {
                send_code(InternalConstants.REPZ_11_138, bl_tree);
                send_bits(count - 11, 7);
            }

            count = 0;
            prevlen = curlen;
            if (nextlen == 0)
            {
                max_count = 138;
                min_count = 3;
            }
            else if (curlen == nextlen)
            {
                max_count = 6;
                min_count = 3;
            }
            else
            {
                max_count = 7;
                min_count = 4;
            }
        }
    }

    private void put_bytes(byte[] p, int start, int len)
    {
        Array.Copy(p, start, pending, pendingCount, len);
        pendingCount += len;
    }

    internal void send_code(int c, short[] tree)
    {
        var c2 = c * 2;
        send_bits(tree[c2] & 0xffff, tree[c2 + 1] & 0xffff);
    }

    internal void send_bits(int value, int length)
    {
        var len = length;
        unchecked
        {
            if (bi_valid > Buf_size - len)
            {
                //int val = value; bi_buf |= (val << bi_valid);
                bi_buf |= (short)((value << bi_valid) & 0xffff);
                //put_short(bi_buf);
                pending[pendingCount++] = (byte)bi_buf;
                pending[pendingCount++] = (byte)(bi_buf >> 8);
                bi_buf = (short)((uint)value >> (Buf_size - bi_valid));
                bi_valid += len - Buf_size;
            }
            else
            {
                //bi_buf |= (value) << bi_valid;
                bi_buf |= (short)((value << bi_valid) & 0xffff);
                bi_valid += len;
            }
        }
    }

    internal void _tr_align()
    {
        send_bits(STATIC_TREES << 1, 3);
        send_code(END_BLOCK, StaticTree.lengthAndLiteralsTreeCodes);
        bi_flush();

        if (1 + last_eob_len + 10 - bi_valid < 9)
        {
            send_bits(STATIC_TREES << 1, 3);
            send_code(END_BLOCK, StaticTree.lengthAndLiteralsTreeCodes);
            bi_flush();
        }

        last_eob_len = 7;
    }

    internal bool _tr_tally(int dist, int lc)
    {
        pending[_distanceOffset + last_lit * 2] = unchecked((byte)((uint)dist >> 8));
        pending[_distanceOffset + last_lit * 2 + 1] = unchecked((byte)dist);
        pending[_lengthOffset + last_lit] = unchecked((byte)lc);
        last_lit++;

        if (dist == 0)
        {
            dyn_ltree[lc * 2]++;
        }
        else
        {
            matches++;
            dist--; // dist = match distance - 1
            dyn_ltree[(Tree.LengthCode[lc] + InternalConstants.LITERALS + 1) * 2]++;
            dyn_dtree[Tree.DistanceCode(dist) * 2]++;
        }

        if ((last_lit & 0x1fff) == 0 && (int)compressionLevel > 2)
        {
            var out_length = last_lit << 3;
            var in_length = strstart - block_start;
            int dcode;
            for (dcode = 0; dcode < InternalConstants.D_CODES; dcode++)
                out_length = (int)(out_length + dyn_dtree[dcode * 2] * (5L + Tree.ExtraDistanceBits[dcode]));
            out_length >>= 3;
            if (matches < last_lit / 2 && out_length < in_length / 2) return true;
        }

        return last_lit == lit_bufsize - 1 || last_lit == lit_bufsize;
    }

    internal void send_compressed_block(short[] ltree, short[] dtree)
    {
        int distance; // distance of matched string
        int lc; // match length or unmatched char (if dist == 0)
        var lx = 0; // running index in l_buf
        int code; // the code to send
        int extra; // number of extra bits to send

        if (last_lit != 0)
            do
            {
                var ix = _distanceOffset + lx * 2;
                distance = ((pending[ix] << 8) & 0xff00) |
                           (pending[ix + 1] & 0xff);
                lc = pending[_lengthOffset + lx] & 0xff;
                lx++;

                if (distance == 0)
                {
                    send_code(lc, ltree); // send a literal byte
                }
                else
                {
                    code = Tree.LengthCode[lc];
                    send_code(code + InternalConstants.LITERALS + 1, ltree);
                    extra = Tree.ExtraLengthBits[code];
                    if (extra != 0)
                    {
                        lc -= Tree.LengthBase[code];
                        send_bits(lc, extra);
                    }

                    distance--; // dist is now the match distance - 1
                    code = Tree.DistanceCode(distance);
                    send_code(code, dtree);
                    extra = Tree.ExtraDistanceBits[code];
                    if (extra != 0)
                    {
                        distance -= Tree.DistanceBase[code];
                        send_bits(distance, extra);
                    }
                }
            } while (lx < last_lit);

        send_code(END_BLOCK, ltree);
        last_eob_len = ltree[END_BLOCK * 2 + 1];
    }

    internal void set_data_type()
    {
        var n = 0;
        var ascii_freq = 0;
        var bin_freq = 0;
        while (n < 7)
        {
            bin_freq += dyn_ltree[n * 2];
            n++;
        }

        while (n < 128)
        {
            ascii_freq += dyn_ltree[n * 2];
            n++;
        }

        while (n < InternalConstants.LITERALS)
        {
            bin_freq += dyn_ltree[n * 2];
            n++;
        }

        data_type = (sbyte)(bin_freq > ascii_freq >> 2 ? Z_BINARY : Z_ASCII);
    }

    internal void bi_flush()
    {
        if (bi_valid == 16)
        {
            pending[pendingCount++] = (byte)bi_buf;
            pending[pendingCount++] = (byte)(bi_buf >> 8);
            bi_buf = 0;
            bi_valid = 0;
        }
        else if (bi_valid >= 8)
        {
            //put_byte((byte)bi_buf);
            pending[pendingCount++] = (byte)bi_buf;
            bi_buf >>= 8;
            bi_valid -= 8;
        }
    }

    internal void bi_windup()
    {
        if (bi_valid > 8)
        {
            pending[pendingCount++] = (byte)bi_buf;
            pending[pendingCount++] = (byte)(bi_buf >> 8);
        }
        else if (bi_valid > 0)
        {
            //put_byte((byte)bi_buf);
            pending[pendingCount++] = (byte)bi_buf;
        }

        bi_buf = 0;
        bi_valid = 0;
    }

    internal void copy_block(int buf, int len, bool header)
    {
        bi_windup(); // align on byte boundary
        last_eob_len = 8; // enough lookahead for inflate

        if (header)
            unchecked
            {
                //put_short((short)len);
                pending[pendingCount++] = (byte)len;
                pending[pendingCount++] = (byte)(len >> 8);
                //put_short((short)~len);
                pending[pendingCount++] = (byte)~len;
                pending[pendingCount++] = (byte)(~len >> 8);
            }

        put_bytes(window, buf, len);
    }

    internal void flush_block_only(bool eof)
    {
        _tr_flush_block(block_start >= 0 ? block_start : -1, strstart - block_start, eof);
        block_start = strstart;
        _codec.flush_pending();
    }

    internal BlockState DeflateNone(FlushType flush)
    {
        var max_block_size = 0xffff;
        int max_start;

        if (max_block_size > pending.Length - 5) max_block_size = pending.Length - 5;

        while (true)
        {
            if (lookahead <= 1)
            {
                _fillWindow();
                if (lookahead == 0 && flush == FlushType.None) return BlockState.NeedMore;
                if (lookahead == 0) break; // flush the current block
            }

            strstart += lookahead;
            lookahead = 0;

            // Emit a stored block if pending will be full:
            max_start = block_start + max_block_size;
            if (strstart == 0 || strstart >= max_start)
            {
                // strstart == 0 is possible when wraparound on 16-bit machine
                lookahead = strstart - max_start;
                strstart = max_start;

                flush_block_only(false);
                if (_codec.AvailableBytesOut == 0)
                    return BlockState.NeedMore;
            }

            if (strstart - block_start >= w_size - MIN_LOOKAHEAD)
            {
                flush_block_only(false);
                if (_codec.AvailableBytesOut == 0) return BlockState.NeedMore;
            }
        }

        flush_block_only(flush == FlushType.Finish);
        if (_codec.AvailableBytesOut == 0)
            return flush == FlushType.Finish ? BlockState.FinishStarted : BlockState.NeedMore;
        return flush == FlushType.Finish ? BlockState.FinishDone : BlockState.BlockDone;
    }

    internal void _tr_stored_block(int buf, int stored_len, bool eof)
    {
        send_bits((STORED_BLOCK << 1) + (eof ? 1 : 0), 3); // send block type
        copy_block(buf, stored_len, true); // with header
    }

    internal void _tr_flush_block(int buf, int stored_len, bool eof)
    {
        int opt_lenb, static_lenb; // opt_len and static_len in bytes
        var max_blindex = 0; // index of last bit length code of non zero freq
        // Build the Huffman trees unless a stored block is forced
        if (compressionLevel > 0)
        {
            // Check if the file is ascii or binary
            if (data_type == Z_UNKNOWN) set_data_type();
            // Construct the literal and distance trees
            treeLiterals.build_tree(this);
            treeDistances.build_tree(this);
            max_blindex = build_bl_tree();
            opt_lenb = (opt_len + 3 + 7) >> 3;
            static_lenb = (static_len + 3 + 7) >> 3;
            if (static_lenb <= opt_lenb) opt_lenb = static_lenb;
        }
        else
        {
            opt_lenb = static_lenb = stored_len + 5; // force a stored block
        }

        if (stored_len + 4 <= opt_lenb && buf != -1)
        {
            _tr_stored_block(buf, stored_len, eof);
        }
        else if (static_lenb == opt_lenb)
        {
            send_bits((STATIC_TREES << 1) + (eof ? 1 : 0), 3);
            send_compressed_block(StaticTree.lengthAndLiteralsTreeCodes, StaticTree.distTreeCodes);
        }
        else
        {
            send_bits((DYN_TREES << 1) + (eof ? 1 : 0), 3);
            send_all_trees(treeLiterals.max_code + 1, treeDistances.max_code + 1, max_blindex + 1);
            send_compressed_block(dyn_ltree, dyn_dtree);
        }

        _InitializeBlocks();
        if (eof) bi_windup();
    }

    private void _fillWindow()
    {
        int n, m;
        int p;
        int more; // Amount of free space at the end of the window.

        do
        {
            more = window_size - lookahead - strstart;
            if (more == 0 && strstart == 0 && lookahead == 0)
            {
                more = w_size;
            }
            else if (more == -1)
            {
                more--;
            }
            else if (strstart >= w_size + w_size - MIN_LOOKAHEAD)
            {
                Array.Copy(window, w_size, window, 0, w_size);
                match_start -= w_size;
                strstart -= w_size; // we now have strstart >= MAX_DIST
                block_start -= w_size;

                n = hash_size;
                p = n;
                do
                {
                    m = head[--p] & 0xffff;
                    head[p] = (short)(m >= w_size ? m - w_size : 0);
                } while (--n != 0);

                n = w_size;
                p = n;
                do
                {
                    m = prev[--p] & 0xffff;
                    prev[p] = (short)(m >= w_size ? m - w_size : 0);
                } while (--n != 0);

                more += w_size;
            }

            if (_codec.AvailableBytesIn == 0) return;
            n = _codec.read_buf(window, strstart + lookahead, more);
            lookahead += n;

            if (lookahead >= MIN_MATCH)
            {
                ins_h = window[strstart] & 0xff;
                ins_h = ((ins_h << hash_shift) ^ (window[strstart + 1] & 0xff)) & hash_mask;
            }
        } while (lookahead < MIN_LOOKAHEAD && _codec.AvailableBytesIn != 0);
    }

    internal BlockState DeflateFast(FlushType flush)
    {
        var hash_head = 0; // head of the hash chain
        bool bflush; // set if current block must be flushed

        while (true)
        {
            if (lookahead < MIN_LOOKAHEAD)
            {
                _fillWindow();
                if (lookahead < MIN_LOOKAHEAD && flush == FlushType.None) return BlockState.NeedMore;
                if (lookahead == 0)
                    break; // flush the current block
            }

            if (lookahead >= MIN_MATCH)
            {
                ins_h = ((ins_h << hash_shift) ^ (window[strstart + (MIN_MATCH - 1)] & 0xff)) & hash_mask;
                hash_head = head[ins_h] & 0xffff;
                prev[strstart & w_mask] = head[ins_h];
                head[ins_h] = unchecked((short)strstart);
            }

            if (hash_head != 0L && ((strstart - hash_head) & 0xffff) <= w_size - MIN_LOOKAHEAD)
                if (compressionStrategy != CompressionStrategy.HuffmanOnly)
                    match_length = longest_match(hash_head);
            if (match_length >= MIN_MATCH)
            {
                bflush = _tr_tally(strstart - match_start, match_length - MIN_MATCH);
                lookahead -= match_length;
                if (match_length <= config.MaxLazy && lookahead >= MIN_MATCH)
                {
                    match_length--; // string at strstart already in hash table
                    do
                    {
                        strstart++;
                        ins_h = ((ins_h << hash_shift) ^ (window[strstart + (MIN_MATCH - 1)] & 0xff)) & hash_mask;
                        //prev[strstart&w_mask]=hash_head=head[ins_h];
                        hash_head = head[ins_h] & 0xffff;
                        prev[strstart & w_mask] = head[ins_h];
                        head[ins_h] = unchecked((short)strstart);
                    } while (--match_length != 0);

                    strstart++;
                }
                else
                {
                    strstart += match_length;
                    match_length = 0;
                    ins_h = window[strstart] & 0xff;
                    ins_h = ((ins_h << hash_shift) ^ (window[strstart + 1] & 0xff)) & hash_mask;
                }
            }
            else
            {
                bflush = _tr_tally(0, window[strstart] & 0xff);
                lookahead--;
                strstart++;
            }

            if (bflush)
            {
                flush_block_only(false);
                if (_codec.AvailableBytesOut == 0)
                    return BlockState.NeedMore;
            }
        }

        flush_block_only(flush == FlushType.Finish);
        if (_codec.AvailableBytesOut == 0)
        {
            if (flush == FlushType.Finish)
                return BlockState.FinishStarted;
            return BlockState.NeedMore;
        }

        return flush == FlushType.Finish ? BlockState.FinishDone : BlockState.BlockDone;
    }

    internal BlockState DeflateSlow(FlushType flush)
    {
        //short hash_head = 0;    // head of hash chain
        var hash_head = 0; // head of hash chain
        bool bflush; // set if current block must be flushed

        while (true)
        {
            if (lookahead < MIN_LOOKAHEAD)
            {
                _fillWindow();
                if (lookahead < MIN_LOOKAHEAD && flush == FlushType.None) return BlockState.NeedMore;
                if (lookahead == 0) break; // flush the current block
            }

            if (lookahead >= MIN_MATCH)
            {
                ins_h = ((ins_h << hash_shift) ^ (window[strstart + (MIN_MATCH - 1)] & 0xff)) & hash_mask;
                //prev[strstart&w_mask]=hash_head=head[ins_h];
                hash_head = head[ins_h] & 0xffff;
                prev[strstart & w_mask] = head[ins_h];
                head[ins_h] = unchecked((short)strstart);
            }

            prev_length = match_length;
            prev_match = match_start;
            match_length = MIN_MATCH - 1;

            if (hash_head != 0 && prev_length < config.MaxLazy &&
                ((strstart - hash_head) & 0xffff) <= w_size - MIN_LOOKAHEAD)
            {
                if (compressionStrategy != CompressionStrategy.HuffmanOnly) match_length = longest_match(hash_head);
                // longest_match() sets match_start
                if (match_length <= 5 && (compressionStrategy == CompressionStrategy.Filtered ||
                                          (match_length == MIN_MATCH && strstart - match_start > 4096)))
                    match_length = MIN_MATCH - 1;
            }

            if (prev_length >= MIN_MATCH && match_length <= prev_length)
            {
                var max_insert = strstart + lookahead - MIN_MATCH;
                // Do not insert strings in hash table beyond this.
                // check_match(strstart-1, prev_match, prev_length);
                bflush = _tr_tally(strstart - 1 - prev_match, prev_length - MIN_MATCH);
                lookahead -= prev_length - 1;
                prev_length -= 2;
                do
                {
                    if (++strstart <= max_insert)
                    {
                        ins_h = ((ins_h << hash_shift) ^ (window[strstart + (MIN_MATCH - 1)] & 0xff)) & hash_mask;
                        //prev[strstart&w_mask]=hash_head=head[ins_h];
                        hash_head = head[ins_h] & 0xffff;
                        prev[strstart & w_mask] = head[ins_h];
                        head[ins_h] = unchecked((short)strstart);
                    }
                } while (--prev_length != 0);

                match_available = 0;
                match_length = MIN_MATCH - 1;
                strstart++;

                if (bflush)
                {
                    flush_block_only(false);
                    if (_codec.AvailableBytesOut == 0)
                        return BlockState.NeedMore;
                }
            }
            else if (match_available != 0)
            {
                bflush = _tr_tally(0, window[strstart - 1] & 0xff);
                if (bflush) flush_block_only(false);
                strstart++;
                lookahead--;
                if (_codec.AvailableBytesOut == 0) return BlockState.NeedMore;
            }
            else
            {
                match_available = 1;
                strstart++;
                lookahead--;
            }
        }

        if (match_available != 0)
        {
            bflush = _tr_tally(0, window[strstart - 1] & 0xff);
            match_available = 0;
        }

        flush_block_only(flush == FlushType.Finish);

        if (_codec.AvailableBytesOut == 0)
        {
            if (flush == FlushType.Finish) return BlockState.FinishStarted;
            return BlockState.NeedMore;
        }

        return flush == FlushType.Finish ? BlockState.FinishDone : BlockState.BlockDone;
    }


    internal int longest_match(int cur_match)
    {
        var chain_length = config.MaxChainLength; // max hash chain length
        var scan = strstart; // current string
        int match; // matched string
        int len; // length of current match
        var best_len = prev_length; // best match length so far
        var limit = strstart > w_size - MIN_LOOKAHEAD ? strstart - (w_size - MIN_LOOKAHEAD) : 0;

        var niceLength = config.NiceLength;
        var wmask = w_mask;
        var strend = strstart + MAX_MATCH;
        var scan_end1 = window[scan + best_len - 1];
        var scan_end = window[scan + best_len];

        // Do not waste too much time if we already have a good match:
        if (prev_length >= config.GoodLength) chain_length >>= 2;
        // Do not look for matches beyond the end of the input. This is necessary to make deflate deterministic.
        if (niceLength > lookahead) niceLength = lookahead;

        do
        {
            match = cur_match;
            if (window[match + best_len] != scan_end || window[match + best_len - 1] != scan_end1 ||
                window[match] != window[scan] || window[++match] != window[scan + 1]) continue;
            scan += 2;
            match++;
            do
            {
            } while (window[++scan] == window[++match] &&
                     window[++scan] == window[++match] &&
                     window[++scan] == window[++match] &&
                     window[++scan] == window[++match] &&
                     window[++scan] == window[++match] &&
                     window[++scan] == window[++match] &&
                     window[++scan] == window[++match] &&
                     window[++scan] == window[++match] && scan < strend);

            len = MAX_MATCH - (strend - scan);
            scan = strend - MAX_MATCH;

            if (len > best_len)
            {
                match_start = cur_match;
                best_len = len;
                if (len >= niceLength) break;
                scan_end1 = window[scan + best_len - 1];
                scan_end = window[scan + best_len];
            }
        } while ((cur_match = prev[cur_match & wmask] & 0xffff) > limit && --chain_length != 0);

        if (best_len <= lookahead) return best_len;
        return lookahead;
    }

    internal int Initialize(ZlibCodec codec, CompressionLevel level)
    {
        return Initialize(codec, level, ZlibConstants.WindowBitsMax);
    }

    internal int Initialize(ZlibCodec codec, CompressionLevel level, int bits)
    {
        return Initialize(codec, level, bits, MEM_LEVEL_DEFAULT, CompressionStrategy.Default);
    }

    internal int Initialize(ZlibCodec codec, CompressionLevel level, int bits, CompressionStrategy compressionStrategy)
    {
        return Initialize(codec, level, bits, MEM_LEVEL_DEFAULT, compressionStrategy);
    }

    internal int Initialize(ZlibCodec codec, CompressionLevel level, int windowBits, int memLevel,
        CompressionStrategy strategy)
    {
        _codec = codec;
        _codec.Message = null;
        // validation
        if (windowBits < 9 || windowBits > 15) throw new ZlibException("windowBits must be in the range 9..15.");
        if (memLevel < 1 || memLevel > MEM_LEVEL_MAX)
            throw new ZlibException($"memLevel must be in the range 1.. {MEM_LEVEL_MAX}");

        _codec.dstate = this;
        w_bits = windowBits;
        w_size = 1 << w_bits;
        w_mask = w_size - 1;

        hash_bits = memLevel + 7;
        hash_size = 1 << hash_bits;
        hash_mask = hash_size - 1;
        hash_shift = (hash_bits + MIN_MATCH - 1) / MIN_MATCH;

        window = new byte[w_size * 2];
        prev = new short[w_size];
        head = new short[hash_size];

        // for memLevel==8, this will be 16384, 16k
        lit_bufsize = 1 << (memLevel + 6);
        pending = new byte[lit_bufsize * 4];
        _distanceOffset = lit_bufsize;
        _lengthOffset = (1 + 2) * lit_bufsize;

        compressionLevel = level;
        compressionStrategy = strategy;

        Reset();
        return ZlibConstants.Z_OK;
    }

    internal void Reset()
    {
        _codec.TotalBytesIn = _codec.TotalBytesOut = 0;
        _codec.Message = null;
        //strm.data_type = Z_UNKNOWN;

        pendingCount = 0;
        nextPending = 0;

        Rfc1950BytesEmitted = false;

        status = WantRfc1950HeaderBytes ? INIT_STATE : BUSY_STATE;
        _codec._Adler32 = Adler.Adler32(0, null, 0, 0);

        last_flush = (int)FlushType.None;

        _InitializeTreeData();
        _InitializeLazyMatch();
    }

    internal int End()
    {
        if (status != INIT_STATE && status != BUSY_STATE && status != FINISH_STATE) return ZlibConstants.Z_STREAM_ERROR;
        // Deallocate in reverse order of allocations:
        pending = null;
        head = null;
        prev = null;
        window = null;
        // free
        // dstate=null;
        return status == BUSY_STATE ? ZlibConstants.Z_DATA_ERROR : ZlibConstants.Z_OK;
    }

    private void SetDeflater()
    {
        switch (config.Flavor)
        {
            case DeflateFlavor.Store:
                DeflateFunction = DeflateNone;
                break;
            case DeflateFlavor.Fast:
                DeflateFunction = DeflateFast;
                break;
            case DeflateFlavor.Slow:
                DeflateFunction = DeflateSlow;
                break;
        }
    }

    internal int SetParams(CompressionLevel level, CompressionStrategy strategy)
    {
        var result = ZlibConstants.Z_OK;
        if (compressionLevel != level)
        {
            var newConfig = Config.Lookup(level);
            // change in the deflate flavor (Fast vs slow vs none)?
            if (newConfig.Flavor != config.Flavor && _codec.TotalBytesIn != 0)
                result = _codec.Deflate(FlushType.Partial);
            compressionLevel = level;
            config = newConfig;
            SetDeflater();
        }

        // no need to flush with change in strategy?  Really?
        compressionStrategy = strategy;
        return result;
    }

    internal int SetDictionary(byte[] dictionary)
    {
        var length = dictionary.Length;
        var index = 0;

        if (dictionary == null || status != INIT_STATE) throw new ZlibException("Stream error.");
        _codec._Adler32 = Adler.Adler32(_codec._Adler32, dictionary, 0, dictionary.Length);

        if (length < MIN_MATCH) return ZlibConstants.Z_OK;
        if (length > w_size - MIN_LOOKAHEAD)
        {
            length = w_size - MIN_LOOKAHEAD;
            index = dictionary.Length - length; // use the tail of the dictionary
        }

        Array.Copy(dictionary, index, window, 0, length);
        strstart = length;
        block_start = length;

        ins_h = window[0] & 0xff;
        ins_h = ((ins_h << hash_shift) ^ (window[1] & 0xff)) & hash_mask;

        for (var n = 0; n <= length - MIN_MATCH; n++)
        {
            ins_h = ((ins_h << hash_shift) ^ (window[n + (MIN_MATCH - 1)] & 0xff)) & hash_mask;
            prev[n & w_mask] = head[ins_h];
            head[ins_h] = (short)n;
        }

        return ZlibConstants.Z_OK;
    }

    internal int Deflate(FlushType flush)
    {
        int old_flush;

        if (_codec.OutputBuffer == null || (_codec.InputBuffer == null && _codec.AvailableBytesIn != 0) ||
            (status == FINISH_STATE && flush != FlushType.Finish))
        {
            _codec.Message = _ErrorMessage[ZlibConstants.Z_NEED_DICT - ZlibConstants.Z_STREAM_ERROR];
            throw new ZlibException($"Something is fishy. [{_codec.Message}]");
        }

        if (_codec.AvailableBytesOut == 0)
        {
            _codec.Message = _ErrorMessage[ZlibConstants.Z_NEED_DICT - ZlibConstants.Z_BUF_ERROR];
            throw new ZlibException("OutputBuffer is full (AvailableBytesOut == 0)");
        }

        old_flush = last_flush;
        last_flush = (int)flush;

        // Write the zlib (rfc1950) header bytes
        if (status == INIT_STATE)
        {
            var header = (Z_DEFLATED + ((w_bits - 8) << 4)) << 8;
            var level_flags = (((int)compressionLevel - 1) & 0xff) >> 1;

            if (level_flags > 3)
                level_flags = 3;
            header |= level_flags << 6;
            if (strstart != 0)
                header |= PRESET_DICT;
            header += 31 - header % 31;

            status = BUSY_STATE;
            //putShortMSB(header);
            unchecked
            {
                pending[pendingCount++] = (byte)(header >> 8);
                pending[pendingCount++] = (byte)header;
            }

            // Save the adler32 of the preset dictionary:
            if (strstart != 0)
            {
                pending[pendingCount++] = (byte)((_codec._Adler32 & 0xFF000000) >> 24);
                pending[pendingCount++] = (byte)((_codec._Adler32 & 0x00FF0000) >> 16);
                pending[pendingCount++] = (byte)((_codec._Adler32 & 0x0000FF00) >> 8);
                pending[pendingCount++] = (byte)(_codec._Adler32 & 0x000000FF);
            }

            _codec._Adler32 = Adler.Adler32(0, null, 0, 0);
        }

        // Flush as much pending output as possible
        if (pendingCount != 0)
        {
            _codec.flush_pending();
            if (_codec.AvailableBytesOut == 0)
            {
                last_flush = -1;
                return ZlibConstants.Z_OK;
            }
        }
        else if (_codec.AvailableBytesIn == 0 && (int)flush <= old_flush && flush != FlushType.Finish)
        {
            // workitem 8557
            //
            // Not sure why this needs to be an error.  pendingCount == 0, which
            // means there's nothing to deflate.  And the caller has not asked
            // for a FlushType.Finish, but...  that seems very non-fatal.  We
            // can just say "OK" and do nothing.
            // _codec.Message = z_errmsg[ZlibConstants.Z_NEED_DICT - (ZlibConstants.Z_BUF_ERROR)];
            // throw new ZlibException("AvailableBytesIn == 0 && flush<=old_flush && flush != FlushType.Finish");
            return ZlibConstants.Z_OK;
        }

        // User must not provide more input after the first FINISH:
        if (status == FINISH_STATE && _codec.AvailableBytesIn != 0)
        {
            _codec.Message = _ErrorMessage[ZlibConstants.Z_NEED_DICT - ZlibConstants.Z_BUF_ERROR];
            throw new ZlibException("status == FINISH_STATE && _codec.AvailableBytesIn != 0");
        }

        // Start a new block or continue the current one.
        if (_codec.AvailableBytesIn != 0 || lookahead != 0 || (flush != FlushType.None && status != FINISH_STATE))
        {
            var bstate = DeflateFunction(flush);

            if (bstate == BlockState.FinishStarted || bstate == BlockState.FinishDone) status = FINISH_STATE;
            if (bstate == BlockState.NeedMore || bstate == BlockState.FinishStarted)
            {
                if (_codec.AvailableBytesOut == 0) last_flush = -1; // avoid BUF_ERROR next call, see above
                return ZlibConstants.Z_OK;
            }

            if (bstate == BlockState.BlockDone)
            {
                if (flush == FlushType.Partial)
                {
                    _tr_align();
                }
                else
                {
                    _tr_stored_block(0, 0, false);
                    if (flush == FlushType.Full)
                        for (var i = 0; i < hash_size; i++)
                            head[i] = 0;
                }

                _codec.flush_pending();
                if (_codec.AvailableBytesOut == 0)
                {
                    last_flush = -1; // avoid BUF_ERROR at next call, see above
                    return ZlibConstants.Z_OK;
                }
            }
        }

        if (flush != FlushType.Finish) return ZlibConstants.Z_OK;
        if (!WantRfc1950HeaderBytes || Rfc1950BytesEmitted) return ZlibConstants.Z_STREAM_END;
        // Write the zlib trailer (adler32)
        pending[pendingCount++] = (byte)((_codec._Adler32 & 0xFF000000) >> 24);
        pending[pendingCount++] = (byte)((_codec._Adler32 & 0x00FF0000) >> 16);
        pending[pendingCount++] = (byte)((_codec._Adler32 & 0x0000FF00) >> 8);
        pending[pendingCount++] = (byte)(_codec._Adler32 & 0x000000FF);
        //putShortMSB((int)(SharedUtils.URShift(_codec._Adler32, 16)));
        //putShortMSB((int)(_codec._Adler32 & 0xffff));
        _codec.flush_pending();
        // If avail_out is zero, the application will call deflate again to flush the rest.
        Rfc1950BytesEmitted = true; // write the trailer only once!
        return pendingCount != 0 ? ZlibConstants.Z_OK : ZlibConstants.Z_STREAM_END;
    }

    internal delegate BlockState CompressFunc(FlushType flush);

    internal class Config
    {
        private static readonly Config[] Table;
        internal DeflateFlavor Flavor;
        internal int GoodLength; // reduce lazy search above this match length
        internal int MaxChainLength;
        internal int MaxLazy; // do not perform lazy search above this match length
        internal int NiceLength; // quit search above this match length

        static Config()
        {
            Table = new[]
            {
                new(0, 0, 0, 0, DeflateFlavor.Store),
                new Config(4, 4, 8, 4, DeflateFlavor.Fast),
                new Config(4, 5, 16, 8, DeflateFlavor.Fast),
                new Config(4, 6, 32, 32, DeflateFlavor.Fast),

                new Config(4, 4, 16, 16, DeflateFlavor.Slow),
                new Config(8, 16, 32, 32, DeflateFlavor.Slow),
                new Config(8, 16, 128, 128, DeflateFlavor.Slow),
                new Config(8, 32, 128, 256, DeflateFlavor.Slow),
                new Config(32, 128, 258, 1024, DeflateFlavor.Slow),
                new Config(32, 258, 258, 4096, DeflateFlavor.Slow)
            };
        }

        private Config(int goodLength, int maxLazy, int niceLength, int maxChainLength, DeflateFlavor flavor)
        {
            GoodLength = goodLength;
            MaxLazy = maxLazy;
            NiceLength = niceLength;
            MaxChainLength = maxChainLength;
            Flavor = flavor;
        }

        public static Config Lookup(CompressionLevel level)
        {
            return Table[(int)level];
        }
    }
}