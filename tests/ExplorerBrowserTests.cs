using System.Runtime.InteropServices;
using System.Text.Json;
// The Shell folder view (the same DefView control File Explorer shows) hosted in a
// window of this ordinary account, in thumbnail view. It requests thumbnails
// through the Windows thumbnail cache and isolation surrogate exactly as a File
// Explorer window does. Explorer refuses to open windows for a second account on
// another account's desktop, so this is how folder thumbnails are checked there.
static class ExplorerBrowserTests
{
 [StructLayout(LayoutKind.Sequential)]struct Rect{public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct FolderSettings{public uint ViewMode,Flags;}
 [ComImport,Guid("DFD3B6B5-C10C-4BE9-85F6-A66969F402F6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IExplorerBrowser
 {
  void Initialize(IntPtr parent,ref Rect rect,ref FolderSettings settings);
  void Destroy();
  void SetRect(IntPtr defer,Rect rect);
  void SetPropertyBag([MarshalAs(UnmanagedType.LPWStr)]string bag);
  void SetEmptyText([MarshalAs(UnmanagedType.LPWStr)]string text);
  void SetFolderSettings(ref FolderSettings settings);
  void Advise(IntPtr events,out uint cookie);
  void Unadvise(uint cookie);
  void SetOptions(uint options);
  void GetOptions(out uint options);
  void BrowseToIDList(IntPtr pidl,uint flags);
  void BrowseToObject([MarshalAs(UnmanagedType.IUnknown)]object item,uint flags);
 }
 [DllImport("shell32.dll",CharSet=CharSet.Unicode,PreserveSig=false)][return:MarshalAs(UnmanagedType.IUnknown)]static extern object SHCreateItemFromParsingName(string path,IntPtr context,[MarshalAs(UnmanagedType.LPStruct)]Guid iid);
 [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr hwnd,IntPtr dc,uint flags);
 public static int Run(string report,string folder)
 {
  var results=new List<object>();int failed=0;
  try{Directory.Delete(Capture.Folder,true);}catch{}
  string decisions=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Whatsinthebox","integration.json");
  using var plan=JsonDocument.Parse(File.ReadAllText(decisions));
  var own=plan.RootElement.GetProperty("thumbnail").EnumerateArray().Select(x=>x.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
  var expected=Directory.GetFiles(folder).Where(x=>own.Contains(Path.GetExtension(x))).ToArray();
  using var form=new Form{ClientSize=new Size(1100,760),StartPosition=FormStartPosition.Manual,Location=new Point(0,0),Text="Whatsinthebox folder view check"};
  form.Show();Application.DoEvents();
  var browser=(IExplorerBrowser)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("71F96385-DDD6-48D3-A0C1-AE06E8B055FB"))!)!;
  try
  {
   var rect=new Rect{Right=form.ClientSize.Width,Bottom=form.ClientSize.Height};
   var settings=new FolderSettings{ViewMode=5,Flags=0}; // FVM_THUMBNAIL
   browser.Initialize(form.Handle,ref rect,ref settings);
   browser.BrowseToObject(SHCreateItemFromParsingName(Path.GetFullPath(folder),IntPtr.Zero,new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")),0);
   var until=DateTime.UtcNow.AddSeconds(45);
   while(DateTime.UtcNow<until&&!expected.All(f=>Capture.Read("thumbnail",f)!=null)){Application.DoEvents();Thread.Sleep(100);}
   for(int i=0;i<30;i++){Application.DoEvents();Thread.Sleep(100);}
   foreach(var file in expected)
   {
    var record=Capture.Read("thumbnail",file);bool ok=Capture.Succeeded(record);
    results.Add(new{name="Folder view thumbnail by Whatsinthebox for "+Path.GetExtension(file),passed=ok,record=record?.ToString()});if(!ok)failed++;
   }
   using var shot=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);
   using(var g=Graphics.FromImage(shot)){IntPtr dc=g.GetHdc();try{PrintWindow(form.Handle,dc,2);}finally{g.ReleaseHdc(dc);}}
   shot.Save(Path.ChangeExtension(report,"png"));
   try{using var screen=new Bitmap(form.Width,form.Height);using(var g=Graphics.FromImage(screen))g.CopyFromScreen(form.Location,Point.Empty,screen.Size);screen.Save(Path.ChangeExtension(report,"screen.png"));}catch{}
  }
  catch(Exception ex){failed++;results.Add(new{name="Folder view",passed=false,error=ex.ToString()});}
  finally{try{browser.Destroy();}catch{}Marshal.ReleaseComObject(browser);form.Close();}
  File.WriteAllText(report,JsonSerializer.Serialize(new{failed,results,user=Environment.UserName},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;
 }
}
