# Contributing

Use .NET 10 and Visual Studio C++ build tools on Windows x64. The Explorer thumbnail provider and preview handler in `src/Native` are C++ compiled with the Windows SDK; the settings application uses .NET. Open `Whatsinthebox.slnx`, or run:

```powershell
./scripts/build.ps1
./scripts/check.ps1
```

`src/Native` contains the Explorer handlers and their decoders; `src` also contains the settings application and shared UI. `Whatsinthebox.Render.exe <file> <out.png> <size>` renders any file through the same native engine. `tests` contains geometry checks and Windows integration tests. `installer` contains the Inno Setup wizard. `website` is a static Vercel site. Build products go into ignored `artifacts`.

To compile an installer, install Inno Setup 6.7 or later:

```powershell
./scripts/build.ps1 -Installer
```

The application includes its runtime. The installer does not install a separate system runtime or need administrator access. Never add personal documents, machine images, credentials or diagnostic screenshots of private files to the repository.

Renderer checks run without opening a window or changing shell registrations. Real Explorer, installer and uninstall checks belong in a disposable Windows test machine. See [release verification](docs/verification.md). A passing command-line suite does not establish shell stability.
