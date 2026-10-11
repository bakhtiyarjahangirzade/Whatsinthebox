namespace Whatsinthebox.UI;
public static class SupportedFiles
{
    // Formats the native Explorer integration renders. Each is registered only
    // where Windows (or another installed program) has no handler of its own.
    public static readonly string[] NativeThumbnail={".psd",".psb",".svg",".svgz",".pdf",".ai",".cdr",".ora",".kra",".qoi",".tga",".ppm",".pgm",".pbm",".pnm"};
    public static readonly string[] NativePreview=NativeThumbnail.Concat(new[]{".ttf",".otf"}).ToArray();
    public static readonly string[] ImageExtensions={".png",".jpg",".jpeg",".bmp",".webp",".gif",".tif",".tiff",".svg",".psd",".psb",".cdr",".ico",".cur",".tga",".dds",".avif",".heic",".heif",".jp2",".j2k",".jxl",".exr",".hdr",".pcx",".qoi",".ppm",".pgm",".pbm",".pnm",".mng",".xcf",".ora",".kra"};
    public static readonly string[] TextExtensions={".txt",".md",".markdown",".json",".xml",".csv",".tsv",".log",".ini",".yaml",".yml",".toml",".cs",".js",".ts",".tsx",".jsx",".py",".go",".rs",".css",".html",".htm",".sql",".c",".h",".cpp",".java",".rb",".ps1",".bat"};
    public static readonly string[] DocumentExtensions={".pdf",".ai",".docx",".xlsx",".pptx",".odt",".ods",".odp",".zip",".cbz"};
    public static readonly string[] FontExtensions={".ttf",".otf"};
    public static string[] All=>ImageExtensions.Concat(TextExtensions).Concat(DocumentExtensions).Concat(FontExtensions).Distinct().ToArray();
    public static string Filter=>L.T("files.supported")+"|"+string.Join(';',All.Select(x=>"*"+x))+"|"+L.T("files.all")+"|*.*";
}
