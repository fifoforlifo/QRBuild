using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QRBuild.CSharp
{
    public static class CSharpCompileExtensions
    {
        public static string ToArgumentString(this CSharpCompileParams p)
        {
            StringBuilder b = new StringBuilder();
            b.AppendFormat("/nologo ");
            //-- Input Options
            foreach (string reference in p.AssemblyReferences)
            {
                b.AppendFormat("/r:\"{0}\" ", reference);
            }
            foreach (string module in p.InputModules)
            {
                b.AppendFormat("/addmodule:\"{0}\" ", module);
            }
            //-- Output Options
            if (!String.IsNullOrEmpty(p.OutputFilePath))
            {
                b.AppendFormat("/out:\"{0}\" ", p.OutputFilePath);
            }
            if (!String.IsNullOrEmpty(p.TargetFormat))
            {
                b.AppendFormat("/target:{0} ", p.TargetFormat);
            }
            //-- Code Generation
            if (p.Debug)
            {
                b.AppendFormat("/debug ");
            }
            if (p.Optimize)
            {
                b.AppendFormat("/optimize ");
            }
            //-- Errors and Warnings
            if (p.WarnAsError) 
            {
                b.AppendFormat("/warnaserror ");
            }
            b.AppendFormat("/warn:{0} ", p.WarnLevel);
            //-- Language
            if (p.Checked)
            {
                b.AppendFormat("/checked ");
            }
            if (p.Unsafe)
            {
                b.AppendFormat("/unsafe ");
            }
            foreach (string define in p.Defines)
            {
                b.AppendFormat("/define:{0} ", define);
            }
            if (!String.IsNullOrEmpty(p.LanguageVersion))
            {
                b.AppendFormat("/langversion:{0} ", p.LanguageVersion);
            }
            //-- Miscellaneous
            // NOTE: p.NoConfig may not be supplied in a response file
            //-- Advanced
            if (!String.IsNullOrEmpty(p.MainType))
            {
                b.AppendFormat("/main:{0} ", p.MainType);
            }
            if (p.FullPaths) 
            {
                b.AppendFormat("/fullpaths ");
            }
            if (!String.IsNullOrEmpty(p.PdbFilePath)) 
            {
                b.AppendFormat("/pdb:\"{0}\"", p.PdbFilePath);
            }
            if (!String.IsNullOrEmpty(p.ModuleAssemblyName)) 
            {
                b.AppendFormat("/moduleassemblyname:{0} ", p.ModuleAssemblyName);
            }

            //  ExtraArgs goes at the end.
            if (!String.IsNullOrEmpty(p.ExtraArgs)) 
            {
                b.AppendFormat("{0} ", p.ExtraArgs);
            }

            //  Source files must go at the very end.
            foreach (string sourceFile in p.Sources) {
                b.AppendFormat("\"{0}\" ", sourceFile);
            }

            return b.ToString();
        }
    }
}
