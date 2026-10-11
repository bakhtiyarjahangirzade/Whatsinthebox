using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using Whatsinthebox.UI;
// Activates the registered native classes the way Windows does: the thumbnail
// provider in process, the preview handler out of process in Preview Host.
static class SystemHostTests
{
 [DllImport("ole32.dll")]static extern int CoCreateInstance(in Guid clsid,IntPtr outer,uint context,in Guid iid,out IntPtr obj);
 [DllImport("shlwapi.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]static extern int SHCreateStreamOnFileEx(string file,uint mode,uint attributes,bool create,IntPtr template,out IntPtr stream);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr bitmap);
 [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr hwnd,IntPtr dc,uint flags);
 [StructLayout(LayoutKind.Sequential)]struct Rect{public int Left,Top,Right,Bottom;}
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int InitStream(IntPtr self,IntPtr stream,uint mode);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetThumbnail(IntPtr self,uint size,out IntPtr bitmap,out uint alpha);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int SetWindow(IntPtr self,IntPtr parent,ref Rect rect);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Call(IntPtr self);
 static T Method<T>(IntPtr ptr,int slot)where T:Delegate=>Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ptr),slot*IntPtr.Size));
 static readonly Guid StreamInit=new("b824b49d-22ac-4161-ac8a-9916e8fa3f7f"),ThumbnailIid=new("e357fccd-a995-4576-b01f-234630154e96"),PreviewIid=new("8895b1c6-b41f-4c1c-a562-0d564250836f");
 static IntPtr Open(string file){Marshal.ThrowExceptionForHR(SHCreateStreamOnFileEx(file,0x40,0,false,IntPtr.Zero,out var stream));return stream;}
 public static int Run(string report,string file)
 {
  var results=new List<object>();int failed=0;void Add(string name,bool passed,object? detail=null){results.Add(new{name,passed,detail});if(!passed)failed++;}
  try
  {
   Guid thumbClass=new(IntegrationIds.Thumbnail);
   Marshal.ThrowExceptionForHR(CoCreateInstance(in thumbClass,IntPtr.Zero,1,in ThumbnailIid,out var thumbnail));
   Marshal.ThrowExceptionForHR(Marshal.QueryInterface(thumbnail,in StreamInit,out var init));
   IntPtr stream=Open(file);Marshal.ThrowExceptionForHR(Method<InitStream>(init,3)(init,stream,0));Marshal.Release(stream);
   foreach(uint size in new uint[]{256,32})
   {
    int hr=Method<GetThumbnail>(thumbnail,3)(thumbnail,size,out var handle,out var alpha);
    if(hr!=0){Add($"Native thumbnail {size}",false,$"0x{hr:X8}");continue;}
    using(var image=Image.FromHbitmap(handle)){bool ok=Math.Max(image.Width,image.Height)==size;Add($"Native thumbnail fills {size} box",ok,new{image.Width,image.Height,alpha});if(size==256)image.Save(Path.ChangeExtension(report,"thumbnail.png"));}
    DeleteObject(handle);
   }
   Marshal.Release(init);Marshal.Release(thumbnail);
  }
  catch(Exception ex){Add("Native thumbnail activation",false,ex.Message);}
  try
  {
   // CLSCTX_LOCAL_SERVER: COM starts the class in its registered surrogate, Preview Host.
   Capture.Clear("preview",file);
   Guid previewClass=new(IntegrationIds.Preview);
   int created=CoCreateInstance(in previewClass,IntPtr.Zero,4,in PreviewIid,out var preview);
   Add("Preview Host activates the native preview handler",created==0,$"0x{created:X8}");
   if(created==0)
   {
    Marshal.ThrowExceptionForHR(Marshal.QueryInterface(preview,in StreamInit,out var init));
    IntPtr stream=Open(file);Marshal.ThrowExceptionForHR(Method<InitStream>(init,3)(init,stream,0));Marshal.Release(stream);
    using var host=new Form{ClientSize=new Size(420,620),ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new Point(20,20)};host.Show();
    var rect=new Rect{Right=420,Bottom=620};
    Marshal.ThrowExceptionForHR(Method<SetWindow>(preview,3)(preview,host.Handle,ref rect));
    Marshal.ThrowExceptionForHR(Method<Call>(preview,5)(preview));
    JsonElement? record=null;var until=DateTime.UtcNow.AddSeconds(20);
    while(record==null&&DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(100);record=Capture.Read("preview",file);}
    for(int i=0;i<10;i++){Application.DoEvents();Thread.Sleep(50);}
    Add("Preview Host renders the file",Capture.Succeeded(record),record?.ToString());
    using(var shot=new Bitmap(420,620)){using(var g=Graphics.FromImage(shot)){IntPtr dc=g.GetHdc();try{PrintWindow(host.Handle,dc,2);}finally{g.ReleaseHdc(dc);}}shot.Save(Path.ChangeExtension(report,"preview.png"));}
    Method<Call>(preview,6)(preview);Marshal.Release(init);Marshal.Release(preview);host.Close();
   }
  }
  catch(Exception ex){Add("Native preview activation",false,ex.ToString());}
  File.WriteAllText(report,JsonSerializer.Serialize(new{failed,results},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;
 }
}
