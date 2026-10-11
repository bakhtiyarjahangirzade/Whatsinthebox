using System.Runtime.InteropServices;
using System.Text.Json;
using Whatsinthebox.UI;
// Real explorer.exe folder windows on the runner's interactive desktop. Each fixture is
// opened with /select so the item is actually selected by Explorer itself, and the
// preview pane is enabled through the documented per-user Explorer settings. Success
// requires the application to report that it rendered exactly that fixture inside
// Explorer's preview pane; screenshots of the Explorer window are kept as evidence.
static class ExplorerTests
{
    delegate bool EnumWindowsProc(IntPtr hwnd,IntPtr data);
    [DllImport("user32.dll")]static extern bool EnumWindows(EnumWindowsProc callback,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr hwnd,System.Text.StringBuilder name,int length);
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint process);
    [DllImport("user32.dll")]static extern IntPtr GetShellWindow();
    [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hwnd,int mode);
    [DllImport("user32.dll")]static extern bool MoveWindow(IntPtr hwnd,int x,int y,int width,int height,bool repaint);
    [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr hwnd,IntPtr dc,uint flags);
    [DllImport("user32.dll")]static extern bool PostMessage(IntPtr hwnd,uint message,IntPtr wp,IntPtr lp);
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr hwnd,out NativeRect rect);
    [StructLayout(LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
    static List<IntPtr> FolderWindows()
    {
        var list=new List<IntPtr>();
        EnumWindows((hwnd,_)=>{var name=new System.Text.StringBuilder(64);GetClassName(hwnd,name,64);if(name.ToString()=="CabinetWClass"&&IsWindowVisible(hwnd))list.Add(hwnd);return true;},IntPtr.Zero);
        return list;
    }
    public static int Run(string report,string folder)
    {
        if(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")!="true")throw new InvalidOperationException("Real Explorer checks require an isolated runner.");
        var results=new List<object>();int failed=0;
        void Stage(string value)=>File.AppendAllText(report+".stage",DateTime.UtcNow.ToString("O")+" "+value+Environment.NewLine);
        GetWindowThreadProcessId(GetShellWindow(),out uint shellProcess);
        string thumbnails=Path.Combine(PreviewStorage.Root,"thumbnail-test.log");if(File.Exists(thumbnails))File.Delete(thumbnails);
        try
        {
            // Documented per-user Explorer state for "Preview pane: on".
            using(var pane=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Modules\GlobalSettings\DetailsContainer"))pane.SetValue("DetailsContainer",new byte[]{2,0,0,0,1,0,0,0},Microsoft.Win32.RegistryValueKind.Binary);
            using(var sizer=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Modules\GlobalSettings\Sizer"))sizer.SetValue("DetailsContainerSizer",new byte[]{0x15,1,0,0,1,0,0,0,0,0,0,0,0xc0,3,0,0},Microsoft.Win32.RegistryValueKind.Binary);
            foreach(string file in Directory.GetFiles(folder).OrderBy(x=>Path.GetExtension(x) switch{".png"=>0,".jpg"=>1,".jpeg"=>2,_=>3}).ThenBy(x=>x))
            {
                string ext=Path.GetExtension(file).ToLowerInvariant(),name=Path.GetFileName(file);
                string state=Path.Combine(PreviewStorage.Root,"embedded-test.json");File.Delete(state);File.Delete(Path.Combine(PreviewStorage.Root,"embedded-test.png"));
                var before=FolderWindows();
                Stage("opening Explorer with "+name+" selected");
                using(System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),"/separate,/select,\""+Path.GetFullPath(file)+"\""){UseShellExecute=false})){}
                IntPtr hwnd=IntPtr.Zero;var until=DateTime.UtcNow.AddSeconds(20);
                while(hwnd==IntPtr.Zero&&DateTime.UtcNow<until){Thread.Sleep(250);hwnd=FolderWindows().FirstOrDefault(x=>!before.Contains(x));}
                if(hwnd==IntPtr.Zero){failed++;results.Add(new{name="Actual Explorer window "+ext,passed=false,error="No new Explorer folder window appeared"});continue;}
                GetWindowThreadProcessId(hwnd,out uint windowProcess);
                ShowWindow(hwnd,9);MoveWindow(hwnd,0,0,1400,900,true);SetForegroundWindow(hwnd);
                bool rendered=false,toggled=false;string? error=null;string? source=null;until=DateTime.UtcNow.AddSeconds(25);
                while(DateTime.UtcNow<until)
                {
                    Thread.Sleep(200);
                    if(File.Exists(state))
                    {
                        try{using var json=JsonDocument.Parse(File.ReadAllText(state));source=json.RootElement.TryGetProperty("source",out var s)?s.GetString():null;
                        if(name.Equals(source,StringComparison.OrdinalIgnoreCase)){rendered=json.RootElement.GetProperty("success").GetBoolean();if(!rendered)error=json.RootElement.TryGetProperty("error",out var e)?e.GetString():"failed";break;}}catch(IOException){}catch(JsonException){}
                    }
                    if(!toggled&&DateTime.UtcNow>until.AddSeconds(-13)){SetForegroundWindow(hwnd);SendKeys.SendWait("%p");toggled=true;Stage("toggled preview pane for "+name);}
                }
                Thread.Sleep(700);
                string shot=Path.ChangeExtension(report,ext.TrimStart('.')+"-explorer.png");
                GetWindowRect(hwnd,out var rect);using(var bitmap=new Bitmap(Math.Max(1,rect.Right-rect.Left),Math.Max(1,rect.Bottom-rect.Top))){using(var graphics=Graphics.FromImage(bitmap)){IntPtr dc=graphics.GetHdc();try{PrintWindow(hwnd,dc,2);}finally{graphics.ReleaseHdc(dc);}}bitmap.Save(shot);}
                if(rendered)File.Copy(Path.Combine(PreviewStorage.Root,"embedded-test.png"),Path.ChangeExtension(report,ext.TrimStart('.')+"-preview.png"),true);
                bool separate=windowProcess!=shellProcess;
                results.Add(new{name="Actual Explorer preview pane renders "+ext,passed=rendered&&separate,windowProcess,shellProcess,separateFromShell=separate,toggledWithAltP=toggled,source,error,screenshot=Path.GetFileName(shot)});
                if(!(rendered&&separate))failed++;
                PostMessage(hwnd,0x0010,IntPtr.Zero,IntPtr.Zero);Thread.Sleep(800);
            }
        }
        catch(Exception ex){failed++;results.Add(new{name="Actual Explorer workflow",passed=false,error=ex.ToString()});}
        // Folder views request thumbnails on their own; record which fixtures Explorer asked us for.
        string[] requests=File.Exists(thumbnails)?File.ReadAllLines(thumbnails):Array.Empty<string>();
        File.WriteAllText(report,JsonSerializer.Serialize(new{failed,results,explorerThumbnailRequests=requests},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;
    }
}
