#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace Whatsinthebox.UI;
public static class WorkerLimits
{
    [StructLayout(LayoutKind.Sequential)] struct BasicLimits
    {
        public long ProcessTime,JobTime;public uint Flags;public nuint MinimumWorkingSet,MaximumWorkingSet;
        public uint ActiveProcesses;public nuint Affinity;public uint Priority,Scheduling;
    }
    [StructLayout(LayoutKind.Sequential)] struct IoCounters {public ulong ReadOperations,WriteOperations,OtherOperations,ReadBytes,WriteBytes,OtherBytes;}
    [StructLayout(LayoutKind.Sequential)] struct ExtendedLimits
    {
        public BasicLimits Basic;public IoCounters Io;public nuint ProcessMemory,JobMemory,PeakProcessMemory,PeakJobMemory;
    }
    sealed class JobHandle():SafeHandleZeroOrMinusOneIsInvalid(true)
    {
        protected override bool ReleaseHandle()=>CloseHandle(handle);
    }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern JobHandle CreateJobObject(IntPtr attributes,string? name);
    [DllImport("kernel32.dll",SetLastError=true)]static extern bool SetInformationJobObject(JobHandle job,int kind,ref ExtendedLimits limits,int size);
    [DllImport("kernel32.dll",SetLastError=true)]static extern bool AssignProcessToJobObject(JobHandle job,IntPtr process);
    [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
    public static IDisposable Attach(Process process)
    {
        var job=CreateJobObject(IntPtr.Zero,null);
        try
        {
            var limits=new ExtendedLimits{Basic=new BasicLimits{Flags=0x100|0x2000},ProcessMemory=768u*1024*1024};
            if(job.IsInvalid||!SetInformationJobObject(job,9,ref limits,Marshal.SizeOf<ExtendedLimits>())||!AssignProcessToJobObject(job,process.Handle))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            return job;
        }
        catch{try{process.Kill(true);}catch{}job.Dispose();throw;}
    }
}
