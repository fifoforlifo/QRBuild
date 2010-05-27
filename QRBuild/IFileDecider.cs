using System.IO;

namespace QRBuild
{
    /// Implementors of this interface decide how file state is represented in DepsCaches.
    /// They are used during the build to determine whether nodes are up-to-date.
    public interface IFileDecider
    {
        /// Returns a version stamp for a file.
        /// This could include information such as 
        /// file-date, file-size, and even an MD5-hash of the contents.
        string GetVersionStamp(string filePath);
    }

    public static class FileDecider
    {
        public static string NonExistentFile = "NE";

        /// Returns true if the queried filePath's FileInfo matches its cachedVersionStamp.
        public static bool IsTargetModified(this IFileDecider fileDecider, string filePath, string cachedVersionStamp)
        {
            //  If it didn't exist previously, return that it's modified.
            if (cachedVersionStamp == NonExistentFile)
            {
                return true;
            }

            //  If queried file doesn't exist, return that it's modified.
            if (!File.Exists(filePath))
            {
                return true;
            }

            string currentVersionStamp = fileDecider.GetVersionStamp(filePath);
            bool result = currentVersionStamp != cachedVersionStamp;
            return result;
        }
    }
}