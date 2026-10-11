// Whatsinthebox native preview engine.
// Copyright (c) Bakhtiyar Jahangirzade. Licensed under the project LICENSE.
//
// Decodes the formats Windows does not preview on its own into a premultiplied
// BGRA image, directly inside the Windows-provided host process (Preview Host or
// the thumbnail surrogate). No background service and no helper process.
#pragma once
#include <windows.h>
#include <objidl.h>
#include <cstdint>
#include <string>
#include <vector>

namespace witb
{
    struct Image
    {
        UINT width = 0, height = 0;
        std::vector<uint32_t> pixels; // premultiplied BGRA, top-down
        bool HasAlpha() const;
    };

    // Largest size that keeps the aspect ratio and fits inside maxWidth x maxHeight.
    void Fit(double width, double height, UINT maxWidth, UINT maxHeight, bool upscale, UINT& outWidth, UINT& outHeight);
    Image Resize(const Image& source, UINT width, UINT height);

    // Buffered random-access reader over a Shell-provided IStream.
    class Source
    {
    public:
        explicit Source(IStream* stream);
        uint64_t Size() const { return size; }
        uint64_t Tell() const { return position; }
        bool Seek(uint64_t offset);
        bool Skip(uint64_t count) { return Seek(position + count); }
        bool Read(void* data, size_t count);
        bool ReadAt(uint64_t offset, void* data, size_t count) { return Seek(offset) && Read(data, count); }
        bool ReadAll(std::vector<uint8_t>& data, uint64_t limit);
        bool U8(uint8_t& v) { return Read(&v, 1); }
        bool BE16(uint16_t& v);
        bool BE32(uint32_t& v);
        bool BE64(uint64_t& v);
        IStream* Stream() const { return stream; }
    private:
        IStream* stream;
        uint64_t size = 0, position = 0, bufferStart = 0;
        std::vector<uint8_t> buffer;
        size_t bufferLength = 0;
    };

    enum class Kind { Unknown, Psd, Pdf, Zip, RiffCdr, Svg, Svgz, Qoi, Tga, Pnm, Font, Wic };

    // Identifies the content from its first bytes, using the extension only for
    // formats without a signature (TGA).
    Kind Detect(Source& source, const std::wstring& extension);

    // Decodes for a target box. Vector content is rendered at the box size;
    // raster content is reduced while decoding when it is larger than the box.
    HRESULT Decode(IStream* stream, const std::wstring& extension, UINT maxWidth, UINT maxHeight, Image& image, std::wstring* detail = nullptr);

    // Internal decoders.
    HRESULT DecodeWic(IStream* stream, UINT maxWidth, UINT maxHeight, Image& image);
    HRESULT DecodeWicMemory(const std::vector<uint8_t>& data, UINT maxWidth, UINT maxHeight, Image& image);
    HRESULT DecodePsd(Source& source, UINT maxWidth, UINT maxHeight, Image& image);
    HRESULT DecodePdf(IStream* stream, UINT maxWidth, UINT maxHeight, Image& image);
    HRESULT DecodeSvg(IStream* stream, UINT maxWidth, UINT maxHeight, Image& image);
    HRESULT DecodeZipPreview(Source& source, UINT maxWidth, UINT maxHeight, Image& image);
    HRESULT DecodeRiffCdr(Source& source, UINT maxWidth, UINT maxHeight, Image& image);
    HRESULT DecodeQoi(Source& source, UINT maxWidth, UINT maxHeight, Image& image);
    HRESULT DecodeTga(Source& source, UINT maxWidth, UINT maxHeight, Image& image);
    HRESULT DecodePnm(Source& source, UINT maxWidth, UINT maxHeight, Image& image);
    HRESULT DecodeFont(Source& source, UINT maxWidth, UINT maxHeight, Image& image);

    // RFC 1951 inflate; wrapper: 0 raw deflate, 1 zlib, 2 gzip.
    bool Inflate(const uint8_t* data, size_t size, int wrapper, std::vector<uint8_t>& output, size_t limit);

    // Accumulates straight (non-premultiplied) channel samples of a large image
    // into a box-filtered image of at most the target size.
    class Reducer
    {
    public:
        Reducer(UINT sourceWidth, UINT sourceHeight, UINT maxWidth, UINT maxHeight, UINT channels);
        UINT Width() const { return width; }
        UINT Height() const { return height; }
        void AddRow(UINT channel, UINT y, const uint8_t* samples, UINT stride = 1);
        float Value(UINT channel, UINT x, UINT y) const;
        void SetChannel(UINT channel, const uint8_t* samples, UINT y, UINT stride) { AddRow(channel, y, samples, stride); }
    private:
        UINT sourceWidth, sourceHeight, width, height, channels;
        std::vector<uint32_t> sums, counts;
        std::vector<UINT> columnMap;
    };

    inline uint32_t Pack(uint8_t r, uint8_t g, uint8_t b, uint8_t a)
    {
        uint32_t pr = (r * a + 127) / 255, pg = (g * a + 127) / 255, pb = (b * a + 127) / 255;
        return (uint32_t(a) << 24) | (pr << 16) | (pg << 8) | pb;
    }

    // Straight RGBA rows to a premultiplied image reduced to the target box.
    Image FromRgba(const std::vector<uint8_t>& rgba, UINT width, UINT height, UINT maxWidth, UINT maxHeight);
    // Premultiplied to straight BGRA, for image encoders.
    std::vector<uint32_t> Straight(const Image& image);
    HRESULT CreateMemoryStream(const std::vector<uint8_t>& data, IStream** stream);
}
