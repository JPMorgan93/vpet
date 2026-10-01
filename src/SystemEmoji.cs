using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Vpet
{
    // Render installed Windows emoji with DirectWrite + Direct2D's color-font flag.
    // The white tile matches the bubble interior. Cache the immutable defaults.
    internal static class SystemEmoji
    {
        static readonly Bitmap[] cache=new Bitmap[Reactions.Names.Length];
        public static Bitmap Image(int index)
        {
            if(cache[index]==null)cache[index]=Render(Reactions.Emoji(index));
            return cache[index];
        }
        [StructLayout(LayoutKind.Sequential)] struct TargetProperties
        { public uint Type,Format,AlphaMode;public float DpiX,DpiY;public uint Usage,MinLevel; }
        [StructLayout(LayoutKind.Sequential)] struct ColorF
        { public float R,G,B,A;public ColorF(float r,float g,float b,float a){R=r;G=g;B=b;A=a;} }
        [StructLayout(LayoutKind.Sequential)] struct RectF {public float Left,Top,Right,Bottom;}
        [DllImport("d2d1.dll",ExactSpelling=true)] static extern int D2D1CreateFactory(uint type,ref Guid iid,IntPtr options,out IntPtr factory);
        [DllImport("dwrite.dll",ExactSpelling=true)] static extern int DWriteCreateFactory(uint type,ref Guid iid,out IntPtr factory);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int CreateTarget(IntPtr self,ref TargetProperties props,out IntPtr target);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int BindDC(IntPtr self,IntPtr dc,ref Native.RECT rect);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int CreateBrush(IntPtr self,ref ColorF color,IntPtr properties,out IntPtr brush);
        [UnmanagedFunctionPointer(CallingConvention.StdCall,CharSet=CharSet.Unicode)] delegate int CreateFormat(IntPtr self,[MarshalAs(UnmanagedType.LPWStr)]string family,IntPtr collection,uint weight,uint style,uint stretch,float size,[MarshalAs(UnmanagedType.LPWStr)]string locale,out IntPtr format);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int SetAlignment(IntPtr self,uint alignment);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void BeginDraw(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void Clear(IntPtr self,ref ColorF color);
        [UnmanagedFunctionPointer(CallingConvention.StdCall,CharSet=CharSet.Unicode)] delegate void DrawText(IntPtr self,[MarshalAs(UnmanagedType.LPWStr)]string text,uint length,IntPtr format,ref RectF rect,IntPtr brush,uint options,uint measuringMode);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int EndDraw(IntPtr self,out ulong tag1,out ulong tag2);
        static T Method<T>(IntPtr instance,int slot) where T:class
        {
            var address=Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance),slot*IntPtr.Size);
            return (T)(object)Marshal.GetDelegateForFunctionPointer(address,typeof(T));
        }
        static void Check(int result){Marshal.ThrowExceptionForHR(result);}
        static Bitmap Render(string text)
        {
            var tile=new Bitmap(96,96,PixelFormat.Format24bppRgb);
            try
            {
                try{DrawNative(tile,text);}
                catch(COMException){DrawFallback(tile,text);}
                catch(DllNotFoundException){DrawFallback(tile,text);}
                catch(EntryPointNotFoundException){DrawFallback(tile,text);}
                return tile;
            }
            catch{tile.Dispose();throw;}
        }
        static void DrawNative(Bitmap tile,string text)
        {
            IntPtr factory=IntPtr.Zero,write=IntPtr.Zero,target=IntPtr.Zero,format=IntPtr.Zero,brush=IntPtr.Zero,dc=IntPtr.Zero;
            using(var graphics=Graphics.FromImage(tile))
            {
                try
                {
                    var factoryId=new Guid("06152247-6f50-465a-9245-118bfd3b6007");
                    var writeId=new Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");
                    Check(D2D1CreateFactory(0,ref factoryId,IntPtr.Zero,out factory));
                    Check(DWriteCreateFactory(0,ref writeId,out write));
                    var props=new TargetProperties{Format=87,AlphaMode=3,DpiX=96,DpiY=96};
                    // ID2D1Factory::CreateDCRenderTarget, ID2D1DCRenderTarget::BindDC.
                    Check(Method<CreateTarget>(factory,16)(factory,ref props,out target));
                    dc=graphics.GetHdc();var bounds=new Native.RECT{Right=tile.Width,Bottom=tile.Height};
                    Check(Method<BindDC>(target,57)(target,dc,ref bounds));
                    // IDWriteFactory::CreateTextFormat; weight normal, style normal, stretch normal.
                    Check(Method<CreateFormat>(write,15)(write,"Segoe UI Emoji",IntPtr.Zero,400,0,5,76,"en-us",out format));
                    Check(Method<SetAlignment>(format,3)(format,2)); // Horizontal center.
                    Check(Method<SetAlignment>(format,4)(format,1)); // Vertical center.
                    var black=new ColorF(0,0,0,1);Check(Method<CreateBrush>(target,8)(target,ref black,IntPtr.Zero,out brush));
                    Method<BeginDraw>(target,48)(target);var white=new ColorF(1,1,1,1);Method<Clear>(target,47)(target,ref white);
                    var rect=new RectF{Right=tile.Width,Bottom=tile.Height};
                    Method<DrawText>(target,27)(target,text,(uint)text.Length,format,ref rect,brush,4,0); // ENABLE_COLOR_FONT.
                    ulong tag1,tag2;Check(Method<EndDraw>(target,49)(target,out tag1,out tag2));
                }
                finally
                {
                    if(brush!=IntPtr.Zero)Marshal.Release(brush);if(format!=IntPtr.Zero)Marshal.Release(format);
                    if(target!=IntPtr.Zero)Marshal.Release(target);if(dc!=IntPtr.Zero)graphics.ReleaseHdc(dc);
                    if(write!=IntPtr.Zero)Marshal.Release(write);if(factory!=IntPtr.Zero)Marshal.Release(factory);
                }
            }
        }
        static void DrawFallback(Bitmap tile,string text)
        {
            using(var graphics=Graphics.FromImage(tile))using(var font=new Font("Segoe UI Emoji",70,FontStyle.Regular,GraphicsUnit.Pixel))
            using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})
            {graphics.Clear(Color.White);graphics.DrawString(text,font,Brushes.Black,new RectangleF(0,0,tile.Width,tile.Height),format);}
        }
    }
}
