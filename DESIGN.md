# Whatsinthebox visual and behavior contract

Windows feature with a small maintenance panel; no main file workspace. Original macOS-inspired pearlescent square icon, translucent blue open box and lens. No Apple icon, logo or SF font is distributed.

Canvas #F5F5F7, surfaces #FFFFFF, text #1D1D1F, secondary #6E6E73, accent #007AFF, borders #D2D2D7. Inter (OFL) for Latin/Cyrillic; Microsoft YaHei UI fallback for Simplified Chinese. Native Windows title bars and accessibility remain.

Rounded controls with visible keyboard focus. The preview toolbar wraps in narrow panels. Fit uses a single uniform scale, letterboxing and pixel rounding. Transparency backgrounds are checkerboard, white or dark. Never stretch content to fill a square.

English, Russian, German, Chinese and Turkish catalogs have identical keys. Installer language is persisted. Language choices remain in their native names. Application-owned dialog buttons use the selected catalog; OS/runtime consent dialogs use their own system language.

Setup completion shows an author portrait and an optional initially checked repository link. Silent install opens no browser. Repair, uninstall and repository are maintenance actions; rendering stays in Explorer.

Version 0.5: shared primary ink #182336; borderless compact maintenance window with rounded white surfaces, no oversized status table. The website uses the blue paper-and-box composition, one download action, and matching Inter type. Finish-page portrait owns the left rail; Inno controls retain the right rail. Update states reserve their geometry and never block previews.


## Installer classic variant — 0.5.1
The owner explicitly selected the classic Windows/Inno Setup wizard reference: blue illustrated left rail, white content pane, native Back/Next/Cancel/Finish buttons, no rounded marketing cards. Setup.iss is the canonical owner of this variant. Uses Inno Setup's bundled classic wizard assets and Tahoma; Chinese keeps Microsoft YaHei UI. Welcome page is visible. Completion keeps the blue rail with the authorized portrait and a muted author caption in the right pane; the native RunList stays in the right pane. The exact original GitHub repository action is checked by default, user-uncheckable, and skipifsilent. App and website keep their existing visual identity.
Native Inno controls own navigation, progress, focus, checkbox states and scrollbars. All installer catalog descriptions use the selected language. Platform consent dialogs retain system-owned behavior.


## Portrait installer and font specimens — 0.6
Setup.iss owns the classic installer variant; App/wizard-portrait.bmp fills the 164x314 native left rail on welcome/completion. No blue installer artwork or separate overlaid portrait remains. Native navigation and default-checked optional original GitHub action remain. FontPreview.cs owns TTF/OTF specimen layout, within the bounded rendering worker: white 1200x1500 canvas, metadata heading, sampled sizes, supported-character rows and localized no-install message. No font is installed. Build output is dist/runtime; published artifacts exclude symbols and local build paths. Public download website uses GitHub Pages.
Fit scales small source images up to the available viewport with one uniform scale. Thumbnails fill the requested maximum edge without cropping or stretching. PDF download permission is a per-file Explorer action with a localized app confirmation defaulting to No. Only the selected PDF Zone.Identifier is removed after consent; no automatic unblocking or global policy changes. Completion explains that Windows may require a new sign-in.
Installer maintenance flow: a first install uses the normal wizard. Existing installations show installed/package versions in the selected language: older supports Update or Repair and update, equal supports Repair, newer blocks downgrade. A disabled Windows integration stays deselected. Same-version repairs use a fresh numbered version folder; no loaded binaries are overwritten. SetupMutex prevents concurrent setup in one session. Own helper shutdown waits at most 15 seconds and failure blocks changes. Restart Manager does not close Explorer or unrelated apps, and installation does not perform a bulk thumbnail-cache refresh.

## Website comparison
The marketing hero shows one Document.pdf before and after in equal-size panels. Before is explicitly an illustration of no working preview handler; after uses the actual 1.0 native Windows test capture. Do not imply every Windows machine lacks PDF support. All comparison labels follow the five-language catalog. Image geometry uses contain with no cropping or stretching. Existing blue/gray website tokens and Inter typography remain canonical.

Comparison sample updated to the user-supplied Azerbaijan Airlines SVG. After is the actual windowless renderer output, not an Explorer screenshot. Captions follow that provenance; do not describe it as a PDF. Preserve its source ratio with contain. Original private files are not bundled with the app.
