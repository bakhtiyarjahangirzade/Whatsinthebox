# Release verification — 1.0.0

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