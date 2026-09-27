using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Vpet
{
    internal static partial class Tests
    {
        static void ToyRefinements()
        {
            var pet=Pet(MovementMode.Static);var toys=Toys(pet);toys.Bounce();
            float previous=0,beforePrevious=0;var peaks=new List<float>();
            for(int i=0;i<220;i++)
            {
                toys.AdvanceBall(.005f);float height=toys.BounceHeight;
                if(previous>beforePrevious&&previous>=height)peaks.Add(previous);
                beforePrevious=previous;previous=height;
            }
            Check(peaks.Count==3,"One click produces exactly three complete bounces");
            Near(peaks[0],18,.03f,"First bounce height");Near(peaks[1]/peaks[0],.5f,.003f,"Second bounce reaches half the first height");Near(peaks[2]/peaks[1],.5f,.003f,"Third bounce reaches half the second height");
            var strengths=new HashSet<int>();float minimum=float.MaxValue,maximum=0;double now=0;
            for(int shot=0;shot<20;shot++)
            {
                toys.SpawnBall(now);pet.Place(toys.Ball);toys.LaunchPull(PointF.Empty,now);
                for(int frame=0;frame<150&&toys.Launcher!=BallLauncher.Pet;frame++){now+=.01;ToyStep(toys,pet,now,.01f);}
                Check(toys.Launcher==BallLauncher.Pet,"Return shot records pet ownership");
                float strength=Geometry.Distance(toys.Velocity,PointF.Empty);strengths.Add((int)strength);minimum=Math.Min(minimum,strength);maximum=Math.Max(maximum,strength);
                Check(strength>=140&&strength<=720,"Pet launch power stays inside the playable range");
            }
            Check(strengths.Count>=15&&maximum-minimum>300,"Pet return launches vary visibly in strength");
            toys.Settings.HelpMessages=false;string path=Path.Combine(artifacts,"toy-help.json");pet.Settings.Save(path);
            Check(!Preferences.Load(path).Toys.HelpMessages,"Help Messages preference survives restart");
            File.WriteAllText(path,"{\"Toys\":{}}");Check(Preferences.Load(path).Toys.HelpMessages,"Existing toy settings enable hover help by default");
            using(var arrow=ToyArtwork.LaunchArrow(new Rectangle(0,0,150,100),new PointF(20,80),new PointF(110,25),1))
            {
                bool onlyRed=true;int visible=0;
                for(int y=0;y<arrow.Height;y++)for(int x=0;x<arrow.Width;x++){var pixel=arrow.GetPixel(x,y);if(pixel.A>0){visible++;onlyRed&=pixel.R>0&&pixel.G==0&&pixel.B==0&&(pixel.A<255||pixel.R==255);}}
                Check(visible>100&&onlyRed,"Launch arrow uses only solid red, with no white outline or second color");arrow.Save(Path.Combine(artifacts,"red-arrow.png"));
            }
            using(var fence=ToyArtwork.Fence(new Size(480,320),1))
            {
                Check(fence.GetPixel(240-6,160-3).ToArgb()==Color.White.ToArgb(),"Center icon has no directional arrowheads");
                Check(fence.GetPixel(240+6,160).B>fence.GetPixel(240+6,160).R&&fence.GetPixel(240,160+6).B>fence.GetPixel(240,160+6).R,"Center icon is a plus symbol");
            }
            using(var help=ToyArtwork.HelpMessage("Click for three bounces. Pull back, then release along the red arrow to launch the ball.",1,1000))help.Save(Path.Combine(artifacts,"toy-hover-help.png"));
        }
    }
}
