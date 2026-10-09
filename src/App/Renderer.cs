using ImageMagick;
using SkiaSharp;
using Svg.Skia;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Whatsinthebox.UI;

namespace Whatsinthebox;

static class Renderer
{
    const long MaxBytes=256L*1024*1024;
    public static readonly Dictionary<string,MagickFormat> RasterFormats=new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"]=MagickFormat.Png,[".jpg"]=MagickFormat.Jpeg,[".jpeg"]=MagickFormat.Jpeg,[".bmp"]=MagickFormat.Bmp,[".webp"]=MagickFormat.WebP,[".gif"]=MagickFormat.Gif,[".tif"]=MagickFormat.Tiff,[".tiff"]=MagickFormat.Tiff,[".psd"]=MagickFormat.Psd,[".psb"]=MagickFormat.Psb,[".ico"]=MagickFormat.Ico,[".cur"]=MagickFormat.Cur,[".tga"]=MagickFormat.Tga,[".dds"]=MagickFormat.Dds,[".avif"]=MagickFormat.Avif,[".heic"]=MagickFormat.Heic,[".heif"]=MagickFormat.Heic,[".jp2"]=MagickFormat.Jp2,[".j2k"]=MagickFormat.J2k,[".jxl"]=MagickFormat.Jxl,[".exr"]=MagickFormat.Exr,[".hdr"]=MagickFormat.Hdr,[".pcx"]=MagickFormat.Pcx,[".qoi"]=MagickFormat.Qoi,[".ppm"]=MagickFormat.Ppm,[".pgm"]=MagickFormat.Pgm,[".pbm"]=MagickFormat.Pbm,[".pnm"]=MagickFormat.Pnm,[".mng"]=MagickFormat.Mng,[".xcf"]=MagickFormat.Xcf
    };
    public static int Run(string input,string output,string result,int page=0)
    {
        RenderInfo info;
        try
        {
            if(new FileInfo(input).Length>MaxBytes)throw new InvalidDataException(L.T("input.limit"));
            byte[] head=new byte[1024];using(var stream=File.OpenRead(input)){stream.ReadExactly(head.AsSpan(0,(int)Math.Min(head.Length,stream.Length)));}
            string ext=Path.GetExtension(input).ToLowerInvariant();
            Environment.SetEnvironmentVariable("MAGICK_CONFIGURE_PATH",AppContext.BaseDirectory);
            if(SupportedFiles.FontExtensions.Contains(ext)){info=FontPreview.Render(input,output);}
            else if(ext==".pdf"||Encoding.ASCII.GetString(head).Contains("%PDF-")){info=RenderPdf(input,output,page);}
            else if(SupportedFiles.TextExtensions.Contains(ext)){info=RenderText(ReadText(input),output,page,L.T("text.mode"),Path.GetFileName(input));}
            else if(head[0]==80&&head[1]==75){info=RenderPackage(input,output,page);}
            else if(ext==".svg"||IsXmlDocument(head)){info=RenderSvg(input,output);}
            else
            {
                bool psd=Encoding.ASCII.GetString(head,0,4)=="8BPS";
                bool supported=RasterFormats.ContainsKey(ext)||psd||head.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})||(head[0]==255&&head[1]==216)||Encoding.ASCII.GetString(head,0,3)=="GIF"||(head[0]==66&&head[1]==77)||Encoding.ASCII.GetString(head,8,4)=="WEBP"||(head[0]==73&&head[1]==73)||(head[0]==77&&head[1]==77)||ext==".witb";
                if(!supported)throw new InvalidDataException(ext==".cdr"?L.T("cdr.unsupported"):L.T("format.unsupported"));
                ResourceLimits.Memory=256UL*1024*1024;ResourceLimits.Disk=0;ResourceLimits.Thread=2;
                ResourceLimits.Width=40000;ResourceLimits.Height=40000;ResourceLimits.MaxMemoryRequest=256UL*1024*1024;
                var settings=new MagickReadSettings {FrameIndex=0,FrameCount=1};
                if(RasterFormats.TryGetValue(ext,out var forced))settings.Format=forced;
                using var image=new MagickImage();image.Ping(input,settings);
                if(!RasterFormats.Values.Contains(image.Format)&&image.Format is not (MagickFormat.Bmp2 or MagickFormat.Bmp3))throw new InvalidDataException(L.T("format.blocked"));
                if((long)image.Width*image.Height>80_000_000||image.Width==0||image.Height==0)throw new InvalidDataException(L.T("image.limit"));
                image.Read(input,settings);image.AutoOrient();int width=(int)image.Width,height=(int)image.Height;
                image.Resize(new MagickGeometry(2400,2400){Greater=true});image.Strip();image.Write(output,MagickFormat.Png);
                info=new(true,psd?L.T("psd.note"):L.T("source.unchanged"),width,height,psd?L.T("mode.psd"):L.T("mode.image"));
            }
        }
        catch(Exception ex){info=new(false,ex is InvalidDataException?ex.Message:L.T("preview.generic"));if(Environment.GetEnvironmentVariable("WHATSINTHEBOX_RENDER_DIAGNOSTICS")=="1")File.WriteAllText(result+".diagnostics",ex.ToString());}
        File.WriteAllText(result,JsonSerializer.Serialize(info));return info.Success?0:1;
    }
    static RenderInfo RenderPdf(string input,string output,int page)
    {
        using var stream=File.OpenRead(input);int count=PDFtoImage.Conversion.GetPageCount(stream,leaveOpen:true);if(count<1)throw new InvalidDataException(L.T("pdf.empty"));
        page=Math.Clamp(page,0,count-1);stream.Position=0;var size=PDFtoImage.Conversion.GetPageSize(stream,page,leaveOpen:true);stream.Position=0;
        var target=PreviewGeometry.Fit(size.Width,size.Height,1600,2000,true);
        using var bitmap=PDFtoImage.Conversion.ToImage(stream,page,leaveOpen:true,options:new PDFtoImage.RenderOptions(Width:target.Width,Height:target.Height,WithAspectRatio:true,WithFormFill:true));using var data=bitmap.Encode(SKEncodedImageFormat.Png,100);using var file=File.Create(output);data.SaveTo(file);
        return new(true,L.T("page.note",page+1,count),bitmap.Width,bitmap.Height,Path.GetExtension(input).Equals(".ai",StringComparison.OrdinalIgnoreCase)?L.T("ai.mode"):"PDF",count);
    }
    static string ReadText(string input)
    {
        using var stream=File.OpenRead(input);using var reader=new StreamReader(stream,Encoding.UTF8,true);var buffer=new char[60000];int n=reader.ReadBlock(buffer,0,buffer.Length);var text=new string(buffer,0,n);if(text.Contains('\0'))throw new InvalidDataException(L.T("text.invalid"));return text+(reader.Peek()>=0?"\n\n["+L.T("text.truncated")+"]":"");
    }
    static RenderInfo RenderText(string text,string output,int page,string mode,string title)
    {
        var lines=new List<string>();foreach(var line in text.Replace("\r","").Replace("\t","    ").Split('\n')){if(line.Length==0)lines.Add("");else for(int i=0;i<line.Length;i+=95)lines.Add(line.Substring(i,Math.Min(95,line.Length-i)));}
        int count=Math.Max(1,(lines.Count+49)/50);page=Math.Clamp(page,0,count-1);using var bitmap=new Bitmap(1200,1500);using var g=Graphics.FromImage(bitmap);g.Clear(Color.FromArgb(247,249,252));using var heading=new Font("Segoe UI",18,FontStyle.Bold);using var body=new Font("Consolas",12);using var ink=new SolidBrush(Color.FromArgb(32,46,64));g.DrawString(title,heading,ink,new RectangleF(48,36,1100,65));g.DrawString(string.Join('\n',lines.Skip(page*50).Take(50)),body,ink,new RectangleF(48,120,1100,1320));bitmap.Save(output,System.Drawing.Imaging.ImageFormat.Png);
        return new(true,L.T("text.note",page+1,count),1200,1500,mode,count);
    }
    static XDocument ReadXml(ZipArchiveEntry entry)
    {
        if(entry.Length>16L*1024*1024)throw new InvalidDataException(L.T("document.limit"));using var stream=entry.Open();using var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=16L*1024*1024});return XDocument.Load(reader);
    }
    static RenderInfo RenderPackage(string input,string output,int page)
    {
        using var zip=ZipFile.OpenRead(input);if(zip.Entries.Count>20000)throw new InvalidDataException(L.T("archive.limit"));
        if(zip.Entries.Any(x=>x.FullName.StartsWith("previews/",StringComparison.OrdinalIgnoreCase)||x.FullName.StartsWith("metadata/",StringComparison.OrdinalIgnoreCase)))return RenderCdr(input,output);
        var merged=zip.GetEntry("mergedimage.png");if(merged!=null){if(merged.Length>32L*1024*1024)throw new InvalidDataException(L.T("embedded.limit"));using var stream=merged.Open();using var original=Image.FromStream(stream);if((long)original.Width*original.Height>80_000_000)throw new InvalidDataException(L.T("embedded.limit"));using var bitmap=new Bitmap(original);bitmap.Save(output,System.Drawing.Imaging.ImageFormat.Png);return new(true,L.T("ora.note"),bitmap.Width,bitmap.Height,"ORA/KRA • "+L.T("embedded.mode"));}
        var word=zip.GetEntry("word/document.xml");if(word!=null){var doc=ReadXml(word);var paragraphs=doc.Descendants().Where(x=>x.Name.LocalName=="p").Select(p=>string.Concat(p.Descendants().Where(x=>x.Name.LocalName=="t").Select(x=>x.Value)));return RenderText(string.Join('\n',paragraphs).Substring(0,Math.Min(60000,string.Join('\n',paragraphs).Length)),output,page,"DOCX • "+L.T("text.preview"),Path.GetFileName(input));}
        var slides=zip.Entries.Where(x=>System.Text.RegularExpressions.Regex.IsMatch(x.FullName,@"^ppt/slides/slide\d+\.xml$")).OrderBy(x=>int.Parse(System.Text.RegularExpressions.Regex.Match(x.Name,@"\d+").Value)).ToArray();
        if(slides.Length>0){page=Math.Clamp(page,0,slides.Length-1);var doc=ReadXml(slides[page]);var text=string.Join('\n',doc.Descendants().Where(x=>x.Name.LocalName=="t").Select(x=>x.Value));var info=RenderText(text,output,0,"PPTX • "+L.T("text.preview"),$"{Path.GetFileName(input)} • {L.T("slide",page+1)}");return info with{PageCount=slides.Length,Message=L.T("slide.note")};}
        var sheets=zip.Entries.Where(x=>System.Text.RegularExpressions.Regex.IsMatch(x.FullName,@"^xl/worksheets/sheet\d+\.xml$")).OrderBy(x=>int.Parse(System.Text.RegularExpressions.Regex.Match(x.Name,@"\d+").Value)).ToArray();
        if(sheets.Length>0){page=Math.Clamp(page,0,sheets.Length-1);var strings=zip.GetEntry("xl/sharedStrings.xml");var shared=strings==null?Array.Empty<string>():ReadXml(strings).Descendants().Where(x=>x.Name.LocalName=="si").Select(x=>string.Concat(x.Descendants().Where(t=>t.Name.LocalName=="t").Select(t=>t.Value))).ToArray();var doc=ReadXml(sheets[page]);var rows=doc.Descendants().Where(x=>x.Name.LocalName=="row").Take(45).Select(row=>string.Join("  |  ",row.Elements().Where(x=>x.Name.LocalName=="c").Take(8).Select(cell=>{string val=cell.Descendants().FirstOrDefault(x=>x.Name.LocalName=="v"||x.Name.LocalName=="t")?.Value??"";if((string?)cell.Attribute("t")=="s"&&int.TryParse(val,out int i)&&i>=0&&i<shared.Length)val=shared[i];return ((string?)cell.Attribute("r")??"")+": "+val;})));var info=RenderText(string.Join('\n',rows),output,0,"XLSX • "+L.T("sheet.values"),$"{Path.GetFileName(input)} • {L.T("sheet",page+1)}");return info with{PageCount=sheets.Length,Message=L.T("sheet.note")};}
        var odf=zip.GetEntry("content.xml");if(odf!=null){var doc=ReadXml(odf);return RenderText(string.Join('\n',doc.Descendants().Where(x=>x.Name.LocalName is "p" or "h").Select(x=>x.Value).Take(2000)),output,page,"OpenDocument • "+L.T("text.preview"),Path.GetFileName(input));}
        if(Path.GetExtension(input).Equals(".cdr",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(L.T("cdr.unsupported"));
        return RenderText(string.Join('\n',zip.Entries.Take(2000).Select(x=>$"{x.Length,12:N0} {L.T("bytes")}  {x.FullName}")),output,page,"ZIP / CBZ • "+L.T("archive.list"),Path.GetFileName(input));
    }
    static RenderInfo RenderCdr(string input,string output)
    {
        using var zip=ZipFile.OpenRead(input);
        if(zip.Entries.Count>20000)throw new InvalidDataException(L.T("archive.limit"));
        var entries=zip.Entries.Where(e=>e.Length>0&&e.Length<=32L*1024*1024&&(e.FullName.StartsWith("previews/",StringComparison.OrdinalIgnoreCase)||e.FullName.StartsWith("metadata/",StringComparison.OrdinalIgnoreCase))&&(e.Name.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||e.Name.EndsWith(".bmp",StringComparison.OrdinalIgnoreCase)||e.Name.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase)||e.Name.EndsWith(".jpeg",StringComparison.OrdinalIgnoreCase))).OrderByDescending(e=>e.FullName.StartsWith("previews/",StringComparison.OrdinalIgnoreCase)).ThenByDescending(e=>e.Length).Take(16);
        foreach(var entry in entries)
        {
            try
            {
                using var stream=entry.Open();using var memory=new MemoryStream();stream.CopyTo(memory);memory.Position=0;
                using var source=Image.FromStream(memory);if((long)source.Width*source.Height>80_000_000)continue;
                int width=source.Width,height=source.Height;float s=Math.Min(1,2400f/Math.Max(width,height));using var bitmap=new Bitmap(Math.Max(1,(int)(width*s)),Math.Max(1,(int)(height*s)));using(var g=Graphics.FromImage(bitmap)){g.DrawImage(source,0,0,bitmap.Width,bitmap.Height);}bitmap.Save(output,System.Drawing.Imaging.ImageFormat.Png);
                return new(true,L.T("embedded.note"),width,height,"CDR • "+L.T("embedded.mode"));
            }
            catch{}
        }
        throw new InvalidDataException(L.T("cdr.unsupported"));
    }
    static RenderInfo RenderSvg(string input,string output)
    {
        if(new FileInfo(input).Length>8L*1024*1024)throw new InvalidDataException(L.T("svg.limit"));
        // Ignore legacy SVG DOCTYPE declarations without fetching or expanding DTDs.
        using var reader=XmlReader.Create(input,new XmlReaderSettings {DtdProcessing=DtdProcessing.Ignore,XmlResolver=null,MaxCharactersInDocument=8L*1024*1024});var document=XDocument.Load(reader);
        if(document.Root?.Name.LocalName!="svg")throw new InvalidDataException(L.T("svg.invalid"));
        foreach(var node in document.Descendants())
        {
            if(new[]{"script","foreignObject","image","animate","animateTransform","set"}.Contains(node.Name.LocalName))throw new InvalidDataException(L.T("svg.active"));
            if(node.Name.LocalName=="style"&&!SafeCss(node.Value))throw new InvalidDataException(L.T("svg.active"));
            foreach(var attr in node.Attributes())
            {
                string val=attr.Value;
                if(attr.Name.LocalName.StartsWith("on",StringComparison.OrdinalIgnoreCase)||(attr.Name.LocalName=="href"&&!val.StartsWith('#'))||(attr.Name.LocalName=="style"&&!SafeCss(val))||(val.Contains("url(",StringComparison.OrdinalIgnoreCase)&&!SafeCss(val)))throw new InvalidDataException(L.T("svg.active"));
            }
        }
        using var safe=new MemoryStream(Encoding.UTF8.GetBytes(document.ToString()));using var svg=new SKSvg();svg.Load(safe);var picture=svg.Picture??throw new InvalidDataException(L.T("svg.invalid"));var bounds=picture.CullRect;
        if(!float.IsFinite(bounds.Width)||!float.IsFinite(bounds.Height)||bounds.Width<=0||bounds.Height<=0||bounds.Width>100000||bounds.Height>100000)throw new InvalidDataException(L.T("svg.invalid"));
        float scale=Math.Min(2f,2400f/Math.Max(bounds.Width,bounds.Height));int w=Math.Max(1,(int)Math.Ceiling(bounds.Width*scale)),h=Math.Max(1,(int)Math.Ceiling(bounds.Height*scale));
        using var surface=SKSurface.Create(new SKImageInfo(w,h));surface.Canvas.Clear(SKColors.Transparent);surface.Canvas.Scale(scale);surface.Canvas.Translate(-bounds.Left,-bounds.Top);surface.Canvas.DrawPicture(picture);using var image=surface.Snapshot();using var data=image.Encode(SKEncodedImageFormat.Png,100);using var file=File.Create(output);data.SaveTo(file);
        return new(true,L.T("svg.note"),(int)Math.Ceiling(bounds.Width),(int)Math.Ceiling(bounds.Height),"SVG • "+L.T("svg.mode"));
    }
    static bool IsXmlDocument(byte[] head)
    {
        string prefix=Encoding.UTF8.GetString(head).TrimStart('\uFEFF',' ','\n','\r','\t');
        return prefix.StartsWith("<svg",StringComparison.OrdinalIgnoreCase)||prefix.StartsWith("<?xml",StringComparison.OrdinalIgnoreCase)||prefix.StartsWith("<!--",StringComparison.Ordinal);
    }
    static bool SafeCss(string value)
    {
        if(value.Contains('@')||value.Contains('\\'))return false;
        foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(value,@"url\s*\(([^)]*)\)",System.Text.RegularExpressions.RegexOptions.IgnoreCase))if(!System.Text.RegularExpressions.Regex.IsMatch(match.Groups[1].Value.Trim().Trim('\'', '"'),@"^#[-\w]+$"))return false;
        return true;
    }
}
