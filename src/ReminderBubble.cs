using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Vpet
{
    // Ordinary child controls keep links and Dismiss accessible; the popup itself never activates.
    internal sealed class ReminderBubble : Form
    {
        readonly Label heading=new Label{Text="Reminder",AutoSize=false,ForeColor=MakerUi.Purple};
        readonly RichTextBox message=ReminderUi.MessageBox(false);
        readonly Button dismiss=MakerUi.Button("Dismiss",null);
        LayerMode? layer;float scale=1;bool tailLeft=true;
        public event Action Dismissed;
        public string ReminderKey {get;private set;}
        public ReminderBubble()
        {
            Text="Vpet reminder";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;
            AutoScaleMode=AutoScaleMode.None;BackColor=Color.White;DoubleBuffered=true;
            Controls.Add(heading);Controls.Add(message);Controls.Add(dismiss);dismiss.Click+=delegate{if(Dismissed!=null)Dismissed();};
            Native.BackgroundAdornments.Add(Handle);FormClosed+=delegate{Native.BackgroundAdornments.Remove(Handle);};
        }
        protected override bool ShowWithoutActivation {get{return true;}}
        protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80|0x08000000;return p;}}
        protected override void WndProc(ref Message m)
        {
            if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
            if(m.Msg==0x46&&m.LParam!=IntPtr.Zero&&layer==LayerMode.UnderAll)
            {
                var position=(Native.WINDOWPOS)Marshal.PtrToStructure(m.LParam,typeof(Native.WINDOWPOS));
                position.InsertAfter=Native.UnderAllTarget(Handle,IntPtr.Zero,IntPtr.Zero);position.Flags=(position.Flags&~4u)|0x210u;Marshal.StructureToPtr(position,m.LParam,false);
            }
            base.WndProc(ref m);
        }
        public void Display(Reminder reminder)
        {ReminderKey=reminder.Id+":"+reminder.Pending;message.Text=reminder.Message;}
        internal static Rectangle PositionFor(Rectangle pet,Rectangle emote,Rectangle work,Size size)
        {
            int gap=12;int x=pet.Right+gap;
            if(x+size.Width>work.Right)x=pet.Left-size.Width-gap;
            x=Math.Max(work.Left,Math.Min(work.Right-size.Width,x));
            int y=Math.Max(work.Top,Math.Min(work.Bottom-size.Height,pet.Top+(pet.Height-size.Height)/2));
            var bounds=new Rectangle(x,y,size.Width,size.Height);
            if(!emote.IsEmpty&&bounds.IntersectsWith(emote))
            {
                if(emote.Bottom+gap+size.Height<=work.Bottom)bounds.Y=emote.Bottom+gap;
                else if(emote.Top-gap-size.Height>=work.Top)bounds.Y=emote.Top-gap-size.Height;
            }
            return bounds;
        }
        public void Place(Rectangle pet,Rectangle emote,Rectangle work,LayerMode mode,float displayScale,Form owner)
        {
            float nextScale=displayScale;
            if(scale!=nextScale||heading.Font.Style!=FontStyle.Bold)
            {
                scale=nextScale;var oldHeading=heading.Font;var oldMessage=message.Font;var oldButton=dismiss.Font;
                heading.Font=new Font("Segoe UI",14*scale,FontStyle.Bold,GraphicsUnit.Pixel);message.Font=new Font("Segoe UI",14*scale,FontStyle.Regular,GraphicsUnit.Pixel);dismiss.Font=new Font("Segoe UI",14*scale,FontStyle.Regular,GraphicsUnit.Pixel);
                // The initial control fonts can be shared system defaults.
                if(oldHeading!=Control.DefaultFont)oldHeading.Dispose();if(oldMessage!=Control.DefaultFont)oldMessage.Dispose();if(oldButton!=Control.DefaultFont)oldButton.Dispose();
            }
            int padding=(int)(18*scale),tail=(int)(12*scale),width=Math.Min((int)(350*scale),work.Width);
            int textWidth=Math.Max(50,width-padding*2-tail);
            int textHeight=TextRenderer.MeasureText(message.Text,message.Font,new Size(textWidth,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl|TextFormatFlags.NoPrefix).Height+(int)(12*scale);
            int height=Math.Min(work.Height,Math.Max((int)(155*scale),textHeight+(int)(96*scale)));
            var bounds=PositionFor(pet,emote,work,new Size(width,height));bool left=bounds.Left>=pet.Right;
            bool changed=Size!=bounds.Size||tailLeft!=left;tailLeft=left;Bounds=bounds;
            int inset=padding+(tailLeft?tail:0);heading.SetBounds(inset,(int)(12*scale),textWidth,(int)(25*scale));
            message.SetBounds(inset,(int)(40*scale),textWidth,Math.Max(20,height-(int)(96*scale)));
            dismiss.AutoSize=false;dismiss.SetBounds(inset+textWidth-(int)(94*scale),height-(int)(44*scale),(int)(94*scale),(int)(32*scale));
            if(changed||Region==null){using(var path=Outline()){var old=Region;Region=new Region(path);if(old!=null)old.Dispose();}Invalidate();}
            if(layer!=mode)
            {
                layer=mode;Owner=mode==LayerMode.UnderAll?null:owner;
                Native.SetWindowPos(Handle,mode==LayerMode.OverEverything?new IntPtr(-1):new IntPtr(-2),0,0,0,0,0x213);
            }
            if(!Visible)Show();
        }
        GraphicsPath Outline()
        {
            float tail=12*scale,r=12*scale,left=tailLeft?tail:1,right=tailLeft?Width-2:Width-tail-1,top=1,bottom=Height-2,mid=Height/2f;
            var path=new GraphicsPath();path.AddArc(left,top,r,r,180,90);path.AddArc(right-r,top,r,r,270,90);
            if(!tailLeft){path.AddLine(right,mid-8*scale,Width-1,mid);path.AddLine(Width-1,mid,right,mid+8*scale);}
            path.AddArc(right-r,bottom-r,r,r,0,90);path.AddArc(left,bottom-r,r,r,90,90);
            if(tailLeft){path.AddLine(left,mid+8*scale,0,mid);path.AddLine(0,mid,left,mid-8*scale);}
            path.CloseFigure();return path;
        }
        protected override void OnPaint(PaintEventArgs e)
        {base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Outline())using(var pen=new Pen(MakerUi.Purple,2*scale))e.Graphics.DrawPath(pen,path);}
    }
}
