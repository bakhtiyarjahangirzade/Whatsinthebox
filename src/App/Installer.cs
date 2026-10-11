using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text.Json;
using Whatsinthebox.UI;
namespace Whatsinthebox;

// Per-user Explorer integration. The native Whatsinthebox.Preview.dll is registered
// as an ordinary in-process thumbnail provider and preview handler, which Windows
// runs in its own isolated hosts. Formats that already have a working handler on
// this PC (from Windows or another program) are left untouched.
static class Installer
{
    public static string Root=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Whatsinthebox");
    static string BackupFile=>Path.Combine(Root,"registration-backup.json");
    const string NativeFile="Whatsinthebox.Preview.dll";
    const string PreviewHostAppId="{6D2B5079-2F0B-48DD-AB7F-97CEC514D30B}";
    sealed record Snapshot(string Path,string? Original,RegistryValueKind Kind,string Installed,bool Existed);
    sealed record PreviewOptions(int? ShowPreviewHandlers,int? IconsOnly);

    // Every class identity any Whatsinthebox version has registered.
    static readonly string[] OwnClasses={IntegrationIds.Preview,IntegrationIds.Thumbnail,IntegrationIds.ManagedPreview,IntegrationIds.ManagedThumbnail,IntegrationIds.LegacyPreview,IntegrationIds.PreviousPreview,IntegrationIds.PreviousThumbnail};
    static bool IsOwn(string? value)=>value!=null&&OwnClasses.Any(x=>x.Equals(value,StringComparison.OrdinalIgnoreCase));

    public static void PromptInstall(IWin32Window owner)
    {
        if(AppDialog.Show(owner,L.T("repair.confirm"),true)!=DialogResult.Yes)return;
        try{Install(true);AppDialog.Show(owner,L.T("repair.done"));}catch(Exception){AppDialog.Show(owner,L.T("repair.failed"));}
    }

    public static void Install(bool inPlace=false)
    {
        var source=AppContext.BaseDirectory;
        if(!File.Exists(Path.Combine(source,"shell",NativeFile)))throw new IOException(L.T("extension.missing"));
        // Start from a clean state: restores what earlier versions replaced and
        // retires the background host that versions before 1.1 used.
        Uninstall();
        Directory.CreateDirectory(Root);string bin=inPlace?source:Path.Combine(Root,"app",Updates.Current.ToString());Directory.CreateDirectory(bin);
        if(!Path.GetFullPath(source).TrimEnd('\\').Equals(Path.GetFullPath(bin).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))foreach(var file in Directory.EnumerateFiles(source,"*",SearchOption.AllDirectories)){var target=Path.Combine(bin,Path.GetRelativePath(source,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target,true);}
        string native=Path.Combine(bin,"shell",NativeFile);

        // Decide per format, before writing anything, which handlers Windows lacks.
        string probe=Path.Combine(Root,"association-check");
        var previewFormats=SupportedFiles.NativePreview.Where(ext=>!HasForeignHandler(ext,IntegrationIds.PreviewInterface,probe)).ToArray();
        var thumbnailFormats=SupportedFiles.NativeThumbnail.Where(ext=>!HasForeignHandler(ext,IntegrationIds.ThumbnailInterface,probe)).ToArray();
        try{Directory.Delete(probe,true);}catch{}
        File.WriteAllText(Path.Combine(Root,"integration.json"),JsonSerializer.Serialize(new{preview=previewFormats,thumbnail=thumbnailFormats,
            leftToOtherHandlers=new{preview=SupportedFiles.NativePreview.Except(previewFormats).ToArray(),thumbnail=SupportedFiles.NativeThumbnail.Except(thumbnailFormats).ToArray()}},new JsonSerializerOptions{WriteIndented=true}));

        var targets=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(var ext in previewFormats)AddTargets(ext,IntegrationIds.PreviewInterface,IntegrationIds.Preview);
        foreach(var ext in thumbnailFormats)AddTargets(ext,IntegrationIds.ThumbnailInterface,IntegrationIds.Thumbnail);
        foreach(var ext in thumbnailFormats)
        {
            string menu=$@"Software\Classes\SystemFileAssociations\{ext}\shell\Whatsinthebox.RefreshThumbnail";
            targets[menu]=L.T("thumbnail.refresh");
            targets[menu+@"\command"]=$"\"{Path.Combine(bin,"Whatsinthebox.exe")}\" --refresh-thumbnail \"%1\"";
        }
        if(previewFormats.Contains(".pdf")||thumbnailFormats.Contains(".pdf"))
        {
            string pdfPermission=@"Software\Classes\SystemFileAssociations\.pdf\shell\Whatsinthebox.AllowPdfPreview";
            targets[pdfPermission]=L.T("pdf.trust.menu");
            targets[pdfPermission+@"\command"]=$"\"{Path.Combine(bin,"Whatsinthebox.exe")}\" --allow-pdf-preview \"%1\"";
        }
        void AddTargets(string ext,string iid,string clsid)
        {
            targets[$@"Software\Classes\{ext}\shellex\{iid}"]=clsid;
            targets[$@"Software\Classes\SystemFileAssociations\{ext}\shellex\{iid}"]=clsid;
            using var fileType=Registry.ClassesRoot.OpenSubKey(ext);
            // The Shell checks the default-app ProgID before the extension key.
            foreach(var progId in Associations.UserProgIds(ext).Append(fileType?.GetValue("") as string??"").Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                targets[$@"Software\Classes\{progId}\shellex\{iid}"]=clsid;
        }

        var backups=new List<Snapshot>();
        foreach(var pair in targets)
        {
            using var key=Registry.CurrentUser.OpenSubKey(pair.Key);
            string? original=key?.GetValue("",null,RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
            var kind=key?.GetValueNames().Contains("")==true?key.GetValueKind(""):RegistryValueKind.String;
            if(IsOwn(original))original=null;
            backups.Add(new(pair.Key,original,kind,pair.Value,key!=null));
        }
        string pending=BackupFile+".tmp";File.WriteAllText(pending,JsonSerializer.Serialize(backups,new JsonSerializerOptions{WriteIndented=true}));File.Move(pending,BackupFile,true);
        try
        {
            foreach(var (id,name) in new[]{(IntegrationIds.Preview,"Whatsinthebox preview"),(IntegrationIds.Thumbnail,"Whatsinthebox thumbnails")})
            {
                using var cls=Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{id}");
                cls.SetValue("",name);cls.SetValue("DisplayName",name);
                if(id==IntegrationIds.Preview)cls.SetValue("AppID",PreviewHostAppId);
                using var server=cls.CreateSubKey("InprocServer32");server.SetValue("",native);server.SetValue("ThreadingModel","Apartment");
            }
            using(var list=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\PreviewHandlers"))list.SetValue(IntegrationIds.Preview,"Whatsinthebox");
            foreach(var pair in targets){using var key=Registry.CurrentUser.CreateSubKey(pair.Key);key.SetValue("",pair.Value);}
            using(var options=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
            {
                string backup=Path.Combine(Root,"preview-options-backup.json");
                if(!File.Exists(backup))File.WriteAllText(backup,JsonSerializer.Serialize(new PreviewOptions(options.GetValue("ShowPreviewHandlers") as int?,options.GetValue("IconsOnly") as int?)));
                options.SetValue("ShowPreviewHandlers",1,RegistryValueKind.DWord);options.SetValue("IconsOnly",0,RegistryValueKind.DWord);
            }
            Notify();
            WriteDiagnostics();
            // Replace thumbnails Explorer cached before installation, in the background.
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(bin,"Whatsinthebox.exe"),"--refresh-cached-common"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden});
        }
        catch{Uninstall();throw;}
    }

    static bool HasForeignHandler(string ext,string iid,string folder)
    {
        string resolved=Associations.ForFile(SampleFile(ext,folder),iid);
        if(!resolved.StartsWith("{")||IsOwn(resolved))return false;
        // A leftover entry of uninstalled software does not count as a handler.
        using var server=Registry.ClassesRoot.OpenSubKey($@"CLSID\{resolved}\InprocServer32")??Registry.ClassesRoot.OpenSubKey($@"CLSID\{resolved}\LocalServer32");
        return server!=null;
    }

    static string SampleFile(string ext,string folder)
    {
        Directory.CreateDirectory(folder);string sample=Path.Combine(folder,"sample"+ext);
        if(!File.Exists(sample))File.WriteAllBytes(sample,Array.Empty<byte>());
        return sample;
    }

    public static void Uninstall()
    {
        WindowsHost.Stop();
        string optionsBackup=Path.Combine(Root,"preview-options-backup.json");
        if(File.Exists(optionsBackup))
        {
            var original=JsonSerializer.Deserialize<PreviewOptions>(File.ReadAllText(optionsBackup))!;
            using var options=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",true);
            foreach(var value in new[]{("ShowPreviewHandlers",original.ShowPreviewHandlers,1),("IconsOnly",original.IconsOnly,0)})
            {
                if(options?.GetValue(value.Item1) is not int current||current!=value.Item3)continue;
                if(value.Item2.HasValue)options.SetValue(value.Item1,value.Item2.Value,RegistryValueKind.DWord);else options.DeleteValue(value.Item1,false);
            }
            File.Delete(optionsBackup);
        }
        // Versions before 1.1 started a background host at sign-in.
        using(var startup=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true))
        {
            string backup=Path.Combine(Root,"startup-backup.json");string? current=startup?.GetValue("Whatsinthebox") as string;
            if(current?.Contains("Whatsinthebox.exe",StringComparison.OrdinalIgnoreCase)==true&&current.Contains("--windows-host")){string? previous=File.Exists(backup)?JsonSerializer.Deserialize<string?>(File.ReadAllText(backup)):null;if(previous!=null)startup!.SetValue("Whatsinthebox",previous);else startup?.DeleteValue("Whatsinthebox",false);}
            if(File.Exists(backup))File.Delete(backup);
        }
        if(File.Exists(BackupFile))
        {
            var backups=JsonSerializer.Deserialize<List<Snapshot>>(File.ReadAllText(BackupFile))!;
            foreach(var snapshot in backups)
            {
                if(!snapshot.Path.StartsWith(@"Software\Classes\",StringComparison.OrdinalIgnoreCase))throw new IOException(L.T("backup.invalid"));
                using var key=Registry.CurrentUser.OpenSubKey(snapshot.Path,true);
                if(key?.GetValue("") as string!=snapshot.Installed)continue;
                string? original=IsOwn(snapshot.Original)?null:snapshot.Original;
                if(original!=null)key.SetValue("",original,snapshot.Kind);else{key.DeleteValue("",false);if(key.ValueCount==0&&key.SubKeyCount==0&&!snapshot.Existed){key.Close();Registry.CurrentUser.DeleteSubKey(snapshot.Path,false);}}
            }
            File.Delete(BackupFile);
        }
        string legacyFile=Path.Combine(Root,"associations.json");
        if(File.Exists(legacyFile))
        {
            var legacy=JsonSerializer.Deserialize<Dictionary<string,string?>>(File.ReadAllText(legacyFile))!;
            foreach(var pair in legacy)
            {
                using var key=Registry.CurrentUser.OpenSubKey($@"Software\Classes\{pair.Key}\shellex\{IntegrationIds.PreviewInterface}",true);
                if(key?.GetValue("") as string!=IntegrationIds.LegacyPreview)continue;
                if(pair.Value!=null&&!IsOwn(pair.Value))key.SetValue("",pair.Value);else key.DeleteValue("",false);
            }
            File.Delete(legacyFile);
        }
        foreach(var id in OwnClasses)
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\CLSID\{id}",false);
            if(id!=IntegrationIds.LegacyPreview)Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\AppID\{id}",false);
            using var list=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\PreviewHandlers",true);list?.DeleteValue(id,false);
        }
        Notify();
    }

    // Read-only record of what Explorer resolves for each format after registration.
    public static void WriteDiagnostics(string? output=null)
    {
        try
        {
            string folder=Path.Combine(Root,"association-check");
            var rows=SupportedFiles.NativePreview.Concat(SupportedFiles.NativeThumbnail).Distinct().Select(x=>Associations.Inspect(x,folder)).ToArray();
            try{Directory.Delete(folder,true);}catch{}
            File.WriteAllText(output??Path.Combine(Root,"association-diagnostics.json"),JsonSerializer.Serialize(new{version=Updates.Current.ToString(),preview=IntegrationIds.Preview,thumbnail=IntegrationIds.Thumbnail,rows},new JsonSerializerOptions{WriteIndented=true}));
        }
        catch(Exception ex){if(output!=null)throw;try{File.WriteAllText(Path.Combine(Root,"association-diagnostics.json"),JsonSerializer.Serialize(new{error=ex.Message}));}catch{}}
    }
    public static string EffectiveHandler(string extension,string iid)
    {
        var result=new System.Text.StringBuilder(1024);uint count=1024;return AssocQueryString(0,16,extension,iid,result,ref count)==0?result.ToString():"";
    }
    [DllImport("shlwapi.dll",CharSet=CharSet.Unicode)]static extern int AssocQueryString(uint flags,uint str,string association,string extra,System.Text.StringBuilder output,ref uint length);
    [DllImport("shell32.dll")]static extern void SHChangeNotify(uint id,uint flags,IntPtr a,IntPtr b);
    static void Notify()=>SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);
}
