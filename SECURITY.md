# Security policy

This is beta software. Do not treat a successful scan as proof of zero vulnerabilities. The renderer is a separate bounded process, not a full OS sandbox.

For security issues, use the repository's private vulnerability reporting feature if available. Do not post exploit details or private files in public issues. Supported fixes target the latest beta version.

No telemetry, input uploads or network rendering are implemented. Version 0.5 adds an HTTPS request to the public GitHub releases API for update checks; it sends no preview files or account credentials. Response size and time are bounded, and download links are restricted to this repository on github.com. The optional installer finish action and settings link open the public GitHub repository in your browser.

Native decoders and the installed .NET runtime must be kept current. See docs/SECURITY-CHECKS.md for checks performed on this release and their limits.
