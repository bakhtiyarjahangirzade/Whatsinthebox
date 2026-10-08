#define AppVersion "0.4.0"
[Setup]
AppId={{1B978517-80B7-49E9-AE4F-F83A9844F190}
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
OutputBaseFilename=Whatsinthebox-0.4.0-Setup
SetupIconFile=App\app.ico
UninstallDisplayIcon={app}\0.4\Whatsinthebox.exe
UninstallDisplayName=Whatsinthebox
WizardStyle=modern
WizardImageFile=App\wizard.bmp
WizardSmallImageFile=App\wizard-small.bmp
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
tr.setup_task={cm:setup_task}
tr.setup_settings={cm:setup_settings}
tr.setup_github=Whatsinthebox GitHub reposunu ziyaret et
tr.setup_author=Geliştiren: Bakhtiyar Jahangirzade
tr.setup_runtime_failed=Microsoft çalışma bileşeni kurulamadı. Yeniden dene veya Windows bağlantısı seçimini kaldır.
tr.setup_registration_failed=Özellik kuruldu ancak Windows bağlantısı onarım gerektiriyor. Ayarları aç.
tr.setup_uninstall_failed=Önceki önizleyiciler geri yüklenemedi. Dosyalar silinmeden kaldırma durduruldu.
tr.setup_license=Whatsinthebox, MIT lisansıyla ücretsiz ve açık kaynaklıdır. Orijinal lisans ve bağımlılık bildirimleri kurulumda bulunur. Dosyalar bilgisayarında kalır. Hesap, ödeme veya abonelik gerekmez.

[Tasks]
Name: "explorer"; Description: "{cm:setup_task}"; Flags: checkedonce

[Files]
Source: "release-v4\*"; DestDir: "{app}\0.4"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\tools\windowsdesktop-runtime-x64.exe"; DestDir: "{tmp}"; Flags: dontcopy

Source: "App\author.bmp"; DestDir: "{tmp}"; Flags: dontcopy
Source: "App\Fonts\Inter-Regular.ttf"; DestDir: "{tmp}"; Flags: dontcopy
Source: "LICENSE"; DestDir: "{app}\0.4"; Flags: ignoreversion

[Icons]
Name: "{group}\Whatsinthebox"; Filename: "{app}\0.4\Whatsinthebox.exe"
Name: "{group}\{cm:uninstall} Whatsinthebox"; Filename: "{uninstallexe}"

[InstallDelete]
Type: files; Name: "{group}\Whatsinthebox.lnk"
Type: files; Name: "{userdesktop}\Whatsinthebox.lnk"

[Run]
Filename: "{app}\0.4\Whatsinthebox.exe"; Description: "{cm:setup_settings}"; Flags: nowait postinstall skipifsilent unchecked

Filename: "https://github.com/bakhtiyarjahangirzade/Whatsinthebox"; Description: "{cm:setup_github}"; Flags: shellexec postinstall skipifsilent

[Code]
function AddFontResourceEx(FileName: String; Flags: LongWord; Reserved: Integer): Integer; external 'AddFontResourceExW@gdi32.dll stdcall';
function InitializeSetup: Boolean;
begin
 ExtractTemporaryFile('Inter-Regular.ttf');
 AddFontResourceEx(ExpandConstant('{tmp}\Inter-Regular.ttf'), 16, 0);
 Result := True;
end;

procedure InitializeWizard;
var Portrait: TBitmapImage; Author: TNewStaticText;
begin
 WizardSelectTasks('explorer');
 WizardForm.Font.Name := 'Inter';
 if ActiveLanguage = 'zh' then WizardForm.Font.Name := 'Microsoft YaHei UI';
 ExtractTemporaryFile('author.bmp');
 Portrait := TBitmapImage.Create(WizardForm);
 Portrait.Parent := WizardForm.FinishedPage;
 Portrait.Left := WizardForm.RunList.Left; Portrait.Top := WizardForm.RunList.Top;
 Portrait.Width := ScaleX(64); Portrait.Height := ScaleY(64);
 Portrait.Stretch := True; Portrait.Bitmap.LoadFromFile(ExpandConstant('{tmp}\author.bmp'));
 Author := TNewStaticText.Create(WizardForm); Author.Parent := WizardForm.FinishedPage;
 Author.Left := Portrait.Left + ScaleX(80); Author.Top := Portrait.Top + ScaleY(22);
 Author.Caption := ExpandConstant('{cm:setup_author}'); Author.AutoSize := True;
 WizardForm.RunList.Top := Portrait.Top + ScaleY(76);
 WizardForm.RunList.Height := ScaleY(64);
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
  Code := 0;
  if FileExists(ExpandConstant('{app}\0.3\Whatsinthebox.exe')) then Exec(ExpandConstant('{app}\0.3\Whatsinthebox.exe'), '--stop-windows-host', '', SW_HIDE, ewWaitUntilTerminated, Code);
  if FileExists(ExpandConstant('{app}\0.4\Whatsinthebox.exe')) then
    Exec(ExpandConstant('{app}\0.4\Whatsinthebox.exe'), '--stop-windows-host', ExpandConstant('{app}\0.4'), SW_HIDE, ewWaitUntilTerminated, Code);
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
  if CurStep = ssPostInstall then Exec(ExpandConstant('{app}\0.4\Whatsinthebox.exe'), '--set-language ' + ActiveLanguage, '', SW_HIDE, ewWaitUntilTerminated, Code);
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('explorer') then begin
    if not Exec(ExpandConstant('{app}\0.4\Whatsinthebox.exe'), '--register', ExpandConstant('{app}\0.4'), SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then begin
      Log('Explorer registration failed: ' + IntToStr(Code));
      if not WizardSilent then MsgBox(ExpandConstant('{cm:setup_registration_failed}'), mbError, MB_OK);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var Code: Integer;
begin
  if CurUninstallStep = usUninstall then begin
    if not Exec(ExpandConstant('{app}\0.4\Whatsinthebox.exe'), '--unregister', ExpandConstant('{app}\0.4'), SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then
      RaiseException(ExpandConstant('{cm:setup_uninstall_failed}'));
  end;
end;
