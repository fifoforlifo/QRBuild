using System;
using System.IO;
using System.Text;

namespace QRBuild.IO
{
    public sealed class QRFileStream
    {
        /// Read all text from the fileStream without closing it.
        public static string ReadAllText(FileStream fileStream)
        {
            // We do NOT dispose of the StreamReader, since that would
            // call fileStream.Close().
            StreamReader sr = new StreamReader(fileStream, Encoding.UTF8);
            sr.BaseStream.Seek(0, SeekOrigin.Begin);
            string result = sr.ReadToEnd();
            return result;
        }

        /// Write all text to the fileStream without closing it.
        public static void WriteAllText(FileStream fileStream, string text)
        {
            // We do NOT dispose of the StreamReader, since that would
            // call fileStream.Close().
            StreamWriter sw = new StreamWriter(fileStream, Encoding.UTF8);
            sw.BaseStream.Seek(0, SeekOrigin.Begin);
            sw.BaseStream.SetLength(0);
            sw.Write(text);
        }
    }
}
