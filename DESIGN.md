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
