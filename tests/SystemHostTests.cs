using System.Runtime.InteropServices;
using System.Text.Json;
using Whatsinthebox.Shell;
using Whatsinthebox.UI;
static class SystemHostTests
{
 [DllImport("ole32.dll")]static extern int CoCreateInstance(in Guid clsid,IntPtr outer,uint context,in Guid iid,out IntPtr obj);
 [DllImport("ole32.dll")]static extern int CoInitializeEx(IntPtr reserved,uint flags);
 [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr hwnd,IntPtr dc,uint flags);
 [DllImport("user32.dll",SetLastError=true)]static extern IntPtr SendMessageTimeout(IntPtr hwnd,uint message,IntPtr wp,IntPtr lp,uint flags,uint timeout,out IntPtr result);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr bitmap);
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHCreateItemFromParsingName(string path,IntPtr context,in Guid iid,out IntPtr item);
 [StructLayout(LayoutKind.Sequential)]struct NativeSize {public int cx,cy;}
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetImage(IntPtr self,NativeSize size,uint flags,out IntPtr bitmap);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int InitFile(IntPtr self,[MarshalAs(UnmanagedType.LPWStr)]string path,uint mode);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int SetWindow(IntPtr self,IntPtr parent,ref Rect rect);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Call(IntPtr self);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetWindow(IntPtr self,out IntPtr hwnd);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetThumbnail(IntPtr self,uint size,out IntPtr bitmap,out uint alpha);
 static T Method<T>(IntPtr ptr,int slot)where T:Delegate=>Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ptr),slot*IntPtr.Size));
 public static int Run(string report,string pdf,bool thumbnailsOnly=false)
 {
  void Stage(string value)=>File.WriteAllText(report+".stage",value);
  var results=new List<object>();int failed=0;int nativeWidth=0,nativeHeight=0;
  try
  {
   Marshal.ThrowExceptionForHR(CoInitializeEx(IntPtr.Zero,2));
   Guid cacheClass=new("50ef4544-ac9f-4a8e-b21b-8a26180db13f"),cacheIid=new("f676c15d-596a-4ce2-8234-33996f445db1");
   int cacheHr=CoCreateInstance(in cacheClass,IntPtr.Zero,1,in cacheIid,out var cache);if(cache!=IntPtr.Zero)Marshal.Release(cache);
   results.Add(new{name="Windows thumbnail cache available",passed=cacheHr==0,hresult=$"0x{cacheHr:X8}"});if(cacheHr!=0)failed++;
   string embedded=Path.Combine(PreviewStorage.Root,"embedded-test.json");if(File.Exists(embedded))File.Delete(embedded);
   Guid thumbClass=new(IntegrationIds.Thumbnail),thumbIid=typeof(IThumbnailProvider).GUID,fileIid=typeof(IInitializeWithFile).GUID;
   Stage("thumbnail activation");Marshal.ThrowExceptionForHR(CoCreateInstance(in thumbClass,IntPtr.Zero,4,in thumbIid,out var thumbnail));Stage("thumbnail initialization");Marshal.ThrowExceptionForHR(Marshal.QueryInterface(thumbnail,in fileIid,out var input));Marshal.ThrowExceptionForHR(Method<InitFile>(input,3)(input,pdf,0));
   Stage("thumbnail render");Marshal.ThrowExceptionForHR(Method<GetThumbnail>(thumbnail,3)(thumbnail,256,out var handle,out var alpha));Stage("thumbnail received");
   using(var image=Image.FromHbitmap(handle)){nativeWidth=image.Width;nativeHeight=image.Height;bool passed=image.Width>20&&image.Height>20;results.Add(new{name="Windows out-of-process PDF thumbnail",passed,width=image.Width,height=image.Height,alpha});if(!passed)failed++;image.Save(Path.ChangeExtension(report,"thumbnail.png"));}DeleteObject(handle);
   Stage("small thumbnail bounds");Marshal.ThrowExceptionForHR(Method<GetThumbnail>(thumbnail,3)(thumbnail,16,out var smallHandle,out _));
   try{using var small=Image.FromHbitmap(smallHandle);bool passed=small.Width>0&&small.Height>0&&small.Width<=16&&small.Height<=16&&Math.Abs((double)small.Width*nativeHeight-(double)small.Height*nativeWidth)<=(nativeWidth+nativeHeight)/2d;results.Add(new{name="Small thumbnail respects requested size and source aspect",passed,width=small.Width,height=small.Height});if(!passed)failed++;}finally{DeleteObject(smallHandle);}
   Marshal.Release(input);Marshal.Release(thumbnail);
   Guid imageIid=new("bcc18b79-ba16-442f-80c4-8a59c30c463b");Stage("Windows Shell thumbnail pipeline");int shellHr=SHCreateItemFromParsingName(pdf,IntPtr.Zero,in imageIid,out var shell);
   if(shellHr==0){try{shellHr=Method<GetImage>(shell,3)(shell,new NativeSize{cx=256,cy=256},8,out var shellBitmap);if(shellHr==0){using var shellImage=Image.FromHbitmap(shellBitmap);bool aspect=Math.Abs((double)shellImage.Width/shellImage.Height-(double)nativeWidth/nativeHeight)<0.015;results.Add(new{name="Windows cached thumbnail preserves rendered aspect",passed=aspect,width=shellImage.Width,height=shellImage.Height});if(!aspect)failed++;shellImage.Save(Path.ChangeExtension(report,"shell-thumbnail.png"));DeleteObject(shellBitmap);}}finally{Marshal.Release(shell);}}
   results.Add(new{name="Windows Shell PDF thumbnail lookup",passed=shellHr==0,hresult=$"0x{shellHr:X8}"});if(shellHr!=0)failed++;
   if(thumbnailsOnly){File.WriteAllText(report,JsonSerializer.Serialize(new{failed,results},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;}
   Guid previewClass=new(IntegrationIds.Preview),previewIid=typeof(IPreviewHandler).GUID,oleIid=typeof(IOleWindow).GUID;
   Stage("native preview bridge activation");Marshal.ThrowExceptionForHR(CoCreateInstance(in previewClass,IntPtr.Zero,1,in previewIid,out var preview));Stage("preview initialization");Marshal.ThrowExceptionForHR(Marshal.QueryInterface(preview,in fileIid,out var initialization));Marshal.ThrowExceptionForHR(Method<InitFile>(initialization,3)(initialization,pdf,0));
   using var parent=new Form{ClientSize=new Size(650,760),ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new Point(-3000,-3000)};parent.Show();var rect=new Rect{Right=650,Bottom=760};Stage("preview SetWindow");Marshal.ThrowExceptionForHR(Method<SetWindow>(preview,3)(preview,parent.Handle,ref rect));Stage("preview DoPreview");Marshal.ThrowExceptionForHR(Method<Call>(preview,5)(preview));Stage("preview window");Marshal.ThrowExceptionForHR(Marshal.QueryInterface(preview,in oleIid,out var ole));Marshal.ThrowExceptionForHR(Method<GetWindow>(ole,3)(ole,out var child));
   bool rendered=false;var until=DateTime.UtcNow.AddSeconds(20);
   using var shot=new Bitmap(650,760);
   while(DateTime.UtcNow<until)
   {
    Application.DoEvents();Thread.Sleep(100);Stage("waiting for server-owned preview capture");
    if(File.Exists(embedded)){using var state=JsonDocument.Parse(File.ReadAllText(embedded));if(state.RootElement.TryGetProperty("success",out var ok)&&ok.GetBoolean()){rendered=true;File.Copy(Path.Combine(PreviewStorage.Root,"embedded-test.png"),Path.ChangeExtension(report,"preview.png"),true);break;}}
   }
   results.Add(new{name="Windows out-of-process PDF preview renders",passed=rendered,evidence="server-owned capture after real out-of-process activation"});if(!rendered)failed++;
   Marshal.ThrowExceptionForHR(Method<Call>(preview,6)(preview));Marshal.Release(ole);Marshal.Release(initialization);Marshal.Release(preview);parent.Close();
  }
  catch(Exception ex){failed++;results.Add(new{name="Windows preview-host integration",passed=false,error=ex.ToString()});}
  File.WriteAllText(report,JsonSerializer.Serialize(new{failed,results},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;
 }
}
