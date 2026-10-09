# Release verification — 1.0.1

Exact application source: `9b58150`. Installer SHA256: `26b7efbe0f5817350afe194a55e24af9d52122444ff2a84d7b28eed2e181bd22`. The published 74,434,270-byte installer is the successful Windows 2025 artifact, not a later rebuild. It remains unsigned.

Both [Windows 2025](https://github.com/bakhtiyarjahangirzade/Whatsinthebox/actions/runs/37990214725) and [Windows 2022](https://github.com/bakhtiyarjahangirzade/Whatsinthebox/actions/runs/37990217849) passed **73 application/UI checks** and **114 installer assertions**. The checks include native preview/thumbnail calls after upgrading with the old DLL still loaded, common image preview preservation, automatic Windows preference configuration and exact preference restoration on removal.

Changes include new class identities to avoid stale factories, PNG XML metadata detection, safe handling of legacy SVG DOCTYPE declarations, smaller fit margins and consistent setup version comparison. Common native Windows image previews remain in place. Only the small native thumbnail forwarding factory opts out of the inaccessible system surrogate; decoding and managed UI remain in the separate application. This is not a full OS sandbox.

Installation and repair run one bounded background pass over existing cached thumbnails in Downloads, Desktop and Documents, including up to two subfolder levels. Enumeration is limited to 512 files per root and skips network, offline, cloud recall and reparse entries. It checks a time/cancellation budget and never clears the global cache or removes file security marks. New files use normal Windows thumbnail requests.

Local PDF inputs use seekable reads up to the library's 4 GiB Windows limit. Named local Shell streams avoid copying the entire input. Anonymous streams remain limited to 256 MiB. A synthetic 300 MB PDF regression fixture passes. Rendering child processes have a 768 MiB process-memory cap and are killed when their owning job closes; UI/thumbnail waits retain their time limits.

Windows can block downloaded files before invoking any full preview handler. No global security policy is changed and download marks are not removed automatically. Damaged or protected documents and practical resource limits remain exceptions; rendering success is not a claim that Windows will allow every file into its preview pane. A dedicated interactive Windows 11 desktop regression remains incomplete.

## Previous 1.0.0 evidence


The published installer is the exact artifact produced and exercised by the Windows verification workflow. Application source: `6804f3daa67ae2090059b7afb7f07735309dc6e8`. Documentation and website publishing do not change that binary.

| Check | Evidence | Result |
|---|---|---|
| Build and dependency audit | [Build checks](https://github.com/bakhtiyarjahangirzade/Whatsinthebox/actions/runs/37920737180) | Passed; no NuGet advisories reported |
| Renderer, formats, locales and stable update selection | 60 windowless checks | Passed |
| UI user flows | 70 combined checks: PDF navigation, failure recovery, rapid selection, consent and actual fit painting | Passed on both environments |
| Geometry | 17 checks and 1,000 deterministic asymmetric cases | Passed |
| Native Shell integration | Native bridge activation, file/stream COM initialization, actual Windows thumbnail cache, tiny thumbnails, PDF preview and targeted refresh | Passed |
| Repeated use | Four native Shell passes per environment | Passed |
| Installer lifecycle | 77 assertions: five languages, repair, missing executable recovery, disabled integration, upgrade from published 0.6, downgrade refusal and removal | Passed on both environments |
| Setup interface | Actual completion screenshots in EN/RU/DE/ZH/TR; portrait rail and checked repository option | Passed |
| Registration restoration | Previous handler restored; owned classes and startup entry removed | Passed |
| Crash event monitoring | Application/Shell crash events during the installer journeys | None reported |
| Signing | Authenticode | Not signed; SHA256 supplied with the release |

## Reproducible evidence

- [Windows Server 2025 verification](https://github.com/bakhtiyarjahangirzade/Whatsinthebox/actions/runs/37920736770)
- [Windows Server 2022 verification](https://github.com/bakhtiyarjahangirzade/Whatsinthebox/actions/runs/37920740412)

Both runs used ordinary user accounts in disposable GitHub Windows machines, synthetic files and real Windows COM/thumbnail APIs. Their `windows-lifecycle-evidence` artifacts include reports, application event results and actual UI screenshots. No private user documents or local lab files were uploaded. The downloadable installer is taken from the successful 2025 run, not rebuilt afterward.

## Scope and limits

These are automated user-flow and developer checks. They are not a human usability study, an exhaustive decoder fuzz campaign or a long-running soak test. A dedicated Windows 11 interactive Start-menu/taskbar regression was not completed; the local VM was unreliable. The earlier workstation incident in `SystemTray.dll` remains an unconfirmed root cause. The 1.0 design keeps the CLR and managed preview work outside Explorer through a small native bridge.

Windows x64 is the supported application architecture; ARM64 Explorer cannot load the x64 bridge. The installer rejects other architectures and Windows builds older than 14393. Keep the operating system supported and updated; see [Microsoft's runtime support policy](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md).

Files render locally. Native decoding has size/time limits and runs separately, but this is not a complete operating-system sandbox. Windows may block downloaded PDFs before invoking any preview handler. The per-file permission action defaults to No and requires explicit consent before removing that file's download mark. No global security policy is weakened.

The installer does not close Explorer or restart Windows, and does not bulk-refresh thumbnail caches. A clean advisory scan and passing tests are evidence about this build, not a promise of zero vulnerabilities or universal format compatibility. The installer is unsigned; a checksum identifies the artifact and is not a publisher signature.