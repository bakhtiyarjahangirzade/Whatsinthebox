// Whatsinthebox native preview engine: shared image, stream and dispatch code.
// Copyright (c) Bakhtiyar Jahangirzade. Licensed under the project LICENSE.
#include "Engine.h"
#include <wincodec.h>
#include <shlwapi.h>
#include <algorithm>
#include <cmath>
#include <cstring>

#pragma comment(lib, "windowscodecs.lib")
#pragma comment(lib, "shlwapi.lib")

namespace witb
{
    bool Image::HasAlpha() const
    {
        for (uint32_t p : pixels) if ((p >> 24) != 0xFF) return true;
        return false;
    }

    void Fit(double width, double height, UINT maxWidth, UINT maxHeight, bool upscale, UINT& outWidth, UINT& outHeight)
    {
        if (!(width > 0) || !(height > 0) || maxWidth == 0 || maxHeight == 0) { outWidth = outHeight = 0; return; }
        double scale = (std::min)(maxWidth / width, maxHeight / height);
        if (!upscale) scale = (std::min)(1.0, scale);
        outWidth = (UINT)std::clamp<long long>(std::llround(width * scale), 1, maxWidth);
        outHeight = (UINT)std::clamp<long long>(std::llround(height * scale), 1, maxHeight);
    }

    // Area averaging when reducing, bilinear when enlarging. Premultiplied input
    // keeps transparent edges free of dark fringes.
    Image Resize(const Image& source, UINT width, UINT height)
    {
        Image out; out.width = width; out.height = height;
        if (!source.width || !source.height || !width || !height) { out.width = out.height = 0; return out; }
        out.pixels.resize(size_t(width) * height);
        if (width == source.width && height == source.height) { out.pixels = source.pixels; return out; }
        double sx = double(source.width) / width, sy = double(source.height) / height;
        for (UINT y = 0; y < height; y++)
        {
            for (UINT x = 0; x < width; x++)
            {
                double acc[4] = {}; double total = 0;
                if (sx > 1 || sy > 1)
                {
                    double x0 = x * sx, x1 = (std::min)((x + 1) * sx, double(source.width));
                    double y0 = y * sy, y1 = (std::min)((y + 1) * sy, double(source.height));
                    for (UINT yy = UINT(y0); yy < UINT(std::ceil(y1)) && yy < source.height; yy++)
                    {
                        double wy = (std::min)(y1, yy + 1.0) - (std::max)(y0, double(yy));
                        if (wy <= 0) continue;
                        const uint32_t* row = &source.pixels[size_t(yy) * source.width];
                        for (UINT xx = UINT(x0); xx < UINT(std::ceil(x1)) && xx < source.width; xx++)
                        {
                            double wx = (std::min)(x1, xx + 1.0) - (std::max)(x0, double(xx));
                            if (wx <= 0) continue;
                            double w = wx * wy; uint32_t p = row[xx];
                            acc[0] += w * (p & 0xFF); acc[1] += w * ((p >> 8) & 0xFF); acc[2] += w * ((p >> 16) & 0xFF); acc[3] += w * (p >> 24);
                            total += w;
                        }
                    }
                }
                else
                {
                    double fx = (std::max)(0.0, (x + 0.5) * sx - 0.5), fy = (std::max)(0.0, (y + 0.5) * sy - 0.5);
                    UINT ix = (std::min)(UINT(fx), source.width - 1), iy = (std::min)(UINT(fy), source.height - 1);
                    UINT jx = (std::min)(ix + 1, source.width - 1), jy = (std::min)(iy + 1, source.height - 1);
                    double ax = fx - ix, ay = fy - iy;
                    const uint32_t c[4] = { source.pixels[size_t(iy) * source.width + ix], source.pixels[size_t(iy) * source.width + jx], source.pixels[size_t(jy) * source.width + ix], source.pixels[size_t(jy) * source.width + jx] };
                    const double w[4] = { (1 - ax) * (1 - ay), ax * (1 - ay), (1 - ax) * ay, ax * ay };
                    for (int i = 0; i < 4; i++) { acc[0] += w[i] * (c[i] & 0xFF); acc[1] += w[i] * ((c[i] >> 8) & 0xFF); acc[2] += w[i] * ((c[i] >> 16) & 0xFF); acc[3] += w[i] * (c[i] >> 24); }
                    total = 1;
                }
                if (total <= 0) total = 1;
                auto v = [&](int i) { return uint32_t(std::clamp(std::lround(acc[i] / total), 0L, 255L)); };
                out.pixels[size_t(y) * width + x] = v(0) | (v(1) << 8) | (v(2) << 16) | (v(3) << 24);
            }
        }
        return out;
    }

    Source::Source(IStream* value) : stream(value)
    {
        STATSTG stat{};
        if (stream && SUCCEEDED(stream->Stat(&stat, STATFLAG_NONAME))) size = stat.cbSize.QuadPart;
        buffer.resize(256 * 1024);
    }

    bool Source::Seek(uint64_t offset)
    {
        if (offset > size) return false;
        position = offset;
        return true;
    }

    bool Source::Read(void* data, size_t count)
    {
        auto* out = static_cast<uint8_t*>(data);
        while (count)
        {
            if (position >= bufferStart && position < bufferStart + bufferLength)
            {
                size_t available = size_t(bufferStart + bufferLength - position);
                size_t n = (std::min)(available, count);
                memcpy(out, buffer.data() + (position - bufferStart), n);
                out += n; count -= n; position += n;
                continue;
            }
            if (position >= size) return false;
            if (count >= buffer.size())
            {
                LARGE_INTEGER move; move.QuadPart = LONGLONG(position);
                if (FAILED(stream->Seek(move, STREAM_SEEK_SET, nullptr))) return false;
                ULONG got = 0;
                ULONG want = ULONG((std::min<size_t>)(count, 0x40000000));
                if (FAILED(stream->Read(out, want, &got)) || got == 0) return false;
                out += got; count -= got; position += got;
                continue;
            }
            LARGE_INTEGER move; move.QuadPart = LONGLONG(position);
            if (FAILED(stream->Seek(move, STREAM_SEEK_SET, nullptr))) return false;
            ULONG got = 0;
            if (FAILED(stream->Read(buffer.data(), ULONG(buffer.size()), &got)) || got == 0) return false;
            bufferStart = position; bufferLength = got;
        }
        return true;
    }

    bool Source::ReadAll(std::vector<uint8_t>& data, uint64_t limit)
    {
        if (size > limit) return false;
        data.resize(size_t(size));
        return size == 0 || ReadAt(0, data.data(), data.size());
    }

    bool Source::BE16(uint16_t& v) { uint8_t b[2]; if (!Read(b, 2)) return false; v = uint16_t(b[0] << 8 | b[1]); return true; }
    bool Source::BE32(uint32_t& v) { uint8_t b[4]; if (!Read(b, 4)) return false; v = uint32_t(b[0]) << 24 | uint32_t(b[1]) << 16 | uint32_t(b[2]) << 8 | b[3]; return true; }
    bool Source::BE64(uint64_t& v) { uint32_t hi, lo; if (!BE32(hi) || !BE32(lo)) return false; v = uint64_t(hi) << 32 | lo; return true; }

    Reducer::Reducer(UINT srcW, UINT srcH, UINT maxW, UINT maxH, UINT ch) : sourceWidth(srcW), sourceHeight(srcH), channels(ch)
    {
        Fit(srcW, srcH, maxW, maxH, false, width, height);
        sums.assign(size_t(width) * height * channels, 0);
        counts.assign(size_t(width) * height * channels, 0);
        columnMap.resize(srcW);
        for (UINT x = 0; x < srcW; x++) columnMap[x] = (std::min)(UINT(uint64_t(x) * width / srcW), width - 1);
    }

    void Reducer::AddRow(UINT channel, UINT y, const uint8_t* samples, UINT stride)
    {
        if (channel >= channels || y >= sourceHeight || !width) return;
        UINT ty = (std::min)(UINT(uint64_t(y) * height / sourceHeight), height - 1);
        size_t base = (size_t(ty) * width) * channels + channel;
        for (UINT x = 0; x < sourceWidth; x++)
        {
            size_t i = base + size_t(columnMap[x]) * channels;
            sums[i] += samples[size_t(x) * stride];
            counts[i]++;
        }
    }

    float Reducer::Value(UINT channel, UINT x, UINT y) const
    {
        size_t i = (size_t(y) * width + x) * channels + channel;
        return counts[i] ? float(sums[i]) / counts[i] : 0.f;
    }

    Image FromRgba(const std::vector<uint8_t>& rgba, UINT w, UINT h, UINT maxW, UINT maxH)
    {
        Image full; full.width = w; full.height = h; full.pixels.resize(size_t(w) * h);
        for (size_t i = 0; i < full.pixels.size(); i++) full.pixels[i] = Pack(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], rgba[i * 4 + 3]);
        UINT tw, th; Fit(w, h, maxW, maxH, false, tw, th);
        return (tw == w && th == h) ? full : Resize(full, tw, th);
    }

    std::vector<uint32_t> Straight(const Image& image)
    {
        std::vector<uint32_t> out(image.pixels.size());
        for (size_t i = 0; i < out.size(); i++)
        {
            uint32_t p = image.pixels[i], a = p >> 24;
            if (a == 0) { out[i] = 0; continue; }
            if (a == 255) { out[i] = p; continue; }
            auto u = [&](int shift) { return (std::min)(255u, (((p >> shift) & 0xFF) * 255 + a / 2) / a); };
            out[i] = (a << 24) | (u(16) << 16) | (u(8) << 8) | u(0);
        }
        return out;
    }

    HRESULT CreateMemoryStream(const std::vector<uint8_t>& data, IStream** stream)
    {
        *stream = SHCreateMemStream(data.empty() ? nullptr : data.data(), UINT(data.size()));
        return *stream ? S_OK : E_OUTOFMEMORY;
    }

    static IWICImagingFactory* Wic()
    {
        static IWICImagingFactory* factory = [] {
            IWICImagingFactory* f = nullptr;
            CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&f));
            return f;
        }();
        return factory;
    }

    HRESULT DecodeWic(IStream* stream, UINT maxW, UINT maxH, Image& image)
    {
        IWICImagingFactory* factory = Wic();
        if (!factory) return E_FAIL;
        LARGE_INTEGER zero{}; stream->Seek(zero, STREAM_SEEK_SET, nullptr);
        IWICBitmapDecoder* decoder = nullptr; IWICBitmapFrameDecode* frame = nullptr; IWICBitmapScaler* scaler = nullptr; IWICFormatConverter* converter = nullptr;
        HRESULT hr = factory->CreateDecoderFromStream(stream, nullptr, WICDecodeMetadataCacheOnDemand, &decoder);
        if (SUCCEEDED(hr)) hr = decoder->GetFrame(0, &frame);
        UINT w = 0, h = 0;
        if (SUCCEEDED(hr)) hr = frame->GetSize(&w, &h);
        if (SUCCEEDED(hr) && (!w || !h)) hr = E_FAIL;
        IWICBitmapSource* current = frame;
        UINT tw = w, th = h;
        if (SUCCEEDED(hr))
        {
            Fit(w, h, maxW, maxH, false, tw, th);
            if (tw != w || th != h)
            {
                hr = factory->CreateBitmapScaler(&scaler);
                if (SUCCEEDED(hr)) hr = scaler->Initialize(frame, tw, th, WICBitmapInterpolationMode(4) /* high-quality cubic */);
                if (FAILED(hr) && scaler) hr = scaler->Initialize(frame, tw, th, WICBitmapInterpolationModeFant);
                current = scaler;
            }
        }
        if (SUCCEEDED(hr)) hr = factory->CreateFormatConverter(&converter);
        if (SUCCEEDED(hr)) hr = converter->Initialize(current, GUID_WICPixelFormat32bppPBGRA, WICBitmapDitherTypeNone, nullptr, 0, WICBitmapPaletteTypeCustom);
        if (SUCCEEDED(hr))
        {
            image.width = tw; image.height = th; image.pixels.resize(size_t(tw) * th);
            hr = converter->CopyPixels(nullptr, tw * 4, UINT(image.pixels.size() * 4), reinterpret_cast<BYTE*>(image.pixels.data()));
        }
        if (converter) converter->Release();
        if (scaler) scaler->Release();
        if (frame) frame->Release();
        if (decoder) decoder->Release();
        return hr;
    }

    HRESULT DecodeWicMemory(const std::vector<uint8_t>& data, UINT maxW, UINT maxH, Image& image)
    {
        IStream* stream = nullptr;
        HRESULT hr = CreateMemoryStream(data, &stream);
        if (SUCCEEDED(hr)) { hr = DecodeWic(stream, maxW, maxH, image); stream->Release(); }
        return hr;
    }

    static std::wstring Lower(std::wstring s) { for (auto& c : s) c = towlower(c); return s; }

    Kind Detect(Source& source, const std::wstring& extensionValue)
    {
        std::wstring ext = Lower(extensionValue);
        uint8_t h[64] = {};
        size_t n = size_t((std::min<uint64_t>)(source.Size(), sizeof h));
        if (!source.ReadAt(0, h, n)) return Kind::Unknown;
        auto starts = [&](const char* sig) { size_t l = strlen(sig); return n >= l && memcmp(h, sig, l) == 0; };
        if (starts("8BPS")) return Kind::Psd;
        if (starts("%PDF")) return Kind::Pdf;
        if (starts("PK\x03\x04")) return Kind::Zip;
        if (starts("RIFF") && n >= 12 && memcmp(h + 8, "CDR", 3) == 0) return Kind::RiffCdr;
        if (starts("qoif")) return Kind::Qoi;
        if (n >= 2 && h[0] == 0x1F && h[1] == 0x8B) return Kind::Svgz;
        if (starts("OTTO") || starts("true") || starts("ttcf") || (n >= 4 && h[0] == 0 && h[1] == 1 && h[2] == 0 && h[3] == 0)) return Kind::Font;
        if (n >= 2 && h[0] == 'P' && h[1] >= '1' && h[1] <= '6' && (ext == L".ppm" || ext == L".pgm" || ext == L".pbm" || ext == L".pnm")) return Kind::Pnm;
        if (ext == L".svg") return Kind::Svg;
        if (ext == L".tga") return Kind::Tga;
        // Text that is an SVG document without the usual extension.
        std::string text(reinterpret_cast<char*>(h), n);
        if (text.find("<svg") != std::string::npos || text.find("<?xml") != std::string::npos) return Kind::Svg;
        return Kind::Wic;
    }

    HRESULT Decode(IStream* stream, const std::wstring& extension, UINT maxW, UINT maxH, Image& image, std::wstring* detail)
    {
        if (!stream || !maxW || !maxH) return E_INVALIDARG;
        Source source(stream);
        if (source.Size() == 0) return E_FAIL;
        Kind kind = Detect(source, extension);
        HRESULT hr = E_FAIL;
        const wchar_t* name = L"unknown";
        try
        {
            switch (kind)
            {
            case Kind::Psd: name = L"psd"; hr = DecodePsd(source, maxW, maxH, image); break;
            case Kind::Pdf: name = L"pdf"; hr = DecodePdf(stream, maxW, maxH, image); break;
            case Kind::Zip: name = L"zip-preview"; hr = DecodeZipPreview(source, maxW, maxH, image); break;
            case Kind::RiffCdr: name = L"cdr-riff"; hr = DecodeRiffCdr(source, maxW, maxH, image); break;
            case Kind::Svg: name = L"svg"; hr = DecodeSvg(stream, maxW, maxH, image); break;
            case Kind::Svgz:
            {
                name = L"svgz";
                std::vector<uint8_t> packed, text;
                if (!source.ReadAll(packed, 64ull << 20) || !Inflate(packed.data(), packed.size(), 2, text, 256u << 20)) { hr = E_FAIL; break; }
                IStream* inner = nullptr; hr = CreateMemoryStream(text, &inner);
                if (SUCCEEDED(hr)) { hr = DecodeSvg(inner, maxW, maxH, image); inner->Release(); }
                break;
            }
            case Kind::Qoi: name = L"qoi"; hr = DecodeQoi(source, maxW, maxH, image); break;
            case Kind::Tga: name = L"tga"; hr = DecodeTga(source, maxW, maxH, image); break;
            case Kind::Pnm: name = L"pnm"; hr = DecodePnm(source, maxW, maxH, image); break;
            case Kind::Font: name = L"font"; hr = DecodeFont(source, maxW, maxH, image); break;
            case Kind::Wic: name = L"wic"; hr = DecodeWic(stream, maxW, maxH, image); break;
            default: break;
            }
        }
        catch (...) { hr = E_UNEXPECTED; }
        if (SUCCEEDED(hr) && (!image.width || !image.height)) hr = E_FAIL;
        if (detail) *detail = name;
        return hr;
    }
}
