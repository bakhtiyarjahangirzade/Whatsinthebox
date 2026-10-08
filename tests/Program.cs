using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using Whatsinthebox.Shell;
using Whatsinthebox.UI;

[ComImport,Guid("00000001-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IClassFactory
{
 [PreserveSig]int CreateInstance(IntPtr outer,in Guid iid,out IntPtr obj);
 [PreserveSig]int LockServer([MarshalAs(UnmanagedType.Bool)]bool locked);
}
static class Program
{
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetClassObject(in Guid clsid,in Guid iid,out IntPtr result);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int CreateInstance(IntPtr self,IntPtr outer,in Guid iid,out IntPtr result);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int SetWindow(IntPtr self,IntPtr hwnd,ref Rect rect);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int SetRect(IntPtr self,ref Rect rect);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int SimpleCall(IntPtr self);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int InitFile(IntPtr self,[MarshalAs(UnmanagedType.LPWStr)]string file,uint mode);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int InitStream(IntPtr self,IntPtr stream,uint mode);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int GetWindow(IntPtr self,out IntPtr hwnd);
 static T Method<T>(IntPtr ptr,int slot) where T:Delegate=>Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ptr),slot*IntPtr.Size));
 [DllImport("shlwapi.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]static extern int SHCreateStreamOnFileEx(string file,uint mode,uint attributes,bool create,IStream? template,out IStream stream);
 [STAThread]static int Main(string[] args)
 {
  ApplicationConfiguration.Initialize();string report=args[0];var results=new List<object>();int failed=0;
  if(args.Length==3&&args[1]=="--thumbnails-only")return SystemHostTests.Run(report,args[2],true);
  if(args.Length==3&&args[1]=="--system-host")return SystemHostTests.Run(report,args[2]);
  var root=Path.Combine(Path.GetTempPath(),"Whatsinthebox-COM-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  try
  {
   var dll=NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory,"shell","Whatsinthebox.Shell.comhost.dll"));var fn=Marshal.GetDelegateForFunctionPointer<GetClassObject>(NativeLibrary.GetExport(dll,"DllGetClassObject"));Guid clsid=new(IntegrationIds.Preview),factoryId=typeof(IClassFactory).GUID;
   Marshal.ThrowExceptionForHR(fn(in clsid,in factoryId,out var factoryPtr));
   Guid previewId=typeof(IPreviewHandler).GUID;Marshal.ThrowExceptionForHR(Method<CreateInstance>(factoryPtr,3)(factoryPtr,IntPtr.Zero,in previewId,out var ptr));Marshal.Release(factoryPtr);
   Guid fileId=typeof(IInitializeWithFile).GUID,streamId=typeof(IInitializeWithStream).GUID,oleId=typeof(IOleWindow).GUID;
   Marshal.ThrowExceptionForHR(Marshal.QueryInterface(ptr,in fileId,out var filePtr));Marshal.ThrowExceptionForHR(Marshal.QueryInterface(ptr,in streamId,out var streamPtr));Marshal.ThrowExceptionForHR(Marshal.QueryInterface(ptr,in oleId,out var olePtr));
   results.Add(new{name="native COM activation and interface negotiation",passed=true});
   using var form=new Form{ClientSize=new Size(420,620)};var hwnd=form.Handle;var rect=new Rect{Right=420,Bottom=620};Marshal.ThrowExceptionForHR(Method<SetWindow>(ptr,3)(ptr,hwnd,ref rect));
   string png=Path.Combine(root,"fixture.png");using(var bitmap=new Bitmap(160,90)){using var g=Graphics.FromImage(bitmap);g.Clear(Color.Orange);bitmap.Save(png,System.Drawing.Imaging.ImageFormat.Png);}
   foreach(var streamMode in new[]{false,true})
   {
    if(streamMode){Marshal.ThrowExceptionForHR(SHCreateStreamOnFileEx(png,0x40,0,false,null,out var stream));var raw=Marshal.GetIUnknownForObject(stream);try{Guid sid=typeof(IStream).GUID;Marshal.ThrowExceptionForHR(Marshal.QueryInterface(raw,in sid,out var actual));try{Marshal.ThrowExceptionForHR(Method<InitStream>(streamPtr,3)(streamPtr,actual,0));}finally{Marshal.Release(actual);}}finally{Marshal.Release(raw);Marshal.ReleaseComObject(stream);}}else Marshal.ThrowExceptionForHR(Method<InitFile>(filePtr,3)(filePtr,png,0));
    Marshal.ThrowExceptionForHR(Method<SimpleCall>(ptr,5)(ptr));Marshal.ThrowExceptionForHR(Method<GetWindow>(olePtr,3)(olePtr,out var child));var view=Control.FromHandle(child)!;bool HasPreview()=>(bool)view.GetType().GetProperty("HasPreview")!.GetValue(view)!;var until=DateTime.UtcNow.AddSeconds(20);while(!HasPreview()&&DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(20);}bool success=HasPreview();if(!success)failed++;results.Add(new{name=streamMode?"COM stream initialization and rendering":"COM file initialization and rendering",passed=success});
    rect.Right=280;rect.Bottom=480;Marshal.ThrowExceptionForHR(Method<SetRect>(ptr,4)(ptr,ref rect));Application.DoEvents();using var screenshot=new Bitmap(view.Width,view.Height);view.DrawToBitmap(screenshot,view.ClientRectangle);screenshot.Save(Path.ChangeExtension(report,streamMode?"stream.png":"file.png"));Marshal.ThrowExceptionForHR(Method<SimpleCall>(ptr,6)(ptr));rect.Right=420;rect.Bottom=620;Marshal.ThrowExceptionForHR(Method<SetRect>(ptr,4)(ptr,ref rect));
   }
   results.Add(new{name="unload and resize",passed=true});
   Marshal.Release(filePtr);Marshal.Release(streamPtr);Marshal.Release(olePtr);Marshal.Release(ptr);
  }
  catch(Exception ex){failed++;results.Add(new{name="COM integration",passed=false,error=ex.ToString()});}
  finally{try{Directory.Delete(root,true);}catch{}}
  File.WriteAllText(report,JsonSerializer.Serialize(new{failed,results},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;
 }
}
