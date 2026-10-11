using System.Runtime.InteropServices;
using Microsoft.Win32;
namespace Whatsinthebox.UI;
// Read-only view of the handlers Explorer resolves for an actual file. The association
// array includes the user's default-app ProgID, the extension, SystemFileAssociations
// and perceived-type entries in the same order the Shell uses for a selected item.
public static class Associations
{
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHCreateItemFromParsingName(string path,IntPtr context,in Guid iid,out IntPtr item);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int BindToHandler(IntPtr self,IntPtr context,in Guid handler,in Guid iid,out IntPtr result);
    [UnmanagedFunctionPointer(CallingConvention.StdCall,CharSet=CharSet.Unicode)]delegate int GetString(IntPtr self,uint flags,uint kind,string? extra,System.Text.StringBuilder? output,ref uint length);
    static T Method<T>(IntPtr ptr,int slot)where T:Delegate=>Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ptr),slot*IntPtr.Size));
    static readonly Guid ItemInterface=new("43826d1e-e718-42ee-bc55-a1e261c37bfe"),AssociationArray=new("bea9ef17-82f1-4f60-9284-4f8db75c3be9"),QueryInterface=new("c46ca590-3c3f-11d2-bee6-0000f805ca57");
    public static string ForFile(string path,string handlerInterface)
    {
        IntPtr item=IntPtr.Zero,query=IntPtr.Zero;
        try
        {
            int hr=SHCreateItemFromParsingName(Path.GetFullPath(path),IntPtr.Zero,in ItemInterface,out item);if(hr<0)return $"item 0x{hr:X8}";
            hr=Method<BindToHandler>(item,3)(item,IntPtr.Zero,in AssociationArray,in QueryInterface,out query);if(hr<0)return $"associations 0x{hr:X8}";
            var output=new System.Text.StringBuilder(260);uint length=(uint)output.Capacity;
            hr=Method<GetString>(query,4)(query,0,16,handlerInterface,output,ref length);return hr<0?$"none 0x{hr:X8}":output.ToString();
        }
        finally{if(query!=IntPtr.Zero)Marshal.Release(query);if(item!=IntPtr.Zero)Marshal.Release(item);}
    }
    // Windows 11 24H2 and later can store the default app under UserChoiceLatest.
    public static IEnumerable<string> UserProgIds(string extension)
    {
        string root=$@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}";
        var values=new List<string?>();
        using(var choice=Registry.CurrentUser.OpenSubKey(root+@"\UserChoice"))values.Add(choice?.GetValue("ProgId") as string);
        using(var latest=Registry.CurrentUser.OpenSubKey(root+@"\UserChoiceLatest"))
        {
            values.Add(latest?.GetValue("ProgId") as string);
            using var nested=latest?.OpenSubKey("ProgId");values.Add(nested?.GetValue("ProgId") as string);values.Add(nested?.GetValue("") as string);
        }
        return values.Where(x=>!string.IsNullOrWhiteSpace(x)&&x!.IndexOfAny(new[]{'\\','/'})<0).Select(x=>x!).Distinct(StringComparer.OrdinalIgnoreCase);
    }
    public static object Inspect(string extension,string folder)
    {
        Directory.CreateDirectory(folder);string sample=Path.Combine(folder,"sample"+extension);
        bool created=!File.Exists(sample);if(created)File.WriteAllBytes(sample,Array.Empty<byte>());
        try{return new{extension,progIds=UserProgIds(extension).ToArray(),preview=ForFile(sample,IntegrationIds.PreviewInterface),thumbnail=ForFile(sample,IntegrationIds.ThumbnailInterface)};}
        finally{if(created){try{File.Delete(sample);}catch{}}}
    }
}
