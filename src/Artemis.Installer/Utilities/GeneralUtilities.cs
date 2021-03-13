using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Artemis.Installer.Utilities
{
    public static class GeneralUtilities
    {
        /// <summary>
        ///     Creates all directories and subdirectories in the specified path unless they already exist with permissions
        ///     allowing access by everyone.
        /// </summary>
        /// <param name="path">The directory to create.</param>
        public static void CreateAccessibleDirectory(string path)
        {
            DirectoryInfo dataDirectory = !Directory.Exists(path) ? Directory.CreateDirectory(path) : new DirectoryInfo(path);

            // On Windows, ensure everyone has permission (important when running as admin)
            DirectorySecurity security = dataDirectory.GetAccessControl();
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            security.AddAccessRule(new FileSystemAccessRule(
                everyone,
                FileSystemRights.Modify | FileSystemRights.Synchronize,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None, AccessControlType.Allow)
            );
            dataDirectory.SetAccessControl(security);
        }
    }
}