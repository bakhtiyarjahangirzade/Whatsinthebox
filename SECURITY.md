# Security

Report suspected vulnerabilities through GitHub private vulnerability reporting when available. Do not publish exploit details, credentials or private sample files in an issue.

Released installers passed the automated Windows checks documented in [docs/verification.md](docs/verification.md). The installer is unsigned; verify the SHA256 published with the release. A checksum identifies a file and does not replace an Authenticode signature.

The native decoders handle complex, untrusted file formats. Windows runs them in its isolated hosts (Preview Host at low integrity, the thumbnail surrogate), and the decoders validate sizes and offsets and limit input. This reduces impact but is not a guarantee. Keep Windows updated. A clean advisory scan does not establish that a build has no vulnerabilities.

Preview files stay local. Update checks send only ordinary HTTPS requests for public GitHub release metadata. The optional per-file PDF permission action removes a download mark only after explicit confirmation; it does not change global Windows security policy.

Dependency license and copyright texts are retained in `NOTICE.txt`. No credentials, personal documents or machine images belong in this repository.
