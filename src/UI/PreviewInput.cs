namespace Whatsinthebox.UI;
public static class PreviewInput
{
    public static string? LocalStreamFile(string? name,long length)
    {
        if(string.IsNullOrWhiteSpace(name)||!Path.IsPathFullyQualified(name)||name.StartsWith(@"\\"))return null;
        try
        {
            var file=new FileInfo(name);
            if(!file.Exists||file.Length!=length||(file.Attributes&(FileAttributes.Offline|FileAttributes.ReparsePoint|(FileAttributes)0x400000|(FileAttributes)0x40000))!=0)return null;
            return file.FullName;
        }
        catch{return null;}
    }
}
