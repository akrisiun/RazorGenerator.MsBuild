using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RazorGenerator.MsBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            string arg1 = args.Length >= 1 ? args[0] : null;
            if (arg1 == "-debug")
            {
                arg1 = null;
                
                Console.WriteLine("Debug?");
                Console.ReadLine();
                Debugger.Launch();
                if (Debugger.IsAttached)
                    Debugger.Break();
            }

            var dir = arg1 ?? Environment.CurrentDirectory;
			
            System.IO.Directory.SetCurrentDirectory(dir);

            var mvcBin = Path.Combine(dir, @"bin\System.Web.Mvc.dll");
            var razorBin = Path.Combine(dir, @"bin\System.Web.Razor.dll");
            
            LoadDllSafe(razorBin);
            LoadDllSafe(mvcBin);
            LoadDllSafe(Path.Combine(dir, @"bin\System.Web.WebPages.Razor.dll"));
            LoadDllSafe(Path.Combine(dir, @"bin\System.Web.WebPages.dll"));

            var task = Razor.Start(dir);

            task.ExecuteCore();
        }

        public static void LoadDllSafe(string dllFile)
        {
            if (!File.Exists(dllFile))
                return;

            try {

                var asm = Assembly.LoadFile(dllFile);
                Console.WriteLine(string.Format("Loaded {0}", asm.FullName));

            } catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
}
