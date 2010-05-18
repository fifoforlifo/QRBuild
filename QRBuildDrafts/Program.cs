using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using QRBuild.CSharp;
using QRBuild.Engine;
using QRBuild.IO;

namespace QRBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            var cscp = new CSharpCompileParams();
            cscp.CscPath = @"C:\Windows\Microsoft.NET\Framework\v3.5\csc";
            cscp.CompileDir = @"K:\work\code\C#\foo";
            string sourceFile = @"K:\work\code\C#\foo\Blah.cs";
            cscp.Sources.Add(sourceFile);
            cscp.OutputFilePath = QRPath.ChangeExtension(sourceFile, ".exe");
            cscp.Platform = CSharpPlatforms.AnyCpu;
            cscp.Debug = true;

            var buildGraph = new BuildGraph();

            var csc = new CSharpCompile(buildGraph, cscp);
            csc.Execute();
            Console.WriteLine(">> Press a key");
            Console.ReadKey();


            {
                string a = Path.GetDirectoryName("a/b.txt");
                string nodir = Path.GetDirectoryName("c.txt");
                string nodir2 = Path.GetDirectoryName("a/");
            }
        }
    }
}
