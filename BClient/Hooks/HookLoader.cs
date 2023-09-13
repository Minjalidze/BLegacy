using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BClient.Hooks
{
    internal static class HookLoader
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        internal static IntPtr MinHookThread;

        internal static bool LoadHooks()
        {
            MinHookThread = LoadWin32Library("rust_Data\\Plugins\\MinHook.dll");
            if (MinHookThread == null) return false;
            
            MinHook.Init();
            return true;
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