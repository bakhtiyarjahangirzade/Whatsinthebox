namespace Whatsinthebox.UI;
public static class SupportedFiles
{
    public static readonly string[] ImageExtensions={".png",".jpg",".jpeg",".bmp",".webp",".gif",".tif",".tiff",".svg",".psd",".psb",".cdr",".ico",".cur",".tga",".dds",".avif",".heic",".heif",".jp2",".j2k",".jxl",".exr",".hdr",".pcx",".qoi",".ppm",".pgm",".pbm",".pnm",".mng",".xcf",".ora",".kra"};
    public static readonly string[] TextExtensions={".txt",".md",".markdown",".json",".xml",".csv",".tsv",".log",".ini",".yaml",".yml",".toml",".cs",".js",".ts",".tsx",".jsx",".py",".go",".rs",".css",".html",".htm",".sql",".c",".h",".cpp",".java",".rb",".ps1",".bat"};
    public static readonly string[] DocumentExtensions={".pdf",".ai",".docx",".xlsx",".pptx",".odt",".ods",".odp",".zip",".cbz"};
    public static string[] All=>ImageExtensions.Concat(TextExtensions).Concat(DocumentExtensions).Distinct().ToArray();
    public static string Filter=>L.T("files.supported")+"|"+string.Join(';',All.Select(x=>"*"+x))+"|"+L.T("files.all")+"|*.*";
}
