using System.Net.NetworkInformation;
using System.Text.Json;
namespace Whatsinthebox;
internal sealed record UpdateResult(string State,string? Version=null,string? Url=null);
internal static class Updates
{
 internal const string Repository="https://github.com/bakhtiyarjahangirzade/Whatsinthebox";
 static string Cache=>Path.Combine(Installer.Root,"updates.json");
 internal static readonly Version Current=new(0,5,1);
 static readonly SemaphoreSlim Gate=new(1,1);
 internal static Version? Parse(string? tag)=>Version.TryParse(tag?.TrimStart('v').Split('-')[0],out var version)?version:null;
 internal static UpdateResult Select(JsonElement releases)
 {
  foreach(var release in releases.EnumerateArray().Where(x=>!x.GetProperty("draft").GetBoolean()).OrderByDescending(x=>Parse(x.GetProperty("tag_name").GetString())??new Version(0,0)))
  {
   var version=Parse(release.GetProperty("tag_name").GetString());if(version==null)continue;
   var asset=release.GetProperty("assets").EnumerateArray().FirstOrDefault(x=>x.GetProperty("name").GetString()==$"Whatsinthebox-{version}-Setup.exe");
   if(asset.ValueKind==JsonValueKind.Undefined)continue;
   string? url=asset.GetProperty("browser_download_url").GetString();
   if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Host!="github.com"||!uri.AbsolutePath.StartsWith("/bakhtiyarjahangirzade/Whatsinthebox/releases/download/",StringComparison.Ordinal))continue;
   return version>Current?new("available",version.ToString(),uri.AbsoluteUri):new("current");
  }
  return new("current");
 }
 internal static async Task<UpdateResult> CheckAsync(bool force=false)
 {
  await Gate.WaitAsync();try
  {
   if(!force&&File.Exists(Cache)&&DateTime.UtcNow-File.GetLastWriteTimeUtc(Cache)<TimeSpan.FromHours(24))try{var cached=JsonSerializer.Deserialize<UpdateResult>(await File.ReadAllTextAsync(Cache));if(cached?.State=="current")return cached;if(cached?.State=="available"&&Parse(cached.Version)>Current&&Uri.TryCreate(cached.Url,UriKind.Absolute,out var link)&&link.Scheme=="https"&&link.Host=="github.com"&&link.AbsolutePath.StartsWith("/bakhtiyarjahangirzade/Whatsinthebox/releases/download/",StringComparison.Ordinal))return cached;}catch{}
   if(!NetworkInterface.GetIsNetworkAvailable())return new("offline");
   using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(6)};client.DefaultRequestHeaders.UserAgent.ParseAdd("Whatsinthebox/0.5.1");client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
   using var response=await client.GetAsync("https://api.github.com/repos/bakhtiyarjahangirzade/Whatsinthebox/releases?per_page=20",HttpCompletionOption.ResponseHeadersRead);
   response.EnsureSuccessStatusCode();if(response.Content.Headers.ContentLength>1048576)return new("error");
   using var stream=await response.Content.ReadAsStreamAsync();using var memory=new MemoryStream();var buffer=new byte[8192];int n;using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(6));while((n=await stream.ReadAsync(buffer,timeout.Token))>0){if(memory.Length+n>1048576)return new("error");memory.Write(buffer,0,n);}
   using var json=JsonDocument.Parse(memory.ToArray());var result=Select(json.RootElement);Directory.CreateDirectory(Installer.Root);await File.WriteAllTextAsync(Cache,JsonSerializer.Serialize(result));return result;
  }
  catch{return new("offline");}finally{Gate.Release();}
 }
}
