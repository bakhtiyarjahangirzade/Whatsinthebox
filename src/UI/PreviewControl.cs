using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text.Json;

namespace Whatsinthebox.UI;

public static class Theme
{
    public static readonly Color Canvas = ColorTranslator.FromHtml("#F5F5F7"), Surface = ColorTranslator.FromHtml("#FFFFFF"), Text = ColorTranslator.FromHtml("#1D1D1F"), Muted = ColorTranslator.FromHtml("#6E6E73"), Primary = ColorTranslator.FromHtml("#182336");
}
public sealed record RenderInfo(bool Success, string Message, int Width=0, int Height=0, string Mode="",int PageCount=1);

public sealed class PreviewControl : UserControl
{
    sealed class PreviewFailure(string message):Exception(message);
    readonly Label title = new(), status = new();
    readonly ImageCanvas canvas = new();
    readonly FlowLayoutPanel toolbar = new();
    CancellationTokenSource? cancellation;
    int generation;bool loading;
    string? currentPath,currentName;int currentPage,pageCount=1;
    Button previous=null!,next=null!;Label pages=new();
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string RendererPath { get; set; } = "";
    public bool Standalone { get; }
    public PreviewControl(bool standalone=false)
    {
        Standalone=standalone;
        BackColor=Theme.Canvas; ForeColor=Theme.Text; Font=UiStyle.Font(11);
        var header=new Panel {Dock=DockStyle.Top,Height=84,Padding=new Padding(16,10,16,8),BackColor=Theme.Surface};
        var brandRow=new Panel{Dock=DockStyle.Top,Height=32};
        var brand=new Label {Text="Whatsinthebox",Dock=DockStyle.Fill,Font=UiStyle.Font(13,FontStyle.Bold),ForeColor=Theme.Primary,Padding=new Padding(8,0,0,0)};
        brandRow.Controls.Add(brand);
        string uiFolder=Path.GetDirectoryName(HostActivity.Renderer(typeof(PreviewControl).Assembly))!;
        string imagePath=Path.Combine(uiFolder,"logo.png");if(!File.Exists(imagePath))imagePath=Path.GetFullPath(Path.Combine(uiFolder,"..","logo.png"));
        if(File.Exists(imagePath)){using var original=Image.FromFile(imagePath);var icon=new PictureBox{Image=new Bitmap(original),SizeMode=PictureBoxSizeMode.Zoom,Dock=DockStyle.Left,Width=32};icon.Disposed+=(_,_)=>icon.Image?.Dispose();brandRow.Controls.Add(icon);}
        title.Text=L.T("file.select"); title.Dock=DockStyle.Bottom;title.Height=25;title.AutoEllipsis=true;
        header.Controls.Add(brandRow);header.Controls.Add(title);
        toolbar.Dock=DockStyle.Top;toolbar.AutoSize=true;toolbar.AutoSizeMode=AutoSizeMode.GrowAndShrink;toolbar.Padding=new Padding(10,8,10,4);toolbar.WrapContents=true;toolbar.BackColor=Theme.Surface;
        AddButton(L.T("fit"),()=>canvas.Fit());AddButton("−",()=>canvas.ZoomBy(0.8f),L.T("zoom.out"));AddButton("+",()=>canvas.ZoomBy(1.25f),L.T("zoom.in"));
        var bg=AddButton(L.T("background.checker"),()=>{}); bg.Click+=(_,_)=>{canvas.BackgroundMode=(canvas.BackgroundMode+1)%3; bg.Text=new[]{L.T("background.checker"),L.T("background.light"),L.T("background.dark")}[canvas.BackgroundMode];canvas.Invalidate();};
        if(standalone) AddButton(L.T("file.select"),ChooseFile);
        previous=AddButton(L.T("previous"),()=>ChangePage(-1));next=AddButton(L.T("next"),()=>ChangePage(1));pages.AutoSize=true;pages.ForeColor=Theme.Muted;pages.Margin=new Padding(8,11,4,0);toolbar.Controls.Add(pages);SetPages(1);
        status.Dock=DockStyle.Bottom;status.Height=65;status.Padding=new Padding(16,10,16,6);status.ForeColor=Theme.Muted;status.Text=L.T("preview.local");
        canvas.Dock=DockStyle.Fill;
        Controls.Add(canvas);Controls.Add(status);Controls.Add(toolbar);Controls.Add(header);
        header.Visible=standalone;
        void SizeStatus(){status.Height=Math.Clamp(TextRenderer.MeasureText(status.Text,status.Font,new Size(Math.Max(80,Width-32),0),TextFormatFlags.WordBreak).Height+24,65,180);}
        Resize+=(_,_)=>SizeStatus();status.TextChanged+=(_,_)=>SizeStatus();
        AllowDrop=standalone;
        DragEnter+=(_,e)=>{if(e.Data?.GetDataPresent(DataFormats.FileDrop)==true)e.Effect=DragDropEffects.Copy;};
        DragDrop+=(_,e)=>{if(e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length>0)_=LoadFileAsync(files[0]);};
    }
    Button AddButton(string text,Action click,string? accessible=null)
    {
        var b=new RoundedButton {Text=text,AutoSize=true,MinimumSize=new Size(42,34),Height=34,FlatStyle=FlatStyle.Flat,BackColor=Theme.Surface,ForeColor=Theme.Text,Margin=new Padding(4),AccessibleName=accessible??text,Cursor=Cursors.Hand};
        b.FlatAppearance.BorderColor=Theme.Muted;b.Click+=(_,_)=>click();toolbar.Controls.Add(b);return b;
    }
    public void ChooseFile()
    {
        using var dialog=new OpenFileDialog {Title=L.T("file.select"),Filter=SupportedFiles.Filter};
        if(dialog.ShowDialog(FindForm())==DialogResult.OK)_=LoadFileAsync(dialog.FileName);
    }
    void SetPages(int count){toolbar.SuspendLayout();try{pageCount=Math.Max(1,count);pages.Text=$"{currentPage+1} / {pageCount}";previous.Visible=next.Visible=pages.Visible=pageCount>1;previous.Enabled=currentPage>0;next.Enabled=currentPage<pageCount-1;}finally{toolbar.ResumeLayout(true);}}
    void ChangePage(int delta){if(currentPath==null)return;int page=Math.Clamp(currentPage+delta,0,pageCount-1);if(page!=currentPage)_=LoadFileAsync(currentPath,currentName,page);}
    public async Task LoadFileAsync(string path,string? displayName=null,int page=0)
    {
        currentPath=path;currentName=displayName;currentPage=page;if(page==0)SetPages(1);previous.Enabled=next.Enabled=false;
        cancellation?.Cancel();cancellation?.Dispose();cancellation=new();var token=cancellation.Token;var version=++generation;loading=true;
        title.Text=displayName??Path.GetFileName(path);canvas.SetImage(null);canvas.Caption=L.T("preview.loading");status.Text=L.T("preview.readonly");
        var dir=Path.Combine(PreviewStorage.Root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try
        {
            string png=Path.Combine(dir,"preview.png"), json=Path.Combine(dir,"result.json");
            using var process=new Process {StartInfo=new ProcessStartInfo(RendererPath) {UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden}};
            foreach(var arg in new[]{"--render",path,png,json,page.ToString(System.Globalization.CultureInfo.InvariantCulture)})process.StartInfo.ArgumentList.Add(arg);
            process.Start();using var limits=WorkerLimits.Attach(process);
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(15));
            try {await process.WaitForExitAsync(timeout.Token);} catch(OperationCanceledException) {try{process.Kill(true);await process.WaitForExitAsync();}catch{} if(token.IsCancellationRequested)return;throw new TimeoutException(L.T("preview.timeout"));}
            if(version!=generation||token.IsCancellationRequested||IsDisposed)return;
            if(!File.Exists(json))throw new IOException(L.T("preview.generic"));
            var info=JsonSerializer.Deserialize<RenderInfo>(await File.ReadAllTextAsync(json,token))!;
            if(!info.Success)throw new PreviewFailure(info.Message);
            SetPages(info.PageCount);
            using var loaded=Image.FromFile(png);canvas.SetImage(new Bitmap(loaded));canvas.Caption="";
            status.Text=$"{info.Width:N0} × {info.Height:N0} {L.T("pixels")} • {info.Mode}\n{info.Message}";
            if(CaptureEnabled(path)){using var shot=new Bitmap(Math.Max(1,Width),Math.Max(1,Height));DrawToBitmap(shot,ClientRectangle);shot.Save(Path.Combine(PreviewStorage.Root,"embedded-test.png"));File.WriteAllText(Path.Combine(PreviewStorage.Root,"embedded-test.json"),JsonSerializer.Serialize(new{success=true,width=Width,height=Height,thread=Environment.CurrentManagedThreadId}));}
        }
        catch(OperationCanceledException){}
        catch(Exception ex) {if(version==generation&&!IsDisposed){canvas.Caption=L.T("preview.failed");canvas.Invalidate();status.Text=ex is PreviewFailure or TimeoutException?ex.Message:L.T("preview.generic");SetPages(pageCount);if(CaptureEnabled(path))File.WriteAllText(Path.Combine(PreviewStorage.Root,"embedded-test.json"),JsonSerializer.Serialize(new{success=false,error=status.Text}));}}
        finally {if(version==generation)loading=false;try{Directory.Delete(dir,true);}catch{}}
    }
    protected override void Dispose(bool disposing){if(disposing){generation++;cancellation?.Cancel();cancellation?.Dispose();}base.Dispose(disposing);}
    public bool HasPreview=>canvas.HasImage&&!loading;
    bool CaptureEnabled(string input){string marker=Path.Combine(Path.GetDirectoryName(RendererPath)!,"test-capture.flag");return File.Exists(marker)&&File.ReadAllText(marker).Trim().Equals(Path.GetFullPath(input),StringComparison.OrdinalIgnoreCase);}
}

public sealed class ImageCanvas : Control
{
    Image? image;float zoom=1;bool fit=true;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Caption {get;set;}=L.T("preview.empty");
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int BackgroundMode {get;set;}
    public ImageCanvas(){DoubleBuffered=true;TabStop=true;BackColor=Theme.Canvas;AccessibleName=L.T("preview.accessible");Resize+=(_,_)=>Invalidate();}
    public void SetImage(Image? value){image?.Dispose();image=value;fit=true;Invalidate();}
    public bool HasImage=>image!=null;
    public void Fit(){fit=true;Invalidate();}
    public void ZoomBy(float multiplier){if(image==null)return;if(fit)zoom=FitScale();fit=false;zoom=Math.Clamp(zoom*multiplier,0.03f,8f);Invalidate();Focus();}
    float FitScale()=>image==null?1:Math.Min(Math.Max(1,Width-16)/(float)image.Width,Math.Max(1,Height-16)/(float)image.Height);
    protected override bool IsInputKey(Keys key)=>key is Keys.Add or Keys.Subtract or Keys.Oemplus or Keys.OemMinus or Keys.D0||base.IsInputKey(key);
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode is Keys.Add or Keys.Oemplus)ZoomBy(1.25f);if(e.KeyCode is Keys.Subtract or Keys.OemMinus)ZoomBy(0.8f);if(e.KeyCode==Keys.D0)Fit();}
    protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);ZoomBy(e.Delta>0?1.15f:0.87f);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;
        if(image==null){TextRenderer.DrawText(g,Caption,Font,new Rectangle(20,20,Math.Max(1,Width-40),Math.Max(1,Height-40)),Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);return;}
        float scale=fit?FitScale():zoom;int w=Math.Max(1,(int)(image.Width*scale)),h=Math.Max(1,(int)(image.Height*scale));var r=new Rectangle((Width-w)/2,(Height-h)/2,w,h);
        var region=g.Save();g.SetClip(r);
        if(BackgroundMode==0){using var a=new SolidBrush(Color.FromArgb(221,227,234));using var b=new SolidBrush(Color.FromArgb(187,198,210));g.FillRectangle(a,r);for(int y=Math.Max(0,r.Top);y<Math.Min(Height,r.Bottom);y+=16)for(int x=Math.Max(0,r.Left);x<Math.Min(Width,r.Right);x+=16)if(((x-Math.Max(0,r.Left))/16+(y-Math.Max(0,r.Top))/16)%2==0)g.FillRectangle(b,x,y,16,16);}
        else g.Clear(BackgroundMode==1?Color.White:Color.FromArgb(29,29,31));
        g.InterpolationMode=InterpolationMode.HighQualityBicubic;using var attributes=new System.Drawing.Imaging.ImageAttributes();attributes.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(image,r,0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);g.Restore(region);
        using var pen=new Pen(Theme.Muted);g.DrawRectangle(pen,r);
    }
    protected override void Dispose(bool disposing){if(disposing)image?.Dispose();base.Dispose(disposing);}
}

