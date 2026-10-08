using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using Whatsinthebox.UI;
namespace Whatsinthebox.Shell;

[ComVisible(true),Guid("E357FCCD-A995-4576-B01F-234630154E96"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IThumbnailProvider {void GetThumbnail(uint edge,out IntPtr bitmap,out uint alphaType);}

[ComVisible(true),Guid("47CCD7B8-35F6-4835-965C-F488331ADE93"),ClassInterface(ClassInterfaceType.None)]
public sealed class ThumbnailProvider : IThumbnailProvider,IInitializeWithStream,IInitializeWithFile
{
 string? file,temp;
 public void Initialize(string path,uint mode){file=path;}
 public void Initialize(IStream stream,uint mode)
 {
  stream.Stat(out var stat,0);if(stat.cbSize>256L*1024*1024)throw new IOException(L.T("input.limit"));
  string extension=Path.GetExtension(stat.pwcsName??"").ToLowerInvariant();if(!SupportedFiles.All.Contains(extension))extension=".witb";
  temp=Path.Combine(PreviewStorage.Root,Guid.NewGuid().ToString("N")+extension);
  using var output=File.Create(temp);var buffer=new byte[65536];var count=Marshal.AllocCoTaskMem(4);
  try{long total=0;while(true){stream.Read(buffer,buffer.Length,count);int n=Marshal.ReadInt32(count);if(n<=0)break;total+=n;if(total>256L*1024*1024)throw new IOException(L.T("input.limit"));output.Write(buffer,0,n);}}finally{Marshal.FreeCoTaskMem(count);}file=temp;
 }
 public void GetThumbnail(uint edge,out IntPtr bitmap,out uint alphaType)
 {
  bitmap=IntPtr.Zero;alphaType=1;if(file==null)throw new IOException(L.T("preview.generic"));
  string directory=Path.Combine(PreviewStorage.Root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
  try
  {
   string png=Path.Combine(directory,"image.png"),json=Path.Combine(directory,"result.json");
   string exe=HostActivity.Renderer(Path.GetDirectoryName(typeof(ThumbnailProvider).Assembly.Location)!);
   using var worker=new Process{StartInfo=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden}};
   foreach(var arg in new[]{"--render",file,png,json})worker.StartInfo.ArgumentList.Add(arg);
   worker.Start();if(!worker.WaitForExit(15000)){worker.Kill(true);worker.WaitForExit();throw new TimeoutException(L.T("thumbnail.timeout"));}
   if(!File.Exists(json)||JsonSerializer.Deserialize<RenderInfo>(File.ReadAllText(json))?.Success!=true)throw new IOException(L.T("thumbnail.failed"));
   using var source=Image.FromFile(png);int size=(int)Math.Clamp(edge,32,1024);var dimensions=PreviewGeometry.Fit(source.Width,source.Height,size,size);int width=dimensions.Width,height=dimensions.Height;
   using var image=new Bitmap(width,height);using(var g=Graphics.FromImage(image)){g.Clear(Color.White);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(source,0,0,width,height);}bitmap=image.GetHbitmap();
  }
  finally{try{Directory.Delete(directory,true);}catch{}}
 }
 ~ThumbnailProvider(){if(temp!=null){try{File.Delete(temp);}catch{}}}
}
