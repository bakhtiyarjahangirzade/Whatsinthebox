# Contributing

Use .NET 10 and Visual Studio C++ build tools on Windows x64. The native Shell bridge is compiled with the Windows SDK; the application and renderer use .NET. Open `Whatsinthebox.slnx`, or run:

```powershell
./scripts/build.ps1
./scripts/check.ps1
```

`src` contains the application, shell bridge and shared UI. `tests` contains geometry checks and Windows integration tests. `installer` contains the Inno Setup wizard. `website` is a static Vercel site. Build products go into ignored `artifacts`.

To compile an installer, install Inno Setup 6.7 or later:

```powershell
./scripts/build.ps1 -Installer
```

The application includes its runtime. The installer does not install a separate system runtime or need administrator access. Never add personal documents, machine images, credentials or diagnostic screenshots of private files to the repository.

Renderer checks run without opening a window or changing shell registrations. Real Explorer, installer and uninstall checks belong in a disposable Windows test machine. See [release verification](docs/verification.md). A passing command-line suite does not establish shell stability.
