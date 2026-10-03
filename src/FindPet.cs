using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace Vpet
{
    public enum FindPetModifier { Alt, Ctrl }
    [DataContract]
    public sealed class FindPetPreferences
    {
        [DataMember] public bool Enabled;
        [DataMember] public FindPetModifier Modifier=FindPetModifier.Alt;
        [DataMember] public int Key=(int)Keys.Menu;
        [OnDeserializing] void Defaults(StreamingContext context){Modifier=FindPetModifier.Alt;Key=(int)Keys.Menu;}
        public void Validate()
        {
            if(!Enum.IsDefined(typeof(FindPetModifier),Modifier))Modifier=FindPetModifier.Alt;
            Key=FindPetKeys.Normalize(Key);if(Key!=0&&!FindPetKeys.Valid(Key))Key=(int)Keys.Menu;
        }
        public int ModifierKey {get{return Modifier==FindPetModifier.Ctrl?(int)Keys.ControlKey:(int)Keys.Menu;}}
        public string Shortcut {get{return Key==0?"No key mapped":Key==ModifierKey?"Double-tap "+FindPetKeys.Name(Key):FindPetKeys.Name(ModifierKey)+" + "+FindPetKeys.Name(Key);}}
    }

    // Track only held key codes and the last modifier tap; no text or keystroke history is recorded.
    internal sealed class FindPetKeys
    {
        readonly HashSet<int> held=new HashSet<int>();
        double lastTap=double.NegativeInfinity;
        bool cleanTap,latched;
        public static int Normalize(int key)
        {
            if(key==(int)Keys.LMenu||key==(int)Keys.RMenu)return (int)Keys.Menu;
            if(key==(int)Keys.LControlKey||key==(int)Keys.RControlKey)return (int)Keys.ControlKey;
            if(key==(int)Keys.LShiftKey||key==(int)Keys.RShiftKey)return (int)Keys.ShiftKey;
            return key;
        }
        public static bool Valid(int key){return key>=8&&key<=254&&Enum.IsDefined(typeof(Keys),(Keys)key);}
        public static string Name(int key)
        {
            key=Normalize(key);if(key==0)return "";if(key==(int)Keys.Menu)return "ALT";if(key==(int)Keys.ControlKey)return "CTRL";
            if(key==(int)Keys.ShiftKey)return "SHIFT";if(key>=48&&key<=57)return ((char)key).ToString();
            return new KeysConverter().ConvertToString((Keys)key);
        }
        public void Reset(){held.Clear();lastTap=double.NegativeInfinity;cleanTap=latched=false;}
        public bool Input(int raw,bool down,double now,FindPetPreferences settings)
        {
            int key=Normalize(raw),primary=settings.ModifierKey,second=settings.Key;
            if(!down)
            {
                held.Remove(raw);
                if(key==primary&&cleanTap&&held.Count==0){lastTap=now;cleanTap=false;}
                if(key==primary||key==second)latched=false;
                return false;
            }
            if(!held.Add(raw))return false; // Ignore keyboard auto-repeat.
            if(!settings.Enabled||!Valid(second)){lastTap=double.NegativeInfinity;return false;}
            if(primary==second)
            {
                if(key!=primary||held.Count!=1){cleanTap=false;lastTap=double.NegativeInfinity;return false;}
                bool trigger=now-lastTap>=0&&now-lastTap<=.5;
                cleanTap=!trigger;lastTap=double.NegativeInfinity;return trigger;
            }
            bool modifier=false,mapped=false;
            foreach(int value in held)
            {
                int normalized=Normalize(value);
                if(normalized==primary)modifier=true;else if(normalized==second)mapped=true;else return false;
            }
            if(!latched&&modifier&&mapped){latched=true;return true;}return false;
        }
    }

    internal sealed class FindPetController : IDisposable
    {
        delegate IntPtr KeyboardHook(int code,IntPtr message,IntPtr data);
        [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SetWindowsHookEx(int id,KeyboardHook callback,IntPtr module,uint thread);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);
        readonly Form window;
        readonly Func<double> now;
        readonly FindPetPreferences settings;
        readonly KeyboardHook callback;
        readonly FindPetKeys keys=new FindPetKeys();
        readonly HashSet<int> suppressed=new HashSet<int>();
        internal readonly FindPetSpotlight Spotlight=new FindPetSpotlight();
        IntPtr hook,captureOwner;
        Action<int> mapped;
        int generation;
        bool mappingQueued,disposed;
        internal bool Capturing {get{return mapped!=null||mappingQueued;}}
        internal bool Hooked {get{return hook!=IntPtr.Zero;}}
        internal Action<int,bool,string> InputObserved;
        public FindPetController(Form window,FindPetPreferences settings,Func<double> now)
        {this.window=window;this.settings=settings;this.now=now;callback=Observe;}
        string EnsureHook()
        {
            if(hook==IntPtr.Zero)hook=SetWindowsHookEx(13,callback,GetModuleHandle(null),0);
            return hook==IntPtr.Zero?new Win32Exception(Marshal.GetLastWin32Error()).Message:null;
        }
        public string ApplySettings()
        {
            generation++;keys.Reset();Spotlight.Hide();
            if(Capturing||(settings.Enabled&&FindPetKeys.Valid(settings.Key)))return EnsureHook();
            StopIfIdle();return null;
        }
        public string BeginCapture(Form owner,Action<int> completed)
        {
            CancelCapture();settings.Key=0;Spotlight.Hide();captureOwner=owner.Handle;mapped=completed;
            string error=EnsureHook();if(error!=null){mapped=null;captureOwner=IntPtr.Zero;}return error;
        }
        public void CancelCapture()
        {generation++;mapped=null;mappingQueued=false;captureOwner=IntPtr.Zero;keys.Reset();StopIfIdle();}
        void Queue(Action action){if(!disposed&&!window.IsDisposed)window.BeginInvoke(action);}
        IntPtr Observe(int code,IntPtr message,IntPtr data)
        {
            if(code>=0&&!disposed)
            {
                long kind=message.ToInt64();bool down=kind==0x100||kind==0x104,up=kind==0x101||kind==0x105;
                if(down||up)
                {
                    int raw=Marshal.ReadInt32(data);
                    if(InputObserved!=null)InputObserved(raw,down,"capture="+Capturing+", suppressed="+suppressed.Contains(raw)+", enabled="+settings.Enabled);
                    if(suppressed.Contains(raw))
                    {if(up){suppressed.Remove(raw);Queue(StopIfIdle);}return new IntPtr(1);}
                    if(mapped!=null&&Native.GetForegroundWindow()==captureOwner&&down)
                    {
                        int key=FindPetKeys.Normalize(raw),token=++generation;var completed=mapped;
                        mapped=null;mappingQueued=true;suppressed.Add(raw);keys.Reset();
                        Queue(delegate
                        {
                            if(disposed||token!=generation)return;
                            mappingQueued=false;captureOwner=IntPtr.Zero;settings.Key=FindPetKeys.Valid(key)?key:0;
                            completed(settings.Key);StopIfIdle();
                        });
                        return new IntPtr(1);
                    }
                    if(!Capturing&&keys.Input(raw,down,now(),settings))
                    {
                        if(InputObserved!=null)InputObserved(raw,down,"matched generation "+generation);
                        int token=generation;Queue(delegate{
                            if(InputObserved!=null)InputObserved(raw,down,"queued token="+token+", generation="+generation+", disposed="+disposed+", capturing="+Capturing+", enabled="+settings.Enabled);
                            if(!disposed&&token==generation&&settings.Enabled&&!Capturing)Spotlight.Trigger(now());
                            if(InputObserved!=null)InputObserved(raw,down,"after trigger active="+Spotlight.Active);
                        });
                    }
                }
            }
            // Ordinary shortcuts pass through to the focused application. Never paint or do I/O here.
            return CallNextHookEx(hook,code,message,data);
        }
        void StopIfIdle()
        {
            if(Capturing||suppressed.Count>0||(settings.Enabled&&FindPetKeys.Valid(settings.Key)))return;
            Stop();
        }
        void Stop(){if(hook!=IntPtr.Zero){UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}keys.Reset();}
        public void Dispose(){if(disposed)return;disposed=true;generation++;Stop();mapped=null;suppressed.Clear();Spotlight.Dispose();}
    }
}
