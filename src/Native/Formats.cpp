// Raster container decoders: Photoshop, CorelDRAW previews, OpenRaster/Krita,
// QOI, Targa and Netpbm.
// Copyright (c) Bakhtiyar Jahangirzade. Licensed under the project LICENSE.
#include "Engine.h"
#include <algorithm>
#include <cmath>
#include <cstring>

namespace witb
{
    // ---------------- Photoshop PSD / PSB: merged composite image ----------------
    namespace
    {
        bool PackBits(const uint8_t* in, size_t inSize, uint8_t* out, size_t outSize)
        {
            size_t i = 0, o = 0;
            while (o < outSize && i < inSize)
            {
                int8_t n = int8_t(in[i++]);
                if (n >= 0)
                {
                    size_t count = size_t(n) + 1;
                    if (i + count > inSize) count = inSize - i;
                    count = (std::min)(count, outSize - o);
                    memcpy(out + o, in + i, count); i += count; o += count;
                }
                else if (n != -128)
                {
                    size_t count = size_t(1 - n);
                    if (i >= inSize) break;
                    count = (std::min)(count, outSize - o);
                    memset(out + o, in[i++], count); o += count;
                }
            }
            if (o < outSize) memset(out + o, 0, outSize - o);
            return true;
        }

        uint8_t Sample(const uint8_t* row, UINT x, UINT depth)
        {
            switch (depth)
            {
            case 1: return (row[x >> 3] >> (7 - (x & 7))) & 1 ? 0 : 255;
            case 16: return row[size_t(x) * 2];
            case 32:
            {
                uint32_t bits = uint32_t(row[size_t(x) * 4]) << 24 | uint32_t(row[size_t(x) * 4 + 1]) << 16 | uint32_t(row[size_t(x) * 4 + 2]) << 8 | row[size_t(x) * 4 + 3];
                float f; memcpy(&f, &bits, 4);
                if (!(f > 0)) return 0;
                // Photoshop 32-bit documents are linear; encode for display.
                float s = f >= 1 ? 1.f : (f <= 0.0031308f ? f * 12.92f : 1.055f * std::pow(f, 1 / 2.4f) - 0.055f);
                return uint8_t(std::lround(s * 255));
            }
            default: return row[x];
            }
        }
    }

    HRESULT DecodePsd(Source& src, UINT maxW, UINT maxH, Image& image)
    {
        uint8_t signature[4]; uint16_t version, channels, depth, mode; uint32_t height, width;
        if (!src.ReadAt(0, signature, 4) || memcmp(signature, "8BPS", 4) || !src.BE16(version) || (version != 1 && version != 2)) return E_FAIL;
        if (!src.Skip(6) || !src.BE16(channels) || !src.BE32(height) || !src.BE32(width) || !src.BE16(depth) || !src.BE16(mode)) return E_FAIL;
        if (!width || !height || !channels || channels > 56 || (depth != 1 && depth != 8 && depth != 16 && depth != 32)) return E_FAIL;
        if (width > 300000 || height > 300000 || uint64_t(channels) * height > 20000000ull) return E_FAIL;
        bool big = version == 2;
        uint32_t length;
        // Color mode data holds the palette for indexed images.
        if (!src.BE32(length)) return E_FAIL;
        std::vector<uint8_t> palette;
        if (mode == 2 && length >= 768) { palette.resize(768); if (!src.Read(palette.data(), 768) || !src.Skip(length - 768)) return E_FAIL; }
        else if (!src.Skip(length)) return E_FAIL;
        if (!src.BE32(length) || !src.Skip(length)) return E_FAIL; // image resources
        uint64_t layerSection = 0;
        if (big) { if (!src.BE64(layerSection)) return E_FAIL; } else { uint32_t v; if (!src.BE32(v)) return E_FAIL; layerSection = v; }
        uint64_t afterLayers = src.Tell() + layerSection;
        // A negative layer count means the first extra channel is the merged transparency.
        bool mergedAlpha = false;
        if (layerSection >= (big ? 10u : 6u))
        {
            uint64_t layerInfo = 0;
            if (big) { if (!src.BE64(layerInfo)) return E_FAIL; } else { uint32_t v; if (!src.BE32(v)) return E_FAIL; layerInfo = v; }
            if (layerInfo >= 2) { uint16_t count; if (!src.BE16(count)) return E_FAIL; mergedAlpha = int16_t(count) < 0; }
        }
        if (!src.Seek(afterLayers)) return E_FAIL;
        uint16_t compression;
        if (!src.BE16(compression) || compression > 1) return E_FAIL;

        UINT colorChannels = mode == 3 ? 3 : mode == 4 ? 4 : 1;
        if (channels < colorChannels) colorChannels = 1;
        bool alpha = channels > colorChannels && (mergedAlpha || (mode == 3 && channels == 4 && depth != 1));
        UINT used = colorChannels + (alpha ? 1 : 0);
        size_t rowBytes = (size_t(width) * depth + 7) / 8;
        Reducer reducer(width, height, maxW, maxH, used);
        std::vector<uint8_t> row(rowBytes), samples(width), packed;
        std::vector<uint32_t> counts;
        if (compression == 1)
        {
            counts.resize(size_t(channels) * height);
            for (auto& c : counts) { if (big) { if (!src.BE32(c)) return E_FAIL; } else { uint16_t v; if (!src.BE16(v)) return E_FAIL; c = v; } }
        }
        for (UINT c = 0; c < channels; c++)
        {
            bool wanted = c < used;
            for (UINT y = 0; y < height; y++)
            {
                if (compression == 1)
                {
                    uint32_t n = counts[size_t(c) * height + y];
                    if (!wanted) { if (!src.Skip(n)) return E_FAIL; continue; }
                    if (n > rowBytes * 2 + 1024) return E_FAIL;
                    packed.resize(n);
                    if (!src.Read(packed.data(), n)) return E_FAIL;
                    PackBits(packed.data(), n, row.data(), rowBytes);
                }
                else
                {
                    if (!wanted) { if (!src.Skip(rowBytes)) return E_FAIL; continue; }
                    if (!src.Read(row.data(), rowBytes)) return E_FAIL;
                }
                for (UINT x = 0; x < width; x++) samples[x] = Sample(row.data(), x, depth);
                reducer.AddRow(c, y, samples.data());
            }
            if (c + 1 >= used && compression == 0) break;
            if (c + 1 >= used && compression == 1) break;
        }
        image.width = reducer.Width(); image.height = reducer.Height();
        image.pixels.resize(size_t(image.width) * image.height);
        for (UINT y = 0; y < image.height; y++)
            for (UINT x = 0; x < image.width; x++)
            {
                auto v = [&](UINT ch) { return reducer.Value(ch, x, y); };
                float r, g, b;
                if (mode == 3) { r = v(0); g = v(1); b = v(2); }
                else if (mode == 4) { float k = v(3) / 255.f; r = v(0) * k; g = v(1) * k; b = v(2) * k; }
                else if (mode == 2 && palette.size() == 768) { int i = std::clamp(int(std::lround(v(0))), 0, 255); r = palette[i]; g = palette[256 + i]; b = palette[512 + i]; }
                else if (mode == 9) { r = g = b = v(0); } // Lab: lightness only
                else { r = g = b = v(0); }
                float a = alpha ? v(colorChannels) : 255.f;
                image.pixels[size_t(y) * image.width + x] = Pack(uint8_t(std::lround(r)), uint8_t(std::lround(g)), uint8_t(std::lround(b)), uint8_t(std::lround(a)));
            }
        return S_OK;
    }

    // ---------------- ZIP containers with an embedded rendering ----------------
    namespace
    {
        struct ZipEntry { std::string name; uint16_t method; uint32_t compressed, size, offset; };

        uint16_t LE16(const uint8_t* p) { return uint16_t(p[0] | p[1] << 8); }
        uint32_t LE32(const uint8_t* p) { return uint32_t(p[0]) | uint32_t(p[1]) << 8 | uint32_t(p[2]) << 16 | uint32_t(p[3]) << 24; }

        bool ZipDirectory(Source& src, std::vector<ZipEntry>& entries)
        {
            uint64_t size = src.Size();
            if (size < 22) return false;
            size_t tail = size_t((std::min<uint64_t>)(size, 65557));
            std::vector<uint8_t> end(tail);
            if (!src.ReadAt(size - tail, end.data(), tail)) return false;
            for (size_t i = tail - 22 + 1; i-- > 0;)
            {
                if (LE32(&end[i]) != 0x06054b50) continue;
                uint16_t count = LE16(&end[i + 10]);
                uint32_t dirSize = LE32(&end[i + 12]), dirOffset = LE32(&end[i + 16]);
                if (uint64_t(dirOffset) + dirSize > size || dirSize > (64u << 20)) return false;
                std::vector<uint8_t> dir(dirSize);
                if (!src.ReadAt(dirOffset, dir.data(), dirSize)) return false;
                size_t p = 0;
                for (uint16_t e = 0; e < count && p + 46 <= dir.size(); e++)
                {
                    if (LE32(&dir[p]) != 0x02014b50) return false;
                    ZipEntry entry;
                    entry.method = LE16(&dir[p + 10]);
                    entry.compressed = LE32(&dir[p + 20]); entry.size = LE32(&dir[p + 24]);
                    uint16_t n = LE16(&dir[p + 28]), x = LE16(&dir[p + 30]), c = LE16(&dir[p + 32]);
                    entry.offset = LE32(&dir[p + 42]);
                    if (p + 46 + n > dir.size()) return false;
                    entry.name.assign(reinterpret_cast<char*>(&dir[p + 46]), n);
                    entries.push_back(entry);
                    p += 46 + size_t(n) + x + c;
                }
                return true;
            }
            return false;
        }

        bool ZipRead(Source& src, const ZipEntry& entry, std::vector<uint8_t>& data)
        {
            uint8_t local[30];
            if (entry.size > (256u << 20) || entry.compressed > (256u << 20) || !src.ReadAt(entry.offset, local, 30) || LE32(local) != 0x04034b50) return false;
            uint64_t start = uint64_t(entry.offset) + 30 + LE16(local + 26) + LE16(local + 28);
            std::vector<uint8_t> raw(entry.compressed);
            if (!src.ReadAt(start, raw.data(), raw.size())) return false;
            if (entry.method == 0) { data = std::move(raw); return true; }
            if (entry.method == 8) return Inflate(raw.data(), raw.size(), 0, data, size_t(entry.size) + 16);
            return false;
        }

        std::string LowerName(std::string s) { for (auto& c : s) c = char(tolower(uint8_t(c))); return s; }
    }

    HRESULT DecodeZipPreview(Source& src, UINT maxW, UINT maxH, Image& image)
    {
        std::vector<ZipEntry> entries;
        if (!ZipDirectory(src, entries)) return E_FAIL;
        // Ordered by quality: full-page renderings before small thumbnails.
        static const char* preferred[] = { "mergedimage.png", "previews/page1.png", "previews/page1.jpg", "metadata/thumbnails/thumbnail.png", "previews/thumbnail.png", "thumbnail.png", "metadata/thumbnails/thumbnail.bmp", "thumbnails/thumbnail.png", "preview.png" };
        const ZipEntry* chosen = nullptr;
        for (const char* want : preferred)
        {
            for (auto& e : entries) if (LowerName(e.name) == want) { chosen = &e; break; }
            if (chosen) break;
        }
        if (!chosen)
        {
            for (auto& e : entries)
            {
                std::string n = LowerName(e.name);
                if (n.rfind("previews/", 0) == 0 && (n.size() > 4 && (n.compare(n.size() - 4, 4, ".png") == 0 || n.compare(n.size() - 4, 4, ".jpg") == 0))) { chosen = &e; break; }
            }
        }
        if (!chosen) return E_FAIL;
        std::vector<uint8_t> data;
        if (!ZipRead(src, *chosen, data)) return E_FAIL;
        return DecodeWicMemory(data, maxW, maxH, image);
    }

    // Pre-X4 CorelDRAW RIFF files carry a device-independent bitmap in a DISP chunk.
    HRESULT DecodeRiffCdr(Source& src, UINT maxW, UINT maxH, Image& image)
    {
        uint64_t pos = 12, end = src.Size();
        while (pos + 8 <= end)
        {
            uint8_t head[8];
            if (!src.ReadAt(pos, head, 8)) return E_FAIL;
            uint32_t size = LE32(head + 4);
            if (!memcmp(head, "LIST", 4)) { pos += 12; continue; }
            if (!memcmp(head, "DISP", 4) && size > 44 && size < (64u << 20))
            {
                std::vector<uint8_t> chunk(size);
                if (!src.ReadAt(pos + 8, chunk.data(), size)) return E_FAIL;
                for (size_t skip = 0; skip <= 8 && skip + 40 <= chunk.size(); skip += 4)
                {
                    if (LE32(&chunk[skip]) != 40) continue;
                    uint16_t bits = LE16(&chunk[skip + 14]); uint32_t used = LE32(&chunk[skip + 32]);
                    uint32_t paletteBytes = (bits <= 8 ? (used ? used : (1u << bits)) : 0) * 4;
                    std::vector<uint8_t> bmp(14 + chunk.size() - skip);
                    bmp[0] = 'B'; bmp[1] = 'M';
                    uint32_t total = uint32_t(bmp.size()), offset = 14 + 40 + paletteBytes;
                    memcpy(&bmp[2], &total, 4); memcpy(&bmp[10], &offset, 4);
                    memcpy(&bmp[14], &chunk[skip], chunk.size() - skip);
                    if (SUCCEEDED(DecodeWicMemory(bmp, maxW, maxH, image))) return S_OK;
                }
                return E_FAIL;
            }
            pos += 8 + size + (size & 1);
        }
        return E_FAIL;
    }

    // ---------------- QOI ----------------
    HRESULT DecodeQoi(Source& src, UINT maxW, UINT maxH, Image& image)
    {
        std::vector<uint8_t> data;
        if (!src.ReadAll(data, 512ull << 20) || data.size() < 22) return E_FAIL;
        uint32_t w = uint32_t(data[4]) << 24 | data[5] << 16 | data[6] << 8 | data[7];
        uint32_t h = uint32_t(data[8]) << 24 | data[9] << 16 | data[10] << 8 | data[11];
        if (!w || !h || uint64_t(w) * h > 64000000ull) return E_FAIL;
        std::vector<uint8_t> rgba(size_t(w) * h * 4);
        uint8_t index[64][4] = {}; uint8_t px[4] = { 0, 0, 0, 255 };
        size_t p = 14, run = 0, end = data.size() - 8;
        for (size_t i = 0; i < size_t(w) * h; i++)
        {
            if (run) run--;
            else if (p < end)
            {
                uint8_t b = data[p++];
                if (b == 0xFE) { px[0] = data[p]; px[1] = data[p + 1]; px[2] = data[p + 2]; p += 3; }
                else if (b == 0xFF) { memcpy(px, &data[p], 4); p += 4; }
                else if ((b & 0xC0) == 0x00) memcpy(px, index[b], 4);
                else if ((b & 0xC0) == 0x40) { px[0] += ((b >> 4) & 3) - 2; px[1] += ((b >> 2) & 3) - 2; px[2] += (b & 3) - 2; }
                else if ((b & 0xC0) == 0x80) { uint8_t b2 = data[p++]; int vg = (b & 0x3F) - 32; px[0] += vg - 8 + ((b2 >> 4) & 0xF); px[1] += vg; px[2] += vg - 8 + (b2 & 0xF); }
                else run = b & 0x3F;
                memcpy(index[(px[0] * 3 + px[1] * 5 + px[2] * 7 + px[3] * 11) % 64], px, 4);
            }
            memcpy(&rgba[i * 4], px, 4);
        }
        image = FromRgba(rgba, w, h, maxW, maxH);
        return S_OK;
    }

    // ---------------- Targa ----------------
    HRESULT DecodeTga(Source& src, UINT maxW, UINT maxH, Image& image)
    {
        std::vector<uint8_t> d;
        if (!src.ReadAll(d, 512ull << 20) || d.size() < 18) return E_FAIL;
        uint8_t idLength = d[0], mapType = d[1], type = d[2];
        uint16_t mapFirst = LE16(&d[3]), mapLength = LE16(&d[5]); uint8_t mapBits = d[7];
        uint16_t w = LE16(&d[12]), h = LE16(&d[14]); uint8_t bits = d[16], descriptor = d[17];
        bool rle = type >= 9; uint8_t base = rle ? type - 8 : type;
        if (!w || !h || uint64_t(w) * h > 64000000ull || (base != 1 && base != 2 && base != 3) || mapType > 1) return E_FAIL;
        size_t p = 18 + idLength;
        std::vector<uint8_t> map;
        if (mapType == 1) { size_t mapBytes = size_t(mapLength) * ((mapBits + 7) / 8); if (p + mapBytes > d.size()) return E_FAIL; map.assign(d.begin() + p, d.begin() + p + mapBytes); p += mapBytes; }
        int bpp = (bits + 7) / 8;
        if (bpp < 1 || bpp > 4) return E_FAIL;
        std::vector<uint8_t> rgba(size_t(w) * h * 4);
        auto color = [&](const uint8_t* s, uint8_t* o)
        {
            uint32_t v = 0; for (int i = 0; i < bpp; i++) v |= uint32_t(s[i]) << (8 * i);
            if (base == 1)
            {
                int mb = (mapBits + 7) / 8; size_t idx = size_t(v - mapFirst) * mb;
                if (idx + mb > map.size()) { o[0] = o[1] = o[2] = 0; o[3] = 255; return; }
                const uint8_t* m = &map[idx];
                if (mb >= 3) { o[0] = m[2]; o[1] = m[1]; o[2] = m[0]; o[3] = mb == 4 ? m[3] : 255; }
                else { uint16_t c = uint16_t(m[0] | m[1] << 8); o[0] = uint8_t(((c >> 10) & 31) * 255 / 31); o[1] = uint8_t(((c >> 5) & 31) * 255 / 31); o[2] = uint8_t((c & 31) * 255 / 31); o[3] = 255; }
            }
            else if (base == 3) { o[0] = o[1] = o[2] = s[0]; o[3] = bpp == 2 ? s[1] : 255; }
            else if (bpp == 2) { o[0] = uint8_t(((v >> 10) & 31) * 255 / 31); o[1] = uint8_t(((v >> 5) & 31) * 255 / 31); o[2] = uint8_t((v & 31) * 255 / 31); o[3] = 255; }
            else { o[0] = s[2]; o[1] = s[1]; o[2] = s[0]; o[3] = bpp == 4 && (descriptor & 15) ? s[3] : 255; }
        };
        size_t total = size_t(w) * h, i = 0;
        while (i < total)
        {
            if (rle)
            {
                if (p >= d.size()) return E_FAIL;
                uint8_t head = d[p++]; size_t count = (head & 0x7F) + 1;
                if (head & 0x80) { if (p + bpp > d.size()) return E_FAIL; uint8_t o[4]; color(&d[p], o); p += bpp; for (size_t k = 0; k < count && i < total; k++, i++) memcpy(&rgba[i * 4], o, 4); }
                else for (size_t k = 0; k < count && i < total; k++, i++) { if (p + bpp > d.size()) return E_FAIL; color(&d[p], &rgba[i * 4]); p += bpp; }
            }
            else { if (p + bpp > d.size()) return E_FAIL; color(&d[p], &rgba[i * 4]); p += bpp; i++; }
        }
        if (!(descriptor & 0x20))
            for (UINT y = 0; y < h / 2u; y++) std::swap_ranges(rgba.begin() + size_t(y) * w * 4, rgba.begin() + size_t(y + 1) * w * 4, rgba.begin() + size_t(h - 1 - y) * w * 4);
        image = FromRgba(rgba, w, h, maxW, maxH);
        return S_OK;
    }

    // ---------------- Netpbm ----------------
    HRESULT DecodePnm(Source& src, UINT maxW, UINT maxH, Image& image)
    {
        std::vector<uint8_t> d;
        if (!src.ReadAll(d, 512ull << 20) || d.size() < 3) return E_FAIL;
        int kind = d[1] - '0'; size_t p = 2;
        auto number = [&](long& v) -> bool
        {
            while (p < d.size()) { if (d[p] == '#') { while (p < d.size() && d[p] != '\n') p++; } else if (isspace(d[p])) p++; else break; }
            if (p >= d.size() || !isdigit(d[p])) return false;
            v = 0; while (p < d.size() && isdigit(d[p])) { v = v * 10 + (d[p++] - '0'); if (v > 1000000) return false; }
            return true;
        };
        long w, h, maxval = 1;
        if (!number(w) || !number(h) || w <= 0 || h <= 0 || uint64_t(w) * uint64_t(h) > 64000000ull || (kind != 1 && kind != 4 && !number(maxval)) || maxval <= 0 || maxval > 65535) return E_FAIL;
        if (kind >= 4) p++;
        int components = (kind == 3 || kind == 6) ? 3 : 1;
        std::vector<uint8_t> rgba(size_t(w) * h * 4);
        for (size_t i = 0; i < size_t(w) * h; i++)
        {
            int v[3] = {};
            for (int c = 0; c < components; c++)
            {
                long s = 0;
                if (kind == 4) { size_t row = i / w, col = i % w; size_t at = p + row * ((w + 7) / 8) + col / 8; if (at >= d.size()) return E_FAIL; s = (d[at] >> (7 - col % 8)) & 1; v[c] = s ? 0 : 255; continue; }
                if (kind <= 3) { if (!number(s)) return E_FAIL; }
                else if (maxval > 255) { if (p + 1 >= d.size()) return E_FAIL; s = d[p] << 8 | d[p + 1]; p += 2; }
                else { if (p >= d.size()) return E_FAIL; s = d[p++]; }
                v[c] = kind == 1 ? (s ? 0 : 255) : int(s * 255 / maxval);
            }
            if (components == 1) v[1] = v[2] = v[0];
            rgba[i * 4] = uint8_t(v[0]); rgba[i * 4 + 1] = uint8_t(v[1]); rgba[i * 4 + 2] = uint8_t(v[2]); rgba[i * 4 + 3] = 255;
        }
        image = FromRgba(rgba, UINT(w), UINT(h), maxW, maxH);
        return S_OK;
    }
}
