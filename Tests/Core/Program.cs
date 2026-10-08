global using System.Drawing;
using Whatsinthebox.UI;
using System.Text.Json;
var results = new List<object>();
void Check(string name, bool passed) {results.Add(new{name,passed});if(!passed)throw new Exception(name);}
foreach(var c in new[]{(1600d,644d,256,256,256,103),(644d,1600d,256,256,103,256),(1600d,644d,650,220,547,220),(1600d,644d,250,300,250,101),(8d,4d,4,4,4,2),(8d,4d,256,256,8,4),(400d,400d,96,96,96,96)}){
 var size=PreviewGeometry.Fit(c.Item1,c.Item2,c.Item3,c.Item4);
 Check($"{c.Item1}x{c.Item2} within {c.Item3}x{c.Item4}",size.Width==c.Item5&&size.Height==c.Item6);
}
Check("explicit upscaling preserves ratio",PreviewGeometry.Fit(8,4,16,16,true)==new Size(16,8));
Check("small embedded artwork fills thumbnail edge",PreviewGeometry.Fit(96,48,256,256,true)==new Size(256,128));
Check("small artwork fits tall preview pane without stretching",PreviewGeometry.Fit(96,48,618,728,true)==new Size(618,309));
var random=new Random(47);
for(int i=0;i<1000;i++){
 double width=random.Next(20,10000),height=random.Next(20,10000);int maxWidth=random.Next(1,1025),maxHeight=random.Next(1,1025);
 var fit=PreviewGeometry.Fit(width,height,maxWidth,maxHeight);
 double scaledWidth,scaledHeight;
 if(width<=maxWidth&&height<=maxHeight){scaledWidth=width;scaledHeight=height;}
 else if(width*maxHeight>=height*maxWidth){scaledWidth=maxWidth;scaledHeight=height*maxWidth/width;}
 else{scaledHeight=maxHeight;scaledWidth=width*maxHeight/height;}
 if(fit.Width<1||fit.Height<1||fit.Width>maxWidth||fit.Height>maxHeight||Math.Abs(fit.Width-scaledWidth)>1||Math.Abs(fit.Height-scaledHeight)>1)throw new Exception($"Random geometry {i}");
}
Check("1000 asymmetric bounds preserve source proportions to pixel precision",true);
foreach(var c in new[]{(0d,1d,1,1),(1d,0d,1,1),(double.NaN,1d,1,1),(1d,double.PositiveInfinity,1,1),(1d,1d,0,1),(1d,1d,1,-1)}){
 bool rejected=false;try{PreviewGeometry.Fit(c.Item1,c.Item2,c.Item3,c.Item4);}catch(ArgumentOutOfRangeException){rejected=true;}
 Check($"invalid geometry {c}",rejected);
}
Console.WriteLine(JsonSerializer.Serialize(new{failed=0,checks=results.Count,randomCases=1000,results}));
