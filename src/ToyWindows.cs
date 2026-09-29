using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed class ToyWindows : IDisposable
    {
        public readonly ToyModel Model;
        readonly PetModel pet;
        readonly LayeredWindow petWindow,crossingWindow;
        readonly Action save;
        readonly Func<double> now;
        internal readonly LayeredWindow Chest=new LayeredWindow(false){Text="Vpet toy chest",Cursor=Cursors.SizeAll};
        internal readonly LayeredWindow Ball=new LayeredWindow(false){Text="Vpet ball",Cursor=Cursors.Hand};
        internal readonly LayeredWindow Triangle=new LayeredWindow(false){Text="Vpet triangle",Cursor=Cursors.Hand};
        internal readonly LayeredWindow Coin=new LayeredWindow(false){Text="Vpet coin",Cursor=Cursors.Hand};
        internal readonly LayeredWindow Card=new LayeredWindow(false){Text="Vpet high or low card",Cursor=Cursors.Hand};
        internal readonly LayeredWindow Die=new LayeredWindow(false){Text="Vpet D20",Cursor=Cursors.Hand};
        internal readonly GameArtwork Games;
        internal readonly LayeredWindow Fence=new LayeredWindow(false){Text="Vpet play zone"};
        internal readonly LayeredWindow Arrow=new LayeredWindow(true){Text="Ball launch direction"};
        internal readonly LayeredWindow Help=new LayeredWindow(true){Text="Toy help"};
        internal readonly ContextMenuStrip Menu=new ContextMenuStrip();
        internal readonly ContextMenuStrip TriangleMenu=new ContextMenuStrip();
        readonly MenuDismissal dismissal,triangleDismissal;
        readonly Bitmap chestImage,ballImage,triangleImage;
        readonly ToyChime chime;
        Point? chestLocation,ballLocation,triangleLocation;
        Bitmap helpImage;
        string helpText;
        Point? helpLocation;
        string fenceKey="";
        LayeredWindow captured;
        Point pointerStart;
        PointF chestStart,triangleStart,coinStart,cardStart,centerStart,pull;
        RectangleF zoneStart;
        ZoneEdge resizeEdges;
        bool moveZone,dragged,disposed;
        LayerMode? layer;
        ToySoundWindow soundWindow;
        internal bool Busy {get{return captured!=null||Menu.Visible||TriangleMenu.Visible||soundWindow!=null;}}
        internal IEnumerable<LayeredWindow> Windows {get{yield return Help;yield return Ball;yield return Triangle;yield return Coin;yield return Card;yield return Die;yield return Arrow;yield return Fence;yield return Chest;}}

        public ToyWindows(PetModel pet,LayeredWindow petWindow,LayeredWindow crossingWindow,Action save,Func<double> now,Random random)
        {
            this.pet=pet;this.petWindow=petWindow;this.crossingWindow=crossingWindow;this.save=save;this.now=now;
            Model=new ToyModel(pet,random);chestImage=ToyArtwork.Chest(Model.Scale);ballImage=ToyArtwork.Ball(Model.Scale);triangleImage=ToyArtwork.Triangle(Model.Scale);
            Games=new GameArtwork(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","reference"));
            chime=new ToyChime(()=>Model.Settings.Sound,()=>Model.Settings.Volume);Model.ChimePlayed+=chime.Play;
            foreach(var window in Windows)
            {
                IntPtr handle=window.Handle;Native.BackgroundAdornments.Add(handle);
                window.FormClosed+=delegate{Native.BackgroundAdornments.Remove(handle);};
                window.MouseDown+=Down;window.MouseMove+=Move;window.MouseUp+=Up;
                window.MouseCaptureChanged+=delegate(object sender,EventArgs e){if(captured==sender&&!captured.Capture)EndGesture(false);};
            }
            var display=new ToolStripMenuItem("Display Play Zone"){CheckOnClick=true};
            display.Click+=delegate{Model.Settings.DisplayZone=display.Checked;fenceKey="";Update();save();};
            Menu.Items.Add(display);
            var ball=new ToolStripMenuItem("Ball"){CheckOnClick=true};
            ball.Click+=delegate{if(ball.Checked)Model.SpawnBall(now());else{EndGesture(false);Model.RemoveBall(now());}Update();};Menu.Items.Add(ball);
            var triangle=new ToolStripMenuItem("Triangle"){CheckOnClick=true};
            triangle.Click+=delegate{if(triangle.Checked)Model.SpawnTriangle();else{TriangleMenu.Close();if(soundWindow!=null)soundWindow.Close();EndGesture(false);Model.RemoveTriangle(now());}Update();};Menu.Items.Add(triangle);
            var coin=new ToolStripMenuItem("Coin"){CheckOnClick=true};coin.Click+=delegate{EndGesture(false);if(coin.Checked)Model.SpawnCoin();else Model.RemoveCoin(now());Update();};Menu.Items.Add(coin);
            var card=new ToolStripMenuItem("Card"){CheckOnClick=true};card.Click+=delegate{EndGesture(false);if(card.Checked)Model.SpawnCard();else Model.RemoveCard(now());Update();};Menu.Items.Add(card);
            var die=new ToolStripMenuItem("D20"){CheckOnClick=true};die.Click+=delegate{EndGesture(false);if(die.Checked)Model.SpawnDie();else Model.RemoveDie(now());Update();};Menu.Items.Add(die);
            // Keep toy entries above this footer; Help Messages stays directly above Close Toy Chest.
            Menu.Items.Add(new ToolStripSeparator());
            var help=new ToolStripMenuItem("Help Messages"){CheckOnClick=true,Checked=Model.Settings.HelpMessages};
            help.Click+=delegate{Model.Settings.HelpMessages=help.Checked;Update();save();};Menu.Items.Add(help);
            Menu.Items.Add("Close Toy Chest",null,delegate{SetVisible(false);});
            Menu.Opening+=delegate{TriangleMenu.Close();display.Checked=Model.Settings.DisplayZone;ball.Checked=Model.HasBall;triangle.Checked=Model.HasTriangle;coin.Checked=Model.HasCoin;card.Checked=Model.HasCard;die.Checked=Model.HasDie;help.Checked=Model.Settings.HelpMessages;Help.Hide();};
            Chest.ContextMenuStrip=Menu;dismissal=new MenuDismissal(Menu);
            TriangleMenu.Items.Add("Sound Setting",null,delegate
            {
                if(soundWindow==null)
                {
                    soundWindow=new ToySoundWindow(Model.Settings.Volume,v=>chime.Play(Model.Settings.Sound,v),v=>{Model.Settings.Volume=v;save();});
                    soundWindow.FormClosed+=delegate{soundWindow=null;};soundWindow.Show();
                }
                soundWindow.Activate();
            });
            string[] soundNames={"Chime","Honk","Drum"};
            for(int i=0;i<soundNames.Length;i++)
            {
                var sound=(TriangleSound)i;var item=new ToolStripMenuItem(soundNames[i]){Tag=sound};
                item.Click+=delegate{Model.Settings.Sound=sound;SyncSounds();save();};TriangleMenu.Items.Add(item);
            }
            TriangleMenu.Opening+=delegate(object sender,System.ComponentModel.CancelEventArgs e)
            {if(!Model.HasTriangle||!Model.Settings.DisplayChest){e.Cancel=true;return;}Menu.Close();SyncSounds();Help.Hide();};
            Triangle.ContextMenuStrip=TriangleMenu;triangleDismissal=new MenuDismissal(TriangleMenu);SyncSounds();
        }
        void SyncSounds(){foreach(ToolStripMenuItem item in TriangleMenu.Items)if(item.Tag is TriangleSound)item.Checked=(TriangleSound)item.Tag==Model.Settings.Sound;}
        public void SetVisible(bool visible)
        {EndGesture(false);Menu.Close();TriangleMenu.Close();if(!visible&&soundWindow!=null)soundWindow.Close();Model.SetVisible(visible,now());fenceKey="";Update();save();}
        public void RecoverDisplays(){EndGesture(false);TriangleMenu.Close();Model.RecoverDisplays();fenceKey="";}
        ZoneEdge HitEdge(Point p)
        {
            var zone=Model.Zone;float tolerance=8*Model.Scale;ZoneEdge result=ZoneEdge.None;
            if(Math.Abs(p.X-zone.Left)<=tolerance)result|=ZoneEdge.Left;
            if(Math.Abs(p.X-zone.Right)<=tolerance)result|=ZoneEdge.Right;
            if(Math.Abs(p.Y-zone.Top)<=tolerance)result|=ZoneEdge.Top;
            if(Math.Abs(p.Y-zone.Bottom)<=tolerance)result|=ZoneEdge.Bottom;
            return result;
        }
        static Cursor ResizeCursor(ZoneEdge edges)
        {
            if(edges==(ZoneEdge.Left|ZoneEdge.Top)||edges==(ZoneEdge.Right|ZoneEdge.Bottom))return Cursors.SizeNWSE;
            if(edges==(ZoneEdge.Right|ZoneEdge.Top)||edges==(ZoneEdge.Left|ZoneEdge.Bottom))return Cursors.SizeNESW;
            return (edges&(ZoneEdge.Left|ZoneEdge.Right))!=0?Cursors.SizeWE:Cursors.SizeNS;
        }
        void Down(object sender,MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left)return;
            var window=(LayeredWindow)sender;if(window==Arrow||window==Help||(!Model.Settings.DisplayChest&&window!=Fence))return;
            triangleStart=Model.Triangle;coinStart=Model.Coin;cardStart=Model.Card;
            pointerStart=Cursor.Position;chestStart=Model.Chest;centerStart=Model.Center;zoneStart=Model.Zone;
            if(window==Fence)
            {
                resizeEdges=HitEdge(pointerStart);moveZone=resizeEdges==ZoneEdge.None&&Geometry.Distance(pointerStart,centerStart)<=22*Model.Scale;
                if(!moveZone&&resizeEdges==ZoneEdge.None)return;
            }
            captured=window;dragged=false;pull=PointF.Empty;
            if(window==Ball)Model.BeginAim();else if(window==Die)Model.BeginDieAim();else Model.Editing=true;
            window.Capture=true;
            if(pet.Settings.Layer==LayerMode.Dynamic)Native.SetWindowPos(petWindow.Handle,IntPtr.Zero,0,0,0,0,0x13);
        }
        void Move(object sender,MouseEventArgs e)
        {
            if(captured==null)
            {
                if(sender==Fence){var edges=HitEdge(Cursor.Position);Fence.Cursor=edges==ZoneEdge.None?Cursors.SizeAll:ResizeCursor(edges);}
                return;
            }
            UpdateGesture();Update();
        }
        void UpdateGesture()
        {
            if(captured==null)return;
            var point=Cursor.Position;pull=new PointF(point.X-pointerStart.X,point.Y-pointerStart.Y);
            if(Geometry.Distance(pull,PointF.Empty)>=4)dragged=true;
            if(!dragged)return;
            if(captured==Chest)Model.DragChest(new PointF(chestStart.X+pull.X,chestStart.Y+pull.Y));
            else if(captured==Triangle)Model.DragTriangle(new PointF(triangleStart.X+pull.X,triangleStart.Y+pull.Y));
            else if(captured==Coin)Model.DragGame(PlayTarget.Coin,new PointF(coinStart.X+pull.X,coinStart.Y+pull.Y));
            else if(captured==Card)Model.DragGame(PlayTarget.Card,new PointF(cardStart.X+pull.X,cardStart.Y+pull.Y));
            else if(captured==Fence)
            {
                if(moveZone)Model.MoveZone(new PointF(centerStart.X+pull.X,centerStart.Y+pull.Y));
                else Model.ResizeZone(zoneStart,resizeEdges,pull);
                fenceKey="";
            }
        }
        void Up(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){UpdateGesture();EndGesture(true);}}
        void EndGesture(bool released)
        {
            if(captured==null)return;var window=captured;captured=null;window.Capture=false;Model.Editing=false;
            if(window==Ball)
            {
                if(released&&dragged&&Geometry.Distance(pull,PointF.Empty)>=4)Model.LaunchPull(pull,now());
                else if(released)Model.Bounce();else Model.CancelAim();
            }
            else if(window==Triangle&&released&&!dragged)Model.PressTriangle(now());
            else if(window==Coin&&released&&!dragged)Model.PressCoin(now());
            else if(window==Card&&released&&!dragged)
            {
                if(Model.WaitingForCardChoice)Model.ChooseCard(pointerStart.Y<Model.Card.Y);
                else if(Model.Target!=PlayTarget.Card||Model.Fetch==FetchPhase.None||Model.Fetch==FetchPhase.Returning||Model.CardRevealed)Model.PressCard(now());
            }
            else if(window==Die)
            {if(released&&dragged)Model.LaunchDiePull(pull,now());else if(released)Model.RollDie(now());else Model.CancelDieAim();}
            Arrow.Hide();pull=PointF.Empty;save();
        }
        public void Update()
        {
            if(disposed)return;
            if(!Model.Settings.DisplayChest){foreach(var window in Windows)if(window!=Fence)window.Hide();UpdateFence();KeepBelowPet();return;}
            KeepBelowPet();
            var chestPoint=new Point((int)Math.Round(Model.Chest.X-chestImage.Width/2f),(int)Math.Round(Model.Chest.Y-chestImage.Height/2f));
            if(chestLocation!=chestPoint||!Chest.Visible){Present(Chest,chestImage,chestPoint);chestLocation=chestPoint;}
            if(Model.HasBall)
            {
                // Bounce toward the table's interior near the top, so even a ball against that wall visibly bounces.
                float direction=Model.Ball.Y-Model.BallBounds.Top>=18*Model.Scale?-1:1;
                float y=Math.Max(Model.BallBounds.Top,Math.Min(Model.BallBounds.Bottom,Model.Ball.Y+direction*Model.BounceHeight));
                var ballPoint=new Point((int)Math.Round(Model.Ball.X-ballImage.Width/2f),(int)Math.Round(y-ballImage.Height/2f));
                if(ballLocation!=ballPoint||!Ball.Visible){Present(Ball,ballImage,ballPoint);ballLocation=ballPoint;}
            }
            else Ball.Hide();
            if(Model.HasTriangle)
            {
                var point=new Point((int)Math.Round(Model.Triangle.X-triangleImage.Width/2f),(int)Math.Round(Model.Triangle.Y-triangleImage.Height/2f));
                if(triangleLocation!=point||!Triangle.Visible){Present(Triangle,triangleImage,point);triangleLocation=point;}
            }
            else Triangle.Hide();
            DrawGames();UpdateFence();
            if((captured==Ball||captured==Die)&&dragged&&Geometry.Distance(pull,PointF.Empty)>=4)DrawArrow();else Arrow.Hide();
            UpdateHelp();
            KeepBelowPet();
        }
        void UpdateFence()
        {
            if(Model.Settings.DisplayZone&&(Model.Settings.DisplayChest||(pet.Settings.SyncPlayZone&&pet.Settings.Movement==MovementMode.Restricted)))
            {
                string key=Model.Zone.ToString();
                if(key!=fenceKey||!Fence.Visible)
                {
                    var bounds=Rectangle.Ceiling(Model.Zone);
                    using(var image=ToyArtwork.Fence(bounds.Size,Model.Scale))Present(Fence,image,bounds.Location);
                    fenceKey=key;
                }
            }
            else Fence.Hide();
        }
        void DrawGames()
        {
            if(Model.HasCoin)
            {
                float bounce=4*Model.CoinFlip*(1-Model.CoinFlip)*55*Model.Scale;
                float direction=Model.Coin.Y-Model.CoinBounds.Top>=55*Model.Scale?-1:1;
                var point=Geometry.Clamp(new PointF(Model.Coin.X,Model.Coin.Y+direction*bounce),Model.CoinBounds);
                using(var image=Games.Coin(Model.Scale,Model.CoinHeads,Model.CoinFlip))Present(Coin,image,new Point((int)(point.X-image.Width/2f),(int)(point.Y-image.Height/2f)));
            }else Coin.Hide();
            if(Model.HasCard)using(var image=GameArtwork.Card(Model.Scale,Model.DrawnCard,Model.CardRevealed||Model.CardFlip>=.5f,Model.CardFlip))Present(Card,image,new Point((int)(Model.Card.X-image.Width/2f),(int)(Model.Card.Y-image.Height/2f)));else Card.Hide();
            if(Model.HasDie)using(var image=GameArtwork.Die(Model.Scale,Model.DieAngle,Model.DieValue,Model.DieRolling))Present(Die,image,new Point((int)(Model.Die.X-image.Width/2f),(int)(Model.Die.Y-image.Height/2f)));else Die.Hide();
        }
        internal string HelpAt(Point point,IntPtr hitWindow)
        {
            if(!Model.Settings.HelpMessages||!Model.Settings.DisplayChest||Menu.Visible||TriangleMenu.Visible)return null;
            if(Model.HasCoin&&hitWindow==Coin.Handle)return "Click the coin. Your pet walks over, flips it, then announces Heads or Tails. Drag to move the coin.";
            if(Model.HasCard&&hitWindow==Card.Handle)return Model.WaitingForCardChoice?"Pick High (top arrow) or Low (bottom arrow). Equal ranks are a draw. Each round starts with a fresh deck.":"Click to start High or Low. After your pet announces a card, choose the top arrow for High or bottom arrow for Low. Drag to move the card.";
            if(Model.HasDie&&hitWindow==Die.Handle)return "Click to roll, or pull back and release along the arrow. Your pet watches the D20 and announces its final value.";
            if(Model.HasTriangle&&Triangle.Visible&&(hitWindow==Triangle.Handle||hitWindow==Help.Handle))
            {
                Point local=Triangle.PointToClient(point);
                if(local.X>=0&&local.Y>=0&&local.X<triangleImage.Width&&local.Y<triangleImage.Height&&triangleImage.GetPixel(local.X,local.Y).A>0)
                    return "Tap to play; your pet repeats your taps after a brief pause. Right-click for Chime, Honk, or Drum. Drag to move the triangle.";
            }
            if(Model.HasBall&&Ball.Visible&&(hitWindow==Ball.Handle||hitWindow==Help.Handle))
            {
                Point local=Ball.PointToClient(point);
                if(local.X>=0&&local.Y>=0&&local.X<ballImage.Width&&local.Y<ballImage.Height&&ballImage.GetPixel(local.X,local.Y).A>0)
                    return "Click for three bounces. Pull back, then release along the red arrow to launch the ball.";
            }
            if(Model.Settings.DisplayZone&&Fence.Visible&&(hitWindow==Fence.Handle||hitWindow==Help.Handle)&&
                (Geometry.Distance(point,Model.Center)<=22*Model.Scale||
                (RectangleF.Inflate(Model.Zone,2,2).Contains(point)&&HitEdge(point)!=ZoneEdge.None)))
                return "Drag the + to move the play zone. Drag an edge or corner to resize it. The fence stays active when hidden.";
            return null;
        }
        void UpdateHelp()
        {
            Point pointer=Cursor.Position;string message=HelpAt(pointer,Native.WindowFromPoint(new Native.POINT(pointer.X,pointer.Y)));
            if(message==null){Help.Hide();return;}
            var display=pet.Displays.Find(d=>d.Id==Model.DisplayId);
            if(message!=helpText||helpImage==null||helpImage.Width>display.Work.Width)
            {if(helpImage!=null)helpImage.Dispose();helpImage=ToyArtwork.HelpMessage(message,Model.Scale,display.Work.Width);helpText=message;helpLocation=null;}
            var position=new Point(Math.Max(display.Work.Left,Math.Min(display.Work.Right-helpImage.Width,(int)Model.Chest.X-helpImage.Width/2)),
                Math.Max(display.Work.Top,(int)(Model.Chest.Y-Model.ChestSize.Height/2)-helpImage.Height-(int)(6*Model.Scale)));
            if(helpLocation!=position||!Help.Visible){Present(Help,helpImage,position);helpLocation=position;}
        }
        void DrawArrow()
        {
            var velocity=Model.PullVelocity(pull);float speed=Geometry.Distance(velocity,PointF.Empty);
            if(speed<.001f){Arrow.Hide();return;}
            float length=Math.Min(150*Model.Scale,speed/5)+Model.Radius;
            PointF start=captured==Die?Model.Die:Model.Ball,end=new PointF(start.X+velocity.X/speed*length,start.Y+velocity.Y/speed*length);
            int pad=(int)Math.Ceiling(14*Model.Scale);
            var bounds=Rectangle.FromLTRB((int)Math.Floor(Math.Min(start.X,end.X))-pad,(int)Math.Floor(Math.Min(start.Y,end.Y))-pad,
                (int)Math.Ceiling(Math.Max(start.X,end.X))+pad,(int)Math.Ceiling(Math.Max(start.Y,end.Y))+pad);
            // Clip aim artwork to a connected work area as well.
            var display=pet.Displays.Find(d=>d.Id==Model.DisplayId);bounds=Rectangle.Intersect(bounds,display.Work);
            if(bounds.Width<=0||bounds.Height<=0)return;
            using(var image=ToyArtwork.LaunchArrow(bounds,start,end,Model.Scale))Present(Arrow,image,bounds.Location);
        }
        static void Present(LayeredWindow window,Bitmap image,Point position)
        {window.Present(image,position);if(!window.Visible)window.Show();}
        void KeepBelowPet()
        {
            IntPtr lowest=petWindow.Handle;
            Native.EnumWindows(delegate(IntPtr window,IntPtr unused)
            {if(window==petWindow.Handle||(crossingWindow!=null&&crossingWindow.Visible&&window==crossingWindow.Handle))lowest=window;return true;},IntPtr.Zero);
            bool changed=layer!=pet.Settings.Layer;layer=pet.Settings.Layer;
            foreach(var window in Windows)
            {
                bool topmost=(Native.GetWindowLongPtr(window.Handle,-20).ToInt64()&8)!=0;
                if(changed||topmost!=(pet.Settings.Layer==LayerMode.OverEverything))
                {window.BehindWindow=IntPtr.Zero;window.SetLayer(pet.Settings.Layer);}
                window.BehindWindow=lowest;
                if(changed||window.Visible)Native.SetWindowPos(window.Handle,lowest,0,0,0,0,0x213);
                lowest=window.Handle;
            }
        }
        public void Dispose()
        {
            if(disposed)return;EndGesture(false);disposed=true;dismissal.Dispose();triangleDismissal.Dispose();Menu.Dispose();TriangleMenu.Dispose();
            if(soundWindow!=null)soundWindow.Close();
            Model.ChimePlayed-=chime.Play;chime.Dispose();
            Games.Dispose();
            foreach(var window in Windows)window.Close();chestImage.Dispose();ballImage.Dispose();triangleImage.Dispose();if(helpImage!=null)helpImage.Dispose();
        }
    }

    // Draw at the desktop scale without introducing extra bitmap assets or opaque window backgrounds.
    internal static class ToyArtwork
    {
        public static Bitmap Triangle(float scale)
        {
            var image=new Bitmap((int)Math.Ceiling(44*scale),(int)Math.Ceiling(44*scale),PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))using(var outline=new Pen(Color.FromArgb(62,58,82),6))using(var metal=new Pen(Color.FromArgb(213,223,238),3))using(var stick=new Pen(Color.FromArgb(119,79,37),3))
            {
                g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;
                // Layered windows pass alpha-zero pixels through before hit testing.
                // One alpha step makes the hollow interior clickable without a visible fill.
                using(var inputFill=new SolidBrush(Color.FromArgb(1,213,223,238)))
                    g.FillPolygon(inputFill,new[]{new PointF(6,36),new PointF(22,8),new PointF(38,36)});
                var points=new[]{new PointF(30,36),new PointF(6,36),new PointF(22,8),new PointF(38,36)};
                outline.LineJoin=metal.LineJoin=LineJoin.Round;g.DrawLines(outline,points);g.DrawLines(metal,points);
                g.DrawLine(Pens.SlateGray,22,1,22,8);g.DrawLine(stick,24,31,40,20);
            }
            return image;
        }
        public static Bitmap LaunchArrow(Rectangle bounds,PointF start,PointF end,float scale)
        {
            var image=new Bitmap(bounds.Width,bounds.Height,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))using(var cap=new AdjustableArrowCap(4,5,true))using(var ink=new Pen(Color.Red,4*scale))
            {g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(-bounds.Left,-bounds.Top);ink.CustomEndCap=cap;g.DrawLine(ink,start,end);}
            return image;
        }
        public static Bitmap HelpMessage(string text,float scale,int maximumWidth)
        {
            int width=Math.Min(maximumWidth,(int)Math.Ceiling(310*scale)),padding=(int)Math.Ceiling(10*scale);
            using(var font=new Font("Segoe UI",12*scale,FontStyle.Regular,GraphicsUnit.Pixel))
            {
                Size size=TextRenderer.MeasureText(text,font,new Size(Math.Max(1,width-padding*2),0),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);
                var image=new Bitmap(width,size.Height+padding*2,PixelFormat.Format32bppArgb);
                using(var g=Graphics.FromImage(image))using(var pen=new Pen(Color.FromArgb(32,115,201),2*scale))
                {
                    g.Clear(Color.White);g.DrawRectangle(pen,scale,scale,image.Width-2*scale-1,image.Height-2*scale-1);
                    TextRenderer.DrawText(g,text,font,new Rectangle(padding,padding,Math.Max(1,width-padding*2),size.Height),Color.FromArgb(45,35,60),Color.White,TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);
                }
                return image;
            }
        }
        public static Bitmap Chest(float scale)
        {
            var image=new Bitmap((int)Math.Ceiling(64*scale),(int)Math.Ceiling(50*scale),PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))
            using(var wood=new SolidBrush(Color.FromArgb(164,86,42)))
            using(var lid=new SolidBrush(Color.FromArgb(208,128,63)))
            using(var gold=new SolidBrush(Color.FromArgb(255,210,94)))
            using(var edge=new Pen(Color.FromArgb(75,45,38),2))
            {
                g.ScaleTransform(scale,scale);
                g.FillRectangle(wood,5,18,54,27);g.FillRectangle(lid,5,9,54,16);g.FillRectangle(lid,10,5,44,4);
                g.DrawRectangle(edge,5,9,54,36);g.DrawLine(edge,10,5,54,5);g.DrawLine(edge,10,5,5,9);g.DrawLine(edge,54,5,59,9);
                g.FillRectangle(gold,12,9,5,35);g.FillRectangle(gold,47,9,5,35);g.DrawLine(edge,5,25,59,25);
                g.DrawLine(edge,19,36,44,36);g.FillRectangle(gold,27,21,10,12);g.DrawRectangle(edge,27,21,10,12);
                g.FillRectangle(Brushes.SaddleBrown,31,24,3,5);
            }
            return image;
        }
        public static Bitmap Ball(float scale)
        {
            int size=(int)Math.Ceiling(26*scale);var image=new Bitmap(size,size,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))using(var fill=new LinearGradientBrush(new Rectangle(0,0,size,size),Color.FromArgb(255,94,88),Color.FromArgb(190,24,37),55))
            using(var edge=new Pen(Color.FromArgb(125,24,39),1.5f*scale))
            {g.SmoothingMode=SmoothingMode.AntiAlias;g.FillEllipse(fill,scale,scale,size-2*scale,size-2*scale);g.DrawEllipse(edge,scale,scale,size-2*scale,size-2*scale);g.FillEllipse(Brushes.MistyRose,6*scale,4*scale,6*scale,4*scale);}
            return image;
        }
        public static Bitmap Fence(Size size,float scale)
        {
            var image=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(image))using(var hit=new Pen(Color.FromArgb(1,255,255,255),12*scale))
            using(var white=new Pen(Color.FromArgb(230,255,255,255),5*scale))using(var line=new Pen(Color.FromArgb(32,115,201),2*scale))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;float inset=3*scale;
                var rect=new RectangleF(inset,inset,size.Width-2*inset-1,size.Height-2*inset-1);
                g.DrawRectangle(hit,rect.X,rect.Y,rect.Width,rect.Height);
                g.DrawRectangle(white,rect.X,rect.Y,rect.Width,rect.Height);line.DashStyle=DashStyle.Dash;
                g.DrawRectangle(line,rect.X,rect.Y,rect.Width,rect.Height);line.DashStyle=DashStyle.Solid;
                foreach(var p in new[]{new PointF(rect.Left,rect.Top),new PointF(rect.Right,rect.Top),new PointF(rect.Left,rect.Bottom),new PointF(rect.Right,rect.Bottom)})
                {g.FillRectangle(Brushes.White,p.X-3*scale,p.Y-3*scale,6*scale,6*scale);g.DrawRectangle(line,p.X-3*scale,p.Y-3*scale,6*scale,6*scale);}
                // A visible ring remains draggable even when the smaller ball rests over the center.
                float x=size.Width/2f,y=size.Height/2f,r=20*scale;
                g.FillEllipse(Brushes.White,x-r,y-r,r*2,r*2);g.DrawEllipse(line,x-r,y-r,r*2,r*2);
                g.DrawLine(line,x-8*scale,y,x+8*scale,y);g.DrawLine(line,x,y-8*scale,x,y+8*scale);
            }
            return image;
        }
    }
}
