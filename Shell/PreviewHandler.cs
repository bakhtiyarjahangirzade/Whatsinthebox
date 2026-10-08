using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Whatsinthebox.UI;

namespace Whatsinthebox.Shell;
[StructLayout(LayoutKind.Sequential)]public struct Rect {public int Left,Top,Right,Bottom;}
[StructLayout(LayoutKind.Sequential)]public struct Msg {public IntPtr hwnd;public uint message;public UIntPtr wParam;public IntPtr lParam;public uint time;public int x,y;public uint lPrivate;}
[ComVisible(true),Guid("8895B1C6-B41F-4C1C-A562-0D564250836F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IPreviewHandler
{
 void SetWindow(IntPtr hwnd,ref Rect rect);void SetRect(ref Rect rect);void DoPreview();void Unload();void SetFocus();void QueryFocus(out IntPtr hwnd);[PreserveSig]int TranslateAccelerator(ref Msg msg);
}
[ComVisible(true),Guid("B7D14566-0509-4CCE-A71F-0A554233BD9B"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IInitializeWithFile {void Initialize([MarshalAs(UnmanagedType.LPWStr)]string path,uint mode);}
[ComVisible(true),Guid("B824B49D-22AC-4161-AC8A-9916E8FA3F7F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IInitializeWithStream {void Initialize(IStream stream,uint mode);}
[ComVisible(true),Guid("FC4801A3-2BA9-11CF-A229-00AA003D7352"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IObjectWithSite {void SetSite([MarshalAs(UnmanagedType.IUnknown)]object? site);void GetSite(ref Guid id,out IntPtr site);}
[ComVisible(true),Guid("00000114-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IOleWindow {void GetWindow(out IntPtr hwnd);void ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)]bool enter);}

[ComVisible(true),Guid("8A04DBB7-7A32-4922-A95A-C33FAC9E733B"),ClassInterface(ClassInterfaceType.None)]
public sealed class PreviewHandler : IPreviewHandler,IInitializeWithFile,IInitializeWithStream,IObjectWithSite,IOleWindow
{
    IntPtr parent;Rect bounds;PreviewControl? view;string? file,temp;object? site;
    public void Initialize(string path,uint mode){file=path;}
    public void Initialize(IStream stream,uint mode)
    {
        stream.Stat(out var stat,0);if(stat.cbSize>256L*1024*1024)throw new COMException(L.T("input.limit"),unchecked((int)0x800700DF));
        string suffix=Path.GetExtension(stat.pwcsName??"").ToLowerInvariant();if(!SupportedFiles.All.Contains(suffix))suffix=".witb";
        var root=PreviewStorage.Root;temp=Path.Combine(root,Guid.NewGuid().ToString("N")+suffix);
        using var output=File.Create(temp);var buffer=new byte[65536];IntPtr read=Marshal.AllocCoTaskMem(4);
        try{long total=0;while(true){stream.Read(buffer,buffer.Length,read);int n=Marshal.ReadInt32(read);if(n<=0)break;total+=n;if(total>256L*1024*1024)throw new IOException(L.T("input.limit"));output.Write(buffer,0,n);}}catch{output.Close();File.Delete(temp);temp=null;throw;}finally{Marshal.FreeCoTaskMem(read);}file=temp;
    }
    public void SetWindow(IntPtr hwnd,ref Rect rect){var copy=rect;if(HostDispatcher.Dispatch(()=>SetWindow(hwnd,ref copy)))return;parent=hwnd;bounds=rect;if(view!=null){SetParent(view.Handle,parent);Resize();}}
    public void SetRect(ref Rect rect){var copy=rect;if(HostDispatcher.Dispatch(()=>SetRect(ref copy)))return;bounds=rect;Resize();}
    void Resize(){if(view!=null)MoveWindow(view.Handle,bounds.Left,bounds.Top,Math.Max(1,bounds.Right-bounds.Left),Math.Max(1,bounds.Bottom-bounds.Top),true);}
    public void DoPreview()
    {
        if(HostDispatcher.Dispatch(DoPreview))return;
        if(file==null||parent==IntPtr.Zero)return;
        if(view!=null){view.Dispose();HostActivity.Closed();}view=new PreviewControl {RendererPath=HostActivity.Renderer(Path.GetDirectoryName(typeof(PreviewHandler).Assembly.Location)!)};HostActivity.Opened();
        view.CreateControl();SetParent(view.Handle,parent);SetWindowLongPtr(view.Handle,-16,new IntPtr(0x40000000|0x10000000|0x02000000|0x04000000));Resize();view.Show();_=view.LoadFileAsync(file,temp!=null?L.T("file.selected"):null);
    }
    public void Unload(){if(HostDispatcher.Dispatch(Unload))return;if(view!=null){view.Dispose();HostActivity.Closed();}view=null;file=null;if(temp!=null){try{File.Delete(temp);}catch{}temp=null;}}
    public void SetFocus(){if(HostDispatcher.Dispatch(SetFocus))return;view?.Focus();}
    public void QueryFocus(out IntPtr hwnd){IntPtr value=IntPtr.Zero;if(HostDispatcher.Dispatch(()=>value=GetFocus())){hwnd=value;return;}hwnd=GetFocus();}
    public int TranslateAccelerator(ref Msg msg)=>1;
    public void SetSite(object? value){site=value;}
    public void GetSite(ref Guid id,out IntPtr value){value=IntPtr.Zero;if(site==null)Marshal.ThrowExceptionForHR(unchecked((int)0x80004005));var unknown=Marshal.GetIUnknownForObject(site!);try{Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknown,in id,out value));}finally{Marshal.Release(unknown);}}
    public void GetWindow(out IntPtr hwnd){IntPtr value=IntPtr.Zero;if(HostDispatcher.Dispatch(()=>value=view?.Handle??parent)){hwnd=value;return;}hwnd=view?.Handle??parent;}
    public void ContextSensitiveHelp(bool enter){}
    [DllImport("user32.dll")]static extern IntPtr SetParent(IntPtr child,IntPtr parent);
    [DllImport("user32.dll")]static extern IntPtr SetWindowLongPtrW(IntPtr hwnd,int index,IntPtr value);
    static IntPtr SetWindowLongPtr(IntPtr hwnd,int index,IntPtr value)=>SetWindowLongPtrW(hwnd,index,value);
    [DllImport("user32.dll")]static extern bool MoveWindow(IntPtr hwnd,int x,int y,int width,int height,bool repaint);
    [DllImport("user32.dll")]static extern IntPtr GetFocus();
}
