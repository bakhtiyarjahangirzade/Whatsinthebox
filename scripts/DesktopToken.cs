using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
public static class DesktopToken
{
 [StructLayout(LayoutKind.Sequential)]struct SidAttributes{public IntPtr Sid;public uint Attributes;}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct Startup{public int Size;public string Reserved,Desktop,Title;public uint X,Y,Width,Height,CharsX,CharsY,Fill,Flags;public ushort Show,ReservedSize;public IntPtr ReservedPointer,Input,Output,Error;}
 [StructLayout(LayoutKind.Sequential)]struct ProcessInfo{public IntPtr Process,Thread;public uint Id,ThreadId;}
 [DllImport("kernel32.dll")]static extern IntPtr GetCurrentProcess();
 [DllImport("advapi32.dll",SetLastError=true)]static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
 [DllImport("advapi32.dll",SetLastError=true)]static extern bool CreateRestrictedToken(IntPtr existing,uint flags,uint disableCount,ref SidAttributes disable,uint deletePrivileges,IntPtr privileges,uint restrictCount,IntPtr restrict,out IntPtr token);
 [DllImport("advapi32.dll",SetLastError=true)]static extern bool SetTokenInformation(IntPtr token,int kind,ref SidAttributes value,int length);
 [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool ConvertStringSidToSid(string value,out IntPtr sid);
 [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool CreateProcessAsUser(IntPtr token,string application,StringBuilder command,IntPtr processAttributes,IntPtr threadAttributes,bool inherit,uint flags,IntPtr environment,string directory,ref Startup startup,out ProcessInfo info);
 [DllImport("advapi32.dll")]static extern uint GetLengthSid(IntPtr sid);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
 [DllImport("kernel32.dll")]static extern IntPtr LocalFree(IntPtr pointer);
 [DllImport("kernel32.dll")]static extern uint WaitForSingleObject(IntPtr handle,uint milliseconds);
 [DllImport("kernel32.dll")]static extern bool GetExitCodeProcess(IntPtr handle,out uint exit);
 public static int Run(string application,string arguments,string directory)
 {
  IntPtr original=IntPtr.Zero,token=IntPtr.Zero,admin=IntPtr.Zero,medium=IntPtr.Zero;ProcessInfo info=new ProcessInfo();
  try{
   if(!OpenProcessToken(GetCurrentProcess(),0xF01FF,out original)||!ConvertStringSidToSid("S-1-5-32-544",out admin)||!ConvertStringSidToSid("S-1-16-8192",out medium))throw new Win32Exception(Marshal.GetLastWin32Error());
   var disabled=new SidAttributes{Sid=admin};if(!CreateRestrictedToken(original,1,1,ref disabled,0,IntPtr.Zero,0,IntPtr.Zero,out token))throw new Win32Exception(Marshal.GetLastWin32Error());
   var integrity=new SidAttributes{Sid=medium,Attributes=0x20};if(!SetTokenInformation(token,25,ref integrity,Marshal.SizeOf(typeof(SidAttributes))+(int)GetLengthSid(medium)))throw new Win32Exception(Marshal.GetLastWin32Error());
   var startup=new Startup{Size=Marshal.SizeOf(typeof(Startup)),Desktop="winsta0\\default"};
   if(!CreateProcessAsUser(token,application,new StringBuilder("\""+application+"\" "+arguments),IntPtr.Zero,IntPtr.Zero,false,0,IntPtr.Zero,directory,ref startup,out info))throw new Win32Exception(Marshal.GetLastWin32Error());
   if(WaitForSingleObject(info.Process,240000)!=0)throw new TimeoutException("Medium desktop process timed out");uint result;GetExitCodeProcess(info.Process,out result);return (int)result;
  }finally{foreach(var handle in new[]{info.Thread,info.Process,token,original})if(handle!=IntPtr.Zero)CloseHandle(handle);if(admin!=IntPtr.Zero)LocalFree(admin);if(medium!=IntPtr.Zero)LocalFree(medium);}
 }
}
