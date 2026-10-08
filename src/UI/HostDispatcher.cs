namespace Whatsinthebox.UI;
public static class HostDispatcher
{
 public static Control? Anchor {get;set;}
 public static bool Dispatch(Action action)
 {
  var anchor=Anchor;if(anchor==null||!anchor.InvokeRequired)return false;
  anchor.Invoke(action);return true;
 }
}
