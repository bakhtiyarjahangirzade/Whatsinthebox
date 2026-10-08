using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
namespace Whatsinthebox.UI;
public static class UiStyle
{
 static bool loaded;
 [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern int AddFontResourceEx(string path,uint flags,IntPtr reserved);
 public static Font Font(float size,FontStyle style=FontStyle.Regular)
 {
  if(!loaded){loaded=true;foreach(var folder in new[]{Path.Combine(AppContext.BaseDirectory,"Fonts"),Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","Fonts"))})if(Directory.Exists(folder))foreach(var file in Directory.GetFiles(folder,"*.ttf"))AddFontResourceEx(file,0x10,IntPtr.Zero);}
  return new Font(L.Language=="zh"?"Microsoft YaHei UI":"Inter",size,style);
 }
}
public sealed class RoundedButton:Button
{
 protected override void OnPaint(PaintEventArgs e)
 {
  e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(1,1,Width-3,Height-3);using var path=new GraphicsPath();const float d=14;path.AddArc(r.X,r.Y,d,d,180,90);path.AddArc(r.Right-d,r.Y,d,d,270,90);path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.X,r.Bottom-d,d,d,90,90);path.CloseFigure();using var fill=new SolidBrush(Enabled?BackColor:Theme.Canvas);e.Graphics.FillPath(fill,path);using var pen=new Pen(Focused?Theme.Primary:Color.FromArgb(210,210,215));e.Graphics.DrawPath(pen,path);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?ForeColor:Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
 }
}
public sealed class LocalePicker:ComboBox
{
 public LocalePicker(){DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=25;}
 protected override void OnDrawItem(DrawItemEventArgs e){e.DrawBackground();if(e.Index>=0)TextRenderer.DrawText(e.Graphics,Items[e.Index]?.ToString(),Font,e.Bounds,e.ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.Left);e.DrawFocusRectangle();}
}
public static class AppDialog
{
 public static DialogResult Show(IWin32Window owner,string message,bool confirm=false)
 {
  using var f=new Form{Text="Whatsinthebox",ClientSize=new Size(520,230),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Theme.Canvas,Font=UiStyle.Font(11)};
  f.Controls.Add(new Label{Text=message,Location=new Point(24,24),Size=new Size(472,136),ForeColor=Theme.Text});
  var ok=new RoundedButton{Text=L.T(confirm?"yes":"ok"),DialogResult=confirm?DialogResult.Yes:DialogResult.OK,Location=new Point(372,180),Size=new Size(124,36),BackColor=Theme.Primary,ForeColor=Color.White};f.Controls.Add(ok);f.AcceptButton=ok;
  if(confirm){var no=new RoundedButton{Text=L.T("no"),DialogResult=DialogResult.No,Location=new Point(236,180),Size=new Size(124,36),BackColor=Color.White};f.Controls.Add(no);f.CancelButton=no;f.ActiveControl=no;}
  return f.ShowDialog(owner);
 }
}
