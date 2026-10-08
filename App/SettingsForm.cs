using System.Diagnostics;
using Whatsinthebox.UI;
namespace Whatsinthebox;
sealed class SettingsForm:Form
{
 readonly Label status=new();
 public SettingsForm(){Build();}
 void Build()
 {
  status.Parent?.Controls.Remove(status);foreach(Control old in Controls.Cast<Control>().ToArray())old.Dispose();Controls.Clear();Text=L.T("settings.window");ClientSize=new Size(740,540);MinimumSize=new Size(740,580);StartPosition=FormStartPosition.CenterScreen;Font=UiStyle.Font(11);BackColor=Theme.Canvas;ForeColor=Theme.Text;Icon=Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(32),ColumnCount=1,RowCount=7};foreach(int height in new[]{82,66,48,146,52,36,28})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
  var header=new Panel{Dock=DockStyle.Fill};header.Controls.Add(new Label{Text=L.T("settings.title"),Location=new Point(88,20),AutoSize=true,Font=UiStyle.Font(22,FontStyle.Bold)});
  var logo=Path.Combine(AppContext.BaseDirectory,"logo.png");if(File.Exists(logo)){using var image=Image.FromFile(logo);var picture=new PictureBox{Image=new Bitmap(image),SizeMode=PictureBoxSizeMode.Zoom,Size=new Size(72,72)};picture.Disposed+=(_,_)=>picture.Image?.Dispose();header.Controls.Add(picture);}layout.Controls.Add(header,0,0);
  layout.Controls.Add(new Label{Text=L.T("settings.intro")+"\n"+L.T("settings.hint"),Dock=DockStyle.Fill,ForeColor=Theme.Muted},0,1);
  var languages=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};languages.Controls.Add(new Label{Text=L.T("language"),AutoSize=true,Margin=new Padding(0,9,12,0)});for(int i=0;i<L.Languages.Length;i++){string code=L.Languages[i];bool selected=code==L.Language;var choice=new RoundedButton{Text=L.LanguageNames[i],Width=104,Height=34,Font=UiStyle.Font(9),BackColor=selected?Theme.Primary:Color.White,ForeColor=selected?Color.White:Theme.Text,Margin=new Padding(0,0,6,0)};choice.Click+=(_,_)=>{L.Set(code);Build();};languages.Controls.Add(choice);}layout.Controls.Add(languages,0,2);
  status.Dock=DockStyle.Fill;status.Padding=new Padding(16);status.BackColor=Color.White;layout.Controls.Add(status,0,3);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(0,12,0,0)};
  void Add(string key,Action action,bool primary=false){var b=new RoundedButton{Text=L.T(key),Width=key=="repair"?245:150,Height=36,BackColor=primary?Theme.Primary:Color.White,ForeColor=primary?Color.White:Theme.Text,Margin=new Padding(0,0,12,0)};b.Click+=(_,_)=>action();actions.Controls.Add(b);}
  Add("repair",()=>{Installer.PromptInstall(this);RefreshStatus();},true);Add("uninstall",()=>{string path=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","unins000.exe"));if(File.Exists(path)){Process.Start(new ProcessStartInfo(path){UseShellExecute=true});Close();}else AppDialog.Show(this,L.T("uninstall.help"));});Add("close",Close);layout.Controls.Add(actions,0,4);
  var link=new LinkLabel{Text=L.T("repository"),AutoSize=true,LinkColor=Theme.Primary,Margin=new Padding(0,12,0,0)};link.LinkClicked+=(_,_)=>Process.Start(new ProcessStartInfo("https://github.com/bakhtiyarjahangirzade/Whatsinthebox"){UseShellExecute=true});layout.Controls.Add(link,0,5);
  layout.Controls.Add(new Label{Text=L.T("footer"),Dock=DockStyle.Fill,Font=UiStyle.Font(9),ForeColor=Theme.Muted},0,6);Controls.Add(layout);RefreshStatus();
 }
 void RefreshStatus(){bool p=Installer.EffectiveHandler(".pdf",IntegrationIds.PreviewInterface).Equals(IntegrationIds.Preview,StringComparison.OrdinalIgnoreCase),t=Installer.EffectiveHandler(".pdf",IntegrationIds.ThumbnailInterface).Equals(IntegrationIds.Thumbnail,StringComparison.OrdinalIgnoreCase);status.Text=L.T("pdf.preview")+": "+L.T(p?"enabled":"needs.repair")+"\n"+L.T("pdf.thumbnail")+": "+L.T(t?"enabled":"needs.repair")+"\n\n"+L.T(p&&t?"ready":"repair.help");}
}
