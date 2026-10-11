// Command-line front end for the native engine, used by the build checks:
//   Whatsinthebox.Render.exe <input> <output.png> <edge>
// Prints one JSON line with the decoder used, the result and the image size.
// Copyright (c) Bakhtiyar Jahangirzade. Licensed under the project LICENSE.
#include "Engine.h"
#include <shlwapi.h>
#include <wincodec.h>
#include <cstdio>
#include <cwchar>

#pragma comment(lib, "ole32.lib")

static HRESULT SavePng(const witb::Image& image, const wchar_t* path)
{
    std::vector<uint32_t> straight = witb::Straight(image);
    IWICImagingFactory* factory = nullptr; IWICStream* out = nullptr; IWICBitmapEncoder* encoder = nullptr; IWICBitmapFrameEncode* frame = nullptr;
    HRESULT hr = CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&factory));
    if (SUCCEEDED(hr)) hr = factory->CreateStream(&out);
    if (SUCCEEDED(hr)) hr = out->InitializeFromFilename(path, GENERIC_WRITE);
    if (SUCCEEDED(hr)) hr = factory->CreateEncoder(GUID_ContainerFormatPng, nullptr, &encoder);
    if (SUCCEEDED(hr)) hr = encoder->Initialize(out, WICBitmapEncoderNoCache);
    if (SUCCEEDED(hr)) hr = encoder->CreateNewFrame(&frame, nullptr);
    if (SUCCEEDED(hr)) hr = frame->Initialize(nullptr);
    WICPixelFormatGUID format = GUID_WICPixelFormat32bppBGRA;
    if (SUCCEEDED(hr)) hr = frame->SetSize(image.width, image.height);
    if (SUCCEEDED(hr)) hr = frame->SetPixelFormat(&format);
    if (SUCCEEDED(hr)) hr = frame->WritePixels(image.height, image.width * 4, UINT(image.pixels.size() * 4), (BYTE*)straight.data());
    if (SUCCEEDED(hr)) hr = frame->Commit();
    if (SUCCEEDED(hr)) hr = encoder->Commit();
    if (frame) frame->Release();
    if (encoder) encoder->Release();
    if (out) out->Release();
    if (factory) factory->Release();
    return hr;
}

int wmain(int argc, wchar_t** argv)
{
    if (argc < 4) { fwprintf(stderr, L"usage: Whatsinthebox.Render <input> <output.png> <edge>\n"); return 2; }
    CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    UINT edge = UINT(_wtoi(argv[3]));
    IStream* stream = nullptr;
    HRESULT hr = SHCreateStreamOnFileEx(argv[1], STGM_READ | STGM_SHARE_DENY_NONE, 0, FALSE, nullptr, &stream);
    witb::Image image; std::wstring decoder;
    LARGE_INTEGER start, end, frequency; QueryPerformanceFrequency(&frequency); QueryPerformanceCounter(&start);
    const wchar_t* dot = wcsrchr(argv[1], L'.');
    if (SUCCEEDED(hr)) hr = witb::Decode(stream, dot ? dot : L"", edge, edge, image, &decoder);
    UINT sourceW = image.width, sourceH = image.height;
    if (SUCCEEDED(hr))
    {
        UINT w, h; witb::Fit(image.width, image.height, edge, edge, true, w, h);
        if (w != image.width || h != image.height) image = witb::Resize(image, w, h);
    }
    QueryPerformanceCounter(&end);
    if (SUCCEEDED(hr)) hr = SavePng(image, argv[2]);
    if (stream) stream->Release();
    wprintf(L"{\"input\":\"%ls\",\"decoder\":\"%ls\",\"hresult\":\"0x%08X\",\"decodedWidth\":%u,\"decodedHeight\":%u,\"width\":%u,\"height\":%u,\"milliseconds\":%.1f}\n",
        PathFindFileNameW(argv[1]), decoder.c_str(), unsigned(hr), sourceW, sourceH, image.width, image.height, double(end.QuadPart - start.QuadPart) * 1000.0 / double(frequency.QuadPart));
    CoUninitialize();
    return SUCCEEDED(hr) ? 0 : 1;
}
