using System;
using System.Collections.Generic;
using System.Linq;
using QRBuild.ProjectSystem.CommandLine;

namespace QRBuild
{
    public static class QRBuild
    {
        public static int Main(string[] args)
        {
            DateTime startTime = DateTime.Now;
            var clHandlers = GetCLHandlers();

            if (args.Length < 1) {
                PrintUsage(clHandlers);
                return -1;
            }

            if (args[0] == "help") {
                if (args.Length < 2) {
                    PrintUsage(clHandlers);
                    return 0;
                }

                foreach (CLHandler clHandler in clHandlers) {
                    if (clHandler.Name == args[1]) {
                        Console.Write(clHandler.LongHelp);
                        return 0;
                    }
                }

                Console.WriteLine("Error: Unknown command {0}", args[1]);
                return -1;
            }
            else {
                // Find the command and execute it.
                foreach (CLHandler clHandler in clHandlers) {
                    if (clHandler.Name == args[0]) {
                        int result = clHandler.Execute(args);
                        DateTime endTime = DateTime.Now;

                        TimeSpan totalTime = endTime - startTime;
                        Console.WriteLine("");
                        Console.WriteLine("# TotalTime                             = {0}", totalTime);
                        Console.WriteLine("");
                        return result;
                    }
                }

                Console.WriteLine("Error: Unknown command {0}", args[0]);
                return -1;
            }
        }

        private static void PrintUsage(IList<CLHandler> clHandlers)
        {
#if false
            string usage = 
"usage: qrbuild COMMAND [ARGS]\n" +
"\n" +
"Built-in commands:\n" +
"  build        Incremental build.\n" +
"  clean        Delete all built files, do not remove directories.\n" +
"  clobber      Delete all built files, remove empty directories.\n" +
"  rebuild      Clean, then build.\n" +
"  version      Print version of QRBuild.\n" +
"Project's commands:\n" +
"  A1           Do thing #1.\n" +
"  A2           Do thing #2.\n" +
"\n" +
"See 'qrbuild help COMMAND' for more information on a specific command.\n" +
"";
            Console.Write(usage);
#else
            Console.Write(
"usage: qrbuild COMMAND [ARGS]\n" +
"\n" +
"The most commonly used commands are:\n");
            foreach (var clHandler in clHandlers) {
                Console.Write("  {0,-12}  {1}\n",
                    clHandler.Name,
                    clHandler.ShortHelp);
            }
            Console.Write(
"\n" +
"See 'qrbuild help COMMAND' for more information on a specific command.\n" +
"");
#endif
        }

        private static IList<CLHandler> GetCLHandlers()
        {
            var clHandlers = new CLHandler[] {
                new CLHandlerBuild(),
                new CLHandlerClean(),
                new CLHandlerClobber(),
                new CLHandlerWipe(),
                new CLHandlerShow(),
                new CLHandlerGraphViz(),
            };
            Array.Sort(clHandlers);
            return clHandlers.ToList();
        }
    }
}
