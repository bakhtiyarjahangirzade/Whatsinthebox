namespace Whatsinthebox.UI;
public static class HostActivity
{
 static int views;
 public static bool HasViews=>Volatile.Read(ref views)>0;
 public static void Opened()=>Interlocked.Increment(ref views);
 public static void Closed()=>Interlocked.Decrement(ref views);
 public static string Renderer(string assemblyFolder)
 {
  string local=Path.Combine(assemblyFolder,"Whatsinthebox.exe");return File.Exists(local)?local:Path.GetFullPath(Path.Combine(assemblyFolder,"..","Whatsinthebox.exe"));
 }
}
