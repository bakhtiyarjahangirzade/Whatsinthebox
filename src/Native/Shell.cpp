// Whatsinthebox Explorer integration: thumbnail provider and preview handler.
// Copyright (c) Bakhtiyar Jahangirzade. Licensed under the project LICENSE.
//
// Both classes are ordinary in-process Shell extensions. Windows loads them in its
// own isolated hosts (thumbnail surrogate, Preview Host), the documented model
// also used by the Windows SDK samples and PowerToys. Nothing keeps running when
// Explorer is not asking for a preview.
#include "Engine.h"
#include <shlobj.h>
#include <shobjidl.h>
#include <thumbcache.h>
#include <propsys.h>
#include <shlwapi.h>
#include <wincodec.h>
#include <new>
#include <string>
#include <cstdio>

#pragma comment(lib, "msimg32.lib")

namespace
{
    // {BF6AA7C4-61E5-49A2-9F54-BB00C2594CD7}
    const CLSID PreviewClass = { 0xbf6aa7c4, 0x61e5, 0x49a2, { 0x9f, 0x54, 0xbb, 0x00, 0xc2, 0x59, 0x4c, 0xd7 } };
    // {62323550-708A-40C9-95A8-0D7056D4F690}
    const CLSID ThumbnailClass = { 0x62323550, 0x708a, 0x40c9, { 0x95, 0xa8, 0x0d, 0x70, 0x56, 0xd4, 0xf6, 0x90 } };

    HMODULE module = nullptr;
    LONG objects = 0;
    const UINT PreviewDecodeEdge = 1600;

    std::wstring Extension(const wchar_t* name)
    {
        if (!name) return L"";
        const wchar_t* dot = wcsrchr(name, L'.');
        const wchar_t* slash = wcsrchr(name, L'\\');
        return (dot && (!slash || dot > slash)) ? std::wstring(dot) : L"";
    }

    std::wstring BaseName(const wchar_t* name)
    {
        if (!name) return L"";
        const wchar_t* slash = wcsrchr(name, L'\\');
        return slash ? slash + 1 : name;
    }

    // Integration tests place test-capture.flag next to this DLL; then each render
    // is recorded in LocalLow, which every Shell host may write to.
    bool CaptureEnabled()
    {
        wchar_t path[MAX_PATH];
        if (!GetModuleFileNameW(module, path, MAX_PATH)) return false;
        PathRemoveFileSpecW(path);
        PathAppendW(path, L"test-capture.flag");
        return GetFileAttributesW(path) != INVALID_FILE_ATTRIBUTES;
    }

    void Record(const wchar_t* kind, const std::wstring& name, HRESULT hr, const std::wstring& decoder, UINT requestW, UINT requestH, UINT w, UINT h, const witb::Image* image)
    {
        if (!CaptureEnabled()) return;
        PWSTR low = nullptr;
        if (FAILED(SHGetKnownFolderPath(FOLDERID_LocalAppDataLow, 0, nullptr, &low))) return;
        std::wstring folder = std::wstring(low) + L"\\Whatsinthebox";
        CoTaskMemFree(low);
        CreateDirectoryW(folder.c_str(), nullptr);
        folder += L"\\capture";
        CreateDirectoryW(folder.c_str(), nullptr);
        std::wstring stem = folder + L"\\" + kind + L"-" + name;
        wchar_t json[1024];
        swprintf(json, 1024, L"{\"kind\":\"%ls\",\"source\":\"%ls\",\"hresult\":\"0x%08X\",\"decoder\":\"%ls\",\"requestWidth\":%u,\"requestHeight\":%u,\"width\":%u,\"height\":%u,\"process\":%lu}",
            kind, name.c_str(), unsigned(hr), decoder.c_str(), requestW, requestH, w, h, GetCurrentProcessId());
        HANDLE file = CreateFileW((stem + L".json").c_str(), GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (file != INVALID_HANDLE_VALUE)
        {
            int bytes = WideCharToMultiByte(CP_UTF8, 0, json, -1, nullptr, 0, nullptr, nullptr);
            std::string utf8(bytes > 0 ? bytes - 1 : 0, '\0');
            if (bytes > 1) WideCharToMultiByte(CP_UTF8, 0, json, -1, &utf8[0], bytes, nullptr, nullptr);
            DWORD written = 0; WriteFile(file, utf8.data(), DWORD(utf8.size()), &written, nullptr); CloseHandle(file);
        }
        if (!image || image->pixels.empty()) return;
        std::vector<uint32_t> straight = witb::Straight(*image);
        IWICImagingFactory* factory = nullptr; IWICStream* out = nullptr; IWICBitmapEncoder* encoder = nullptr; IWICBitmapFrameEncode* frame = nullptr;
        if (SUCCEEDED(CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&factory))) && SUCCEEDED(factory->CreateStream(&out)) &&
            SUCCEEDED(out->InitializeFromFilename((stem + L".png").c_str(), GENERIC_WRITE)) && SUCCEEDED(factory->CreateEncoder(GUID_ContainerFormatPng, nullptr, &encoder)) &&
            SUCCEEDED(encoder->Initialize(out, WICBitmapEncoderNoCache)) && SUCCEEDED(encoder->CreateNewFrame(&frame, nullptr)) && SUCCEEDED(frame->Initialize(nullptr)))
        {
            WICPixelFormatGUID format = GUID_WICPixelFormat32bppBGRA;
            frame->SetSize(image->width, image->height); frame->SetPixelFormat(&format);
            frame->WritePixels(image->height, image->width * 4, UINT(image->pixels.size() * 4), (BYTE*)straight.data());
            frame->Commit(); encoder->Commit();
        }
        if (frame) frame->Release();
        if (encoder) encoder->Release();
        if (out) out->Release();
        if (factory) factory->Release();
    }

    HBITMAP ToBitmap(const witb::Image& image)
    {
        BITMAPINFO info{};
        info.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        info.bmiHeader.biWidth = LONG(image.width);
        info.bmiHeader.biHeight = -LONG(image.height);
        info.bmiHeader.biPlanes = 1; info.bmiHeader.biBitCount = 32; info.bmiHeader.biCompression = BI_RGB;
        void* bits = nullptr;
        HBITMAP bitmap = CreateDIBSection(nullptr, &info, DIB_RGB_COLORS, &bits, nullptr, 0);
        if (bitmap && bits) memcpy(bits, image.pixels.data(), image.pixels.size() * 4);
        return bitmap;
    }

    // ---------------------------------------------------------------- thumbnails
    class Thumbnail final : public IThumbnailProvider, public IInitializeWithStream
    {
        LONG references = 1; IStream* stream = nullptr; std::wstring name;
    public:
        Thumbnail() { InterlockedIncrement(&objects); }
        ~Thumbnail() { if (stream) stream->Release(); InterlockedDecrement(&objects); }
        IFACEMETHODIMP QueryInterface(REFIID iid, void** value) override
        {
            static const QITAB table[] = { QITABENT(Thumbnail, IThumbnailProvider), QITABENT(Thumbnail, IInitializeWithStream), { nullptr, 0 } };
            return QISearch(this, table, iid, value);
        }
        IFACEMETHODIMP_(ULONG) AddRef() override { return InterlockedIncrement(&references); }
        IFACEMETHODIMP_(ULONG) Release() override { ULONG n = InterlockedDecrement(&references); if (!n) delete this; return n; }
        IFACEMETHODIMP Initialize(IStream* value, DWORD) override
        {
            if (!value) return E_INVALIDARG;
            if (stream) return HRESULT_FROM_WIN32(ERROR_ALREADY_INITIALIZED);
            stream = value; stream->AddRef();
            STATSTG stat{};
            if (SUCCEEDED(stream->Stat(&stat, STATFLAG_DEFAULT)) && stat.pwcsName) { name = stat.pwcsName; CoTaskMemFree(stat.pwcsName); }
            return S_OK;
        }
        IFACEMETHODIMP GetThumbnail(UINT size, HBITMAP* bitmap, WTS_ALPHATYPE* alpha) override
        {
            if (!bitmap || !alpha) return E_POINTER;
            *bitmap = nullptr; *alpha = WTSAT_UNKNOWN;
            if (!stream || !size) return E_UNEXPECTED;
            witb::Image image; std::wstring decoder;
            HRESULT hr = witb::Decode(stream, Extension(name.c_str()), size, size, image, &decoder);
            if (SUCCEEDED(hr))
            {
                // Fill the requested square along the longer side, keeping the ratio.
                UINT w, h; witb::Fit(image.width, image.height, size, size, true, w, h);
                if (w != image.width || h != image.height) image = witb::Resize(image, w, h);
                *bitmap = ToBitmap(image);
                *alpha = image.HasAlpha() ? WTSAT_ARGB : WTSAT_RGB;
                if (!*bitmap) hr = E_OUTOFMEMORY;
            }
            Record(L"thumbnail", BaseName(name.c_str()), hr, decoder, size, size, image.width, image.height, nullptr);
            return hr;
        }
    };

    // ------------------------------------------------------------ preview pane
    const wchar_t* WindowClass = L"WhatsintheboxPreviewWindow";

    const wchar_t* FailureText()
    {
        switch (PRIMARYLANGID(GetUserDefaultUILanguage()))
        {
        case LANG_TURKISH: return L"Bu dosya önizlenemedi.";
        case LANG_RUSSIAN: return L"Не удалось показать этот файл.";
        case LANG_GERMAN: return L"Diese Datei kann nicht angezeigt werden.";
        case LANG_CHINESE: return L"无法预览此文件。";
        default: return L"This file can't be previewed.";
        }
    }

    class Preview final : public IPreviewHandler, public IInitializeWithStream, public IInitializeWithFile, public IObjectWithSite, public IOleWindow, public IPreviewHandlerVisuals
    {
        LONG references = 1;
        IStream* stream = nullptr; IUnknown* site = nullptr;
        std::wstring name;
        HWND parent = nullptr, window = nullptr;
        RECT bounds{};
        witb::Image image; HRESULT result = S_FALSE; bool decoded = false;
        HBITMAP scaled = nullptr; UINT scaledW = 0, scaledH = 0;
        COLORREF background = GetSysColor(COLOR_WINDOW), text = GetSysColor(COLOR_WINDOWTEXT);
        HFONT font = nullptr;

        static LRESULT CALLBACK Procedure(HWND hwnd, UINT message, WPARAM wp, LPARAM lp)
        {
            auto* self = reinterpret_cast<Preview*>(GetWindowLongPtrW(hwnd, GWLP_USERDATA));
            if (message == WM_NCCREATE) { self = static_cast<Preview*>(reinterpret_cast<CREATESTRUCTW*>(lp)->lpCreateParams); SetWindowLongPtrW(hwnd, GWLP_USERDATA, LONG_PTR(self)); }
            if (self)
            {
                if (message == WM_PAINT) { self->Paint(hwnd); return 0; }
                if (message == WM_ERASEBKGND) return 1;
                if (message == WM_SIZE) { self->Rescale(); InvalidateRect(hwnd, nullptr, FALSE); return 0; }
                if (message == WM_NCDESTROY) SetWindowLongPtrW(hwnd, GWLP_USERDATA, 0);
            }
            return DefWindowProcW(hwnd, message, wp, lp);
        }

        static void Register()
        {
            WNDCLASSEXW wc{ sizeof wc };
            if (GetClassInfoExW(module, WindowClass, &wc)) return;
            wc = WNDCLASSEXW{ sizeof wc };
            wc.lpfnWndProc = Procedure; wc.hInstance = module; wc.lpszClassName = WindowClass;
            wc.hCursor = LoadCursorW(nullptr, IDC_ARROW);
            RegisterClassExW(&wc);
        }

        RECT ImageRect() const
        {
            RECT client{}; if (window) GetClientRect(window, &client);
            const int margin = 8;
            UINT w, h;
            int availableW = client.right - 2 * margin, availableH = client.bottom - 2 * margin - (client.bottom >= 160 ? 18 : 0);
            if (availableW < 1 || availableH < 1 || !image.width) return RECT{};
            witb::Fit(image.width, image.height, UINT(availableW), UINT(availableH), true, w, h);
            int x = margin + (availableW - int(w)) / 2, y = margin + (availableH - int(h)) / 2;
            return RECT{ x, y, x + int(w), y + int(h) };
        }

        void Rescale()
        {
            if (scaled) { DeleteObject(scaled); scaled = nullptr; }
            RECT r = ImageRect();
            UINT w = UINT(r.right - r.left), h = UINT(r.bottom - r.top);
            if (!w || !h || !image.width) return;
            witb::Image fitted = witb::Resize(image, w, h);
            scaled = ToBitmap(fitted); scaledW = w; scaledH = h;
        }

        void Paint(HWND hwnd)
        {
            PAINTSTRUCT ps; HDC dc = BeginPaint(hwnd, &ps);
            RECT client; GetClientRect(hwnd, &client);
            HDC memory = CreateCompatibleDC(dc);
            HBITMAP canvas = CreateCompatibleBitmap(dc, (std::max)(1L, client.right), (std::max)(1L, client.bottom));
            HGDIOBJ old = SelectObject(memory, canvas);
            HBRUSH brush = CreateSolidBrush(background); FillRect(memory, &client, brush); DeleteObject(brush);
            SetBkMode(memory, TRANSPARENT);
            HGDIOBJ oldFont = font ? SelectObject(memory, font) : nullptr;
            if (scaled)
            {
                RECT r = ImageRect();
                HDC source = CreateCompatibleDC(dc); HGDIOBJ previous = SelectObject(source, scaled);
                BLENDFUNCTION blend{ AC_SRC_OVER, 0, 255, AC_SRC_ALPHA };
                AlphaBlend(memory, r.left, r.top, int(scaledW), int(scaledH), source, 0, 0, int(scaledW), int(scaledH), blend);
                SelectObject(source, previous); DeleteDC(source);
            }
            else if (decoded && FAILED(result))
            {
                ::SetTextColor(memory, text);
                RECT r = client; InflateRect(&r, -16, -16);
                DrawTextW(memory, FailureText(), -1, &r, DT_CENTER | DT_VCENTER | DT_WORDBREAK);
            }
            if (client.bottom >= 160 && decoded)
            {
                // Quiet attribution, as in an image caption.
                ::SetTextColor(memory, RGB((GetRValue(background) * 2 + GetRValue(text)) / 3, (GetGValue(background) * 2 + GetGValue(text)) / 3, (GetBValue(background) * 2 + GetBValue(text)) / 3));
                RECT r{ 8, client.bottom - 20, client.right - 10, client.bottom - 2 };
                DrawTextW(memory, L"Whatsinthebox", -1, &r, DT_RIGHT | DT_SINGLELINE | DT_BOTTOM);
            }
            if (oldFont) SelectObject(memory, oldFont);
            BitBlt(dc, 0, 0, client.right, client.bottom, memory, 0, 0, SRCCOPY);
            SelectObject(memory, old); DeleteObject(canvas); DeleteDC(memory);
            EndPaint(hwnd, &ps);
        }

    public:
        Preview() { InterlockedIncrement(&objects); }
        ~Preview() { Unload(); if (site) site->Release(); if (font) DeleteObject(font); InterlockedDecrement(&objects); }
        IFACEMETHODIMP QueryInterface(REFIID iid, void** value) override
        {
            static const QITAB table[] = {
                QITABENT(Preview, IPreviewHandler), QITABENT(Preview, IInitializeWithStream), QITABENT(Preview, IInitializeWithFile),
                QITABENT(Preview, IObjectWithSite), QITABENT(Preview, IOleWindow), QITABENT(Preview, IPreviewHandlerVisuals), { nullptr, 0 } };
            return QISearch(this, table, iid, value);
        }
        IFACEMETHODIMP_(ULONG) AddRef() override { return InterlockedIncrement(&references); }
        IFACEMETHODIMP_(ULONG) Release() override { ULONG n = InterlockedDecrement(&references); if (!n) delete this; return n; }

        IFACEMETHODIMP Initialize(IStream* value, DWORD) override
        {
            if (!value) return E_INVALIDARG;
            if (stream) stream->Release();
            stream = value; stream->AddRef();
            STATSTG stat{};
            if (SUCCEEDED(stream->Stat(&stat, STATFLAG_DEFAULT)) && stat.pwcsName) { name = stat.pwcsName; CoTaskMemFree(stat.pwcsName); }
            return S_OK;
        }
        IFACEMETHODIMP Initialize(LPCWSTR path, DWORD) override
        {
            if (!path) return E_INVALIDARG;
            IStream* file = nullptr;
            HRESULT hr = SHCreateStreamOnFileEx(path, STGM_READ | STGM_SHARE_DENY_NONE, 0, FALSE, nullptr, &file);
            if (FAILED(hr)) return hr;
            if (stream) stream->Release();
            stream = file; name = path;
            return S_OK;
        }

        IFACEMETHODIMP SetWindow(HWND hwnd, const RECT* rect) override
        {
            if (!hwnd || !rect) return E_INVALIDARG;
            parent = hwnd; bounds = *rect;
            if (window) { SetParent(window, parent); SetWindowPos(window, nullptr, bounds.left, bounds.top, bounds.right - bounds.left, bounds.bottom - bounds.top, SWP_NOZORDER | SWP_NOACTIVATE); }
            return S_OK;
        }
        IFACEMETHODIMP SetRect(const RECT* rect) override
        {
            if (!rect) return E_INVALIDARG;
            bounds = *rect;
            if (window) SetWindowPos(window, nullptr, bounds.left, bounds.top, bounds.right - bounds.left, bounds.bottom - bounds.top, SWP_NOZORDER | SWP_NOACTIVATE);
            return S_OK;
        }
        IFACEMETHODIMP DoPreview() override
        {
            if (!parent || !stream) return E_UNEXPECTED;
            if (!window)
            {
                Register();
                window = CreateWindowExW(0, WindowClass, nullptr, WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS, bounds.left, bounds.top, bounds.right - bounds.left, bounds.bottom - bounds.top, parent, nullptr, module, this);
                if (!window) return HRESULT_FROM_WIN32(GetLastError());
            }
            if (!decoded)
            {
                std::wstring decoder;
                result = witb::Decode(stream, Extension(name.c_str()), PreviewDecodeEdge, PreviewDecodeEdge, image, &decoder);
                decoded = true;
                Rescale();
                Record(L"preview", BaseName(name.c_str()), result, decoder, UINT(bounds.right - bounds.left), UINT(bounds.bottom - bounds.top), scaledW, scaledH, nullptr);
            }
            InvalidateRect(window, nullptr, FALSE);
            return S_OK;
        }
        IFACEMETHODIMP Unload() override
        {
            if (window) { DestroyWindow(window); window = nullptr; }
            if (scaled) { DeleteObject(scaled); scaled = nullptr; }
            if (stream) { stream->Release(); stream = nullptr; }
            image = witb::Image(); decoded = false; result = S_FALSE;
            return S_OK;
        }
        IFACEMETHODIMP SetFocus() override { if (window) ::SetFocus(window); return S_OK; }
        IFACEMETHODIMP QueryFocus(HWND* hwnd) override { if (!hwnd) return E_INVALIDARG; *hwnd = GetFocus(); return *hwnd ? S_OK : HRESULT_FROM_WIN32(GetLastError()); }
        IFACEMETHODIMP TranslateAccelerator(MSG* message) override
        {
            HRESULT hr = S_FALSE; IPreviewHandlerFrame* frame = nullptr;
            if (site && SUCCEEDED(site->QueryInterface(IID_PPV_ARGS(&frame)))) { hr = frame->TranslateAccelerator(message); frame->Release(); }
            return hr;
        }

        IFACEMETHODIMP SetSite(IUnknown* value) override { if (site) { site->Release(); site = nullptr; } if (value) { site = value; site->AddRef(); } return S_OK; }
        IFACEMETHODIMP GetSite(REFIID iid, void** value) override { if (!value) return E_POINTER; *value = nullptr; return site ? site->QueryInterface(iid, value) : E_FAIL; }
        IFACEMETHODIMP GetWindow(HWND* hwnd) override { if (!hwnd) return E_INVALIDARG; *hwnd = window ? window : parent; return S_OK; }
        IFACEMETHODIMP ContextSensitiveHelp(BOOL) override { return E_NOTIMPL; }

        IFACEMETHODIMP SetBackgroundColor(COLORREF color) override { background = color; if (window) InvalidateRect(window, nullptr, FALSE); return S_OK; }
        IFACEMETHODIMP SetFont(const LOGFONTW* value) override { if (!value) return E_INVALIDARG; if (font) DeleteObject(font); LOGFONTW small = *value; font = CreateFontIndirectW(&small); return S_OK; }
        IFACEMETHODIMP SetTextColor(COLORREF color) override { text = color; return S_OK; }
    };

    template <class T> class Factory final : public IClassFactory
    {
        LONG references = 1;
    public:
        Factory() { InterlockedIncrement(&objects); }
        ~Factory() { InterlockedDecrement(&objects); }
        IFACEMETHODIMP QueryInterface(REFIID iid, void** value) override
        {
            static const QITAB table[] = { QITABENT(Factory, IClassFactory), { nullptr, 0 } };
            return QISearch(this, table, iid, value);
        }
        IFACEMETHODIMP_(ULONG) AddRef() override { return InterlockedIncrement(&references); }
        IFACEMETHODIMP_(ULONG) Release() override { ULONG n = InterlockedDecrement(&references); if (!n) delete this; return n; }
        IFACEMETHODIMP CreateInstance(IUnknown* outer, REFIID iid, void** value) override
        {
            if (!value) return E_POINTER;
            *value = nullptr;
            if (outer) return CLASS_E_NOAGGREGATION;
            T* object = new (std::nothrow) T();
            if (!object) return E_OUTOFMEMORY;
            HRESULT hr = object->QueryInterface(iid, value);
            object->Release();
            return hr;
        }
        IFACEMETHODIMP LockServer(BOOL lock) override { if (lock) InterlockedIncrement(&objects); else InterlockedDecrement(&objects); return S_OK; }
    };
}

BOOL APIENTRY DllMain(HMODULE handle, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH) { module = handle; DisableThreadLibraryCalls(handle); }
    return TRUE;
}

STDAPI DllGetClassObject(REFCLSID clsid, REFIID iid, void** value)
{
    if (!value) return E_POINTER;
    *value = nullptr;
    IClassFactory* factory = nullptr;
    if (IsEqualCLSID(clsid, PreviewClass)) factory = new (std::nothrow) Factory<Preview>();
    else if (IsEqualCLSID(clsid, ThumbnailClass)) factory = new (std::nothrow) Factory<Thumbnail>();
    else return CLASS_E_CLASSNOTAVAILABLE;
    if (!factory) return E_OUTOFMEMORY;
    HRESULT hr = factory->QueryInterface(iid, value);
    factory->Release();
    return hr;
}

STDAPI DllCanUnloadNow() { return objects == 0 ? S_OK : S_FALSE; }
