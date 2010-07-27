using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QRBuild.IO;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public static class MsvcLinkerExtensions
    {
        private static string FindFileWithExt(IEnumerable<string> paths, string extension)
        {
            foreach (string path in paths) {
                if (Path.GetExtension(path) == extension) {
                    return path;
                }
            }
            return null;
        }
        
        public static MsvcLinkerParams Canonicalize(this MsvcLinkerParams p)
        {
            MsvcLinkerParams o = new MsvcLinkerParams();
            //-- Meta
            if (String.IsNullOrEmpty(p.VcBinDir)) {
                throw new InvalidOperationException("VcBinDir not specified");
            }
            if (String.IsNullOrEmpty(p.CompileDir)) {
                throw new InvalidOperationException("CompileDir not specified");
            }
            o.CompileDir = QRPath.GetCanonical(p.CompileDir);
            o.BuildFileDir = String.IsNullOrEmpty(p.BuildFileDir)
                ? p.CompileDir
                : QRPath.GetCanonical(p.BuildFileDir);
            o.VcBinDir = QRPath.GetAbsolutePath(p.VcBinDir, p.CompileDir);
            o.ToolChain = p.ToolChain;
            o.AdditionalOptions = p.AdditionalOptions;

            //-- Input
            if (p.Inputs.Count == 0) {
                throw new InvalidOperationException("No Inputs specified");
            }
            foreach (string path in p.Inputs) {
                string absPath = QRPath.GetAbsolutePath(path, o.CompileDir);
                o.Inputs.Add(absPath);
            }
            foreach (string path in p.InputModules) {
                string absPath = QRPath.GetAbsolutePath(path, o.CompileDir);
                o.InputModules.Add(absPath);
            }
            if (!String.IsNullOrEmpty(p.DefFilePath)) {
                o.DefFilePath = QRPath.GetAbsolutePath(p.DefFilePath, o.CompileDir);
            }
            foreach (string path in p.DefaultLib) {
                o.DefaultLib.Add(path);
            }
            foreach (string path in p.NoDefaultLib) {
                o.NoDefaultLib.Add(path);
            }

            //-- Output
            o.Debug = p.Debug;
            o.Dll = p.Dll;
            o.SubSystem = p.SubSystem;
            if (!String.IsNullOrEmpty(p.OutputFilePath)) {
                o.OutputFilePath = QRPath.GetAbsolutePath(p.OutputFilePath, o.CompileDir);
            }
            else {
                string firstObj = FindFileWithExt(o.Inputs, ".obj");
                string newExtension = p.Dll ? ".dll" : ".exe";
                o.OutputFilePath = QRPath.ChangeExtension(firstObj, newExtension);
            }
            if (!String.IsNullOrEmpty(p.PdbFilePath)) {
                o.PdbFilePath = QRPath.GetAbsolutePath(p.PdbFilePath, o.CompileDir);
            }
            else {
                if (p.Debug) {
                    string firstObj = FindFileWithExt(o.Inputs, ".obj");
                    o.OutputFilePath = QRPath.ChangeExtension(firstObj, ".pdb");
                }
            }
            if (!String.IsNullOrEmpty(p.ImpLibPath)) {
                o.ImpLibPath = QRPath.GetAbsolutePath(p.ImpLibPath, o.CompileDir);
            }
            else {
                if (p.Dll) {
                    string firstObj = FindFileWithExt(o.Inputs, ".obj");
                    o.OutputFilePath = QRPath.ChangeExtension(firstObj, ".lib");
                }
            }
            if (!String.IsNullOrEmpty(p.MapFilePath)) {
                o.MapFilePath = QRPath.GetAbsolutePath(p.MapFilePath, o.CompileDir);
            }

            //-- Options
            foreach (string path in p.DelayLoad) {
                o.DelayLoad.Add(path);
            }
            o.Entry = p.Entry;
            foreach (string symbol in p.Export) {
                o.Export.Add(symbol);
            }
            o.Force = p.Force;
            foreach (string symbol in p.Include) {
                o.Include.Add(symbol);
            }
            o.Incremental = p.Incremental;
            o.NoAssembly = p.NoAssembly;
            o.NxCompat = p.NxCompat;
            o.OptRef = p.OptRef;
            o.Stack = p.Stack;
            o.Verbose = p.Verbose;
            o.Version = p.Version;
            o.WarningsAsErrors = p.WarningsAsErrors;

            return o;
        }

        /// This function assumes p is canonicalized.
        public static string ToArgumentString(this MsvcLinkerParams p)
        {
            StringBuilder b = new StringBuilder();
            
            //-- Input
            //  Inputs comes last
            foreach (string path in p.InputModules) {
                b.AppendFormat("/ASSEMBLYMODULE:{0} ", path);
            }
            if (!String.IsNullOrEmpty(p.DefFilePath)) {
                b.AppendFormat("/DEF:{0} ", p.DefFilePath);
            }
            foreach (string path in p.DefaultLib) {
                b.AppendFormat("/DEFAULTLIB:{0} ", path);
            }
            foreach (string path in p.NoDefaultLib) {
                b.AppendFormat("/NODEFAULTLIB:{0} ", path);
            }
            
            //-- Output
            if (p.Debug) {
                b.Append("/DEBUG ");
            }
            if (p.Dll) {
                b.Append("/DLL ");
            }
            else {
                if (p.SubSystem == MsvcSubSystem.Console) {
                    b.Append("/SUBSYSTEM:CONSOLE ");
                }
                else if (p.SubSystem == MsvcSubSystem.Windows) {
                    b.Append("/SUBSYSTEM:WINDOWS ");
                }
                else if (p.SubSystem == MsvcSubSystem.Native) {
                    b.Append("/SUBSYSTEM:NATIVE ");
                }
                else if (p.SubSystem == MsvcSubSystem.Posix) {
                    b.Append("/SUBSYSTEM:POSIX ");
                }
            }
            b.AppendFormat("/OUT:{0} ", p.OutputFilePath);
            if (!String.IsNullOrEmpty(p.PdbFilePath)) {
                b.AppendFormat("/PDB:{0} ", p.PdbFilePath);
            }
            if (!String.IsNullOrEmpty(p.ImpLibPath)) {
                b.AppendFormat("/IMPLIB:{0} ", p.ImpLibPath);
            }
            if (!String.IsNullOrEmpty(p.MapFilePath)) {
                b.AppendFormat("/MAP:{0} ", p.MapFilePath);
            }
            
            //-- Options
            foreach (string path in p.DelayLoad) {
                b.AppendFormat("/DELAYLOAD:{0} ", path);
            }
            if (!String.IsNullOrEmpty(p.Entry)) {
                b.AppendFormat("/ENTRY:{0} ", p.Entry);
            }
            foreach (string symbol in p.Export) {
                b.AppendFormat("/EXPORT:{0} ", symbol);
            }
            if (p.Force != MsvcForce.Default) {
                if (p.Force == MsvcForce.Multiple) {
                    b.Append("/FORCE:MULTIPLE ");
                }
                else if (p.Force == MsvcForce.Unresolved) {
                    b.Append("/FORCE:UNRESOLVED ");
                }
                else if (p.Force == MsvcForce.Multiple) {
                    b.Append("/FORCE:MULTIPLE|UNRESOLVED ");
                }
            }
            foreach (string symbol in p.Include) {
                b.AppendFormat("/INCLUDE:{0} ", symbol);
            }
            if (!p.Incremental) {
                b.Append("/INCREMENTAL:NO ");
            }
            if (p.NoAssembly) {
                b.Append("/NOASSEMBLY ");
            }
            b.AppendFormat("/NXCOMPAT{0} ", p.NxCompat ? "" : ":NO");
            if (p.OptRef == MsvcOptRef.OptRef) {
                b.Append("/OPT:REF ");
            }
            else if (p.OptRef == MsvcOptRef.OptRef) {
                b.Append("/OPT:NOREF ");
            }
            if (!String.IsNullOrEmpty(p.Stack)) {
                b.AppendFormat("/STACK:{0} ", p.Stack);
            }
            if (p.Verbose) {
                b.Append("/VERBOSE ");
            }
            if (!String.IsNullOrEmpty(p.Version)) {
                b.AppendFormat("/VERSION:{0} ", p.Version);
            }
            if (p.WarningsAsErrors) {
                b.Append("/WX ");
            }

            //-- Inputs
            foreach (string path in p.Inputs) {
                b.AppendFormat("\"{0}\" ", path);
            }

            return b.ToString();
        }
    }
}
