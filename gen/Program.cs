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

            var domainDir = AppDomain.CurrentDomain.BaseDirectory ?? "";
            if (domainDir.EndsWith(@"bin\Debug\netcoreapp2.0\")
                || domainDir.EndsWith(@"bin\Debug\net46\"))
                domainDir = Path.GetFullPath(domainDir + @"..\..\..\..");
            else 
                domainDir = Environment.CurrentDirectory;
            var dir = Path.GetFullPath(arg1 ?? domainDir) + @"\";
			
            System.IO.Directory.SetCurrentDirectory(dir);


            BaseDir = dir;
            var asm = LoadDllSafe(dir + @"RazorGenerator.MsBuild.dll");
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            
            object task = null; // RazorGenerator.MsBuild.Razor.Start(dir);
            var taskType = asm.GetType("RazorGenerator.MsBuild.Razor"); // Razor.Start(dir);
            var Start = taskType.GetMethod("Start", BindingFlags.Static| BindingFlags.Public);
            task = Start.Invoke(null, new object[] { dir });

            var ExecuteCore = taskType.GetMethod("ExecuteCore", BindingFlags.Static| BindingFlags.Public);
            ExecuteCore.Invoke(null, new object[] { task });
            // RazorGenerator.MsBuild.Razor.ExecuteCore(task);
        }

        static string BaseDir {get; set; }

        // ResolveEventHandler(object sender, ResolveEventArgs args);
        static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            var dir = BaseDir;
            var mvcBin = Path.Combine(dir, @"bin\System.Web.Mvc.dll");
            var razorBin = Path.Combine(dir, @"bin\System.Web.Razor.dll");
            var name = args.Name;
            Assembly asm =null;
            if (name.Contains("Web.Razor"))
               asm = LoadDllSafe(razorBin);
            else if (name.Contains("Web.Razor"))
               asm = LoadDllSafe(mvcBin);
            else if (name.Contains("Web.WebPages.Razor"))
               asm = LoadDllSafe(Path.Combine(dir, @"bin\System.Web.WebPages.Razor.dll"));
            else if (name.Contains("Web.WebPages"))
               asm = LoadDllSafe(Path.Combine(dir, @"bin\System.Web.WebPages.dll"));
            else if (name.Contains("Build.Framework"))
               asm = LoadDllSafe(dir + @"Microsoft.Build.Framework.dll");
            else if (name.Contains("Build.Utilities"))
               asm = LoadDllSafe(dir + @"Microsoft.Build.Utilities.v4.0.dll");

            // Could not load file or assembly
            //  'Microsoft.Build.Utilities.v4.0, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL'. Reference assemblies should not be loaded for execution.  They can only be loaded in the Reflection-only loader context. (Exception from HRESULT: 0x80131058)

            return asm;
        }

        public static Assembly LoadDllSafe(string dllFile)
        {
            Assembly asm = null;
            if (!File.Exists(dllFile))
                return asm;

            try {

                asm = Assembly.LoadFile(dllFile);
                Console.WriteLine(string.Format("Loaded {0}", asm.FullName));

            } catch (Exception ex) { Console.WriteLine(ex.Message); }
            return asm;
        }
    }
}
