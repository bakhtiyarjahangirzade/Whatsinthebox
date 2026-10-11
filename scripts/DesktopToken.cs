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
 [DllImport("advapi32.dll",SetLastError=true)]static extern bool SetTokenInformation(IntPtr token,int kind,ref IntPtr value,int length);
 [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string value,uint revision,out IntPtr descriptor,IntPtr size);
 [DllImport("advapi32.dll",SetLastError=true)]static extern bool GetSecurityDescriptorDacl(IntPtr descriptor,out bool present,out IntPtr dacl,out bool defaulted);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
 [DllImport("kernel32.dll")]static extern IntPtr LocalFree(IntPtr pointer);
 [DllImport("kernel32.dll")]static extern uint WaitForSingleObject(IntPtr handle,uint milliseconds);
 [DllImport("kernel32.dll")]static extern bool GetExitCodeProcess(IntPtr handle,out uint exit);
 // The runner's unfiltered administrator token owns new objects as Administrators and
 // its default DACL grants only Administrators and SYSTEM. Once Administrators is
 // deny-only, child processes created by this token cannot be opened by their own
 // creator, so CreateProcess fails with ERROR_ACCESS_DENIED. Make the user the owner
 // and grant the user and SYSTEM in the default DACL, as a filtered UAC token does.
 public static int Run(string application,string arguments,string directory,string userSid)
 {
  IntPtr original=IntPtr.Zero,token=IntPtr.Zero,admin=IntPtr.Zero,medium=IntPtr.Zero,user=IntPtr.Zero,descriptor=IntPtr.Zero;ProcessInfo info=new ProcessInfo();
  try{
   if(!OpenProcessToken(GetCurrentProcess(),0xF01FF,out original)||!ConvertStringSidToSid("S-1-5-32-544",out admin)||!ConvertStringSidToSid("S-1-16-8192",out medium))throw new Win32Exception(Marshal.GetLastWin32Error());
   var disabled=new SidAttributes{Sid=admin};if(!CreateRestrictedToken(original,1,1,ref disabled,0,IntPtr.Zero,0,IntPtr.Zero,out token))throw new Win32Exception(Marshal.GetLastWin32Error());
   var integrity=new SidAttributes{Sid=medium,Attributes=0x20};if(!SetTokenInformation(token,25,ref integrity,Marshal.SizeOf(typeof(SidAttributes))+(int)GetLengthSid(medium)))throw new Win32Exception(Marshal.GetLastWin32Error());
   if(!ConvertStringSidToSid(userSid,out user)||!SetTokenInformation(token,4,ref user,IntPtr.Size))throw new Win32Exception(Marshal.GetLastWin32Error());
   bool present,defaulted;IntPtr dacl;
   if(!ConvertStringSecurityDescriptorToSecurityDescriptor("D:(A;;GA;;;"+userSid+")(A;;GA;;;SY)",1,out descriptor,IntPtr.Zero)||!GetSecurityDescriptorDacl(descriptor,out present,out dacl,out defaulted)||!SetTokenInformation(token,6,ref dacl,IntPtr.Size))throw new Win32Exception(Marshal.GetLastWin32Error());
   var startup=new Startup{Size=Marshal.SizeOf(typeof(Startup)),Desktop="winsta0\\default"};
   if(!CreateProcessAsUser(token,application,new StringBuilder("\""+application+"\" "+arguments),IntPtr.Zero,IntPtr.Zero,false,0,IntPtr.Zero,directory,ref startup,out info))throw new Win32Exception(Marshal.GetLastWin32Error());
   if(WaitForSingleObject(info.Process,900000)!=0)throw new TimeoutException("Medium desktop process timed out");uint result;GetExitCodeProcess(info.Process,out result);return (int)result;
  }finally{foreach(var handle in new[]{info.Thread,info.Process,token,original})if(handle!=IntPtr.Zero)CloseHandle(handle);if(admin!=IntPtr.Zero)LocalFree(admin);if(medium!=IntPtr.Zero)LocalFree(medium);if(user!=IntPtr.Zero)LocalFree(user);if(descriptor!=IntPtr.Zero)LocalFree(descriptor);}
 }
}
