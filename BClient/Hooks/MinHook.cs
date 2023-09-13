using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BClient.Hooks
{
    internal static class MinHook
    {
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false)]
        private static extern IntPtr GetProcAddress(IntPtr module, string procName);

        internal const BindingFlags AnyType = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private enum Status
        {
            MH_Ok = 0
        }
        
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate Status Initialize();
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate Status CreateHook(IntPtr pTarget, IntPtr pDetour, out IntPtr ppOriginal);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate Status EnableHook(IntPtr pTarget);

        internal static void Init()
        {
            var init = GetExport<Initialize>(HookLoader.MinHookThread, "MH_Initialize");
            var create = GetExport<CreateHook>(HookLoader.MinHookThread, "MH_CreateHook");
            var enable = GetExport<EnableHook>(HookLoader.MinHookThread, "MH_EnableHook");

            if (init is null || create is null || enable is null || init() != Status.MH_Ok) return;

            foreach (var method in (from type in Assembly.GetExecutingAssembly().GetTypes()
                         from methodInfo in type.GetMethods(AnyType)
                         select methodInfo).Where(t => t.IsDefined(typeof(HookMethod), true)))
            {
                if (method.GetCustomAttributes(typeof(HookMethod), true).FirstOrDefault() is not HookMethod attribute)
                    continue;
                create(attribute.Method.MethodHandle.GetFunctionPointer(), method.MethodHandle.GetFunctionPointer(),
                    out _);
            }

            enable(IntPtr.Zero);
        }

        private static TProcType GetExport<TProcType>(IntPtr moduleHandle, string exportName) where TProcType : class
        {
            var pointer = GetProcAddress(moduleHandle, exportName);
            if (pointer == IntPtr.Zero) return null;

            var function = Marshal.GetDelegateForFunctionPointer(pointer, typeof(TProcType));
            return function as TProcType;
        }
    }
}