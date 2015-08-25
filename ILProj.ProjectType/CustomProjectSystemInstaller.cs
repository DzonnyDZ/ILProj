﻿using System;
using System.IO.Compression;
using System.Security;
using IO = System.IO;

namespace Dzonny.ILProj
{
    /// <summary>Handles installation of custom project sytem</summary>
    internal static class CustomProjectSystemInstaller
    {
        /// <summary>Gets path where custom project system is supposed to be installed</summary>
        private static string LocalCustomProjectSystemPath => IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomProjectSystems", "ILProj");

        /// <summary>Gets path where packed custom project system is saved and can be installed from</summary>
        private static string ZippedCustomProjectSystemPath => IO.Path.Combine(IO.Path.GetDirectoryName(typeof(CustomProjectSystemInstaller).Assembly.Location), "CustomBuildSystem.zip");

        /// <summary>Determines if it is necessary to install custom project file system locally</summary>
        /// <returns>True if custom project system is not installed locally or if it has to be upgraded; false if locally installed custom project system is up to date.</returns>
        /// <exception cref="IO.FileNotFoundException">File where zipped custom project system is supposed to be stored is not found</exception>
        /// <exception cref="IO.PathTooLongException">The path to file where zipped custom project file system is supposed to be stored exeeds system-define maximum path length.</exception>
        /// <exception cref="IO.IOException">An I/O error occurred while opening ZIP file with zipped custom project system</exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     The caller does not have the required permission to access path where zipped custom project system is supposed to be stored. -or-
        ///     Reading of version file is not supported on the current platform. -or-
        ///     Version file is missing in local custom project system, but there is a directory instead with same path. -or-
        ///     The caller does not have the required permission to read version file from local custom project system
        /// </exception>
        /// <exception cref="IO.InvalidDataException">File where zipped custom project system is stored is corrupted (cannot be interpreted as ZIP file)</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission to read version file in local custom project system</exception>
        /// <exception cref="NotSupportedException">The ZIP archive whre zipped custom project system is stored does not support reading.</exception>
        /// <exception cref="IO.InvalidDataException">
        ///     The ZIP archive where zipped custom project system is stored is corrupt, and its entries cannot be retrieved. -or-
        ///     The entry "version.txt" is either missing from the archive or is corrupt and cannot be read. -or-
        ///     The entry "version.txt" has been compressed by using a compression method that is not supported.
        /// </exception>
        /// <exception cref="FormatException">
        ///     Version text stored in "version.txt" entry of ZIP file containing zipped custom project system has fewer than two or more than four version components. -or-
        ///     At least one component in of version text is not an integer.
        /// </exception>
        /// <exception cref="OverflowException">
        ///     At least one component in version text stored in "version.txt" entry of ZIP file containing zipped custom project system represents a number that is greater than <see cref="Int32.MaxValue"/>.
        /// </exception>
        public static bool NeedsDeployment()
        {
            if (!IO.Directory.Exists(LocalCustomProjectSystemPath)) return true;
            using (var z = new ZippedCustomProjectSystem(ZippedCustomProjectSystemPath))
            {
                Version localVersion;
                try
                {
                    localVersion = new LocalCustomProjectSystem(LocalCustomProjectSystemPath).GetVersion();
                }
                catch (Exception ex) when (ex is IO.IOException || ex is FormatException || ex is OverflowException)
                {
                    return true;
                }
                return localVersion < z.GetVersion();
            }
        }

        /// <summary>Deploys custom project system locally from zipped archive</summary>
        public static void Deploy()
        {
            using (var zipped = new ZippedCustomProjectSystem(ZippedCustomProjectSystemPath))
            {
                if (IO.Directory.Exists(LocalCustomProjectSystemPath))
                    IO.Directory.Delete(LocalCustomProjectSystemPath);
                zipped.ExtractToDirectory(LocalCustomProjectSystemPath);
            }
        }
    }

    /// <summary>Base class for custom project system storage</summary>
    internal abstract class CustomProjectSystem
    {
        /// <summary>CTor - creates a new instance of the <see cref="CustomProjectSystem"/> class</summary>
        protected CustomProjectSystem() { }
        /// <summary>When overriden in derived class gets version of the custom project system</summary>
        /// <returns>Version of custom project system</returns>
        public abstract Version GetVersion();
    }

    /// <summary>Represents custom project system stored in file system (files and folders)</summary>
    internal class LocalCustomProjectSystem : CustomProjectSystem
    {
        /// <summary>Gets path of folder where the custom project system is stored</summary>
        public string Folder { get; }

        /// <summary>CTor - creatses a new instance of the <see cref="LocalCustomProjectSystem"/> class</summary>
        /// <param name="folder">Folder where the custom project system is stored</param>
        /// <exception cref="ArgumentNullException"><paramref name="folder"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="folder"/> is an empty string</exception>
        /// <exception cref="IO.DirectoryNotFoundException">Directory <paramref name="folder"/> does not exist (or is not accessible or is not directory)</exception>
        public LocalCustomProjectSystem(string folder)
        {
            if (folder == null) throw new ArgumentNullException(nameof(folder));
            if (folder == string.Empty) throw new ArgumentException("Value cannot be an empty string", nameof(folder));
            if (!IO.Directory.Exists(folder)) throw new IO.DirectoryNotFoundException($"Directory not found: {folder}");
            this.Folder = folder;
        }

        /// <summary>Gets version of the custom project system</summary>
        /// <returns>Version of custom project system</returns>
        /// <exception cref="IO.PathTooLongException">The path in <see cref="Folder"/> is too long</exception>
        /// <exception cref="IO.DirectoryNotFoundException">The path in <see cref="Folder"/> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IO.IOException">An I/O error occurred while opening the version file.</exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     Reading of version file is not supported on the current platform. -or-
        ///     Version file is missing in local custom project system, but there is a directory instead with same path. -or-
        ///     The caller does not have the required permission to read version file from local custom project system
        /// </exception>
        /// <exception cref="IO.FileNotFoundException">The version file is not found in local custom project file system</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission to read version file in local custom project system</exception>
        /// <exception cref="FormatException">
        ///     Text stored in version file of local custom project system has fewer than two or more than four version components. -or-
        ///     At least one component in version file of local custom project system is less than zero. -or-
        ///     At least one component in version file of local custom project system is not an integer.
        /// </exception>
        /// <exception cref="OverflowException">At least one component in version file of local custom project system represents a number that is greater than <see cref="Int32.MaxValue"/>.</exception>

        public override Version GetVersion()
        {
            string text = IO.File.ReadAllText(IO.Path.Combine(Folder, "version.txt"));
            try
            {
                return Version.Parse(text);
            }
            catch (ArgumentException ex)
            {
                throw new FormatException(ex.Message, ex);
            }
        }
    }

    /// <summary>Represents custom project system stored in a ZIP file</summary>
    internal class ZippedCustomProjectSystem : CustomProjectSystem, IDisposable
    {
        /// <summary>Opened ZIP archive containing the custom project system</summary>
        private readonly ZipArchive zip;
        /// <summary>Stream that contains ZIPped data</summary>
        private readonly IO.Stream zipStream;
        /// <summary>Gets path of ZIP file containing the custom project system</summary>
        public string ZipPath { get; }
        /// <summary>True when this object has already been diposed</summary>
        private bool disposed;

        /// <summary>CTor - creatse a new instance of the <see cref="ZippedCustomProjectSystem"/> class</summary>
        /// <param name="zipPath">Path of ZIP file containing custom project system</param>
        /// <exception cref="ArgumentNullException"><paramref name="zipPath"/> is null</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="zipPath"/> is an empty string -or- 
        ///     <paramref name="zipPath"/> contains only white space, or contains one or more invalid characters as defined by <see cref="IO.Path.InvalidPathChars"/>.
        /// </exception>
        /// <exception cref="IO.FileNotFoundException">File <paramref name="zipPath"/> does not exist (or is not reachable or is not file)</exception>
        /// <exception cref="IO.PathTooLongException">The path, file name, or both specified in <paramref name="zipPath"/> exceed the system-defined maximum length.</exception>
        /// <exception cref="IO.IOException">An I/O error occurred while opening the file specified in <paramref name="zipPath"/>.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission to access <paramref name="zipPath"/>.</exception>
        /// <exception cref="IO.InvalidDataException">The contents of <paramref name="zipPath"/> file could not be interpreted as a zip archive.</exception>
        public ZippedCustomProjectSystem(string zipPath)
        {
            if (zipPath == null) throw new ArgumentNullException(nameof(zipPath));
            if (zipPath == string.Empty) throw new ArgumentException("Value cannot be an empty string", nameof(zipPath));
            if (!IO.File.Exists(zipPath)) throw new IO.FileNotFoundException("File not found", zipPath);
            zipStream = IO.File.Open(zipPath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read);
            zip = new ZipArchive(zipStream, ZipArchiveMode.Read, true);
            ZipPath = zipPath;
        }

        /// <summary>Releases resource required by this instance</summary>
        public void Dispose()
        {
            zipStream.Dispose();
            zip.Dispose();
            disposed = true;
        }

        /// <summary>Gets version of the custom project system</summary>
        /// <returns>Version of custom project system</returns>
        /// <exception cref="ObjectDisposedException">This instance has already been disposed</exception>
        /// <exception cref="NotSupportedException">The ZIP archive whre zipped custom project system is stored does not support reading.</exception>
        /// <exception cref="IO.InvalidDataException">
        ///     The ZIP archive where zipped custom project system is stored is corrupt, and its entries cannot be retrieved. -or-
        ///     The entry "version.txt" is either missing from the archive or is corrupt and cannot be read. -or-
        ///     The entry "version.txt" has been compressed by using a compression method that is not supported.
        /// </exception>
        /// <exception cref="FormatException">
        ///     Version text stored in "version.txt" entry of ZIP file containing zipped custom project system has fewer than two or more than four version components. -or-
        ///     At least one component in of version text is not an integer.
        /// </exception>
        /// <exception cref="OverflowException">
        ///     At least one component in version text stored in "version.txt" entry of ZIP file containing zipped custom project system represents a number that is greater than <see cref="Int32.MaxValue"/>.
        /// </exception>
        public override Version GetVersion()
        {
            if (disposed) throw new ObjectDisposedException(nameof(ZippedCustomProjectSystem));
            string version;
            using (var entry = zip.GetEntry("version.txt").Open())
            using (var r = new IO.StreamReader(entry))
                version = r.ReadToEnd();
            try
            {
                return Version.Parse(version);
            }
            catch (ArgumentException ex)
            {
                throw new FormatException(ex.Message, ex);
            }
        }

        /// <summary>Extracts all files and foilders of the zipped ustom project system to given directory</summary>
        /// <param name="directory">Path of directory to extract custom project system to</param>
        /// <exception cref="ArgumentNullException"><paramref name="directory"/> is null</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="directory"/> is an empty string -or-
        ///     <paramref name="directory"/> contains only white space, or contains at least one invalid character.
        /// </exception>
        /// <exception cref="ObjectDisposedException">This instance has already been disposed</exception>
        /// <exception cref="IO.PathTooLongException">The path specified in <paramref name="directory"/> exceeds the system-defined maximum length.</exception>
        /// <exception cref="IO.DirectoryNotFoundException">The path specified in <paramref name="directory"/> is invalid (for example, it is on an unmapped drive).</exception>
        /// <exception cref="IO.IOException">
        ///     The directory specified by <paramref name="directory"/> already exists. -or-
        ///     The name of an entry in the archive is <see cref="String.Empty"/>, contains only white space, or contains at least one invalid character. -or-
        ///     Extracting an entry from the archive would create a file that is outside the directory specified by <paramref name="directory"/>. -or-
        ///     Two or more entries in the archive have the same name.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission to write to the destination <paramref name="directory"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="directory"/> contains an invalid format.</exception>
        /// <exception cref="IO.InvalidDataException">An archive entry cannot be found or is corrupt. -or- An archive entry was compressed by using a compression method that is not supported.</exception>
        public void ExtractToDirectory(string directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            if (directory == string.Empty) throw new ArgumentException("Value cannot be an empty string", nameof(directory));
            if (disposed) throw new ObjectDisposedException(nameof(ZippedCustomProjectSystem));
            zip.ExtractToDirectory(directory);
        }

    }
}