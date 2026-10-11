// Integration checks for the native Explorer handlers; run only on isolated runners.
static class Program
{
 [STAThread]static int Main(string[] args)
 {
  ApplicationConfiguration.Initialize();
  if(args.Length==3&&args[1]=="--system-host")return SystemHostTests.Run(args[0],args[2]);
  if(args.Length==3&&args[1]=="--shell-formats")return ShellFormatTests.Run(args[0],args[2]);
  if(args.Length==3&&args[1]=="--explorer")return ExplorerTests.Run(args[0],args[2]);
  Console.Error.WriteLine("usage: Whatsinthebox.Tests <report.json> --system-host|--shell-formats|--explorer <path>");
  return 2;
 }
}
// Records the native handlers write when test-capture.flag sits next to the DLL.
static class Capture
{
 public static string Folder=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"Low","Whatsinthebox","capture");
 public static string File(string kind,string source)=>Path.Combine(Folder,kind+"-"+Path.GetFileName(source)+".json");
 public static void Clear(string kind,string source){try{System.IO.File.Delete(File(kind,source));}catch{}}
 public static System.Text.Json.JsonElement? Read(string kind,string source)
 {
  string path=File(kind,source);
  if(!System.IO.File.Exists(path))return null;
  try{using var json=System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path));return json.RootElement.Clone();}catch{return null;}
 }
 public static bool Succeeded(System.Text.Json.JsonElement? record)=>record is {} r&&r.GetProperty("hresult").GetString()=="0x00000000"&&r.GetProperty("width").GetInt32()>0&&r.GetProperty("height").GetInt32()>0;
}
