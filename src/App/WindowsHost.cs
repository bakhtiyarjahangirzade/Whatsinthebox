namespace Whatsinthebox;
// Versions before 1.1 kept a background COM host running. Explorer now loads the
// native handlers directly; this only stops such a host during upgrade or removal.
static class WindowsHost
{
 public static string HostName=>"Local\\Whatsinthebox.WindowsHost."+System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value;
 public static void Stop(){if(EventWaitHandle.TryOpenExisting(HostName+".stop",out var signal)){using(signal)signal.Set();}}
}
