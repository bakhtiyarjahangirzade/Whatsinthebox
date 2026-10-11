using System.Runtime.InteropServices;
using System.Text.Json;
using Whatsinthebox.UI;
// Per-format check through the real Shell association array and thumbnail pipeline
// (thumbnail cache and isolation surrogate), as an ordinary user after registration.
// The native handler records what it rendered, so a Windows built-in thumbnail
// cannot be mistaken for ours.
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
  var results=new List<object>();int failed=0;void Add(object row,bool passed){results.Add(row);if(!passed)failed++;}
  CoInitializeEx(IntPtr.Zero,2);
  // Formats the installer left to a handler that was already present on this PC.
  string decisions=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Whatsinthebox","integration.json");
  using var plan=JsonDocument.Parse(File.ReadAllText(decisions));
  var ownPreview=plan.RootElement.GetProperty("preview").EnumerateArray().Select(x=>x.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
  var ownThumbnail=plan.RootElement.GetProperty("thumbnail").EnumerateArray().Select(x=>x.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
  results.Add(new{name="Installer decisions",passed=true,plan=plan.RootElement.ToString()});
  foreach(string file in Directory.GetFiles(folder).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
  {
   string ext=Path.GetExtension(file).ToLowerInvariant();
   try
   {
    bool previewExpected=ownPreview.Contains(ext),thumbExpected=ownThumbnail.Contains(ext);
    string preview=Associations.ForFile(file,IntegrationIds.PreviewInterface),thumb=Associations.ForFile(file,IntegrationIds.ThumbnailInterface);
    // Formats Windows renders itself must stay with Windows.
    bool previewOk=previewExpected?preview.Equals(IntegrationIds.Preview,StringComparison.OrdinalIgnoreCase):!preview.Equals(IntegrationIds.Preview,StringComparison.OrdinalIgnoreCase);
    bool thumbOk=thumbExpected?thumb.Equals(IntegrationIds.Thumbnail,StringComparison.OrdinalIgnoreCase):!thumb.Equals(IntegrationIds.Thumbnail,StringComparison.OrdinalIgnoreCase);
    Add(new{name=(previewExpected?"Whatsinthebox":"Windows")+" preview handler for "+ext,passed=previewOk,resolved=preview},previewOk);
    Add(new{name=(thumbExpected?"Whatsinthebox":"Windows")+" thumbnail handler for "+ext,passed=thumbOk,resolved=thumb},thumbOk);
    Capture.Clear("thumbnail",file);
    Guid factoryId=new("bcc18b79-ba16-442f-80c4-8a59c30c463b");
    Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(file,IntPtr.Zero,in factoryId,out var factory));
    int width=0,height=0,hr;
    try{hr=Method<GetImage>(factory,3)(factory,new NativeSize{cx=256,cy=256},0x8,out var bitmap);if(hr==0){using(var image=Image.FromHbitmap(bitmap)){width=image.Width;height=image.Height;image.Save(Path.ChangeExtension(report,ext.TrimStart('.')+"-shell-thumbnail.png"));}DeleteObject(bitmap);}}
    finally{Marshal.Release(factory);}
    if(!thumbExpected){results.Add(new{name="Windows thumbnail for "+ext,passed=true,hresult=$"0x{hr:X8}",width,height});continue;}
    var record=Capture.Read("thumbnail",file);
    bool ours=hr==0&&Capture.Succeeded(record);
    Add(new{name="Shell thumbnail rendered by Whatsinthebox for "+ext,passed=ours,hresult=$"0x{hr:X8}",width,height,record=record?.ToString()},ours);
    bool fill=false;
    if(ours){int w=record!.Value.GetProperty("width").GetInt32(),h=record.Value.GetProperty("height").GetInt32();fill=Math.Max(w,h)==256&&Math.Abs((double)width/height-(double)w/h)<=0.02+2.0/Math.Min(width,height);}
    Add(new{name="Shell thumbnail keeps source aspect and fills the 256 box for "+ext,passed=fill,width,height},fill);
   }
   catch(Exception ex){Add(new{name="Shell format "+ext,passed=false,error=ex.ToString()},false);}
  }
  File.WriteAllText(report,JsonSerializer.Serialize(new{failed,results},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;
 }
}
