using System;
using System.Drawing;
using System.Windows.Forms;

namespace Vpet
{
    internal sealed partial class SettingsWindow
    {
        void AddFindMyVpet(TableLayoutPanel page)
        {
            var prefs=pet.Model.Settings.FindPet;
            Add(page,Copy("Find My Vpet",true));
            var enabled=new CheckBox{Name="FindPetEnabled",Text="Enable Find My Vpet",AutoSize=true,Checked=prefs.Enabled};Add(page,enabled);
            var modifier=Choice(new[]{"ALT","CTRL"},(int)prefs.Modifier,100);modifier.Name="FindPetModifier";
            var key=new TextBox{Name="FindPetKey",Text=FindPetKeys.Name(prefs.Key),ReadOnly=true,ShortcutsEnabled=false,Width=165,BackColor=Color.White,Cursor=Cursors.Hand,Margin=new Padding(0,4,0,4),AccessibleName="Shortcut key; click to map a key"};
            Add(page,Row(modifier,Copy("+"),key));
            var message=Copy(prefs.Shortcut+". Click the key box to change it.");message.Name="FindPetStatus";Add(page,message);
            Add(page,Copy("When enabled, your shortcut circles the pet and dims the surrounding screens, fading away over one second. The circle follows a moving pet. ALT + ALT means double-tap ALT; CTRL + CTRL means double-tap CTRL."));
            bool updating=false;
            Action apply=delegate
            {
                string error=pet.FindPet.ApplySettings();
                if(error!=null){updating=true;prefs.Enabled=false;enabled.Checked=false;updating=false;message.Text="Could not enable the shortcut: "+error;}
                pet.Save();
            };
            Action cancel=delegate
            {
                if(!pet.FindPet.Capturing)return;pet.FindPet.CancelCapture();message.Text="No key mapped. Click the key box and press a key to try again.";apply();
            };
            enabled.CheckedChanged+=delegate{if(updating)return;cancel();prefs.Enabled=enabled.Checked;apply();};
            modifier.SelectedIndexChanged+=delegate{cancel();prefs.Modifier=(FindPetModifier)modifier.SelectedIndex;message.Text=prefs.Shortcut+". Click the key box to change it.";apply();};
            Action capture=delegate
            {
                key.Text="";message.Text="Press any key to map it.";
                string error=pet.FindPet.BeginCapture(this,delegate(int mapped)
                {
                    if(IsDisposed)return;key.Text=FindPetKeys.Name(mapped);
                    message.Text=mapped==0?"That key could not be mapped. Click the box to try again.":"Key mapped successfully: "+prefs.Shortcut+".";
                    apply();
                });
                pet.Save();if(error!=null)message.Text="The key could not be mapped: "+error;
            };
            key.Click+=delegate{capture();};
            key.KeyDown+=delegate(object sender,KeyEventArgs e){if(!pet.FindPet.Capturing&&(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space)){e.SuppressKeyPress=true;capture();}};
            key.Leave+=delegate{cancel();};Deactivate+=delegate{cancel();};FormClosed+=delegate{pet.FindPet.CancelCapture();};
        }
    }
}
