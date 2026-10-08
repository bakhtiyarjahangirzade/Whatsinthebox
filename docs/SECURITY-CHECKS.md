# Release security and verification — 0.4.0 beta

Checked on Windows x64, 8 October 2026. This is an engineering check, not an independent audit or certification.

| Check | Result |
|---|---|
| Renderer, hostile SVG, bounded document/archive fixtures, UI, aspect ratios and locale catalogs | 53 passed |
| Native COM file/stream initialization, rendering, resize and unload | 4 passed |
| Out-of-process PDF thumbnail, native Windows Shell thumbnail lookup, out-of-process preview | 3 passed |
| Installer in English, Russian, German, Simplified Chinese and Turkish | Installations returned success |
| NuGet direct and transitive vulnerability query | No known vulnerable package reported by the configured NuGet source |
| Static credential/private-path pattern review of public source | No matching findings |
| Microsoft Defender custom scan of release files | Completed; no matching Whatsinthebox detection reported |
| Setup signing | Unsigned beta |

The Defender signatures available during the check were dated 6 October 2026. A local scan does not substitute for code review or guarantee future safety. The public release includes SHA-256 checksums.

SVG regression cases reject scripts, DTD/entity declarations, external references and unsafe CSS. ImageMagick delegates/remote-active coders are disabled. Document readers have entry/count/size limits and do not execute formulas, scripts or macros. PDF rendering and native image codecs remain attack surfaces; keep dependencies updated.

The renderer is a separate process with bounded preview timeout, not an OS sandbox. Windows may load the small thumbnail adapter into Shell. A crash can leave local temporary previews. No user documents, private account credentials, installer logs or workstation screenshots are included in the public source. The author portrait was regenerated and published with the owner’s explicit request.

The screenshots are actual application/native preview control captures using synthetic or project-owned fixtures. They are not fabricated Explorer mockups. Live Explorer UI capture and comprehensive testing across different PCs were not performed. Automated checks are fixtures, not guarantees of compatibility with every real file. The first public version remains a beta.

See verification.json for the individual automated results. Setup/uninstall lifecycle was also checked locally; a concise result is included in installer-lifecycle.json. Raw registry backups and local installer logs are intentionally excluded.


## 0.5 checks
55 renderer/UI/localization/update-selection checks passed. Newer published beta releases are supported; foreign download URLs are rejected. Five installer completion-page assertions passed: author portrait is visible, the original wizard image is hidden, and portrait bounds do not overlap the run list. The website resolver selects an uploaded setup asset, rejects foreign links, and retains a direct download fallback when API access fails.

Update checks use the public GitHub HTTPS API. No file contents are sent; API requests include a product/version User-Agent. Automatic results are cached for 24 hours; manual checks bypass the cache. No update is executed or installed automatically.


## 0.5.1 classic installer
Classic native Inno wizard with its bundled blue artwork, welcome page, Tahoma and an author portrait on completion. Original GitHub repository action remains optional and checked by default; silent installs open no browser. 55 app checks passed. Isolated installer layout checks completed in all five languages. No new file-rendering behavior is introduced.
