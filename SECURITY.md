# Security

Report suspected vulnerabilities through GitHub private vulnerability reporting when available. Do not publish exploit details, credentials or private sample files in an issue.

There is currently no approved 1.0 release. A Windows shell stability report is under investigation; release checks are tracked in [docs/verification.md](docs/verification.md).

Native decoders handle complex, untrusted file formats. A separate process and input/time limits reduce impact, but do not provide a complete operating-system sandbox. Keep Windows and the .NET Desktop Runtime updated. A clean advisory scan does not establish that a build has no vulnerabilities.

Preview files stay local. Update checks send only ordinary HTTPS requests for public GitHub release metadata. The optional per-file PDF permission action removes a download mark only after explicit confirmation; it does not change global Windows security policy.

Dependency license and copyright texts are retained in `NOTICE.txt`. No credentials, personal documents or machine images belong in this repository.
