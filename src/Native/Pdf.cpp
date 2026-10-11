// PDF (and PDF-compatible Illustrator) first page through the PDF renderer built
// into Windows (Windows.Data.Pdf). No third-party PDF engine is shipped.
// Copyright (c) Bakhtiyar Jahangirzade. Licensed under the project LICENSE.
#include "Engine.h"
#ifdef _MSC_VER
#include <shcore.h>
#include <thread>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Storage.Streams.h>
#include <winrt/Windows.Data.Pdf.h>
#pragma comment(lib, "windowsapp.lib")
#pragma comment(lib, "shcore.lib")

namespace witb
{
    HRESULT DecodePdf(IStream* stream, UINT maxW, UINT maxH, Image& image)
    {
        // The Shell's stream belongs to the calling apartment; copy it once so the
        // Windows PDF renderer can work on its own thread without calling back.
        Source source(stream);
        std::vector<uint8_t> data;
        if (!source.ReadAll(data, 768ull << 20)) return E_OUTOFMEMORY;
        HRESULT result = E_FAIL;
        std::thread worker([&] {
            try
            {
                winrt::init_apartment(winrt::apartment_type::multi_threaded);
                using namespace winrt::Windows::Storage::Streams;
                using namespace winrt::Windows::Data::Pdf;
                InMemoryRandomAccessStream input;
                {
                    DataWriter writer(input);
                    writer.WriteBytes(winrt::array_view<const uint8_t>(data.data(), data.data() + data.size()));
                    writer.StoreAsync().get();
                    writer.DetachStream();
                }
                input.Seek(0);
                PdfDocument document = PdfDocument::LoadFromStreamAsync(input).get();
                if (document.PageCount() == 0) throw winrt::hresult_error(E_FAIL);
                PdfPage page = document.GetPage(0);
                auto size = page.Size();
                UINT tw, th; Fit(size.Width, size.Height, maxW, maxH, true, tw, th);
                PdfPageRenderOptions options;
                options.DestinationWidth(tw); options.DestinationHeight(th);
                InMemoryRandomAccessStream output;
                page.RenderToStreamAsync(output, options).get();
                output.Seek(0);
                IStream* rendered = nullptr;
                winrt::check_hresult(CreateStreamOverRandomAccessStream(winrt::get_unknown(output), IID_PPV_ARGS(&rendered)));
                result = DecodeWic(rendered, tw, th, image);
                rendered->Release();
            }
            catch (winrt::hresult_error const& e) { result = e.code(); }
            catch (...) { result = E_FAIL; }
            winrt::uninit_apartment();
        });
        worker.join();
        return result;
    }
}
#else
namespace witb { HRESULT DecodePdf(IStream*, UINT, UINT, Image&) { return E_NOTIMPL; } }
#endif
