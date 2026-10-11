// SVG through Direct2D and font specimens through DirectWrite.
// Copyright (c) Bakhtiyar Jahangirzade. Licensed under the project LICENSE.
#include "Engine.h"
#if __has_include(<d2d1_3.h>)
#include <d2d1_3.h>
#include <dwrite_3.h>
#include <wincodec.h>
#include <algorithm>
#include <cmath>
#include <cstring>
#include <string>

#pragma comment(lib, "d2d1.lib")
#pragma comment(lib, "dwrite.lib")

namespace witb
{
    namespace
    {
        template <class T> struct Ptr
        {
            T* p = nullptr;
            ~Ptr() { if (p) p->Release(); }
            T** operator&() { return &p; }
            T* operator->() const { return p; }
            operator T*() const { return p; }
        };

        ID2D1Factory1* D2D()
        {
            static ID2D1Factory1* factory = [] {
                ID2D1Factory1* f = nullptr;
                D2D1_FACTORY_OPTIONS options{};
                D2D1CreateFactory(D2D1_FACTORY_TYPE_MULTI_THREADED, __uuidof(ID2D1Factory1), &options, reinterpret_cast<void**>(&f));
                return f;
            }();
            return factory;
        }

        // Software render target over a premultiplied WIC bitmap: works in the
        // low-integrity Preview Host and in sessions without a GPU.
        struct Canvas
        {
            Ptr<IWICImagingFactory> wic; Ptr<IWICBitmap> bitmap; Ptr<ID2D1RenderTarget> target;
            UINT width = 0, height = 0;
            HRESULT Create(UINT w, UINT h)
            {
                width = w; height = h;
                HRESULT hr = CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&wic));
                if (SUCCEEDED(hr)) hr = wic->CreateBitmap(w, h, GUID_WICPixelFormat32bppPBGRA, WICBitmapCacheOnLoad, &bitmap);
                if (SUCCEEDED(hr) && !D2D()) hr = E_FAIL;
                D2D1_RENDER_TARGET_PROPERTIES props = D2D1::RenderTargetProperties(D2D1_RENDER_TARGET_TYPE_SOFTWARE, D2D1::PixelFormat(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_PREMULTIPLIED), 96.f, 96.f);
                if (SUCCEEDED(hr)) hr = D2D()->CreateWicBitmapRenderTarget(bitmap, props, &target);
                return hr;
            }
            HRESULT CopyTo(Image& image)
            {
                image.width = width; image.height = height; image.pixels.resize(size_t(width) * height);
                WICRect rect{ 0, 0, INT(width), INT(height) };
                return bitmap->CopyPixels(&rect, width * 4, UINT(image.pixels.size() * 4), reinterpret_cast<BYTE*>(image.pixels.data()));
            }
        };

        double Length(const std::string& text, const char* name)
        {
            // Reads a plain numeric attribute of the root element (px, pt, mm units).
            size_t svg = text.find("<svg");
            if (svg == std::string::npos) return 0;
            size_t close = text.find('>', svg);
            std::string root = text.substr(svg, close == std::string::npos ? std::string::npos : close - svg);
            std::string key = std::string(" ") + name + "=";
            size_t at = root.find(key);
            if (at == std::string::npos) { key[0] = '\n'; at = root.find(key); }
            if (at == std::string::npos) { key[0] = '\t'; at = root.find(key); }
            if (at == std::string::npos) return 0;
            at += key.size();
            if (at >= root.size() || (root[at] != '"' && root[at] != '\'')) return 0;
            char quote = root[at++];
            size_t end = root.find(quote, at);
            std::string value = root.substr(at, end - at);
            char* stop = nullptr; double v = strtod(value.c_str(), &stop);
            std::string unit = stop ? std::string(stop) : "";
            if (unit.find('%') != std::string::npos) return 0;
            if (unit.rfind("pt", 0) == 0) v *= 96.0 / 72; else if (unit.rfind("mm", 0) == 0) v *= 96.0 / 25.4; else if (unit.rfind("cm", 0) == 0) v *= 96.0 / 2.54; else if (unit.rfind("in", 0) == 0) v *= 96.0;
            return v > 0 ? v : 0;
        }

        bool ViewBox(const std::string& text, double& w, double& h)
        {
            size_t svg = text.find("<svg");
            if (svg == std::string::npos) return false;
            size_t close = text.find('>', svg);
            std::string root = text.substr(svg, close == std::string::npos ? std::string::npos : close - svg);
            size_t at = root.find("viewBox=");
            if (at == std::string::npos) return false;
            at += 8; if (at >= root.size()) return false;
            char quote = root[at++];
            std::string value = root.substr(at, root.find(quote, at) - at);
            for (auto& c : value) if (c == ',') c = ' ';
            double v[4]; const char* at2 = value.c_str();
            for (int i = 0; i < 4; i++) { char* stop = nullptr; v[i] = strtod(at2, &stop); if (stop == at2) return false; at2 = stop; }
            w = v[2]; h = v[3];
            return w > 0 && h > 0;
        }
    }

    HRESULT DecodeSvg(IStream* stream, UINT maxW, UINT maxH, Image& image)
    {
        Source source(stream);
        std::vector<uint8_t> data;
        if (!source.ReadAll(data, 64ull << 20)) return E_FAIL;
        std::string text(data.begin(), data.end());
        double vw = 0, vh = 0, w = Length(text, "width"), h = Length(text, "height");
        bool hasViewBox = ViewBox(text, vw, vh);
        if (!(w > 0 && h > 0)) { if (hasViewBox) { w = vw; h = vh; } else { w = 300; h = 150; } }
        UINT tw, th; Fit(w, h, maxW, maxH, true, tw, th);
        Canvas canvas;
        HRESULT hr = canvas.Create(tw, th);
        Ptr<ID2D1DeviceContext5> context;
        if (SUCCEEDED(hr)) hr = canvas.target->QueryInterface(IID_PPV_ARGS(&context));
        Ptr<ID2D1SvgDocument> document;
        IStream* copy = nullptr;
        if (SUCCEEDED(hr)) hr = CreateMemoryStream(data, &copy);
        if (SUCCEEDED(hr)) hr = context->CreateSvgDocument(copy, D2D1::SizeF(float(tw), float(th)), &document);
        if (copy) copy->Release();
        if (SUCCEEDED(hr))
        {
            // Scale the drawing to the target: make the root fill the viewport and
            // give it a viewBox when the document only declares a size.
            Ptr<ID2D1SvgElement> root;
            document->GetRoot(&root);
            if (root)
            {
                if (!hasViewBox) { D2D1_SVG_VIEWBOX box{ 0, 0, float(w), float(h) }; root->SetAttributeValue(L"viewBox", D2D1_SVG_ATTRIBUTE_POD_TYPE_VIEWBOX, &box, sizeof box); }
                D2D1_SVG_LENGTH full{ 100.f, D2D1_SVG_LENGTH_UNITS_PERCENTAGE };
                root->SetAttributeValue(L"width", D2D1_SVG_ATTRIBUTE_POD_TYPE_LENGTH, &full, sizeof full);
                root->SetAttributeValue(L"height", D2D1_SVG_ATTRIBUTE_POD_TYPE_LENGTH, &full, sizeof full);
            }
            context->BeginDraw();
            context->Clear(D2D1::ColorF(0, 0, 0, 0));
            context->DrawSvgDocument(document);
            hr = context->EndDraw();
        }
        if (SUCCEEDED(hr)) hr = canvas.CopyTo(image);
        return hr;
    }

    HRESULT DecodeFont(Source& source, UINT maxW, UINT maxH, Image& image)
    {
        std::vector<uint8_t> data;
        if (!source.ReadAll(data, 64ull << 20)) return E_FAIL;
        Ptr<IDWriteFactory5> dwrite;
        HRESULT hr = DWriteCreateFactory(DWRITE_FACTORY_TYPE_ISOLATED, __uuidof(IDWriteFactory5), reinterpret_cast<IUnknown**>(&dwrite));
        Ptr<IDWriteInMemoryFontFileLoader> loader; Ptr<IDWriteFontFile> file; Ptr<IDWriteFontSetBuilder1> builder; Ptr<IDWriteFontSet> set; Ptr<IDWriteFontCollection1> collection;
        if (SUCCEEDED(hr)) hr = dwrite->CreateInMemoryFontFileLoader(&loader);
        if (SUCCEEDED(hr)) hr = dwrite->RegisterFontFileLoader(loader);
        if (SUCCEEDED(hr)) hr = loader->CreateInMemoryFontFileReference(dwrite, data.data(), UINT32(data.size()), nullptr, &file);
        if (SUCCEEDED(hr)) hr = dwrite->CreateFontSetBuilder(&builder);
        if (SUCCEEDED(hr)) hr = builder->AddFontFile(file);
        if (SUCCEEDED(hr)) hr = builder->CreateFontSet(&set);
        if (SUCCEEDED(hr)) hr = dwrite->CreateFontCollectionFromFontSet(set, &collection);
        std::wstring family = L"";
        if (SUCCEEDED(hr))
        {
            Ptr<IDWriteFontFamily1> fam;
            if (collection->GetFontFamilyCount() == 0) hr = E_FAIL;
            else if (SUCCEEDED(collection->GetFontFamily(0, &fam)))
            {
                Ptr<IDWriteLocalizedStrings> names;
                if (SUCCEEDED(fam->GetFamilyNames(&names)) && names->GetCount())
                {
                    UINT32 len = 0; names->GetStringLength(0, &len);
                    family.resize(len + 1); names->GetString(0, &family[0], len + 1); family.resize(len);
                }
            }
        }
        // Specimen laid out on a 4:3 sheet, then reduced to the target box.
        const UINT sheetW = 1200, sheetH = 900;
        UINT tw, th; Fit(sheetW, sheetH, maxW, maxH, true, tw, th);
        float scale = float(tw) / sheetW;
        Canvas canvas;
        if (SUCCEEDED(hr)) hr = canvas.Create(tw, th);
        if (SUCCEEDED(hr))
        {
            Ptr<ID2D1SolidColorBrush> ink, muted;
            canvas.target->CreateSolidColorBrush(D2D1::ColorF(0.11f, 0.11f, 0.12f), &ink);
            canvas.target->CreateSolidColorBrush(D2D1::ColorF(0.43f, 0.43f, 0.45f), &muted);
            canvas.target->BeginDraw();
            canvas.target->Clear(D2D1::ColorF(1, 1, 1, 1));
            canvas.target->SetTransform(D2D1::Matrix3x2F::Scale(scale, scale));
            struct Line { const wchar_t* text; float size; } lines[] = {
                { L"ABCDEFGHIJKLMNOPQRSTUVWXYZ", 40 }, { L"abcdefghijklmnopqrstuvwxyz", 40 }, { L"0123456789 .,;:!?&@()", 40 },
                { L"The quick brown fox jumps over the lazy dog.", 28 }, { L"Aa Bb Cc", 96 } };
            float y = 40;
            Ptr<IDWriteTextFormat> label;
            dwrite->CreateTextFormat(L"Segoe UI", nullptr, DWRITE_FONT_WEIGHT_SEMI_BOLD, DWRITE_FONT_STYLE_NORMAL, DWRITE_FONT_STRETCH_NORMAL, 26, L"", &label);
            if (label && !family.empty()) { canvas.target->DrawText(family.c_str(), UINT32(family.size()), label, D2D1::RectF(48, y, sheetW - 48.f, y + 40), muted); y += 64; }
            for (auto& line : lines)
            {
                Ptr<IDWriteTextFormat> format;
                if (FAILED(dwrite->CreateTextFormat(family.empty() ? L"Segoe UI" : family.c_str(), collection, DWRITE_FONT_WEIGHT_NORMAL, DWRITE_FONT_STYLE_NORMAL, DWRITE_FONT_STRETCH_NORMAL, line.size, L"", &format))) continue;
                format->SetWordWrapping(DWRITE_WORD_WRAPPING_NO_WRAP);
                canvas.target->DrawText(line.text, UINT32(wcslen(line.text)), format, D2D1::RectF(48, y, sheetW - 48.f, y + line.size * 1.6f), ink, D2D1_DRAW_TEXT_OPTIONS_CLIP);
                y += line.size * 1.5f + 18;
            }
            hr = canvas.target->EndDraw();
        }
        if (SUCCEEDED(hr)) hr = canvas.CopyTo(image);
        if (loader) dwrite->UnregisterFontFileLoader(loader);
        return hr;
    }
}
#else
namespace witb
{
    HRESULT DecodeSvg(IStream*, UINT, UINT, Image&) { return E_NOTIMPL; }
    HRESULT DecodeFont(Source&, UINT, UINT, Image&) { return E_NOTIMPL; }
}
#endif
