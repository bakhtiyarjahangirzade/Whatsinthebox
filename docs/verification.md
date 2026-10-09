# Release verification

The 1.0 release is **not approved yet**. A reported Explorer/Start-menu incident resolved after disabling the application's shell integration. The event log pointed to `SystemTray.dll`; causation remains unconfirmed. No 1.0 installer has been published.

| Check | Latest evidence | Status |
|---|---|---|
| Application and shell build | Release compilation | Passed |
| Renderer, format, localization and update suite | 60 windowless checks | Passed |
| Geometry | 17 checks and 1,000 deterministic cases | Passed |
| Private PDF compatibility | 16 pages across nine files, plus a synthetic header-offset case | Passed locally; private inputs excluded |
| Dependency advisories | NuGet audit with transitive dependencies | No advisories reported at the time of the check |
| Installer compilation | Five-language maintenance wizard | Passed |
| Helper shutdown | Missing, responsive and unresponsive helper cases | Passed in isolated process tests |
| Real Explorer thumbnail and preview integration | Current candidate; VM verification attempt incomplete | Blocked |
| Clean install, upgrade, repair, downgrade rejection, removal | Current candidate | Pending |
| Start menu and taskbar stability after install/removal | Current candidate | Pending |
| Installer signing and download verification | Release candidate | Pending |

The latest isolated Windows attempt returned a successful installer exit code, but did not complete the shared-UI report. A diagnostic retry also stalled while the VM logged repeated timing catch-up failures. These runs do not establish an application root cause or count as passed UI/lifecycle tests. The test machine must provide a reliable result before release approval.

## Required Windows scenarios

Use a disposable Windows x64 machine, with snapshots before changes. Test a clean install in all five languages; old-version update; same-version repair; refusal to install over a newer version; preservation of a disabled integration; removal and restoration of previous handlers. Repeat installation without duplicate startup helpers. Exercise original-aspect thumbnails, narrow/wide preview panes, large inputs, malformed inputs, font specimens, PDF page navigation and explicit download-permission cancellation/acceptance.

Check the Start menu, taskbar and Explorer before and after each lifecycle operation. Record application event logs, return codes and actual screenshots from the test machine. A crash, stale registration, unexpected application shutdown or incomplete rollback blocks release.

## Boundaries

Files are rendered locally. Native decoders run in a separate process with size/time limits; this is not a complete operating-system sandbox. Downloaded PDFs may be blocked by Windows before invoking a handler. The optional per-file action requires consent before removing a download mark. No global security policy is weakened automatically.

Passing scans are evidence about the checked build, not a guarantee of zero vulnerabilities or universal file-format compatibility.
