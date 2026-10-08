using System.Runtime.InteropServices;
using Whatsinthebox.UI;

namespace Whatsinthebox;

static class PdfPreviewPermission
{
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    static extern bool DeleteFileW(string file);
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)]
    static extern void SHChangeNotify(uint id,uint flags,string file,IntPtr other);

    public static int Prompt(string file)
    {
        try
        {
            file=Path.GetFullPath(file);
            if(!File.Exists(file)||!Path.GetExtension(file).Equals(".pdf",StringComparison.OrdinalIgnoreCase))throw new IOException();
            if(!HasInternetMarker(file)){AppDialog.Show(null,L.T("pdf.trust.unmarked"));return 0;}
            if(AppDialog.Show(null,L.T("pdf.trust.confirm"),true,true)!=DialogResult.Yes)return 0;
            if(!DeleteFileW(file+":Zone.Identifier"))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            SHChangeNotify(0x2000,0x5,file,IntPtr.Zero);
            try{ThumbnailRefresh.Refresh(file);}catch{}
            AppDialog.Show(null,L.T("pdf.trust.done"));
            return 0;
        }
        catch{AppDialog.Show(null,L.T("pdf.trust.failed"));return 1;}
    }

    internal static bool HasInternetMarker(string file)
    {
        try
        {
            using var stream=new FileStream(file+":Zone.Identifier",FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
            using var reader=new StreamReader(stream);
            var buffer=new char[4096];int count=reader.ReadBlock(buffer,0,buffer.Length);
            return new string(buffer,0,count).Split('\n').Any(line=>line.Trim() is "ZoneId=3" or "ZoneId=4");
        }
        catch(FileNotFoundException){return false;}
        catch(DirectoryNotFoundException){return false;}
    }
}
