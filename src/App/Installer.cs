using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text.Json;
using Whatsinthebox.UI;
namespace Whatsinthebox;
static class Installer
{
    public static string Root=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Whatsinthebox");
    static string BackupFile=>Path.Combine(Root,"registration-backup.json");
    sealed record Snapshot(string Path,string? Original,RegistryValueKind Kind,string Installed,bool Existed);
    public static void PromptInstall(IWin32Window owner)
    {
        if(AppDialog.Show(owner,L.T("repair.confirm"),true)!=DialogResult.Yes)return;
        try{Install(true);AppDialog.Show(owner,L.T("repair.done"));}catch(Exception){AppDialog.Show(owner,L.T("repair.failed"));}
    }
    public static void Install(bool inPlace=false)
    {
        var runtime=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"dotnet","shared","Microsoft.WindowsDesktop.App");
        if(!Directory.Exists(runtime)||!Directory.EnumerateDirectories(runtime).Any(p=>Path.GetFileName(p).StartsWith("10.")))throw new IOException(L.T("runtime.required"));
        var source=AppContext.BaseDirectory;
        if(!File.Exists(Path.Combine(source,"shell","Whatsinthebox.Shell.comhost.dll")))throw new IOException(L.T("extension.missing"));
        Directory.CreateDirectory(Root);string bin=inPlace?source:Path.Combine(Root,"app",Updates.Current.ToString());Directory.CreateDirectory(bin);
        if(!Path.GetFullPath(source).TrimEnd('\\').Equals(Path.GetFullPath(bin).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))foreach(var file in Directory.EnumerateFiles(source,"*",SearchOption.AllDirectories)){var target=Path.Combine(bin,Path.GetRelativePath(source,file));Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target,true);}
        var targets=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(var ext in SupportedFiles.All)AddTargets(ext,IntegrationIds.PreviewInterface,IntegrationIds.Preview);
        foreach(var ext in SupportedFiles.ImageExtensions.Concat(new[]{".pdf",".ai"}))AddTargets(ext,IntegrationIds.ThumbnailInterface,IntegrationIds.Thumbnail);
        foreach(var ext in ThumbnailRefresh.Extensions)
        {
            string menu=$@"Software\Classes\SystemFileAssociations\{ext}\shell\Whatsinthebox.RefreshThumbnail";
            targets[menu]=L.T("thumbnail.refresh");
            targets[menu+@"\command"]=$"\"{Path.Combine(bin,"Whatsinthebox.exe")}\" --refresh-thumbnail \"%1\"";
        }
        string pdfPermission=@"Software\Classes\SystemFileAssociations\.pdf\shell\Whatsinthebox.AllowPdfPreview";
        targets[pdfPermission]=L.T("pdf.trust.menu");
        targets[pdfPermission+@"\command"]=$"\"{Path.Combine(bin,"Whatsinthebox.exe")}\" --allow-pdf-preview \"%1\"";
        void AddTargets(string ext,string iid,string clsid)
        {
            targets[$@"Software\Classes\{ext}\shellex\{iid}"]=clsid;
            targets[$@"Software\Classes\SystemFileAssociations\{ext}\shellex\{iid}"]=clsid;
            using var choice=Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext}\UserChoice");
            using var fileType=Registry.ClassesRoot.OpenSubKey(ext);
            foreach(var progId in new[]{choice?.GetValue("ProgId") as string,fileType?.GetValue("") as string}.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                targets[$@"Software\Classes\{progId}\shellex\{iid}"]=clsid;
        }
        string legacyFile=Path.Combine(Root,"associations.json");var legacy=File.Exists(legacyFile)?JsonSerializer.Deserialize<Dictionary<string,string?>>(File.ReadAllText(legacyFile))!:new();
        var backups=File.Exists(BackupFile)?JsonSerializer.Deserialize<List<Snapshot>>(File.ReadAllText(BackupFile))!:new();
        foreach(var pair in targets)
        {
            if(backups.Any(x=>x.Path.Equals(pair.Key,StringComparison.OrdinalIgnoreCase)))continue;
            using var key=Registry.CurrentUser.OpenSubKey(pair.Key);string? original=key?.GetValue("",null,RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
            var kind=key?.GetValueNames().Contains("")==true?key.GetValueKind(""):RegistryValueKind.String;
            if(original is IntegrationIds.LegacyPreview or IntegrationIds.Preview or IntegrationIds.Thumbnail)
            {
                string? ext=SupportedFiles.All.FirstOrDefault(e=>pair.Key.Equals($@"Software\Classes\{e}\shellex\{IntegrationIds.PreviewInterface}",StringComparison.OrdinalIgnoreCase));
                original=ext!=null&&legacy.TryGetValue(ext,out var previous)?previous:null;
            }
            backups.Add(new(pair.Key,original,kind,pair.Value,key!=null));
        }
        string pending=BackupFile+".tmp";File.WriteAllText(pending,JsonSerializer.Serialize(backups,new JsonSerializerOptions{WriteIndented=true}));File.Move(pending,BackupFile,true);
        try
        {
            foreach(var id in new[]{IntegrationIds.Preview,IntegrationIds.Thumbnail})
            {
                using var cls=Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{id}");cls.SetValue("","Whatsinthebox Windows extension");cls.DeleteValue("AppID",false);
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\AppID\{id}",false);
                using var local=cls.CreateSubKey("LocalServer32");local.SetValue("",$"\"{Path.Combine(bin,"Whatsinthebox.exe")}\" --windows-host");local.SetValue("ServerExecutable",Path.Combine(bin,"Whatsinthebox.exe"));
                // Both file and stream initialization are supported; retain Windows' default thumbnail isolation.
                cls.DeleteValue("DisableProcessIsolation",false);
                using var server=cls.CreateSubKey("InprocServer32");server.SetValue("",Path.Combine(bin,"shell","Whatsinthebox.Shell.comhost.dll"));server.SetValue("ThreadingModel","Apartment");
            }
            using(var list=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\PreviewHandlers"))list.SetValue(IntegrationIds.Preview,"Whatsinthebox");
            foreach(var pair in targets){using var key=Registry.CurrentUser.CreateSubKey(pair.Key);key.SetValue("",pair.Value);}
            Notify();
            if(!EffectiveHandler(".pdf",IntegrationIds.PreviewInterface).Equals(IntegrationIds.Preview,StringComparison.OrdinalIgnoreCase))throw new IOException(L.T("registration.failed"));
            if(!EffectiveHandler(".pdf",IntegrationIds.ThumbnailInterface).Equals(IntegrationIds.Thumbnail,StringComparison.OrdinalIgnoreCase))throw new IOException(L.T("registration.failed"));
            using(var startup=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                string backup=Path.Combine(Root,"startup-backup.json");
                if(!File.Exists(backup)){string? original=startup.GetValue("Whatsinthebox") as string;if(original?.Contains("Whatsinthebox.exe",StringComparison.OrdinalIgnoreCase)==true&&original.Contains("--windows-host"))original=null;File.WriteAllText(backup,JsonSerializer.Serialize(original));}
                startup.SetValue("Whatsinthebox",$"\"{Path.Combine(bin,"Whatsinthebox.exe")}\" --windows-host");
            }
            WindowsHost.Stop();Thread.Sleep(600);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(bin,"Whatsinthebox.exe"),"--windows-host"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden});
        }
        catch{Uninstall();throw;}
    }
    public static void Uninstall()
    {
        WindowsHost.Stop();
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
                if(snapshot.Original!=null)key.SetValue("",snapshot.Original,snapshot.Kind);else{key.DeleteValue("",false);if(key.ValueCount==0&&key.SubKeyCount==0&&!snapshot.Existed){key.Close();Registry.CurrentUser.DeleteSubKey(snapshot.Path,false);}}
            }
            File.Delete(BackupFile);
        }
        string legacyFile=Path.Combine(Root,"associations.json");
        if(File.Exists(legacyFile))
        {
            var legacy=JsonSerializer.Deserialize<Dictionary<string,string?>>(File.ReadAllText(legacyFile))!;
            foreach(var pair in legacy.Where(x=>SupportedFiles.All.Contains(x.Key)))
            {
                using var key=Registry.CurrentUser.OpenSubKey($@"Software\Classes\{pair.Key}\shellex\{IntegrationIds.PreviewInterface}",true);
                if(key?.GetValue("") as string!=IntegrationIds.LegacyPreview)continue;
                if(pair.Value!=null)key.SetValue("",pair.Value);else key.DeleteValue("",false);
            }
            File.Delete(legacyFile);
        }
        foreach(var id in new[]{IntegrationIds.Preview,IntegrationIds.Thumbnail,IntegrationIds.LegacyPreview})
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\CLSID\{id}",false);
            if(id!=IntegrationIds.LegacyPreview)Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\AppID\{id}",false);
            using var list=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\PreviewHandlers",true);list?.DeleteValue(id,false);
        }
        Notify();
    }
    public static string EffectiveHandler(string extension,string iid)
    {
        var result=new System.Text.StringBuilder(1024);uint count=1024;return AssocQueryString(0,16,extension,iid,result,ref count)==0?result.ToString():"";
    }
    [DllImport("shlwapi.dll",CharSet=CharSet.Unicode)]static extern int AssocQueryString(uint flags,uint str,string association,string extra,System.Text.StringBuilder output,ref uint length);
    [DllImport("shell32.dll")]static extern void SHChangeNotify(uint id,uint flags,IntPtr a,IntPtr b);
    static void Notify()=>SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);
}
