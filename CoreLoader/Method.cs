using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoreLoader
{
    public static class Method
    {
        #region [struct] Result
        public struct Result
        {
            private static Type _type;
            private static object _value;            
            public Result(object value, Type type) { _value = value; _type = type; }
            private static bool IsNull => Equals(_value, null);
        }
        #endregion

        // Method Private Environments //
        private static readonly Dictionary<string, Type> AssemblyTypes = new Dictionary<string, Type>();
        private static readonly Dictionary<string, Assembly> AssemblyNames = new Dictionary<string, Assembly>();

        // Method: Initialize //
        #region [Public] Initialize
        public static bool Initialize()
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (AssemblyNames != null && !AssemblyNames.ContainsValue(a))
                    {
                        AssemblyNames.Add(a.GetName().Name, a);
                        foreach (var T in a.GetTypes())
                        {
                            if (T.FullName != null && (T.FullName.Contains("<") || T.FullName.Contains("$") || T.FullName.Contains("`"))) continue;
                            if (T.FullName == null) continue;
                            var name = T.FullName.Replace("+", "."); 
                            if (!AssemblyTypes.ContainsKey(name)) AssemblyTypes.Add(name, T);
                        }
                    }
                }
                catch (Exception) { /* Ignore all exceptions */ };
            }
            return true;
        }
        #endregion

        // Method: SetValue //
        #region [Public] SetValue(method, value)
        public static bool SetValue(string method, object value)
        {
            if (method.Contains("."))
            {
                var valueClass = method.Substring(0, method.LastIndexOf('.'));
                if (AssemblyTypes.ContainsKey(valueClass))
                {
                    var valueMethod = method.Replace(valueClass + ".", "");
                    var fieldValue = AssemblyTypes[valueClass].GetField(valueMethod);
                    if (fieldValue != null) { fieldValue.SetValue(null, value); return true; }
                    var propertyValue = AssemblyTypes[valueClass].GetProperty(valueMethod);
                    if (propertyValue != null) { propertyValue.SetValue(null, value, null); return true; }
                }
            }

            var format = $"Assembly type not defined for method \"{method}\".";
            return false;
        }
        #endregion

        // Method: Invoke //
        #region [Public] Invoke(method)
        public static Result Invoke(string method)
        {
            if (method.Contains("."))
            {
                var invokeClass = method.Substring(0, method.LastIndexOf('.'));
                var invokeMethod = method.Replace(invokeClass + ".", "get_");
                return Invoke(invokeClass, AssemblyTypes[invokeClass].GetMethod(invokeMethod) != null ? invokeMethod : method.Replace(invokeClass + ".", ""), null);
            }

            var format = $"Assembly type not defined for method \"{method}\".";
            return new Result(null, null);
        }
        #endregion
        #region [Public] Invoke(method, args)
        public static Result Invoke(string method, params object[] args)
        {
            if (method.Contains("."))
            {
                var invokeClass = method.Substring(0, method.LastIndexOf('.'));
                var invokeMethod = method.Replace(invokeClass + ".", "");
                return Invoke(invokeClass, invokeMethod, null, args);
            }

            var format = $"Assembly type not defined for method \"{method}\".";
            return new Result(null, null);
        }
        #endregion
        #region [Public] Invoke(object, method, args)
        public static Result InvokeTo(object obj, string method, params object[] args)
        {
            if (method.Contains("."))
            {
                var invokeClass = method.Substring(0, method.LastIndexOf('.'));
                var invokeMethod = method.Replace(invokeClass + ".", "");
                return Invoke(invokeClass, invokeMethod, obj, args);
            }

            var format = $"Assembly type not defined for method \"{method}\".";
            return new Result(null, null);
        }
        #endregion
        #region [Public] Invoke(type, method, target, args)
        public static Result Invoke(string type, string method, object target, params object[] args)
        {
            if (AssemblyTypes.ContainsKey(type)) try
                {
                    // Try to get field to invoke from class type //
                    foreach (var info in AssemblyTypes[type].GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        if (info.Name == method) return new Result(info.GetValue(target), info.FieldType);
                    }

                    var stringArgs = ""; if (args == null) args = new object[0]; var invokeTypes = new Type[args.Length];

                    for (var i = 0; i < args.Length; i++)
                    {
                        if (args[i] == null) { invokeTypes[i] = null; stringArgs += "null,"; } else { invokeTypes[i] = args[i].GetType(); stringArgs += args[i] + ","; }
                    }

                    // Try to get method to invoke from class type //
                    foreach (var info in AssemblyTypes[type].GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        if (info.Name == method) return new Result(info.Invoke(target, args), info.ReturnType);
                    }

                    // Output errors when invoke of method or field is failed //
                    var format = $"Assembly type \"{type}.{method}({stringArgs})\" not exists for invoke.";
                }
                catch (Exception e)
                {
                    var s = e.ToString();
                }
            else
            {
                var format = $"Assembly type \"{type}\" not exists for invoke.";
            }
            return new Result(null, null);
        }
        #endregion
    }
}
