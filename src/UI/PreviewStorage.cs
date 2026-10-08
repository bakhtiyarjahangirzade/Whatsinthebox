using System.Runtime.InteropServices;
namespace Whatsinthebox.UI;
public static class PreviewStorage
{
    // Preview handlers run in a low-integrity host. LocalLow, unlike Local/Temp,
    // is writable there without disabling Windows preview isolation.
    public static string Root
    {
        get
        {
            Guid folder=new("A520A1A4-1780-4FF6-BD18-167343C5AF16");
            Marshal.ThrowExceptionForHR(SHGetKnownFolderPath(in folder,0,IntPtr.Zero,out var ptr));
            try{var root=Path.Combine(Marshal.PtrToStringUni(ptr)!,"Whatsinthebox","previews");Directory.CreateDirectory(root);return root;}finally{Marshal.FreeCoTaskMem(ptr);}
        }
    }
    [DllImport("shell32.dll")]static extern int SHGetKnownFolderPath(in Guid id,uint flags,IntPtr token,out IntPtr path);
}
