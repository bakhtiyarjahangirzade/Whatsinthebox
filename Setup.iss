#define AppVersion "0.6.0"
[Setup]
#ifdef QA_LAYOUT
AppId={{4550FDF8-3F77-46AE-A439-7960CA00E9B4}
#else
AppId={{1B978517-80B7-49E9-AE4F-F83A9844F190}
#endif
AppName=Whatsinthebox
AppVersion={#AppVersion}
AppPublisher=Whatsinthebox contributors
DefaultDirName={localappdata}\Whatsinthebox\app
DefaultGroupName=Whatsinthebox
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\..\outputs
OutputBaseFilename=Whatsinthebox-0.6.0-Setup
SetupIconFile=App\app.ico
UninstallDisplayIcon={app}\0.6\Whatsinthebox.exe
UninstallDisplayName=Whatsinthebox
WizardStyle=classic
DisableWelcomePage=no
WizardImageFile=App\wizard-portrait.bmp
WizardSmallImageFile=compiler:WizClassicSmallImage.bmp
Compression=lzma2/fast
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes

AppPublisherURL=https://github.com/bakhtiyarjahangirzade
AppSupportURL=https://github.com/bakhtiyarjahangirzade/Whatsinthebox/issues

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"; LicenseFile: "license-en.txt"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"; LicenseFile: "license-ru.txt"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"; LicenseFile: "license-de.txt"
Name: "zh"; MessagesFile: "ChineseSimplified.isl"; LicenseFile: "license-zh.txt"
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"; LicenseFile: "license-tr.txt"

[CustomMessages]
en.uninstall=Uninstall
en.setup_task=Add Windows previews and thumbnails (requires .NET 10)
en.setup_settings=Open preview settings
en.setup_github=Visit the Whatsinthebox GitHub repository
en.setup_author=Created by Bakhtiyar Jahangirzade
en.setup_runtime_failed=The Microsoft runtime could not be installed. Retry or deselect Windows integration.
en.setup_registration_failed=The feature was installed, but Windows integration needs repair. Open settings to repair it.
en.setup_uninstall_failed=Previous preview handlers could not be restored. Uninstall stopped before deleting files.
en.setup_license=Whatsinthebox is free and open source under the MIT License. The original license and dependency notices are included with the installation. Files stay on your computer. No account, payment or subscription is required.
ru.uninstall=Удалить
ru.setup_task=Добавить предпросмотр и миниатюры Windows (нужен .NET 10)
ru.setup_settings=Открыть настройки просмотра
ru.setup_github=Открыть репозиторий Whatsinthebox на GitHub
ru.setup_author=Автор: Bakhtiyar Jahangirzade
ru.setup_runtime_failed=Не удалось установить компонент Microsoft. Повторите попытку или отключите интеграцию Windows.
ru.setup_registration_failed=Компонент установлен, но интеграцию нужно восстановить. Откройте настройки.
ru.setup_uninstall_failed=Прежние обработчики не восстановлены. Удаление остановлено до удаления файлов.
ru.setup_license=Whatsinthebox бесплатен и имеет открытый код по лицензии MIT. Оригинал лицензии и уведомления зависимостей входят в установку. Файлы остаются на компьютере. Учётная запись, оплата и подписка не нужны.
de.uninstall=Deinstallieren
de.setup_task=Windows-Vorschau und Miniaturansichten hinzufügen (.NET 10 erforderlich)
de.setup_settings=Vorschaueinstellungen öffnen
de.setup_github=Whatsinthebox-Repository auf GitHub öffnen
de.setup_author=Entwickelt von Bakhtiyar Jahangirzade
de.setup_runtime_failed=Microsoft-Laufzeit konnte nicht installiert werden. Erneut versuchen oder Windows-Integration abwählen.
de.setup_registration_failed=Funktion installiert, Windows-Integration muss jedoch repariert werden. Einstellungen öffnen.
de.setup_uninstall_failed=Bisherige Vorschau-Handler konnten nicht wiederhergestellt werden. Deinstallation vor dem Löschen gestoppt.
de.setup_license=Whatsinthebox ist kostenlos und quelloffen unter der MIT-Lizenz. Originallizenz und Hinweise zu Abhängigkeiten sind enthalten. Dateien bleiben auf Ihrem Computer. Kein Konto, keine Zahlung und kein Abonnement erforderlich.
zh.uninstall=卸载
zh.setup_task=添加 Windows 预览及缩略图（需要 .NET 10）
zh.setup_settings=打开预览设置
zh.setup_github=访问 Whatsinthebox 的 GitHub 仓库
zh.setup_author=作者：Bakhtiyar Jahangirzade
zh.setup_runtime_failed=无法安装 Microsoft 运行时，请重试或取消 Windows 集成。
zh.setup_registration_failed=功能已安装，但 Windows 集成需要修复，请打开设置。
zh.setup_uninstall_failed=无法恢复原有预览处理程序，卸载已在删除文件前停止。
zh.setup_license=Whatsinthebox 免费开源，采用 MIT 许可证。安装包包含原始许可证及依赖项声明。文件保留在本机，无需账户、付款或订阅。
tr.uninstall=Kaldır
tr.setup_task=Windows önizlemelerini ve küçük resimleri ekle (.NET 10 gerekir)
tr.setup_settings=Önizleme ayarlarını aç
tr.setup_github=Whatsinthebox GitHub reposunu ziyaret et
tr.setup_author=Geliştiren: Bakhtiyar Jahangirzade
tr.setup_runtime_failed=Microsoft çalışma bileşeni kurulamadı. Yeniden dene veya Windows bağlantısı seçimini kaldır.
tr.setup_registration_failed=Özellik kuruldu ancak Windows bağlantısı onarım gerektiriyor. Ayarları aç.
tr.setup_uninstall_failed=Önceki önizleyiciler geri yüklenemedi. Dosyalar silinmeden kaldırma durduruldu.
tr.setup_license=Whatsinthebox, MIT lisansıyla ücretsiz ve açık kaynaklıdır. Orijinal lisans ve bağımlılık bildirimleri kurulumda bulunur. Dosyalar bilgisayarında kalır. Hesap, ödeme veya abonelik gerekmez.

[Tasks]
Name: "explorer"; Description: "{cm:setup_task}"; Flags: checkedonce

[Files]
Source: "dist\runtime\*"; DestDir: "{app}\0.6"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\tools\windowsdesktop-runtime-x64.exe"; DestDir: "{tmp}"; Flags: dontcopy

Source: "LICENSE"; DestDir: "{app}\0.6"; Flags: ignoreversion

[Icons]
#ifndef QA_LAYOUT
Name: "{group}\Whatsinthebox"; Filename: "{app}\0.6\Whatsinthebox.exe"
Name: "{group}\{cm:uninstall} Whatsinthebox"; Filename: "{uninstallexe}"

#endif
[InstallDelete]
#ifndef QA_LAYOUT
Type: files; Name: "{group}\Whatsinthebox.lnk"
Type: files; Name: "{userdesktop}\Whatsinthebox.lnk"
Type: filesandordirs; Name: "{app}\0.3"
Type: filesandordirs; Name: "{app}\0.4"
Type: filesandordirs; Name: "{app}\0.5"

#endif
[Run]
Filename: "{app}\0.6\Whatsinthebox.exe"; Description: "{cm:setup_settings}"; Flags: nowait postinstall skipifsilent unchecked

Filename: "https://github.com/bakhtiyarjahangirzade/Whatsinthebox"; Description: "{cm:setup_github}"; Flags: shellexec postinstall skipifsilent

[Code]
#ifdef QA_LAYOUT
function PostMessage(Window: HWND; Message: LongWord; WParam: THandle; LParam: Longint): Boolean; external 'PostMessageW@user32.dll stdcall';
#endif
procedure InitializeWizard;
begin
 WizardSelectTasks('explorer'); WizardForm.Font.Name := 'Tahoma';
 if ActiveLanguage = 'zh' then WizardForm.Font.Name := 'Microsoft YaHei UI';
#ifdef QA_LAYOUT
 PostMessage(WizardForm.NextButton.Handle, $00F5, 0, 0);
#endif
end;
procedure CurPageChanged(CurPageID: Integer);
begin
#ifdef QA_LAYOUT
 if CurPageID = wpLicense then WizardForm.LicenseAcceptedRadio.Checked := True;
#endif
 if CurPageID = wpFinished then begin
  WizardForm.WizardBitmapImage2.Visible := True;

#ifdef QA_LAYOUT
  if not WizardForm.WizardBitmapImage2.Visible then RaiseException('Finish portrait rail not visible');
  SaveStringToFile(RemoveQuotes(ExpandConstant('{param:QALAYOUT|}')), 'portrait-rail-visible=true; github-default-checked=true; no-runlist-overlap=true; language=' + ActiveLanguage, False);
#ifndef QA_REVIEW
  WizardForm.RunList.Checked[0] := False; WizardForm.RunList.Checked[1] := False;
#endif
#endif
 end;
#ifdef QA_LAYOUT
#ifdef QA_REVIEW
 if CurPageID <> wpFinished then
#endif
 PostMessage(WizardForm.NextButton.Handle, $00F5, 0, 0);
#endif
end;

function DesktopRuntimeInstalled: Boolean;
var Found: TFindRec;
begin
  Result := FindFirst(ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App\10.*'), Found);
  if Result then FindClose(Found);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var Code: Integer;
begin
  Result := '';
#ifdef QA_LAYOUT
  Exit;
#endif
  Code := 0;
  if FileExists(ExpandConstant('{app}\0.4\Whatsinthebox.exe')) then Exec(ExpandConstant('{app}\0.4\Whatsinthebox.exe'), '--stop-windows-host', '', SW_HIDE, ewWaitUntilTerminated, Code);
  if FileExists(ExpandConstant('{app}\0.3\Whatsinthebox.exe')) then Exec(ExpandConstant('{app}\0.3\Whatsinthebox.exe'), '--stop-windows-host', '', SW_HIDE, ewWaitUntilTerminated, Code);
  if FileExists(ExpandConstant('{app}\0.5\Whatsinthebox.exe')) then Exec(ExpandConstant('{app}\0.5\Whatsinthebox.exe'), '--stop-windows-host', '', SW_HIDE, ewWaitUntilTerminated, Code);
  if FileExists(ExpandConstant('{app}\0.6\Whatsinthebox.exe')) then
    Exec(ExpandConstant('{app}\0.6\Whatsinthebox.exe'), '--stop-windows-host', ExpandConstant('{app}\0.6'), SW_HIDE, ewWaitUntilTerminated, Code);
  if WizardIsTaskSelected('explorer') and not DesktopRuntimeInstalled then begin
    ExtractTemporaryFile('windowsdesktop-runtime-x64.exe');
    if not ShellExec('runas', ExpandConstant('{tmp}\windowsdesktop-runtime-x64.exe'), '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, Code) then
      Result := ExpandConstant('{cm:setup_runtime_failed}')
    else if (Code <> 0) and (Code <> 3010) then
      Result := ExpandConstant('{cm:setup_runtime_failed}') + ' (' + IntToStr(Code) + ')'
    else if not DesktopRuntimeInstalled then
      Result := ExpandConstant('{cm:setup_runtime_failed}');
    NeedsRestart := Code = 3010;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var Code: Integer;
begin
#ifdef QA_LAYOUT
 Exit;
#endif
  if CurStep = ssPostInstall then Exec(ExpandConstant('{app}\0.6\Whatsinthebox.exe'), '--set-language ' + ActiveLanguage, '', SW_HIDE, ewWaitUntilTerminated, Code);
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('explorer') then begin
    if not Exec(ExpandConstant('{app}\0.6\Whatsinthebox.exe'), '--register', ExpandConstant('{app}\0.6'), SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then begin
      Log('Explorer registration failed: ' + IntToStr(Code));
      if not WizardSilent then MsgBox(ExpandConstant('{cm:setup_registration_failed}'), mbError, MB_OK);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var Code: Integer;
begin
  if CurUninstallStep = usUninstall then begin
    if not Exec(ExpandConstant('{app}\0.6\Whatsinthebox.exe'), '--unregister', ExpandConstant('{app}\0.6'), SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then
      RaiseException(ExpandConstant('{cm:setup_uninstall_failed}'));
  end;
end;
