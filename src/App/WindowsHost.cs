using System.Runtime.InteropServices;
using Whatsinthebox.Shell;
using Whatsinthebox.UI;
namespace Whatsinthebox;

[ComVisible(true),Guid("00000001-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IClassFactory
{
 [PreserveSig]int CreateInstance(IntPtr outer,in Guid iid,out IntPtr obj);
 [PreserveSig]int LockServer([MarshalAs(UnmanagedType.Bool)]bool locked);
}
[ComVisible(true),ClassInterface(ClassInterfaceType.None)]
public sealed class WindowsFactory : IClassFactory
{
 readonly bool thumbnail;
 public WindowsFactory(bool isThumbnail){thumbnail=isThumbnail;}
 public int CreateInstance(IntPtr outer,in Guid iid,out IntPtr obj)
 {
  obj=IntPtr.Zero;if(outer!=IntPtr.Zero)return unchecked((int)0x80040110);
   try{WindowsHost.LastRequest=DateTime.UtcNow;object instance=thumbnail?new ThumbnailProvider():new PreviewHandler();IntPtr unknown=Marshal.GetIUnknownForObject(instance);try{return Marshal.QueryInterface(unknown,in iid,out obj);}finally{Marshal.Release(unknown);}}catch(Exception ex){return Marshal.GetHRForException(ex);}
 }
 public int LockServer(bool locked){WindowsHost.LastRequest=DateTime.UtcNow;return 0;}
}
static class WindowsHost
{
 public static DateTime LastRequest=DateTime.UtcNow;
 public static string HostName=>"Local\\Whatsinthebox.WindowsHost."+System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value;
 [DllImport("ole32.dll")]static extern int CoRegisterClassObject(in Guid clsid,IntPtr unknown,uint context,uint flags,out uint cookie);
 [DllImport("ole32.dll")]static extern int CoRevokeClassObject(uint cookie);
 [DllImport("ole32.dll")]static extern int CoResumeClassObjects();
 [DllImport("ole32.dll")]static extern int CoInitializeEx(IntPtr reserved,uint mode);
 public static int Run()
 {
  using var mutex=new Mutex(true,HostName,out bool created);if(!created)return 0;
  using var stop=new EventWaitHandle(false,EventResetMode.ManualReset,HostName+".stop");
  var cookies=new List<uint>();var factories=new List<WindowsFactory>();
  try
  {
   Marshal.ThrowExceptionForHR(CoInitializeEx(IntPtr.Zero,2));
   using var anchor=new Control();anchor.CreateControl();_=anchor.Handle;HostDispatcher.Anchor=anchor;
   foreach(var pair in new[]{(IntegrationIds.Preview,false),(IntegrationIds.Thumbnail,true)})
   {
    var factory=new WindowsFactory(pair.Item2);factories.Add(factory);IntPtr unknown=Marshal.GetIUnknownForObject(factory);Guid id=new(pair.Item1);
    try{Marshal.ThrowExceptionForHR(CoRegisterClassObject(in id,unknown,4,5,out uint cookie));cookies.Add(cookie);}finally{Marshal.Release(unknown);}
   }
   Marshal.ThrowExceptionForHR(CoResumeClassObjects());
   _=Updates.CheckAsync();
   var nextUpdate=DateTime.UtcNow.AddHours(24);
   using var context=new ApplicationContext();using var timer=new System.Windows.Forms.Timer{Interval=500};timer.Tick+=(_,_)=>{if(stop.WaitOne(0))context.ExitThread();if(DateTime.UtcNow>=nextUpdate){nextUpdate=DateTime.UtcNow.AddHours(24);_=Updates.CheckAsync();}};timer.Start();Application.Run(context);return 0;
  }
  finally{foreach(uint cookie in cookies)CoRevokeClassObject(cookie);HostDispatcher.Anchor=null;GC.KeepAlive(factories);}
 }
 public static void Stop(){if(EventWaitHandle.TryOpenExisting(HostName+".stop",out var signal)){using(signal)signal.Set();}}
}
