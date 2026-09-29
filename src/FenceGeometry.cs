using System;
using System.Collections.Generic;
using System.Drawing;

namespace Vpet
{
    internal static class FenceGeometry
    {
        internal static DisplayArea Nearest(List<DisplayArea> displays,PointF point)
        {
            DisplayArea best=displays[0];float distance=float.MaxValue;
            foreach(var display in displays){float d=Geometry.Distance(point,Geometry.Clamp(point,display.Work));if(d<distance){distance=d;best=display;}}
            return best;
        }
        internal static SizeF Minimum(DisplayArea display,Size frame)
        {
            var size=display.PetSize(frame);return new SizeF(Math.Min(display.Work.Width,Math.Max(160*display.Scale,size.Width+16*display.Scale)),
                Math.Min(display.Work.Height,Math.Max(140*display.Scale,2*(size.Height+8*display.Scale))));
        }
        internal static RectangleF Fit(RectangleF requested,DisplayArea display,Size frame)
        {
            var work=display.Work;var minimum=Minimum(display,frame);
            float width=Math.Min(work.Width,Math.Max(minimum.Width,requested.Width)),height=Math.Min(work.Height,Math.Max(minimum.Height,requested.Height));
            return new RectangleF(Math.Max(work.Left,Math.Min(work.Right-width,requested.X)),Math.Max(work.Top,Math.Min(work.Bottom-height,requested.Y)),width,height);
        }
        internal static RectangleF Resize(RectangleF original,ZoneEdge edges,PointF delta,DisplayArea display,Size frame)
        {
            var work=display.Work;var min=Minimum(display,frame);float left=original.Left,top=original.Top,right=original.Right,bottom=original.Bottom;
            if((edges&ZoneEdge.Left)!=0)left=Math.Max(work.Left,Math.Min(right-min.Width,left+delta.X));
            if((edges&ZoneEdge.Right)!=0)right=Math.Min(work.Right,Math.Max(left+min.Width,right+delta.X));
            if((edges&ZoneEdge.Top)!=0)top=Math.Max(work.Top,Math.Min(bottom-min.Height,top+delta.Y));
            if((edges&ZoneEdge.Bottom)!=0)bottom=Math.Min(work.Bottom,Math.Max(top+min.Height,bottom+delta.Y));
            return RectangleF.FromLTRB(left,top,right,bottom);
        }
        internal static ZoneEdge Hit(RectangleF zone,Point p,float scale)
        {
            float tolerance=8*scale;ZoneEdge result=ZoneEdge.None;
            if(Math.Abs(p.X-zone.Left)<=tolerance)result|=ZoneEdge.Left;if(Math.Abs(p.X-zone.Right)<=tolerance)result|=ZoneEdge.Right;
            if(Math.Abs(p.Y-zone.Top)<=tolerance)result|=ZoneEdge.Top;if(Math.Abs(p.Y-zone.Bottom)<=tolerance)result|=ZoneEdge.Bottom;return result;
        }
        internal static PointF Center(RectangleF zone){return new PointF(zone.X+zone.Width/2,zone.Y+zone.Height/2);}
    }
}
