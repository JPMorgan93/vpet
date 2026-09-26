using System;
using System.Drawing;
using System.Collections.Generic;

namespace Vpet
{
    // Both display fragments use the same animation frame and crossing progress. At a
    // shared edge they form one continuous sprite; gaps/offsets use matched edge fragments.
    public sealed class DisplayCrossing
    {
        public DisplayArea From,To;
        public PointF Exit,Entry;
        public Size SourceSize,DestinationSize;
        public int Direction;
        public float Progress;
        public bool Horizontal {get{return Direction==0||Direction==4;}}
        int Sign {get{return Direction==0||Direction==2?1:-1;}}
        public float TravelLength {get{return Horizontal?(SourceSize.Width+DestinationSize.Width)/2f:(SourceSize.Height+DestinationSize.Height)/2f;}}
        public PointF SourceAnchor {get{return Horizontal?new PointF(Exit.X+Sign*SourceSize.Width*Progress,Exit.Y):new PointF(Exit.X,Exit.Y+Sign*SourceSize.Height*Progress);}}
        public PointF DestinationAnchor {get{return Horizontal?new PointF(Entry.X-Sign*DestinationSize.Width*(1-Progress),Entry.Y):new PointF(Entry.X,Entry.Y-Sign*DestinationSize.Height*(1-Progress));}}
        public bool FitsCircle(PointF center,float radius)
        {
            float saved=Progress;Progress=0;var incoming=DestinationAnchor;Progress=1;var outgoing=SourceAnchor;Progress=saved;
            return Geometry.Distance(Exit,center)<=radius&&Geometry.Distance(Entry,center)<=radius&&Geometry.Distance(incoming,center)<=radius&&Geometry.Distance(outgoing,center)<=radius;
        }
        public static DisplayArea NextDisplay(DisplayArea from,DisplayArea target,List<DisplayArea> displays,Size cell)
        {
            // Prefer touching/nearby displays, including normal taskbar gaps. This routes
            // through a middle monitor instead of jumping straight to the far monitor.
            var visited=new Dictionary<string,string>();var queue=new Queue<DisplayArea>();visited[from.Id]=null;queue.Enqueue(from);
            while(queue.Count>0)
            {
                var current=queue.Dequeue();
                if(current.Id==target.Id)
                {string next=target.Id;while(visited[next]!=from.Id&&visited[next]!=null)next=visited[next];return displays.Find(d=>d.Id==next);}
                foreach(var candidate in displays)
                {
                    if(visited.ContainsKey(candidate.Id))continue;
                    var a=current.Work;var b=candidate.Work;Size sa=current.PetSize(cell),sb=candidate.PetSize(cell);
                    int overlapY=Math.Min(a.Bottom,b.Bottom)-Math.Max(a.Top,b.Top),overlapX=Math.Min(a.Right,b.Right)-Math.Max(a.Left,b.Left);
                    bool horizontal=(Math.Abs(a.Right-b.Left)<=64||Math.Abs(b.Right-a.Left)<=64)&&overlapY>=Math.Max(sa.Height,sb.Height);
                    bool vertical=(Math.Abs(a.Bottom-b.Top)<=64||Math.Abs(b.Bottom-a.Top)<=64)&&overlapX>=Math.Max(sa.Width,sb.Width);
                    if(horizontal||vertical){visited[candidate.Id]=current.Id;queue.Enqueue(candidate);}
                }
            }
            return target;
        }
        public static DisplayCrossing Plan(DisplayArea from,DisplayArea to,Size cell,PointF destination)
        {
            var result=new DisplayCrossing{From=from,To=to,SourceSize=from.PetSize(cell),DestinationSize=to.PetSize(cell)};
            RectangleF a=from.Allowed(cell),b=to.Allowed(cell);
            float dx=(b.Left+b.Right-a.Left-a.Right)/2,dy=(b.Top+b.Bottom-a.Top-a.Bottom)/2;
            bool sideBySide=to.Work.Left>=from.Work.Right||from.Work.Left>=to.Work.Right;
            bool stacked=to.Work.Top>=from.Work.Bottom||from.Work.Top>=to.Work.Bottom;
            bool horizontal=sideBySide&&(!stacked||Math.Abs(dx)>=Math.Abs(dy));
            if(horizontal)
            {
                float low=Math.Max(a.Top,b.Top),high=Math.Min(a.Bottom,b.Bottom);
                float y=low<=high?Math.Max(low,Math.Min(high,destination.Y)):destination.Y;
                result.Direction=dx>=0?0:4;
                result.Exit=Geometry.Clamp(new PointF(dx>=0?a.Right:a.Left,y),a);
                result.Entry=Geometry.Clamp(new PointF(dx>=0?b.Left:b.Right,y),b);
            }
            else
            {
                float low=Math.Max(a.Left,b.Left),high=Math.Min(a.Right,b.Right);
                float x=low<=high?Math.Max(low,Math.Min(high,destination.X)):destination.X;
                result.Direction=dy>=0?2:6;
                result.Exit=Geometry.Clamp(new PointF(x,dy>=0?a.Bottom:a.Top),a);
                result.Entry=Geometry.Clamp(new PointF(x,dy>=0?b.Top:b.Bottom),b);
            }
            return result;
        }
    }
}
