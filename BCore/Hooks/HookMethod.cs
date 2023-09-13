using System;
using System.Reflection;

namespace BCore.Hooks;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true)]
internal class HookMethod : Attribute
{
    internal readonly MethodInfo Method;

    internal HookMethod(Type type, string methodName)
    {
        Method = type.GetMethod(methodName, MinHook.AnyType);
    }
}