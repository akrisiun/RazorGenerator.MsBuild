using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorGenerator.MsBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = args.Length > 1 ? args[0] : Environment.CurrentDirectory;
			
            System.IO.Directory.SetCurrentDirectory(dir);
            var task = Razor.Start(dir);

            task.ExecuteCore();
        }
    }
}
