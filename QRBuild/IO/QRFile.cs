using System;
using System.IO;

namespace QRBuild.IO
{
    public static class QRFile
    {
        public static bool Delete(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (System.Exception)
            {
            	return false;
            }
        }

        public static void Delete(params string[] paths)
        {
            foreach (string path in paths) {
                Delete(path);
            }
        }
    }
}
