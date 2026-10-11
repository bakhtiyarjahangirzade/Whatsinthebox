using System.Runtime.InteropServices;
using System.Text.Json;
using Whatsinthebox.UI;
// Per-format check through the real Shell association array and thumbnail pipeline,
// as an ordinary user after registration. The application records which fixture it
// rendered, so a Windows built-in image thumbnail cannot be mistaken for ours.
static class ShellFormatTests
{
 [DllImport("ole32.dll")]static extern int CoInitializeEx(IntPtr reserved,uint flags);
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHCreateItemFromParsingName(string path,IntPtr context,in Guid iid,out IntPtr item);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr bitmap);
 [StructLayout(LayoutKind.Sequential)]struct NativeSize{public int cx,cy;}
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetImage(IntPtr self,NativeSize size,uint flags,out IntPtr bitmap);
 static T Method<T>(IntPtr ptr,int slot)where T:Delegate=>Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ptr),slot*IntPtr.Size));
 public static int Run(string report,string folder)
 {
  var results=new List<object>();int failed=0;
  CoInitializeEx(IntPtr.Zero,2);
  string log=Path.Combine(PreviewStorage.Root,"thumbnail-test.log");
  var thumbnails=new HashSet<string>(SupportedFiles.ImageExtensions.Concat(new[]{".pdf",".ai"}),StringComparer.OrdinalIgnoreCase);
  foreach(string file in Directory.GetFiles(folder).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
  {
   string ext=Path.GetExtension(file).ToLowerInvariant();
   try
   {
    string preview=Associations.ForFile(file,IntegrationIds.PreviewInterface);bool previewOk=preview.Equals(IntegrationIds.Preview,StringComparison.OrdinalIgnoreCase);
    results.Add(new{name="Shell resolves Whatsinthebox preview for "+ext,passed=previewOk,resolved=preview});if(!previewOk)failed++;
    if(!thumbnails.Contains(ext))continue;
    string thumb=Associations.ForFile(file,IntegrationIds.ThumbnailInterface);bool thumbOk=thumb.Equals(IntegrationIds.Thumbnail,StringComparison.OrdinalIgnoreCase);
    results.Add(new{name="Shell resolves Whatsinthebox thumbnail for "+ext,passed=thumbOk,resolved=thumb});if(!thumbOk)failed++;
    if(File.Exists(log))File.Delete(log);
    Guid factoryId=new("bcc18b79-ba16-442f-80c4-8a59c30c463b");
    Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(file,IntPtr.Zero,in factoryId,out var factory));
    int width=0,height=0,hr;
    try{hr=Method<GetImage>(factory,3)(factory,new NativeSize{cx=256,cy=256},0x8,out var bitmap);if(hr==0){using(var image=Image.FromHbitmap(bitmap)){width=image.Width;height=image.Height;image.Save(Path.ChangeExtension(report,ext.TrimStart('.')+"-shell-thumbnail.png"));}DeleteObject(bitmap);}}
    finally{Marshal.Release(factory);}
    JsonElement? record=null;
    if(File.Exists(log))foreach(var line in File.ReadAllLines(log)){using var json=JsonDocument.Parse(line);if(json.RootElement.GetProperty("source").GetString()!.Equals(Path.GetFileName(file),StringComparison.OrdinalIgnoreCase))record=json.RootElement.Clone();}
    bool ours=hr==0&&record!=null;
    bool aspect=false;double expected=0;
    if(ours){expected=record!.Value.GetProperty("sourceWidth").GetDouble()/record.Value.GetProperty("sourceHeight").GetDouble();aspect=Math.Max(width,height)>=200&&Math.Abs((double)width/height-expected)<=Math.Max(0.02,expected*2.0/Math.Min(width,height));}
    results.Add(new{name="Shell thumbnail rendered by Whatsinthebox for "+ext,passed=ours,hresult=$"0x{hr:X8}",width,height});if(!ours)failed++;
    results.Add(new{name="Shell thumbnail keeps source aspect and fills 256 box for "+ext,passed=aspect,width,height,expectedAspect=Math.Round(expected,4)});if(!aspect)failed++;
   }
   catch(Exception ex){failed++;results.Add(new{name="Shell format "+ext,passed=false,error=ex.ToString()});}
  }
  File.WriteAllText(report,JsonSerializer.Serialize(new{failed,results},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;
 }
}
