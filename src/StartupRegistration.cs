using System;
using System.IO;
using Microsoft.Win32;

namespace Vpet
{
    internal static class StartupRegistration
    {
        const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
        public static string Command(string executable)
        {
            if(string.IsNullOrWhiteSpace(executable)||executable.IndexOf('"')>=0)throw new InvalidDataException("Invalid startup application path.");
            string command="\""+Path.GetFullPath(executable)+"\" --startup";
            if(command.Length>260)throw new InvalidDataException("Move Vpet to a shorter folder path before enabling startup.");
            return command;
        }
        internal static void Apply(RegistryKey key,bool enabled,string executable)
        {
            if(enabled)key.SetValue("Vpet",Command(executable),RegistryValueKind.String);
            else key.DeleteValue("Vpet",false);
        }
        public static void SetEnabled(bool enabled,string executable)
        {
            if(enabled)
            {using(var key=Registry.CurrentUser.CreateSubKey(RunKey))Apply(key,true,executable);}
            else
            {using(var key=Registry.CurrentUser.OpenSubKey(RunKey,true))if(key!=null)Apply(key,false,executable);}
        }
    }
}
