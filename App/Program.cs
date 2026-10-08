using Whatsinthebox.UI;

namespace Whatsinthebox;
static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--set-language"){L.Set(args[1]);return 0;}
        int locale=Array.IndexOf(args,"--locale");if(locale>=0&&locale+1<args.Length&&L.Languages.Contains(args[locale+1]))L.Override=args[locale+1];
        if(args.Length>=4&&args[0]=="--render")return Renderer.Run(args[1],args[2],args[3],args.Length>4?int.Parse(args[4]):0);
        if(args.Length>0&&args[0]=="--register"){try{Installer.Install(true);return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"setup-error.txt"),ex.Message);return 1;}}
        if(args.Length>0&&args[0]=="--unregister"){try{Installer.Uninstall();return 0;}catch{return 1;}}
        if(args.Contains("--stop-windows-host")){WindowsHost.Stop();return 0;}
        ApplicationConfiguration.Initialize();
        if(args.Contains("--windows-host")){try{return WindowsHost.Run();}catch(Exception ex){File.WriteAllText(Path.Combine(PreviewStorage.Root,"host-error.txt"),ex.ToString());return 1;}}
        if(args.Length>0&&args[0]=="--self-test")return SelfTest.Run(args.Length>1?args[1]:"test-results.json");
        if(args.Length==3&&args[0]=="--capture")return SelfTest.Capture(args[1],args[2]);
        if(args.Length>=2&&args[0]=="--capture-settings"){using var form=new SettingsForm();form.Show();Application.DoEvents();using var image=new Bitmap(form.Width,form.Height);form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(args[1]);form.Close();return 0;}
        Application.Run(new SettingsForm());return 0;
    }
}
