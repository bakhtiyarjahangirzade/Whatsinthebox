using System.Globalization;
using System.Text.Json;
namespace Whatsinthebox.UI;
public static class L
{
 public static readonly string[] Languages={"en","ru","de","zh","tr"};
 public static readonly string[] LanguageNames={"English","Русский","Deutsch","简体中文","Türkçe"};
 static readonly Dictionary<string,Dictionary<string,string>> Catalog=Load();
 public static string? Override {get;set;}
 static string Settings=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Whatsinthebox","language.json");
 static Dictionary<string,Dictionary<string,string>> Load(){using var stream=typeof(L).Assembly.GetManifestResourceStream("Whatsinthebox.UI.translations.json")!;return JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,string>>>(stream)!;}
 public static string Language
 {
  get{if(Override!=null)return Override;try{if(File.Exists(Settings)){var code=JsonSerializer.Deserialize<string>(File.ReadAllText(Settings));if(code!=null&&Languages.Contains(code))return code;}}catch{}string sys=CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;return Languages.Contains(sys)?sys:"en";}
 }
 public static string T(string key,params object[] args)
 {
  string value=Catalog[Language].TryGetValue(key,out var translated)?translated:key;return args.Length==0?value:string.Format(CultureInfo.GetCultureInfo(Language=="zh"?"zh-CN":Language),value,args);
 }
 public static void Set(string code){if(!Languages.Contains(code))throw new ArgumentException("Unsupported locale");Directory.CreateDirectory(Path.GetDirectoryName(Settings)!);File.WriteAllText(Settings,JsonSerializer.Serialize(code));Override=null;}
 public static IReadOnlyDictionary<string,Dictionary<string,string>> All=>Catalog;
}
