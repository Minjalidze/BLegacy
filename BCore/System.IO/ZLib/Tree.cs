namespace System.IO.Compression.Zlib;

internal sealed class Tree
{
    internal const int Buf_size = 8 * 2;

    private static readonly int HEAP_SIZE = 2 * InternalConstants.L_CODES + 1;

    // extra bits for each length code
    internal static readonly int[] ExtraLengthBits =
        { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };

    // extra bits for each distance code
    internal static readonly int[] ExtraDistanceBits =
        { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };

    // extra bits for each bit length code
    internal static readonly int[] extra_blbits = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 7 };
    internal static readonly sbyte[] bl_order = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

    private static readonly sbyte[] _dist_code =
    {
        0, 1, 2, 3, 4, 4, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7,
        8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9,
        10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10,
        11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11,
        12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
        12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
        13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
        13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
        0, 0, 16, 17, 18, 18, 19, 19, 20, 20, 20, 20, 21, 21, 21, 21,
        22, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25,
        26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
        26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
        27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27,
        27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
        29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
        29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
        29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
        29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29
    };

    internal static readonly sbyte[] LengthCode =
    {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 8, 9, 9, 10, 10, 11, 11,
        12, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 14, 15, 15, 15, 15,
        16, 16, 16, 16, 16, 16, 16, 16, 17, 17, 17, 17, 17, 17, 17, 17,
        18, 18, 18, 18, 18, 18, 18, 18, 19, 19, 19, 19, 19, 19, 19, 19,
        20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20,
        21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21,
        22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
        23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
        25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25,
        25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25,
        26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
        26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
        27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27,
        27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 28
    };


    internal static readonly int[] LengthBase =
    {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 20, 24, 28,
        32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 0
    };

    internal static readonly int[] DistanceBase =
    {
        0, 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192,
        256, 384, 512, 768, 1024, 1536, 2048, 3072, 4096, 6144, 8192, 12288, 16384, 24576
    };

    internal short[] dyn_tree; // the dynamic tree
    internal int max_code; // largest code with non zero frequency
    internal StaticTree staticTree; // the corresponding static tree

    internal static int DistanceCode(int dist)
    {
        return dist < 256 ? _dist_code[dist] : _dist_code[256 + SharedUtils.URShift(dist, 7)];
    }

    internal void gen_bitlen(DeflateManager s)
    {
        var tree = dyn_tree;
        var stree = staticTree.treeCodes;
        var extra = staticTree.extraBits;
        var base_Renamed = staticTree.extraBase;
        var max_length = staticTree.maxLength;
        int h; // heap index
        int n, m; // iterate over the tree elements
        int bits; // bit length
        int xbits; // extra bits
        short f; // frequency
        var overflow = 0; // number of elements with bit length too large

        for (bits = 0; bits <= InternalConstants.MAX_BITS; bits++) s.bl_count[bits] = 0;

        tree[s.heap[s.heap_max] * 2 + 1] = 0; // root of the heap

        for (h = s.heap_max + 1; h < HEAP_SIZE; h++)
        {
            n = s.heap[h];
            bits = tree[tree[n * 2 + 1] * 2 + 1] + 1;
            if (bits > max_length)
            {
                bits = max_length;
                overflow++;
            }

            tree[n * 2 + 1] = (short)bits;

            if (n > max_code) continue; // not a leaf node

            s.bl_count[bits]++;
            xbits = 0;
            if (n >= base_Renamed) xbits = extra[n - base_Renamed];
            f = tree[n * 2];
            s.opt_len += f * (bits + xbits);
            if (stree != null) s.static_len += f * (stree[n * 2 + 1] + xbits);
        }

        if (overflow == 0) return;

        do
        {
            bits = max_length - 1;
            while (s.bl_count[bits] == 0) bits--;
            s.bl_count[bits]--;
            s.bl_count[bits + 1] = (short)(s.bl_count[bits + 1] + 2);
            s.bl_count[max_length]--;
            overflow -= 2;
        } while (overflow > 0);

        for (bits = max_length; bits != 0; bits--)
        {
            n = s.bl_count[bits];
            while (n != 0)
            {
                m = s.heap[--h];
                if (m > max_code)
                    continue;
                if (tree[m * 2 + 1] != bits)
                {
                    s.opt_len = (int)(s.opt_len + (bits - (long)tree[m * 2 + 1]) * tree[m * 2]);
                    tree[m * 2 + 1] = (short)bits;
                }

                n--;
            }
        }
    }

    internal void build_tree(DeflateManager s)
    {
        var tree = dyn_tree;
        var stree = staticTree.treeCodes;
        var elems = staticTree.elems;
        int n, m; // iterate over heap elements
        var max_code = -1; // largest code with non zero frequency
        int node; // new node being created                       
        s.heap_len = 0;
        s.heap_max = HEAP_SIZE;

        for (n = 0; n < elems; n++)
            if (tree[n * 2] != 0)
            {
                s.heap[++s.heap_len] = max_code = n;
                s.depth[n] = 0;
            }
            else
            {
                tree[n * 2 + 1] = 0;
            }

        while (s.heap_len < 2)
        {
            node = s.heap[++s.heap_len] = max_code < 2 ? ++max_code : 0;
            tree[node * 2] = 1;
            s.depth[node] = 0;
            s.opt_len--;
            if (stree != null) s.static_len -= stree[node * 2 + 1];
        }

        this.max_code = max_code;

        for (n = s.heap_len / 2; n >= 1; n--) s.pqdownheap(tree, n);

        node = elems;
        do
        {
            n = s.heap[1];
            s.heap[1] = s.heap[s.heap_len--];
            s.pqdownheap(tree, 1);
            m = s.heap[1]; // m = node of next least frequency

            s.heap[--s.heap_max] = n; // keep the nodes sorted by frequency
            s.heap[--s.heap_max] = m;

            // Create a new node father of n and m
            tree[node * 2] = unchecked((short)(tree[n * 2] + tree[m * 2]));
            s.depth[node] = (sbyte)(Math.Max((byte)s.depth[n], (byte)s.depth[m]) + 1);
            tree[n * 2 + 1] = tree[m * 2 + 1] = (short)node;

            // and insert the new node in the heap
            s.heap[1] = node++;
            s.pqdownheap(tree, 1);
        } while (s.heap_len >= 2);

        s.heap[--s.heap_max] = s.heap[1];
        gen_bitlen(s);
        gen_codes(tree, max_code, s.bl_count);
    }

    internal static void gen_codes(short[] tree, int max_code, short[] bl_count)
    {
        var next_code = new short[InternalConstants.MAX_BITS + 1]; // next code value for each bit length
        short code = 0; // running code value
        int bits; // bit index
        int n; // code index

        for (bits = 1; bits <= InternalConstants.MAX_BITS; bits++)
            unchecked
            {
                next_code[bits] = code = (short)((code + bl_count[bits - 1]) << 1);
            }

        for (n = 0; n <= max_code; n++)
        {
            int len = tree[n * 2 + 1];
            if (len == 0) continue;
            tree[n * 2] = unchecked((short)bi_reverse(next_code[len]++, len));
        }
    }

    internal static int bi_reverse(int code, int len)
    {
        var res = 0;
        do
        {
            res |= code & 1;
            code >>= 1;
            res <<= 1;
        } while (--len > 0);

        return res >> 1;
    }
}