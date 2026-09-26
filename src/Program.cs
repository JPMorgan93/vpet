using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Vpet
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if(args.Length>0&&args[0]=="--apply-update")
            {
                try{Updates.ApplyUpdate(args);}catch(Exception ex){MessageBox.Show(ex.Message,"Vpet update could not start",MessageBoxButtons.OK,MessageBoxIcon.Error);}return;
            }
            Native.EnableDpi();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            bool smoke=Array.IndexOf(args,"--smoke-test")>=0;
            string root=AppDomain.CurrentDomain.BaseDirectory;
            string data=smoke?Path.Combine(root,"smoke-data"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"VpetPrototype");
            string output=Path.Combine(root,"smoke-output");
            bool first;
            using(var mutex=new Mutex(true,smoke?"Local\\VpetPrototypeSmoke":"Local\\VpetPrototype",out first))
            {
                if(!first){MessageBox.Show("Vpet is already running. Use its tray icon to open settings.","Vpet");return;}
                Action<Exception> report=delegate(Exception ex)
                {
                    try{Directory.CreateDirectory(data);File.WriteAllText(Path.Combine(data,"error.log"),ex.ToString());}catch{}
                    if(smoke){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"smoke-error.txt"),ex.ToString());Environment.Exit(1);}
                    MessageBox.Show("Vpet could not continue.\n\n"+ex.Message+"\n\nDetails: "+Path.Combine(data,"error.log"),"Vpet",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    Application.Exit();
                };
                Application.ThreadException+=delegate(object sender,ThreadExceptionEventArgs e){report(e.Exception);};
                try{Application.Run(new PetWindow(data,Path.Combine(root,"assets","reference","Base Vpet Sprite Sheet.png"),smoke,output));}
                catch(Exception ex){report(ex);}
            }
        }
    }
}
