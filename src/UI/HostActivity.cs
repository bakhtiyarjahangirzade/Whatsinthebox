namespace Whatsinthebox.UI;
public static class HostActivity
{
 static int views;
 public static bool HasViews=>Volatile.Read(ref views)>0;
 public static void Opened()=>Interlocked.Increment(ref views);
 public static void Closed()=>Interlocked.Decrement(ref views);
 [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("SingleFile","IL3000",Justification="The assembly path is used only in the external COM DLL host; bundled application calls use the process path.")]
 public static string Renderer(System.Reflection.Assembly assembly)
 {
  string? process=Environment.ProcessPath;
  if(process!=null&&Path.GetFileName(process).Equals("Whatsinthebox.exe",StringComparison.OrdinalIgnoreCase))return process;
  string assemblyFolder=Path.GetDirectoryName(assembly.Location)??AppContext.BaseDirectory;
  string local=Path.Combine(assemblyFolder,"Whatsinthebox.exe");return File.Exists(local)?local:Path.GetFullPath(Path.Combine(assemblyFolder,"..","Whatsinthebox.exe"));
 }
}
