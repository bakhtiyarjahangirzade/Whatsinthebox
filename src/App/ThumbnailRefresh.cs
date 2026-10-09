using System.Runtime.InteropServices;
using Whatsinthebox.UI;

namespace Whatsinthebox;

static class ThumbnailRefresh
{
    [DllImport("ole32.dll")] static extern int CoCreateInstance(in Guid clsid, IntPtr outer, uint context, in Guid iid, out IntPtr obj);
    [DllImport("ole32.dll")] static extern int CoInitializeEx(IntPtr reserved, uint flags);
    [DllImport("ole32.dll")] static extern void CoUninitialize();
    [DllImport("shell32.dll", CharSet=CharSet.Unicode)] static extern int SHCreateItemFromParsingName(string path, IntPtr context, in Guid iid, out IntPtr item);
    [DllImport("shell32.dll", CharSet=CharSet.Unicode)] static extern void SHChangeNotify(uint id, uint flags, string path, IntPtr other);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int GetThumbnail(IntPtr self, IntPtr item, uint size, uint flags, out IntPtr bitmap, out uint cacheFlags, out Guid thumbnailId);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int GetBitmap(IntPtr self, out IntPtr bitmap);
    static T Method<T>(IntPtr obj, int slot) where T:Delegate => Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(obj), slot*IntPtr.Size));
    static readonly Guid CacheClass=new("50ef4544-ac9f-4a8e-b21b-8a26180db13f"), CacheInterface=new("f676c15d-596a-4ce2-8234-33996f445db1"), ItemInterface=new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
    public static readonly string[] Extensions=SupportedFiles.ImageExtensions.Concat(new[]{".pdf",".ai"}).ToArray();
    public static List<Size> Refresh(string path)
    {
        if(!File.Exists(path)||!Extensions.Contains(Path.GetExtension(path).ToLowerInvariant()))throw new IOException(L.T("format.unsupported"));
        int initialized=CoInitializeEx(IntPtr.Zero,2);Marshal.ThrowExceptionForHR(initialized);
        IntPtr cache=IntPtr.Zero,item=IntPtr.Zero;var results=new List<Size>();
        try
        {
            Marshal.ThrowExceptionForHR(CoCreateInstance(in CacheClass,IntPtr.Zero,1,in CacheInterface,out cache));
            Marshal.ThrowExceptionForHR(SHCreateItemFromParsingName(Path.GetFullPath(path),IntPtr.Zero,in ItemInterface,out item));
            foreach(uint size in new uint[]{96,256,768,1024})
            {
                IntPtr shared=IntPtr.Zero;
                try
                {
                    // Re-extract rather than resize a stale cached image. ISharedBitmap owns its HBITMAP.
                    Marshal.ThrowExceptionForHR(Method<GetThumbnail>(cache,3)(cache,item,size,4,out shared,out _,out _));
                    Marshal.ThrowExceptionForHR(Method<GetBitmap>(shared,3)(shared,out var handle));
                    using var image=Image.FromHbitmap(handle);results.Add(image.Size);
                }
                finally{if(shared!=IntPtr.Zero)Marshal.Release(shared);}
            }
            SHChangeNotify(0x2000,0x5,Path.GetFullPath(path),IntPtr.Zero);
            return results;
        }
        finally{if(item!=IntPtr.Zero)Marshal.Release(item);if(cache!=IntPtr.Zero)Marshal.Release(cache);CoUninitialize();}
    }
    static bool IsCached(string path)
    {
        int initialized=CoInitializeEx(IntPtr.Zero,2);if(initialized<0)return false;
        IntPtr cache=IntPtr.Zero,item=IntPtr.Zero,shared=IntPtr.Zero;
        try
        {
            if(CoCreateInstance(in CacheClass,IntPtr.Zero,1,in CacheInterface,out cache)<0||SHCreateItemFromParsingName(path,IntPtr.Zero,in ItemInterface,out item)<0)return false;
            int hr=Method<GetThumbnail>(cache,3)(cache,item,96,1,out shared,out _,out _);return hr>=0;
        }
        finally{if(shared!=IntPtr.Zero)Marshal.Release(shared);if(item!=IntPtr.Zero)Marshal.Release(item);if(cache!=IntPtr.Zero)Marshal.Release(cache);CoUninitialize();}
    }
    public static int RefreshCommonFolders(CancellationToken cancellation=default)
    {
        var until=DateTime.UtcNow.AddSeconds(60);int refreshed=0;
        var folders=new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"),Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)};
        foreach(var folder in folders.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if(!Directory.Exists(folder)||folder.StartsWith(@"\\"))continue;
            foreach(var path in ExistingFiles(folder,cancellation))
            {
                if(cancellation.IsCancellationRequested||DateTime.UtcNow>=until)return refreshed;
                if(!Extensions.Contains(Path.GetExtension(path).ToLowerInvariant()))continue;
                try{if((File.GetAttributes(path)&(FileAttributes.Offline|(FileAttributes)0x400000|(FileAttributes)0x40000))!=0)continue;if(IsCached(path)){Refresh(path);refreshed++;}}catch{}
            }
        }
        return refreshed;
    }
    static IEnumerable<string> ExistingFiles(string root,CancellationToken cancellation)
    {
        var pending=new Queue<(string Path,int Depth)>();pending.Enqueue((root,0));int inspected=0,folders=0;
        while(pending.Count>0&&inspected<512&&folders++<64&&!cancellation.IsCancellationRequested)
        {
            var folder=pending.Dequeue();string[] entries;
            try{entries=Directory.GetFileSystemEntries(folder.Path).Take(512-inspected).ToArray();}catch{continue;}
            foreach(string path in entries)
            {
                if(cancellation.IsCancellationRequested)yield break;
                FileAttributes attributes;try{attributes=File.GetAttributes(path);}catch{continue;}
                if((attributes&(FileAttributes.ReparsePoint|FileAttributes.Offline|(FileAttributes)0x400000|(FileAttributes)0x40000))!=0)continue;
                if((attributes&FileAttributes.Directory)!=0){if(folder.Depth<2)pending.Enqueue((path,folder.Depth+1));continue;}
                inspected++;yield return path;if(inspected>=512)yield break;
            }
        }
    }
}
