using System;
using System.IO;

namespace CoreLoader
{
    public class Boot
    {
        public static void Load()
        {
            var bytes = File.ReadAllBytes("rust_server_Data\\Managed\\BCore.dll");
            AppDomain.CurrentDomain.Load(bytes);
            
            Method.Initialize();
            Method.Invoke("BCore.Bootstrapper.Load");
        }
    }
}