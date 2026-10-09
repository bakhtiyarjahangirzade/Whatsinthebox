using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using Whatsinthebox.UI;
namespace Whatsinthebox;
sealed class SettingsForm:Form
{
 Label update=null!;RoundedButton download=null!;UpdateResult? result;
 [DllImport("user32.dll")]static extern bool ReleaseCapture();
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
 public SettingsForm(){Build();Shown+=async(_,_)=>await Check(false);}
 void Build()
 {
  foreach(Control old in Controls.Cast<Control>().ToArray())old.Dispose();Controls.Clear();AutoScaleMode=AutoScaleMode.Dpi;FormBorderStyle=FormBorderStyle.None;Text="Whatsinthebox";ClientSize=new Size(620,574);StartPosition=FormStartPosition.CenterScreen;Font=UiStyle.Font(10);BackColor=Theme.Canvas;ForeColor=Theme.Text;
  using(var path=new GraphicsPath()){path.AddArc(0,0,24,24,180,90);path.AddArc(Width-25,0,24,24,270,90);path.AddArc(Width-25,Height-25,24,24,0,90);path.AddArc(0,Height-25,24,24,90,90);path.CloseFigure();Region?.Dispose();Region=new Region(path);}
  var header=new Panel{Location=new Point(28,26),Size=new Size(510,70)};header.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};Controls.Add(header);
  string logo=Path.Combine(AppContext.BaseDirectory,"logo.png");if(File.Exists(logo)){using var image=Image.FromFile(logo);var pic=new PictureBox{Image=new Bitmap(image),Size=new Size(58,58),SizeMode=PictureBoxSizeMode.Zoom};pic.Disposed+=(_,_)=>pic.Image?.Dispose();header.Controls.Add(pic);}
  header.Controls.Add(new Label{Text="Whatsinthebox",AutoSize=true,Font=UiStyle.Font(22,FontStyle.Bold),Location=new Point(76,0)});header.Controls.Add(new Label{Text=L.T("settings.title")+" · v"+Updates.Current,AutoSize=true,ForeColor=Theme.Muted,Location=new Point(78,43)});
  var close=new RoundedButton{Text="×",AccessibleName=L.T("close"),Location=new Point(557,18),Size=new Size(34,34),BackColor=Theme.Canvas};close.Click+=(_,_)=>Close();Controls.Add(close);
  bool ready=Installer.EffectiveHandler(".pdf",IntegrationIds.PreviewInterface).Equals(IntegrationIds.Preview,StringComparison.OrdinalIgnoreCase)&&Installer.EffectiveHandler(".pdf",IntegrationIds.ThumbnailInterface).Equals(IntegrationIds.Thumbnail,StringComparison.OrdinalIgnoreCase);
  var card=new SurfacePanel{Location=new Point(28,116),Size=new Size(564,136)};card.Controls.Add(new Label{Text=L.T(ready?"ready.short":"needs.repair"),Location=new Point(22,17),Size=new Size(520,29),Font=UiStyle.Font(15,FontStyle.Bold),ForeColor=ready?Color.FromArgb(30,115,83):Theme.Text});card.Controls.Add(new Label{Text=L.T("settings.intro"),Location=new Point(22,53),Size=new Size(520,36)});card.Controls.Add(new Label{Text=L.T("settings.hint"),Location=new Point(22,92),Size=new Size(520,32),ForeColor=Theme.Muted,Font=UiStyle.Font(9)});Controls.Add(card);
  var updates=new SurfacePanel{Location=new Point(28,266),Size=new Size(564,96)};updates.Controls.Add(new Label{Text=L.T("updates.title"),Location=new Point(22,16),AutoSize=true,Font=UiStyle.Font(11,FontStyle.Bold)});update=new Label{Text=L.T("updates.checking"),Location=new Point(22,47),Size=new Size(360,35),ForeColor=Theme.Muted};updates.Controls.Add(update);var check=new RoundedButton{Text=L.T("updates.check"),Location=new Point(414,29),Size=new Size(128,38),BackColor=Theme.Canvas,Font=UiStyle.Font(9)};check.Click+=async(_,_)=>{check.Enabled=false;await Check(true);if(!check.IsDisposed)check.Enabled=true;};updates.Controls.Add(check);Controls.Add(updates);
  var languages=new FlowLayoutPanel{Location=new Point(28,378),Size=new Size(564,40),WrapContents=false};for(int i=0;i<L.Languages.Length;i++){string code=L.Languages[i];bool selected=code==L.Language;var b=new RoundedButton{Text=L.LanguageNames[i],Size=new Size(104,36),Margin=new Padding(0,0,8,0),BackColor=selected?Theme.Primary:Color.White,ForeColor=selected?Color.White:Theme.Text,Font=UiStyle.Font(9)};b.Click+=(_,_)=>{L.Set(code);Build();_=Check(false);};languages.Controls.Add(b);}Controls.Add(languages);
  var actions=new FlowLayoutPanel{Location=new Point(28,430),Size=new Size(564,40),WrapContents=false};void Action(string key,System.Action fn,int width){var b=new RoundedButton{Text=L.T(key),Size=new Size(width,38),Margin=new Padding(0,0,10,0),BackColor=Color.White,Font=UiStyle.Font(9)};b.Click+=(_,_)=>fn();actions.Controls.Add(b);}Action("repair",()=>{Installer.PromptInstall(this);Build();_=Check(false);},204);Action("repository",()=>Open(Updates.Repository),160);Action("uninstall",()=>{var path=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","unins000.exe"));if(File.Exists(path))Process.Start(new ProcessStartInfo(path){UseShellExecute=true});else AppDialog.Show(this,L.T("uninstall.help"));},170);Controls.Add(actions);
  download=new RoundedButton{Text=L.T("updates.download"),Location=new Point(28,480),Size=new Size(564,38),BackColor=Theme.Primary,ForeColor=Color.White,Visible=result?.State=="available"};download.Click+=(_,_)=>{if(result?.Url is string url)Open(url);};Controls.Add(download);Controls.Add(new Label{Text=L.T("footer"),Location=new Point(28,535),Size=new Size(564,24),TextAlign=ContentAlignment.MiddleCenter,ForeColor=Theme.Muted,Font=UiStyle.Font(8)});
 }
 static void Open(string url)=>Process.Start(new ProcessStartInfo(url){UseShellExecute=true});
 async Task Check(bool force){var label=update;label.Text=L.T("updates.checking");var state=await Updates.CheckAsync(force);if(IsDisposed||label.IsDisposed)return;result=state;label.Text=L.T("updates."+state.State,state.Version??"");download.Visible=state.State=="available";}
}

