using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using QRBuild.CSharp;

namespace QRBuild
{
    class Program
    {
        static void Main(string[] args)
        {
            var cscp = new CSharpCompileParams();
            cscp.CscPath = @"C:\Windows\Microsoft.NET\Framework\v3.5\csc";
            cscp.SourceRoot = @"K:\work\code\C#\foo";
            cscp.Sources.Add(@"K:\work\code\C#\foo\Blah.cs");
            cscp.Platform = CSharpPlatforms.AnyCpu;
            cscp.Debug = true;
            
            var csc = new CSharpCompile(cscp);
            csc.Execute();
            Console.WriteLine(">> Press a key");
            Console.ReadKey();
        }
    }
}
