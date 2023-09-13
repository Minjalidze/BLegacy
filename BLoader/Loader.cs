using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BLoader
{
    public class Loader
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);
        
        private static IntPtr _nativeThread = IntPtr.Zero;
        
        public static void Load()
        {
            BClient.Bootstrapper.Initialize();
            /*var file = File.ReadAllBytes(@"rust_Data\Managed\BClient.dll");
            var assembly = Assembly.Load(file);

            var type = assembly.GetType("BClient.Bootstrapper");
            var methodInfo = type.GetMethod("Initialize");
            
            var constructorParameters = new object[0];
            var instance = Activator.CreateInstance(type, constructorParameters);
            
            var parameters = new object[0];
            methodInfo?.Invoke(instance, parameters);*/

            // _nativeThread = LoadWin32Library(@"rust_Data\Plugins\BNative.dll");
            // if (_nativeThread == IntPtr.Zero) Process.GetCurrentProcess().Kill();
        }
        
        private static IntPtr LoadWin32Library(string libPath)
        {
            if (string.IsNullOrEmpty(libPath)) throw new ArgumentNullException(nameof(libPath));

            var moduleHandle = LoadLibrary(libPath);
            if (moduleHandle != IntPtr.Zero) return moduleHandle;

            var lastError = Marshal.GetLastWin32Error();
            var innerEx = new Win32Exception(lastError);
            innerEx.Data.Add("LastWin32Error", lastError);

            throw new Exception("can't load DLL " + libPath, innerEx);
        }
    }
}