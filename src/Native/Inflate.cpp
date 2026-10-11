// Compact RFC 1950/1951/1952 decoder used for ZIP-based design files and SVGZ.
// Copyright (c) Bakhtiyar Jahangirzade. Licensed under the project LICENSE.
// Structure follows the canonical-Huffman approach described in RFC 1951.
#include "Engine.h"
#include <cstring>

namespace witb
{
    namespace
    {
        struct State
        {
            const uint8_t* in; size_t size, pos = 0;
            uint32_t bitBuffer = 0; int bitCount = 0;
            std::vector<uint8_t>* out; size_t limit;
            bool failed = false;
            int Bits(int need)
            {
                uint32_t value = bitBuffer;
                while (bitCount < need)
                {
                    if (pos >= size) { failed = true; return 0; }
                    value |= uint32_t(in[pos++]) << bitCount;
                    bitCount += 8;
                }
                bitBuffer = value >> need;
                bitCount -= need;
                return int(value & ((1u << need) - 1));
            }
        };

        struct Huffman { short count[16]; short symbol[288]; };

        int DecodeSymbol(State& s, const Huffman& h)
        {
            int code = 0, first = 0, index = 0;
            for (int len = 1; len < 16; len++)
            {
                code |= s.Bits(1);
                if (s.failed) return -1;
                int count = h.count[len];
                if (code - count < first) return h.symbol[index + (code - first)];
                index += count; first += count; first <<= 1; code <<= 1;
            }
            return -1;
        }

        bool Construct(Huffman& h, const short* length, int n)
        {
            memset(h.count, 0, sizeof h.count);
            for (int i = 0; i < n; i++) h.count[length[i]]++;
            if (h.count[0] == n) return true;
            int left = 1;
            for (int len = 1; len < 16; len++) { left <<= 1; left -= h.count[len]; if (left < 0) return false; }
            short offs[16]; offs[1] = 0;
            for (int len = 1; len < 15; len++) offs[len + 1] = short(offs[len] + h.count[len]);
            for (int i = 0; i < n; i++) if (length[i]) h.symbol[offs[length[i]]++] = short(i);
            return true;
        }

        const short lengthBase[29] = { 3,4,5,6,7,8,9,10,11,13,15,17,19,23,27,31,35,43,51,59,67,83,99,115,131,163,195,227,258 };
        const short lengthExtra[29] = { 0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,3,3,3,3,4,4,4,4,5,5,5,5,0 };
        const short distBase[30] = { 1,2,3,4,5,7,9,13,17,25,33,49,65,97,129,193,257,385,513,769,1025,1537,2049,3073,4097,6145,8193,12289,16385,24577 };
        const short distExtra[30] = { 0,0,0,0,1,1,2,2,3,3,4,4,5,5,6,6,7,7,8,8,9,9,10,10,11,11,12,12,13,13 };

        bool Codes(State& s, const Huffman& lengths, const Huffman& distances)
        {
            for (;;)
            {
                int symbol = DecodeSymbol(s, lengths);
                if (symbol < 0) return false;
                if (symbol < 256)
                {
                    if (s.out->size() >= s.limit) return false;
                    s.out->push_back(uint8_t(symbol));
                }
                else if (symbol == 256) return true;
                else
                {
                    symbol -= 257;
                    if (symbol >= 29) return false;
                    int len = lengthBase[symbol] + s.Bits(lengthExtra[symbol]);
                    int ds = DecodeSymbol(s, distances);
                    if (ds < 0 || ds >= 30) return false;
                    size_t dist = size_t(distBase[ds]) + size_t(s.Bits(distExtra[ds]));
                    if (s.failed || dist > s.out->size() || s.out->size() + len > s.limit) return false;
                    size_t from = s.out->size() - dist;
                    for (int i = 0; i < len; i++) s.out->push_back((*s.out)[from + i]);
                }
            }
        }

        bool Stored(State& s)
        {
            s.bitBuffer = 0; s.bitCount = 0;
            if (s.pos + 4 > s.size) return false;
            unsigned len = s.in[s.pos] | (s.in[s.pos + 1] << 8);
            unsigned nlen = s.in[s.pos + 2] | (s.in[s.pos + 3] << 8);
            s.pos += 4;
            if (len != (~nlen & 0xFFFF) || s.pos + len > s.size || s.out->size() + len > s.limit) return false;
            s.out->insert(s.out->end(), s.in + s.pos, s.in + s.pos + len);
            s.pos += len;
            return true;
        }

        bool Fixed(State& s)
        {
            static Huffman lengths, distances; static bool ready = false;
            if (!ready)
            {
                short l[288];
                int i = 0;
                for (; i < 144; i++) l[i] = 8;
                for (; i < 256; i++) l[i] = 9;
                for (; i < 280; i++) l[i] = 7;
                for (; i < 288; i++) l[i] = 8;
                Construct(lengths, l, 288);
                for (i = 0; i < 30; i++) l[i] = 5;
                Construct(distances, l, 30);
                ready = true;
            }
            return Codes(s, lengths, distances);
        }

        bool Dynamic(State& s)
        {
            static const short order[19] = { 16,17,18,0,8,7,9,6,10,5,11,4,12,3,13,2,14,1,15 };
            int nlen = s.Bits(5) + 257, ndist = s.Bits(5) + 1, ncode = s.Bits(4) + 4;
            if (s.failed || nlen > 286 || ndist > 30) return false;
            short lengths[320] = {};
            for (int i = 0; i < ncode; i++) lengths[order[i]] = short(s.Bits(3));
            Huffman lencode{}, distcode{};
            if (!Construct(lencode, lengths, 19)) return false;
            int index = 0;
            while (index < nlen + ndist)
            {
                int symbol = DecodeSymbol(s, lencode);
                if (symbol < 0) return false;
                if (symbol < 16) lengths[index++] = short(symbol);
                else
                {
                    short value = 0; int repeat;
                    if (symbol == 16) { if (index == 0) return false; value = lengths[index - 1]; repeat = 3 + s.Bits(2); }
                    else if (symbol == 17) repeat = 3 + s.Bits(3);
                    else repeat = 11 + s.Bits(7);
                    if (s.failed || index + repeat > nlen + ndist) return false;
                    while (repeat--) lengths[index++] = value;
                }
            }
            if (lengths[256] == 0) return false;
            if (!Construct(lencode, lengths, nlen) || !Construct(distcode, lengths + nlen, ndist)) return false;
            return Codes(s, lencode, distcode);
        }
    }

    bool Inflate(const uint8_t* data, size_t size, int wrapper, std::vector<uint8_t>& output, size_t limit)
    {
        size_t start = 0;
        if (wrapper == 1) { if (size < 2) return false; start = 2; }
        else if (wrapper == 2)
        {
            if (size < 10 || data[0] != 0x1F || data[1] != 0x8B || data[2] != 8) return false;
            uint8_t flags = data[3]; start = 10;
            if (flags & 4) { if (start + 2 > size) return false; start += 2 + (data[start] | (data[start + 1] << 8)); }
            if (flags & 8) { while (start < size && data[start]) start++; start++; }
            if (flags & 16) { while (start < size && data[start]) start++; start++; }
            if (flags & 2) start += 2;
            if (start >= size) return false;
        }
        State s{ data + start, size - start };
        s.out = &output; s.limit = limit;
        output.clear();
        int last;
        do
        {
            last = s.Bits(1);
            int type = s.Bits(2);
            if (s.failed) return false;
            bool ok = type == 0 ? Stored(s) : type == 1 ? Fixed(s) : type == 2 ? Dynamic(s) : false;
            if (!ok || s.failed) return false;
        } while (!last);
        return true;
    }
}
