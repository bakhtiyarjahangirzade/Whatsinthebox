# Release verification — 1.1.0 (candidate)

1.1.0 replaces the 1.0 Explorer integration. 1.0 forwarded Explorer's requests from a small native bridge to a background .NET server; on real Explorer the low-integrity Preview Host never reached that server, so the preview pane stayed blank ([trace run](https://github.com/bakhtiyarjahangirzade/Whatsinthebox/actions/runs/38147300391)). 1.1.0 ships `Whatsinthebox.Preview.dll`, a native thumbnail provider and preview handler that Windows loads in its own isolated hosts. Nothing runs in the background or at sign-in.

Status: candidate. The checks below passed on disposable GitHub Windows machines. No release has been published from this source yet.

| Check | Windows Server 2025 | Windows Server 2022 |
|---|---|---|
| Native engine: every fixture decoded at 256 and 1024 px, aspect kept, box filled | Passed | Passed |
| **Real File Explorer windows, preview pane**: PSD, SVG, CDR, ORA, TGA, PPM drawn by Whatsinthebox inside Preview Host, fitted to the pane | [Passed](https://github.com/bakhtiyarjahangirzade/Whatsinthebox/actions/runs/38153137293) | [Passed](https://github.com/bakhtiyarjahangirzade/Whatsinthebox/actions/runs/38153793019), PDF as well |
| Real File Explorer keeps Windows' own preview for PNG, JPEG, BMP, GIF, TIFF, WebP and fonts | Passed | Passed |
| **Folder view thumbnails** as an ordinary account: Shell folder view in thumbnail mode, each registered format drawn by Whatsinthebox through the Windows thumbnail cache and isolation surrogate | Passed (7 formats) | Passed (7 formats) |
| Shell association and thumbnail pipeline per format, ordinary account (53 checks) | Passed | Passed |
| Preview Host activation of the registered class, thumbnails at 256 and 32 px | Passed | Passed |
| Installer journeys: five languages, repair, missing-file repair, disabled integration, upgrade from published 0.6.0 and from 1.0.0 with the old DLL held loaded, downgrade refusal, removal restoring previous handlers and preferences, no sign-in helper (119 assertions) | Passed | Passed |
| Application or Shell crash events during installer journeys | None | None |
| Signing | Not signed | Not signed |

Formats are registered only where the PC has no working handler of its own. On Windows Server 2025 the Microsoft Edge PDF previewer was present, so the PDF preview stayed with Edge while PDF thumbnails came from Whatsinthebox.

## How the real Explorer checks run

The GitHub Windows desktop belongs to the runner's built-in administrator, whose Explorer runs elevated. Windows ignores per-user extensions in elevated processes, and Explorer opens no windows for a second account on that desktop. The checks therefore run twice on the same desktop:

1. File Explorer windows started with a medium-integrity token derived from that account select each file (`explorer /select`), show the preview pane and capture the screen. The handler writes a small record of what it drew when a test flag file sits next to it.
2. The ordinary test account hosts the Shell folder view (the same view control File Explorer uses) in thumbnail mode and requires every registered format to be drawn by Whatsinthebox.

Server editions share the Windows 11 Explorer, but this is not a test on Windows 10/11 client editions or on real user PCs. Decoders were exercised with 16,500 randomly damaged files without a crash (under Wine); this is not a complete fuzzing campaign.

## Limits

- Per-user installation: on accounts whose Explorer runs elevated (UAC off, built-in Administrator) Windows does not load per-user extensions.
- Windows can block downloaded files from the preview pane. The optional per-file action removes a PDF's download mark only after confirmation.
- Password-protected or damaged files, PSD files saved without a composite image and CDR files without an embedded preview are not shown.
- The settings application still contains the 1.0 .NET renderer for its own window; Explorer does not load it.

The 1.0.1 report remains in the repository history.
