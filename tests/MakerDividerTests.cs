using System;
using System.Drawing;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static void DividerMouse(Control divider,int message,Point screen)
        {Cursor.Position=screen;var local=divider.PointToClient(screen);Native.SendMessage(divider.Handle,(uint)message,new IntPtr(message==0x202?0:1),new IntPtr((local.Y<<16)|(local.X&0xffff)));}
        static void DragDivider(Panel divider,Point destination)
        {var grip=divider.PointToScreen(new Point(divider.Width/2,divider.Height/2));DividerMouse(divider,0x201,grip);DividerMouse(divider,0x200,destination);DividerMouse(divider,0x202,destination);Application.DoEvents();}
        static void MakerDividerInteractions(SpriteMakerWindow maker)
        {
            Point cursor=Cursor.Position;
            try
            {
                maker.ClientSize=new Size(1100,800);Application.DoEvents();var workspace=MakerField<TableLayoutPanel>(maker,"workspace");var divider=MakerField<Panel>(maker,"workspaceDivider");var sheet=MakerField<SpriteSheetView>(maker,"sheet");
                var before=workspace.GetColumnWidths();float zoom=sheet.Zoom;var draft=sheet.Draft;bool dirty=maker.Dirty;int cycle=maker.Cycle,slot=maker.Slot;
                var grip=divider.PointToScreen(new Point(divider.Width/2,divider.Height/2));DragDivider(divider,new Point(grip.X+100,grip.Y));var moved=workspace.GetColumnWidths();
                Check(moved[0]>before[0]+80&&moved[2]<before[2]-80,"Dragging the invisible divider changes both pane widths");
                Check(sheet.Zoom==zoom&&sheet.Draft==draft&&maker.Dirty==dirty&&maker.Cycle==cycle&&maker.Slot==slot,"Divider drag preserves zoom, draft, selection, and project state");
                float share=moved[0]/(float)(moved[0]+moved[2]);maker.ClientSize=new Size(1300,900);Application.DoEvents();var grown=workspace.GetColumnWidths();
                Near(grown[0]/(float)(grown[0]+grown[2]),share,.01f,"Window resize retains the user's pane ratio");
                DragDivider(divider,workspace.PointToScreen(new Point(-500,workspace.Height/2)));Check(workspace.GetColumnWidths()[0]>=350,"Divider keeps the animation controls usable at the left limit");
                DragDivider(divider,workspace.PointToScreen(new Point(workspace.Width+500,workspace.Height/2)));Check(workspace.GetColumnWidths()[2]>=240,"Divider keeps the preview usable at the right limit");
                grip=divider.PointToScreen(new Point(divider.Width/2,divider.Height/2));DividerMouse(divider,0x201,grip);divider.Capture=false;int left=workspace.GetColumnWidths()[0];DividerMouse(divider,0x200,new Point(grip.X-90,grip.Y));
                Check(workspace.GetColumnWidths()[0]==left,"Lost capture ends pane resizing");
                maker.ClientSize=new Size(1100,800);Application.DoEvents();DragDivider(divider,workspace.PointToScreen(new Point((workspace.Width-divider.Width)/2+divider.Width/2,workspace.Height/2)));
                CaptureForm(maker,"maker-adjustable-preview");
            }
            finally{Cursor.Position=cursor;}
        }
    }
}
