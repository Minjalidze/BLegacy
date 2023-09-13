using System;
using System.Diagnostics;
using System.Net;

namespace BCUpdate
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var processes = Process.GetProcessesByName("rust");
            if (processes.Length > 0) foreach (var t in processes) t.Kill();

            Console.WriteLine("Обновление клиента...");
            
            var webClient = new WebClient();
            var fileList = webClient.DownloadString("http://blessrust.site/API/GetAssemblies.php").Split(':');
            foreach (var file in fileList)
            {
                webClient.DownloadFile(new Uri($"http://blessrust.site/BF/{file}"), AppDomain.CurrentDomain.BaseDirectory + $@"\rust_Data\Managed\{file}");
            }
            
            Process.Start("rust.exe");
        }
    }
}