using System.IO.Compression;
using System.Text.Json;
using Whatsinthebox.UI;

namespace Whatsinthebox;
static class SelfTest
{
    public static int Capture(string file,string output)
    {
        using var form=new Form{ClientSize=new Size(900,680)};var preview=new PreviewControl(true){Dock=DockStyle.Fill,RendererPath=Environment.ProcessPath!};form.Controls.Add(preview);form.Show();var task=preview.LoadFileAsync(file);var until=DateTime.UtcNow.AddSeconds(20);while(!task.IsCompleted&&DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(20);}if(!preview.HasPreview)return 1;using var bitmap=new Bitmap(preview.Width,preview.Height);preview.DrawToBitmap(bitmap,preview.ClientRectangle);bitmap.Save(output);form.Close();return 0;
    }
    public static int Run(string report,bool includeUi=true)
    {
        L.Override="tr";
        var root=Path.Combine(Path.GetTempPath(),"Whatsinthebox-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);var results=new List<object>();int failed=0;
        void Check(string name,string input,bool expected,string? mode=null,int page=0,int? pages=null){string output=Path.Combine(root,"out.png"),json=Path.Combine(root,"out.json");int code=Renderer.Run(input,output,json,page);var info=JsonSerializer.Deserialize<RenderInfo>(File.ReadAllText(json))!;bool ok=info.Success==expected&&(mode==null||info.Mode.Contains(mode))&&(pages==null||info.PageCount==pages);if(!ok)failed++;results.Add(new{name,passed=ok,info});}
        try
        {
            using(var bmp=new Bitmap(160,90)){using(var g=Graphics.FromImage(bmp)){g.Clear(Color.CornflowerBlue);g.FillRectangle(Brushes.White,20,20,40,40);}foreach(var entry in new[]{("png",System.Drawing.Imaging.ImageFormat.Png),("jpg",System.Drawing.Imaging.ImageFormat.Jpeg),("bmp",System.Drawing.Imaging.ImageFormat.Bmp),("gif",System.Drawing.Imaging.ImageFormat.Gif),("tiff",System.Drawing.Imaging.ImageFormat.Tiff)}){string p=Path.Combine(root,"sample."+entry.Item1);bmp.Save(p,entry.Item2);Check(entry.Item1,p,true);}}
            string svg=Path.Combine(root,"sample.svg");File.WriteAllText(svg,"<svg xmlns='http://www.w3.org/2000/svg' width='160' height='90'><rect width='160' height='90' fill='#0088cc'/><circle cx='80' cy='45' r='20' fill='white'/></svg>");Check("svg",svg,true,"SVG");
            string bad=Path.Combine(root,"external.svg");File.WriteAllText(bad,"<svg xmlns='http://www.w3.org/2000/svg'><image href='https://example.com/image.png'/></svg>");Check("svg external blocked",bad,false);
            File.WriteAllText(bad,"<!DOCTYPE svg [<!ENTITY xxe SYSTEM 'file:///C:/Windows/win.ini'>]><svg xmlns='http://www.w3.org/2000/svg'>&xxe;</svg>");Check("svg DTD blocked",bad,false);
            File.WriteAllText(bad,"<svg xmlns='http://www.w3.org/2000/svg' width='160' height='90'><style>.main { fill: #0088cc; }</style><rect class='main' width='160' height='90'/></svg>");Check("SVG safe style block",bad,true,"SVG");
            File.WriteAllText(bad,"<svg xmlns='http://www.w3.org/2000/svg' width='160' height='90'><style>@import url(https://example.com/style.css);</style><rect width='160' height='90'/></svg>");Check("SVG CSS import blocked",bad,false);
            string cdr=Path.Combine(root,"sample.cdr");using(var zip=ZipFile.Open(cdr,ZipArchiveMode.Create)){zip.CreateEntryFromFile(Path.Combine(root,"sample.png"),"previews/page1.png");}Check("CDR embedded preview fixture",cdr,true,"CDR");
            string empty=Path.Combine(root,"empty.cdr");using(var zip=ZipFile.Open(empty,ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("content/data.bin").Open());writer.Write("not a preview");}Check("CDR missing preview",empty,false);
            string font=Path.Combine(AppContext.BaseDirectory,"Fonts","Inter-Regular.ttf");Check("TTF specimen",font,true,L.T("font.mode"));
            string brokenFont=Path.Combine(root,"broken.ttf");File.WriteAllText(brokenFont,"broken font");Check("invalid font rejected",brokenFont,false);
            string largeFont=Path.Combine(root,"large.otf");using(var fontFile=File.Create(largeFont))fontFile.SetLength(32L*1024*1024+1);Check("font input limit",largeFont,false);
            File.WriteAllText(bad,"broken file");Check("malformed file",bad,false);
            string psd=Path.Combine(root,"sample.psd");using(var image=new ImageMagick.MagickImage(ImageMagick.MagickColors.CornflowerBlue,160,90)){image.Write(psd,ImageMagick.MagickFormat.Psd);image.Write(Path.Combine(root,"sample.webp"),ImageMagick.MagickFormat.WebP);}Check("PSD composite",psd,true,"PSD");Check("WebP",Path.Combine(root,"sample.webp"),true);
            foreach(var ext in new[]{".ico",".cur",".tga",".dds",".avif",".jp2",".j2k",".jxl",".exr",".hdr",".pcx",".qoi",".ppm",".pgm",".pbm",".pnm",".psb",".mng"})
            {
                string path=Path.Combine(root,"extended"+ext);try{using var image=new ImageMagick.MagickImage(ImageMagick.MagickColors.CornflowerBlue,160,90);image.Write(path,Renderer.RasterFormats[ext]);Check("extended "+ext,path,true);}catch(Exception ex){failed++;results.Add(new{name="extended "+ext,passed=false,error=ex.Message});}
            }
            string text=Path.Combine(root,"example.md");File.WriteAllText(text,"# Yerel önizleme\nDosya içeriği burada.\n"+string.Join('\n',Enumerable.Range(1,120).Select(i=>$"Satır {i}")));Check("text pages",text,true,"Metin",1,3);
            string word=Path.Combine(root,"sample.docx");using(var zip=ZipFile.Open(word,ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("word/document.xml").Open());writer.Write("<document><body><p><r><t>Merhaba Whatsinthebox</t></r></p></body></document>");}Check("DOCX text fixture",word,true,"DOCX");
            string sheet=Path.Combine(root,"sample.xlsx");using(var zip=ZipFile.Open(sheet,ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("xl/worksheets/sheet1.xml").Open());writer.Write("<worksheet><sheetData><row><c r='A1' t='inlineStr'><is><t>Ürün</t></is></c><c r='B1'><v>42</v></c></row></sheetData></worksheet>");}Check("XLSX values fixture",sheet,true,"XLSX");
            string slides=Path.Combine(root,"sample.pptx");using(var zip=ZipFile.Open(slides,ZipArchiveMode.Create)){for(int i=1;i<=2;i++){using var writer=new StreamWriter(zip.CreateEntry($"ppt/slides/slide{i}.xml").Open());writer.Write($"<slide><p><t>Slayt {i}</t></p></slide>");}}Check("PPTX slide navigation fixture",slides,true,"PPTX",1,2);
            string odf=Path.Combine(root,"sample.odt");using(var zip=ZipFile.Open(odf,ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("content.xml").Open());writer.Write("<document><p>OpenDocument örneği</p></document>");}Check("ODF text fixture",odf,true,"OpenDocument");
            string ora=Path.Combine(root,"sample.ora");using(var zip=ZipFile.Open(ora,ZipArchiveMode.Create)){zip.CreateEntryFromFile(Path.Combine(root,"sample.png"),"mergedimage.png");}Check("ORA/KRA merged image fixture",ora,true,"ORA/KRA");
            string generic=Path.Combine(root,"sample.zip");using(var zip=ZipFile.Open(generic,ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("readme.txt").Open());writer.Write("local");}Check("ZIP listing",generic,true,"ZIP");
            string pdf=Path.Combine(root,"sample.pdf");File.WriteAllBytes(pdf,MakePdf());Check("PDF first page",pdf,true,"PDF",0,2);Check("PDF second page",pdf,true,"PDF",1,2);
            string prefixedPdf=Path.Combine(root,"prefixed.pdf");File.WriteAllBytes(prefixedPdf,MakePdf("%"+new string(' ',600)+"\n"));Check("PDF header after leading metadata",prefixedPdf,true,"PDF",0,2);
            foreach(var dimensions in new[]{(1600,900),(900,1600),(1000,1000),(12000,100)})
            {
                var fit=PreviewGeometry.Fit(dimensions.Item1,dimensions.Item2,256,256);bool ok=fit.Width<=256&&fit.Height<=256&&Math.Abs(fit.Width*dimensions.Item2-fit.Height*dimensions.Item1)<=(dimensions.Item1+dimensions.Item2)/2;results.Add(new{name=$"aspect ratio {dimensions}",passed=ok});if(!ok)failed++;
            }
            using(var rendered=Image.FromFile(Path.Combine(root,"out.png"))){bool ok=Math.Abs((double)rendered.Width/rendered.Height-612d/792)<.002;results.Add(new{name="PDF source aspect ratio",passed=ok});if(!ok)failed++;}
            foreach(var language in L.Languages){L.Override=language;bool ok=L.All[language].Keys.Order().SequenceEqual(L.All["en"].Keys.Order())&&L.All[language].Values.All(v=>!string.IsNullOrWhiteSpace(v));foreach(var pair in L.All[language])try{string.Format(pair.Value,1,2,3,4);}catch{ok=false;}results.Add(new{name="complete locale "+language,passed=ok});if(!ok)failed++;}L.Override="tr";
            using(var releases=JsonDocument.Parse("[{\"tag_name\":\"v9.0.0\",\"draft\":false,\"assets\":[{\"name\":\"Whatsinthebox-9.0.0-Setup.exe\",\"browser_download_url\":\"https://github.com/bakhtiyarjahangirzade/Whatsinthebox/releases/download/v9.0.0/Whatsinthebox-9.0.0-Setup.exe\"}]}]")){bool ok=Updates.Select(releases.RootElement).State=="available";results.Add(new{name="newer stable update selected",passed=ok});if(!ok)failed++;}
            using(var releases=JsonDocument.Parse("[{\"tag_name\":\"v9.0.0\",\"draft\":false,\"assets\":[{\"name\":\"Whatsinthebox-9.0.0-Setup.exe\",\"browser_download_url\":\"https://evil.invalid/Setup.exe\"}]}]")){bool ok=Updates.Select(releases.RootElement).State=="current";results.Add(new{name="foreign update download rejected",passed=ok});if(!ok)failed++;}
            using(var releases=JsonDocument.Parse("[{\"tag_name\":\"v9.0.0-rc.1\",\"draft\":false,\"prerelease\":true,\"assets\":[{\"name\":\"Whatsinthebox-9.0.0-Setup.exe\",\"browser_download_url\":\"https://github.com/bakhtiyarjahangirzade/Whatsinthebox/releases/download/v9.0.0-rc.1/Whatsinthebox-9.0.0-Setup.exe\"}]}]")){bool ok=Updates.Select(releases.RootElement).State=="current";results.Add(new{name="prerelease excluded from stable updates",passed=ok});if(!ok)failed++;}
            using(var releases=JsonDocument.Parse("[{\"tag_name\":\"v9.0.0-rc.1\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"Whatsinthebox-9.0.0-Setup.exe\",\"browser_download_url\":\"https://github.com/bakhtiyarjahangirzade/Whatsinthebox/releases/download/v9.0.0-rc.1/Whatsinthebox-9.0.0-Setup.exe\"}]}]")){bool ok=Updates.Select(releases.RootElement).State=="current";results.Add(new{name="release-candidate tag rejected even if mislabeled stable",passed=ok});if(!ok)failed++;}
            // Shared actual UI: validate a worker render and a narrow pane without Explorer changes.
            if(includeUi){using var form=new Form{Width=420,Height=600};var preview=new PreviewControl(true){Dock=DockStyle.Fill,RendererPath=Environment.ProcessPath!};form.Controls.Add(preview);form.Show();var load=preview.LoadFileAsync(svg);var until=DateTime.UtcNow.AddSeconds(20);while(!load.IsCompleted&&DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(20);}bool ui=load.IsCompletedSuccessfully&&preview.HasPreview;results.Add(new{name="shared UI render",passed=ui});if(!ui)failed++;
            using var shot=new Bitmap(preview.Width,preview.Height);preview.DrawToBitmap(shot,preview.ClientRectangle);shot.Save(Path.ChangeExtension(report,"png"));form.Close();
            foreach(var dimensions in new[]{(160,90,800,600),(90,160,420,600),(529,256,650,220),(1600,900,180,600)})
            {
                using var fitForm=new Form{ClientSize=new Size(dimensions.Item3,dimensions.Item4)};
                var imageCanvas=new ImageCanvas{Dock=DockStyle.Fill,BackgroundMode=1};fitForm.Controls.Add(imageCanvas);
                var source=new Bitmap(dimensions.Item1,dimensions.Item2);using(var g=Graphics.FromImage(source))g.Clear(Color.Orange);imageCanvas.SetImage(source);
                fitForm.Show();Application.DoEvents();using var screenshot=new Bitmap(imageCanvas.Width,imageCanvas.Height);imageCanvas.DrawToBitmap(screenshot,imageCanvas.ClientRectangle);
                int left=screenshot.Width,top=screenshot.Height,right=-1,bottom=-1;
                for(int y=0;y<screenshot.Height;y++)for(int x=0;x<screenshot.Width;x++){var pixel=screenshot.GetPixel(x,y);if(pixel.R>245&&pixel.G is >150 and <180&&pixel.B<10){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}}
                int width=right-left+1,height=bottom-top+1;var expected=PreviewGeometry.Fit(dimensions.Item1,dimensions.Item2,dimensions.Item3-32,dimensions.Item4-32,true);
                bool fits=width>0&&height>0&&Math.Abs(width-expected.Width)<=3&&Math.Abs(height-expected.Height)<=3;
                results.Add(new{name=$"actual UI fit {dimensions}",passed=fits,width,height});if(!fits)failed++;
                screenshot.Save(Path.Combine(Path.GetDirectoryName(report)!, $"fit-{dimensions.Item1}x{dimensions.Item2}-{dimensions.Item3}x{dimensions.Item4}.png"));fitForm.Close();
            }
            }
            File.WriteAllText(report,JsonSerializer.Serialize(new{failed,results},new JsonSerializerOptions{WriteIndented=true}));return failed==0?0:1;
        }
        finally{try{Directory.Delete(root,true);}catch{}}
    }
    public static byte[] MakePdf(string prefix="")
    {
        var objects=new[]{"<< /Type /Catalog /Pages 2 0 R >>","<< /Type /Pages /Kids [4 0 R 6 0 R] /Count 2 >>","<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>","<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents 5 0 R >>","","<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents 7 0 R >>",""};
        for(int i=0;i<2;i++){string content=$"BT /F1 28 Tf 60 690 Td (Whatsinthebox - Page {i+1}) Tj ET";objects[i==0?4:6]=$"<< /Length {content.Length} >>\nstream\n{content}\nendstream";}
        var data=new System.Text.StringBuilder(prefix+"%PDF-1.4\n");var offsets=new List<int>{0};for(int i=0;i<objects.Length;i++){offsets.Add(System.Text.Encoding.ASCII.GetByteCount(data.ToString()));data.Append($"{i+1} 0 obj\n{objects[i]}\nendobj\n");}int xref=System.Text.Encoding.ASCII.GetByteCount(data.ToString());data.Append($"xref\n0 {objects.Length+1}\n0000000000 65535 f \n");foreach(var n in offsets.Skip(1))data.Append($"{n:D10} 00000 n \n");data.Append($"trailer\n<< /Size {objects.Length+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");return System.Text.Encoding.ASCII.GetBytes(data.ToString());
    }
}
