using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QRBuild.IO;

namespace QRBuild.Translations.ToolChain.Msvc
{
    public static class MsvcLibExtensions
    {
        public static MsvcLibParams Canonicalize(this MsvcLibParams p)
        {
            MsvcLibParams o = new MsvcLibParams();
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
            o.OutputType = p.OutputType;

            //-- Input
            if (p.Inputs.Count == 0) {
                throw new InvalidOperationException("No Inputs specified");
            }
            foreach (string path in p.Inputs) {
                string absPath = QRPath.GetAbsolutePath(path, o.CompileDir);
                o.Inputs.Add(absPath);
            }
            if (!String.IsNullOrEmpty(p.DefFilePath)) {
                o.DefFilePath = QRPath.GetAbsolutePath(p.DefFilePath, o.CompileDir);
            }
            foreach (string path in p.NoDefaultLib) {
                o.NoDefaultLib.Add(path);
            }

            //-- Output
            o.SubSystem = p.SubSystem;
            if (!String.IsNullOrEmpty(p.OutputFilePath)) {
                o.OutputFilePath = QRPath.GetAbsolutePath(p.OutputFilePath, o.CompileDir);
            }
            else {
                string firstInput = o.Inputs[0];
                string newExtension = ".lib";
                o.OutputFilePath = QRPath.ChangeExtension(firstInput, newExtension);
            }

            //-- Options
            foreach (string symbol in p.Export) {
                o.Export.Add(symbol);
            }
            foreach (string symbol in p.Include) {
                o.Include.Add(symbol);
            }
            o.LinkTimeCodeGeneration = p.LinkTimeCodeGeneration;
            if (o.OutputType == MsvcLibOutputType.ImportLibrary) {
                if (String.IsNullOrEmpty(p.DllNameForImportLibrary)) {
                    throw new InvalidOperationException("DllName is required if creating an import library.");
                }
                o.DllNameForImportLibrary = p.DllNameForImportLibrary;
            }
            o.NoLogo = p.NoLogo;
            o.Verbose = p.Verbose;
            o.WarningsAsErrors = p.WarningsAsErrors;

            return o;
        }

        /// This function assumes p is canonicalized.
        public static string ToArgumentString(this MsvcLibParams p)
        {
            StringBuilder b = new StringBuilder();
            
            //-- Input
            //  Inputs comes last
            if (p.OutputType == MsvcLibOutputType.ImportLibrary &&
                !String.IsNullOrEmpty(p.DefFilePath)) {
                b.AppendFormat("/DEF:{0} ", p.DefFilePath);
            }
            foreach (string path in p.NoDefaultLib) {
                b.AppendFormat("/NODEFAULTLIB:{0} ", path);
            }
            
            //-- Output
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
            b.AppendFormat("/OUT:{0} ", p.OutputFilePath);

            //-- Options
            foreach (string symbol in p.Export) {
                b.AppendFormat("/EXPORT:{0} ", symbol);
            }
            foreach (string symbol in p.Include) {
                b.AppendFormat("/INCLUDE:{0} ", symbol);
            }
            if (p.LinkTimeCodeGeneration) {
                b.Append("/LTCG ");
            }
            if (p.OutputType == MsvcLibOutputType.ImportLibrary &&
                !String.IsNullOrEmpty(p.DllNameForImportLibrary)) {
                b.AppendFormat("/NAME:{0} ", p.DllNameForImportLibrary);
            }
            if (p.NoLogo) {
                b.Append("/NOLOGO ");
            }
            if (p.Verbose) {
                b.Append("/VERBOSE ");
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
