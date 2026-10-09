#ifndef AppVersion
#define AppVersion "1.0.0"
#endif
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
OutputDir=..\artifacts\installer
OutputBaseFilename=Whatsinthebox-{#AppVersion}-Setup
SetupIconFile=..\src\App\app.ico
UninstallDisplayIcon={app}\{code:InstallBin}\Whatsinthebox.exe
UninstallDisplayName=Whatsinthebox
WizardStyle=classic
DisableWelcomePage=no
WizardImageFile=..\src\App\wizard-portrait.bmp
WizardSmallImageFile=compiler:WizClassicSmallImage.bmp
Compression=lzma2/fast
SolidCompression=yes
CloseApplications=no
RestartApplications=no
SetupMutex=Local\Whatsinthebox.Setup
DisableProgramGroupPage=yes

AppPublisherURL=https://github.com/bakhtiyarjahangirzade
AppSupportURL=https://github.com/bakhtiyarjahangirzade/Whatsinthebox/issues

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"; LicenseFile: "..\LICENSE"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"; LicenseFile: "..\LICENSE"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"; LicenseFile: "..\LICENSE"
Name: "zh"; MessagesFile: "ChineseSimplified.isl"; LicenseFile: "..\LICENSE"
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"; LicenseFile: "..\LICENSE"

[CustomMessages]
en.uninstall=Uninstall
en.setup_task=Add Windows previews and thumbnails
en.setup_settings=Open preview settings
en.setup_github=Visit the Whatsinthebox GitHub repository
en.setup_author=Created by Bakhtiyar Jahangirzade
en.setup_runtime_failed=The Microsoft runtime could not be installed. Retry or deselect Windows integration.
en.setup_registration_failed=The feature was installed, but Windows integration needs repair. Open settings to repair it.
en.setup_uninstall_failed=Previous preview handlers could not be restored. Uninstall stopped before deleting files.
en.setup_license=Whatsinthebox is free and open source under the MIT License. The original license and dependency notices are included with the installation. Files stay on your computer. No account, payment or subscription is required.
ru.uninstall=Удалить
ru.setup_task=Добавить предпросмотр и миниатюры Windows
ru.setup_settings=Открыть настройки просмотра
ru.setup_github=Открыть репозиторий Whatsinthebox на GitHub
ru.setup_author=Автор: Bakhtiyar Jahangirzade
ru.setup_runtime_failed=Не удалось установить компонент Microsoft. Повторите попытку или отключите интеграцию Windows.
ru.setup_registration_failed=Компонент установлен, но интеграцию нужно восстановить. Откройте настройки.
ru.setup_uninstall_failed=Прежние обработчики не восстановлены. Удаление остановлено до удаления файлов.
ru.setup_license=Whatsinthebox бесплатен и имеет открытый код по лицензии MIT. Оригинал лицензии и уведомления зависимостей входят в установку. Файлы остаются на компьютере. Учётная запись, оплата и подписка не нужны.
de.uninstall=Deinstallieren
de.setup_task=Windows-Vorschau und Miniaturansichten hinzufügen
de.setup_settings=Vorschaueinstellungen öffnen
de.setup_github=Whatsinthebox-Repository auf GitHub öffnen
de.setup_author=Entwickelt von Bakhtiyar Jahangirzade
de.setup_runtime_failed=Microsoft-Laufzeit konnte nicht installiert werden. Erneut versuchen oder Windows-Integration abwählen.
de.setup_registration_failed=Funktion installiert, Windows-Integration muss jedoch repariert werden. Einstellungen öffnen.
de.setup_uninstall_failed=Bisherige Vorschau-Handler konnten nicht wiederhergestellt werden. Deinstallation vor dem Löschen gestoppt.
de.setup_license=Whatsinthebox ist kostenlos und quelloffen unter der MIT-Lizenz. Originallizenz und Hinweise zu Abhängigkeiten sind enthalten. Dateien bleiben auf Ihrem Computer. Kein Konto, keine Zahlung und kein Abonnement erforderlich.
zh.uninstall=卸载
zh.setup_task=添加 Windows 预览及缩略图
zh.setup_settings=打开预览设置
zh.setup_github=访问 Whatsinthebox 的 GitHub 仓库
zh.setup_author=作者：Bakhtiyar Jahangirzade
zh.setup_runtime_failed=无法安装 Microsoft 运行时，请重试或取消 Windows 集成。
zh.setup_registration_failed=功能已安装，但 Windows 集成需要修复，请打开设置。
zh.setup_uninstall_failed=无法恢复原有预览处理程序，卸载已在删除文件前停止。
zh.setup_license=Whatsinthebox 免费开源，采用 MIT 许可证。安装包包含原始许可证及依赖项声明。文件保留在本机，无需账户、付款或订阅。
tr.uninstall=Kaldır
tr.setup_task=Windows önizlemelerini ve küçük resimleri ekle
tr.setup_settings=Önizleme ayarlarını aç
tr.setup_github=Whatsinthebox GitHub reposunu ziyaret et
tr.setup_author=Geliştiren: Bakhtiyar Jahangirzade
tr.setup_runtime_failed=Microsoft çalışma bileşeni kurulamadı. Yeniden dene veya Windows bağlantısı seçimini kaldır.
tr.setup_registration_failed=Özellik kuruldu ancak Windows bağlantısı onarım gerektiriyor. Ayarları aç.
tr.setup_uninstall_failed=Önceki önizleyiciler geri yüklenemedi. Dosyalar silinmeden kaldırma durduruldu.
tr.setup_license=Whatsinthebox, MIT lisansıyla ücretsiz ve açık kaynaklıdır. Orijinal lisans ve bağımlılık bildirimleri kurulumda bulunur. Dosyalar bilgisayarında kalır. Hesap, ödeme veya abonelik gerekmez.

en.setup_existing=Whatsinthebox is already installed
en.setup_versions=Installed: %1   •   Setup: %2
en.setup_choose=Choose how to continue. Your files and language settings are preserved.
en.setup_update=Update — keep current settings
en.setup_repair_update=Repair and update — restore Windows integration
en.setup_repair=Repair this version
en.setup_newer=A newer version is already installed. Close this setup and use the current version.
en.setup_stop_failed=The preview helper could not be stopped safely. No application files were replaced. Close open previews and try again.
ru.setup_existing=Whatsinthebox уже установлен
ru.setup_versions=Установлено: %1   •   Установка: %2
ru.setup_choose=Выберите действие. Ваши файлы и настройки языка сохранятся.
ru.setup_update=Обновить — сохранить настройки
ru.setup_repair_update=Восстановить и обновить — восстановить интеграцию Windows
ru.setup_repair=Восстановить эту версию
ru.setup_newer=Уже установлена более новая версия. Закройте эту установку и используйте текущую версию.
ru.setup_stop_failed=Не удалось безопасно остановить службу предпросмотра. Файлы приложения не заменены. Закройте предпросмотр и повторите попытку.
de.setup_existing=Whatsinthebox ist bereits installiert
de.setup_versions=Installiert: %1   •   Setup: %2
de.setup_choose=Wählen Sie eine Aktion. Ihre Dateien und Spracheinstellungen bleiben erhalten.
de.setup_update=Aktualisieren — Einstellungen beibehalten
de.setup_repair_update=Reparieren und aktualisieren — Windows-Integration wiederherstellen
de.setup_repair=Diese Version reparieren
de.setup_newer=Eine neuere Version ist bereits installiert. Schließen Sie dieses Setup und verwenden Sie die aktuelle Version.
de.setup_stop_failed=Der Vorschauhelfer konnte nicht sicher beendet werden. Anwendungsdateien wurden nicht ersetzt. Schließen Sie offene Vorschauen und versuchen Sie es erneut.
zh.setup_existing=Whatsinthebox 已安装
zh.setup_versions=已安装：%1   •   安装包：%2
zh.setup_choose=请选择操作。您的文件和语言设置将保留。
zh.setup_update=更新 — 保留当前设置
zh.setup_repair_update=修复并更新 — 恢复 Windows 集成
zh.setup_repair=修复此版本
zh.setup_newer=已安装更新版本。请关闭此安装程序并使用当前版本。
zh.setup_stop_failed=无法安全停止预览辅助进程。应用文件未被替换。请关闭打开的预览后重试。
tr.setup_existing=Whatsinthebox zaten kurulu
tr.setup_versions=Kurulu: %1   •   Setup: %2
tr.setup_choose=Nasıl devam edileceğini seç. Dosyaların ve dil ayarların korunur.
tr.setup_update=Güncelle — mevcut ayarları koru
tr.setup_repair_update=Onar ve güncelle — Windows bağlantısını yeniden kur
tr.setup_repair=Bu sürümü onar
tr.setup_newer=Daha yeni bir sürüm zaten kurulu. Bu setupı kapat ve mevcut sürümü kullan.
tr.setup_stop_failed=Önizleme yardımcısı güvenli şekilde durdurulamadı. Uygulama dosyaları değiştirilmedi. Açık önizlemeleri kapatıp yeniden dene.

[Tasks]
Name: "explorer"; Description: "{cm:setup_task}"; Flags: checkedonce

[Files]
Source: "..\artifacts\app\*"; DestDir: "{app}\{code:InstallBin}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "StopHelper.ps1"; DestDir: "{tmp}"; Flags: dontcopy



[Icons]
#ifndef QA_LAYOUT
Name: "{group}\Whatsinthebox"; Filename: "{app}\{code:InstallBin}\Whatsinthebox.exe"
Name: "{group}\{cm:uninstall} Whatsinthebox"; Filename: "{uninstallexe}"

#endif
[InstallDelete]
#ifndef QA_LAYOUT
Type: files; Name: "{group}\Whatsinthebox.lnk"
Type: files; Name: "{userdesktop}\Whatsinthebox.lnk"




#endif
[Run]
Filename: "{app}\{code:InstallBin}\Whatsinthebox.exe"; Description: "{cm:setup_settings}"; Flags: nowait postinstall skipifsilent unchecked

Filename: "https://github.com/bakhtiyarjahangirzade/Whatsinthebox"; Description: "{cm:setup_github}"; Flags: shellexec postinstall skipifsilent

[Code]
var
  RegistrationFailed: Boolean;
  MaintenancePage: TInputOptionWizardPage;
  InstalledExe, InstalledVersion, InstalledRoot, BinaryFolder: String;
  InstalledComparison: Integer;

function InstallBin(Value: String): String;
begin
  if BinaryFolder = '' then Result := '{#AppVersion}' else Result := BinaryFolder;
end;

procedure DetectInstalled;
var Found: TFindRec; VersionMS, VersionLS: Cardinal; Packed, Best: Int64; RegisteredRoot, RegisteredIcon: String;
begin
  InstalledExe := ''; InstalledVersion := ''; Best := -1;
#ifdef QA_LAYOUT
  Exit;
#endif
  InstalledRoot := ExpandConstant('{localappdata}\Whatsinthebox\app');
  if RegQueryStringValue(HKCU64, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{1B978517-80B7-49E9-AE4F-F83A9844F190}_is1', 'InstallLocation', RegisteredRoot) and (RegisteredRoot <> '') then InstalledRoot := RegisteredRoot;
  if RegQueryStringValue(HKCU64, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{1B978517-80B7-49E9-AE4F-F83A9844F190}_is1', 'DisplayIcon', RegisteredIcon) then begin
    RegisteredIcon := RemoveQuotes(RegisteredIcon);
    if GetVersionNumbers(RegisteredIcon, VersionMS, VersionLS) then begin
      InstalledExe := RegisteredIcon;
      Best := PackVersionNumbers(VersionMS, VersionLS);
    end;
  end;
  if InstalledExe = '' then
  if FindFirst(AddBackslash(InstalledRoot) + '*', Found) then begin
    try
      repeat
        if (Found.Name <> '.') and (Found.Name <> '..') and
           GetVersionNumbers(AddBackslash(InstalledRoot) + Found.Name + '\Whatsinthebox.exe', VersionMS, VersionLS) then begin
          Packed := PackVersionNumbers(VersionMS, VersionLS);
          if (Best = -1) or (ComparePackedVersion(Packed, Best) > 0) then begin
            Best := Packed;
            InstalledExe := AddBackslash(InstalledRoot) + Found.Name + '\Whatsinthebox.exe';
          end;
        end;
      until not FindNext(Found);
    finally FindClose(Found); end;
  end;
  if InstalledExe <> '' then begin
    GetVersionNumbersString(InstalledExe, InstalledVersion);
    InstalledComparison := ComparePackedVersion(Best, {#StrToVersion(AppVersion)});
    Log('Detected installation: ' + InstalledVersion + '; setup: {#AppVersion}; comparison: ' + IntToStr(InstalledComparison));
  end;
end;

function GetCustomSetupExitCode: Integer;
begin
  if RegistrationFailed then Result := 20 else Result := 0;
end;

#ifdef QA_LAYOUT
function PostMessage(Window: HWND; Message: LongWord; WParam: THandle; LParam: Longint): Boolean; external 'PostMessageW@user32.dll stdcall';
#endif
procedure InitializeWizard;
begin
 DetectInstalled;
 WizardForm.Font.Name := 'Tahoma';
 if ActiveLanguage = 'zh' then WizardForm.Font.Name := 'Microsoft YaHei UI';
 if InstalledExe <> '' then begin
   MaintenancePage := CreateInputOptionPage(wpWelcome, CustomMessage('setup_existing'),
     FmtMessage(CustomMessage('setup_versions'), [InstalledVersion, '{#AppVersion}']),
     CustomMessage('setup_choose'), True, False);
   WizardForm.DirEdit.Text := InstalledRoot;
   if InstalledComparison < 0 then begin
     MaintenancePage.Add(CustomMessage('setup_update'));
     MaintenancePage.Add(CustomMessage('setup_repair_update'));
   end else if InstalledComparison = 0 then
     MaintenancePage.Add(CustomMessage('setup_repair'))
   else MaintenancePage.Add(CustomMessage('setup_newer'));
   MaintenancePage.SelectedValueIndex := 0;
   if not RegKeyExists(HKCU64, 'Software\Classes\CLSID\{47CCD7B8-35F6-4835-965C-F488331ADE93}') and
     (Pos('/TASKS=', Uppercase(GetCmdTail)) = 0) and (Pos('/MERGETASKS=', Uppercase(GetCmdTail)) = 0) then
     WizardSelectTasks('');
 end;
#ifdef QA_LAYOUT
 PostMessage(WizardForm.NextButton.Handle, $00F5, 0, 0);
#endif
end;
procedure CurPageChanged(CurPageID: Integer);
begin
 if MaintenancePage <> nil then
   if (CurPageID = MaintenancePage.ID) and (InstalledComparison > 0) then
     WizardForm.NextButton.Enabled := False;
#ifdef QA_LAYOUT
 if CurPageID = wpLicense then WizardForm.LicenseAcceptedRadio.Checked := True;
#endif
 if CurPageID = wpFinished then begin
  WizardForm.WizardBitmapImage2.Visible := True;

#ifdef QA_LAYOUT
  if not WizardForm.WizardBitmapImage2.Visible then RaiseException('Finish portrait rail not visible');
  if (WizardForm.RunList.Items.Count <> 2) or not WizardForm.RunList.Checked[1] then RaiseException('Repository option must be checked by default');
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

function PrepareToInstall(var NeedsRestart: Boolean): String;
var Code, FolderIndex: Integer; RepairRequested: Boolean;
begin
  Result := '';
#ifdef QA_LAYOUT
  Exit;
#endif
  DetectInstalled;
  if (InstalledExe <> '') and (InstalledComparison > 0) then begin
    Result := CustomMessage('setup_newer'); Exit;
  end;
  ExtractTemporaryFile('StopHelper.ps1');
  if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + ExpandConstant('{tmp}\StopHelper.ps1') + '"',
    '', SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then begin
    Result := CustomMessage('setup_stop_failed'); Exit;
  end;
  RepairRequested := False;
  if InstalledExe <> '' then begin
    RepairRequested := InstalledComparison = 0;
    if MaintenancePage <> nil then
      if MaintenancePage.SelectedValueIndex = 1 then RepairRequested := True;
  end;
  if RepairRequested then begin
    if not Exec(InstalledExe, '--unregister', ExtractFileDir(InstalledExe), SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then begin
      Result := CustomMessage('setup_uninstall_failed'); Exit;
    end;
  end;
  BinaryFolder := '{#AppVersion}'; FolderIndex := 0;
  while DirExists(AddBackslash(WizardDirValue) + BinaryFolder) do begin
    FolderIndex := FolderIndex + 1;
    BinaryFolder := '{#AppVersion}-' + IntToStr(FolderIndex);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var Code: Integer;
begin
#ifdef QA_LAYOUT
 Exit;
#endif
  if CurStep = ssPostInstall then Exec(ExpandConstant('{app}\{code:InstallBin}\Whatsinthebox.exe'), '--set-language ' + ActiveLanguage, '', SW_HIDE, ewWaitUntilTerminated, Code);
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('explorer') then begin
    if not Exec(ExpandConstant('{app}\{code:InstallBin}\Whatsinthebox.exe'), '--register', ExpandConstant('{app}\{code:InstallBin}'), SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then begin
      RegistrationFailed := True;
      Log('Explorer registration failed: ' + IntToStr(Code));
      if not WizardSilent then MsgBox(ExpandConstant('{cm:setup_registration_failed}'), mbError, MB_OK);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var Code: Integer;
begin
  if CurUninstallStep = usUninstall then begin
    DetectInstalled;
    if (InstalledExe = '') or not Exec(InstalledExe, '--unregister', ExtractFileDir(InstalledExe), SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then
      RaiseException(ExpandConstant('{cm:setup_uninstall_failed}'));
  end;
end;
