using System.Runtime.InteropServices;
using System.Text.Json;
using Whatsinthebox.UI;
static class ExplorerTests
{
    [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hwnd,int mode);
    [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr hwnd,IntPtr dc,uint flags);
    public static int Run(string report,string folder)
    {
        if(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")!="true")throw new InvalidOperationException("Real Explorer checks require an isolated runner.");
        var results=new List<object>();dynamic? shell=null,window=null;int failed=0;
        void Stage(string value)=>File.WriteAllText(report+".stage",value);
        try
        {
            using(var preferences=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Modules\GlobalSettings\DetailsContainer"))preferences.SetValue("DetailsContainer",new byte[]{1,0,0,0,1,0,0,0},Microsoft.Win32.RegistryValueKind.Binary);
            Stage("launching Explorer in isolated user session");
            using var explorer=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),"/separate,\""+Path.GetFullPath(folder)+"\""){UseShellExecute=true});
            Stage("creating Shell automation object");shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application")!);
            Stage("finding owned folder window");
            var until=DateTime.UtcNow.AddSeconds(30);
            while(window==null&&DateTime.UtcNow<until)
            {
                dynamic windows=shell.Windows();try{for(int i=0;i<(int)windows.Count;i++){dynamic candidate=windows.Item(i);try{string url=candidate.LocationURL;if(Uri.TryCreate(url,UriKind.Absolute,out var uri)&&uri.IsFile&&Path.GetFullPath(uri.LocalPath).TrimEnd('\\').Equals(Path.GetFullPath(folder).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase)){window=candidate;break;}}catch{}}}finally{Marshal.ReleaseComObject(windows);}Application.DoEvents();Thread.Sleep(250);
            }
            if(window==null)throw new IOException("An actual Explorer folder window could not be opened.");
            Stage("owned Explorer window found");IntPtr hwnd=new((long)window.HWND);ShowWindow(hwnd,5);SetForegroundWindow(hwnd);
            bool toggled=false;
            foreach(string file in Directory.GetFiles(folder).OrderBy(x=>Path.GetExtension(x)==".png"?0:1).ThenBy(x=>x))
            {
                string state=Path.Combine(PreviewStorage.Root,"embedded-test.json");File.Delete(state);File.Delete(Path.Combine(PreviewStorage.Root,"embedded-test.png"));
                Stage("selecting "+Path.GetExtension(file));dynamic view=window.Document;dynamic item=view.Folder.ParseName(Path.GetFileName(file));view.SelectItem(item,29);Marshal.ReleaseComObject(item);Marshal.ReleaseComObject(view);
                bool rendered=false;until=DateTime.UtcNow.AddSeconds(20);
                while(DateTime.UtcNow<until)
                {
                    Application.DoEvents();Thread.Sleep(150);
                    if(File.Exists(state)){using var json=JsonDocument.Parse(File.ReadAllText(state));rendered=json.RootElement.TryGetProperty("success",out var ok)&&ok.GetBoolean();break;}
                    if(!toggled&&DateTime.UtcNow>until.AddSeconds(-15)){SetForegroundWindow(hwnd);SendKeys.SendWait("%p");toggled=true;}
                }
                results.Add(new{name="Actual Explorer selection "+Path.GetExtension(file),passed=rendered});if(!rendered)failed++;
                if(rendered)File.Copy(Path.Combine(PreviewStorage.Root,"embedded-test.png"),Path.ChangeExtension(report,Path.GetExtension(file).TrimStart('.')+"-preview.png"),true);
                using var screenshot=new Bitmap(1400,1000);using(var graphics=Graphics.FromImage(screenshot)){IntPtr dc=graphics.GetHdc();try{PrintWindow(hwnd,dc,2);}finally{graphics.ReleaseHdc(dc);}}screenshot.Save(Path.ChangeExtension(report,Path.GetExtension(file).TrimStart('.')+"-explorer.png"));
                if(!rendered)break;
            }
        }
        catch(Exception ex){failed++;results.Add(new{name="Actual Explorer workflow",passed=false,error=ex.ToString()});}
        finally{Stage("closing owned window");try{window?.Quit();}catch{}if(window!=null)Marshal.ReleaseComObject(window);if(shell!=null)Marshal.ReleaseComObject(shell);}
        File.WriteAllText(report,JsonSerializer.Serialize(new{failed,results},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;
    }
}
