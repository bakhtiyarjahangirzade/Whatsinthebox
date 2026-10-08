namespace Whatsinthebox.UI;
public static class PreviewGeometry
{
 public static Size Fit(double width,double height,int maxWidth,int maxHeight,bool upscale=false)
 {
  if(!double.IsFinite(width)||!double.IsFinite(height)||width<=0||height<=0)throw new ArgumentOutOfRangeException(nameof(width));
  double scale=Math.Min(maxWidth/width,maxHeight/height);if(!upscale)scale=Math.Min(1,scale);
  return new Size(Math.Clamp((int)Math.Round(width*scale),1,maxWidth),Math.Clamp((int)Math.Round(height*scale),1,maxHeight));
 }
}
