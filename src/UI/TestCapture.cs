using System.Text.Json;
namespace Whatsinthebox.UI;
// Isolated integration tests list exact fixture paths in test-capture.flag next to the
// application. Explorer may hand the handler a stream copy, so the original file name
// is matched as well. Nothing is recorded unless such a flag file exists.
public static class TestCapture
{
    static string[] Targets(string rendererPath)
    {
        try
        {
            string marker=Path.Combine(Path.GetDirectoryName(rendererPath)!,"test-capture.flag");
            return File.Exists(marker)?File.ReadAllLines(marker).Select(x=>x.Trim()).Where(x=>x.Length>0&&Path.IsPathFullyQualified(x)).ToArray():Array.Empty<string>();
        }
        catch{return Array.Empty<string>();}
    }
    public static string? Match(string rendererPath,string? path,string? sourceName)
    {
        foreach(var target in Targets(rendererPath))
        {
            if(path!=null&&target.Equals(Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase))return target;
            if(!string.IsNullOrWhiteSpace(sourceName)&&Path.GetFileName(target).Equals(Path.GetFileName(sourceName),StringComparison.OrdinalIgnoreCase))return target;
        }
        return null;
    }
    public static void RecordThumbnail(string rendererPath,string? path,string? sourceName,uint edge,int width,int height,int sourceWidth,int sourceHeight)
    {
        string? target=Match(rendererPath,path,sourceName);if(target==null)return;
        try{File.AppendAllText(Path.Combine(PreviewStorage.Root,"thumbnail-test.log"),JsonSerializer.Serialize(new{source=Path.GetFileName(target),edge,width,height,sourceWidth,sourceHeight,process=Environment.ProcessId,time=DateTime.UtcNow})+Environment.NewLine);}catch{}
    }
    // Integration-test trace of the Shell calls the handler receives; inactive without a flag file.
    public static void Trace(string rendererPath,string message)
    {
        if(Targets(rendererPath).Length==0)return;
        try{File.AppendAllText(Path.Combine(PreviewStorage.Root,"handler-trace.log"),DateTime.UtcNow.ToString("O")+" pid="+Environment.ProcessId+" "+message+Environment.NewLine);}catch{}
    }
}
