using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRTS
{
    class _Program
    {        
        static int Main(string[] args)
        {
            if (args.Count() != 1)
            {
                Console.WriteLine("Usage: SRTS AssemblyName");
                return 1;
            }

            LoadReferencedAssemblies(args[0]);

            var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), args[0]);
            var asm = System.Reflection.Assembly.LoadFile(path);

            Console.WriteLine(SignalRModule.Create(asm));

            return 0;
        }

        private static void LoadReferencedAssemblies(string assembly)
        {
            var sourceAssemblyDirectory = Path.GetDirectoryName(assembly);

            foreach (var file in Directory.GetFiles(sourceAssemblyDirectory, "*.dll"))
            {
                File.Copy(file, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, new FileInfo(file).Name), true);
            }
        }
    }
}
