using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
                Debugger.Launch();
                Debugger.Break();
            }

            var dir = arg1 ?? Environment.CurrentDirectory;
			
            System.IO.Directory.SetCurrentDirectory(dir);
            var task = Razor.Start(dir);

            task.ExecuteCore();
        }
    }
}
