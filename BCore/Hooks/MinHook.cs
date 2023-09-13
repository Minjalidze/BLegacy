using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BCore.Hooks;

internal static class MinHook
{
    internal const BindingFlags AnyType =
        BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true, CharSet = CharSet.Ansi,
        BestFitMapping = false)]
    private static extern IntPtr GetProcAddress(IntPtr module, string procName);

    public static void Init()
    {
        var mhInit = GetExport<MhInitialize>(HookLoader.MinHookThread, "MH_Initialize");
        var mhCreate = GetExport<MhCreateHook>(HookLoader.MinHookThread, "MH_CreateHook");
        var mhEnable = GetExport<MhEnableHook>(HookLoader.MinHookThread, "MH_EnableHook");

        if (mhInit is null || mhCreate is null || mhEnable is null || mhInit() != MhStatus.MhOk) return;

        foreach (var method in (from type in Assembly.GetExecutingAssembly().GetTypes()
                     from methodInfo in type.GetMethods(AnyType)
                     select methodInfo).Where(t => t.IsDefined(typeof(HookMethod), true)))
            try
            {
                if (method.GetCustomAttributes(typeof(HookMethod), true).FirstOrDefault() is not HookMethod attribute)
                    continue;
                mhCreate(attribute.Method.MethodHandle.GetFunctionPointer(), method.MethodHandle.GetFunctionPointer(),
                    out _);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                Debug.Log(method.Name);
                throw;
            }

        mhEnable(IntPtr.Zero);
    }

    private static TProcType GetExport<TProcType>(IntPtr moduleHandle, string exportName) where TProcType : class
    {
        var pointer = GetProcAddress(moduleHandle, exportName);
        if (pointer == IntPtr.Zero) return null;

        var function = Marshal.GetDelegateForFunctionPointer(pointer, typeof(TProcType));
        return function as TProcType;
    }

    private enum MhStatus
    {
        MhOk = 0
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate MhStatus MhInitialize();

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate MhStatus MhCreateHook(IntPtr pTarget, IntPtr pDetour, out IntPtr ppOriginal);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate MhStatus MhEnableHook(IntPtr pTarget);
}