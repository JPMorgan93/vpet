using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Vpet
{
    internal static partial class Tests
    {
        static Rectangle ArcadeControlBounds(Control control,Control relative)
        {return new Rectangle(relative.PointToClient(control.PointToScreen(Point.Empty)),control.Size);}
        static bool ArcadeNeutral(Bitmap image)
        {for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).ToArgb()!=ArcadeWindow.Neutral.ToArgb())return false;return true;}
        static void ArcadeLayoutWindows(ArcadeWindow arcade,ref double now)
        {
            var canvas=MakerField<DoubleBufferedPanel>(arcade,"canvas");var picker=MakerField<ComboBox>(arcade,"background");
            var practice=MakerField<CheckBox>(arcade,"practice");var modes=MakerField<RadioButton[]>(arcade,"modes");
            var song=MakerField<Label>(arcade,"songTime");var score=MakerField<Button>(arcade,"score");var top=MakerField<Panel>(arcade,"top");
            var volume=MakerField<TrackBar>(arcade,"volume");var panel=MakerField<Panel>(arcade,"volumePanel");
            var title=MakerField<Label>(arcade,"volumeLabel");var value=MakerField<Label>(arcade,"volumeValue");
            foreach(var game in new[]{ArcadeGame.Dance,ArcadeGame.Simon})
            {
                arcade.OpenGame(game);if(game==ArcadeGame.Dance)WaitForDance(arcade);
                foreach(var size in new[]{new Size(1040,790),new Size(700,590),new Size(1150,650),new Size(700,950)})
                {
                    arcade.ClientSize=size;Application.DoEvents();arcade.Step();var scene=arcade.SceneBounds;
                    Check(Math.Abs(scene.Width-scene.Height)<.001&&scene.Width>350&&scene.Left>=0&&scene.Top>=-.001&&scene.Right<=canvas.Width+.001&&scene.Bottom<=canvas.Height+.001,"Square "+game+" scene fits the resized canvas: "+size);
                    Check(!song.Visible== (game==ArcadeGame.Simon)&&!panel.Visible==(game==ArcadeGame.Simon),"Song clock and volume are shown only in Dance: "+size);
                    Check(picker.Parent.ClientRectangle.Contains(picker.Bounds)&&modes.All(m=>m.Parent.ClientRectangle.Contains(m.Bounds)),"Difficulty and background remain fully inside the header: "+game+size);
                    if(game==ArcadeGame.Dance)
                    {
                        var timeBox=ArcadeControlBounds(song,top);var scoreBox=ArcadeControlBounds(score,top);var practiceBox=ArcadeControlBounds(practice,top);var pickerBox=ArcadeControlBounds(picker,top);
                        Check(practice.Parent==modes[2].Parent&&practice.Left>modes[2].Right&&Math.Abs(practice.Top-modes[2].Top)<=1&&practice.Parent.ClientRectangle.Contains(practice.Bounds),"Practice stays beside the difficulty choices: "+size);
                        Check(song.Parent.ClientRectangle.Contains(song.Bounds)&&timeBox.Right<=scoreBox.Left&&!timeBox.IntersectsWith(practiceBox)&&!timeBox.IntersectsWith(pickerBox),"Header song time fits to the left of Score Card without covering controls: "+size+" clock="+timeBox+" score="+scoreBox+" practice="+practiceBox+" background="+pickerBox+" parent="+song.Parent.ClientRectangle);
                        Check(volume.Parent==panel&&title.Parent==panel&&value.Parent==panel&&panel.BackColor==ArcadeWindow.Neutral&&volume.BackColor==panel.BackColor&&title.BackColor==Color.Transparent&&value.BackColor==Color.Transparent,"Volume title, slider and percentage share one neutral panel: "+size);
                        Check(canvas.ClientRectangle.Contains(panel.Bounds)&&panel.Controls.Cast<Control>().All(c=>panel.ClientRectangle.Contains(c.Bounds))&&title.Top>=8&&value.Bottom<=panel.Height-8,"Entire volume column and its padding fit: "+size);
                    }
                    picker.SelectedIndex=game==ArcadeGame.Dance?2:1;
                    foreach(var region in new[]{new Rectangle(230,750,30,30),new Rectangle(470,32,60,24),new Rectangle(465,950,70,26)})
                        using(var blank=ArcadeRegion(arcade,region))Check(ArcadeNeutral(blank),"Off is the neutral canvas color, with no game title or bottom timer: "+game+size+region);
                    picker.SelectedIndex=0;
                    if(size.Width==700&&size.Height==590)CaptureForm(arcade,game==ArcadeGame.Dance?"dance-square-minimum":"simon-square-minimum");
                }
                arcade.ClientSize=new Size(1040,790);Application.DoEvents();arcade.Step();
                if(game==ArcadeGame.Dance)
                {
                    using(var padding=ArcadeRegion(arcade,new Rectangle(74,31,12,9)))Check(ArcadeNeutral(padding),"Misses have neutral padding over the visible floor");
                    foreach(var point in new[]{new Point(500,364),new Point(500,636),new Point(364,500),new Point(636,500)})
                        using(var padding=ArcadeRegion(arcade,new Rectangle(point.X-34,point.Y-34,3,3)))Check(ArcadeNeutral(padding),"Every fixed directional square has neutral padding over the floor: "+point);
                    using(var column=new Bitmap(panel.Width,panel.Height))
                    {panel.DrawToBitmap(column,panel.ClientRectangle);Check(Enumerable.Range(0,column.Height).All(y=>column.GetPixel(2,y).ToArgb()==ArcadeWindow.Neutral.ToArgb()),"Volume column keeps one continuous background from top to bottom");}
                    var clock=arcade.SongPosition;arcade.SongPosition=()=>3;arcade.Dance.Chart.Clear();arcade.Dance.Chart.Add(new DanceTarget(ArcadeLane.Up,4));
                    arcade.StartGame();now+=3.01;arcade.Step();arcade.Step();
                    Check(song.Text.StartsWith("Song\n0:03 / ")&&song.Visible,"Header song time advances with music playback");
                    using(var target=ArcadeRegion(arcade,new Rectangle(481,186,8,8)))
                    {var color=target.GetPixel(target.Width/2,target.Height/2);Check(color.R>240&&color.G>100&&color.B>200,"Moving targets render as bright pink against the shaded floor");}
                    volume.Value=25;MakerField<ArcadeMusic>(arcade,"music").SetVolume(0);CaptureForm(arcade,"dance-square-bright");arcade.StopGame();volume.Value=0;arcade.SongPosition=clock;
                    practice.Checked=true;arcade.Step();Check(song.Visible&&song.Parent.ClientRectangle.Contains(song.Bounds),"Practice retains the song timer even though scores are hidden");practice.Checked=false;
                }
                else CaptureForm(arcade,"simon-square-default");
            }
            arcade.OpenGame(ArcadeGame.Dance);WaitForDance(arcade);
        }
    }
}
