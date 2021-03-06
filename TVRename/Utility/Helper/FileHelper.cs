// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// Copyright (c) TV Rename. This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MediaInfo;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using Alphaleonis.Win32.Filesystem;
using Humanizer;
using JetBrains.Annotations;

namespace TVRename
{
    public static class FileHelper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static int GetFrameWidth([NotNull] this FileInfo movieFile)
        {
            return GetMetaDetails(movieFile, o => o.Properties.System.Video.FrameWidth, Parse, "GetFrameWidth",
                wrapper => wrapper.Width, i => i);
        }

        public enum VideoComparison
        {
            firstFileBetter,
            secondFileBetter,
            similar,
            cantTell,
            same
        }

        public static VideoComparison BetterQualityFile(FileInfo incumbantFile, [NotNull] FileInfo newFile)
        {
            if (!newFile.IsMovieFile())
            {
                return VideoComparison.firstFileBetter;
            }

            if (!incumbantFile.IsMovieFile())
            {
                return VideoComparison.secondFileBetter;
            }

            int encumbantLength = incumbantFile.GetFilmLength();
            int newFileLength = newFile.GetFilmLength();
            int encumbantFrameWidth = incumbantFile.GetFrameWidth();
            int newFileFrameWidth = newFile.GetFrameWidth();

            bool newFileContainsTerm =
                TVSettings.Instance.PriorityReplaceTermsArray.Any(term => newFile.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

            bool incumbantFileContainsTerm =
                TVSettings.Instance.PriorityReplaceTermsArray.Any(term => incumbantFile.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

            float percentMargin = TVSettings.Instance.replaceMargin;
            float marginMultiplier = (percentMargin + 100) / 100;

            bool incumbantFileIsMuchLonger = encumbantLength > newFileLength * marginMultiplier;
            bool newFileIsMuchLonger = encumbantLength * marginMultiplier < newFileLength;

            bool newFileIsBetterQuality = encumbantFrameWidth * marginMultiplier < newFileFrameWidth;
            bool incumbantFileIsBetterQuality = encumbantFrameWidth > newFileFrameWidth * marginMultiplier;

            if (encumbantLength == newFileLength && encumbantFrameWidth == newFileFrameWidth &&
                newFile.Length == incumbantFile.Length)
            {
                return VideoComparison.same;
            }

            if (   encumbantLength == -1
                || newFileLength == -1
                || encumbantFrameWidth == -1
                || newFileFrameWidth == -1)
            {
                return VideoComparison.cantTell;
            }

            if (incumbantFileIsBetterQuality && !newFileIsMuchLonger)
            {
                return VideoComparison.firstFileBetter;  //existing file is better quality
            }

            if (incumbantFileIsMuchLonger && !newFileIsBetterQuality)
            {
                return VideoComparison.firstFileBetter;  //existing file is longer
            }

            if (newFileIsBetterQuality && !incumbantFileIsMuchLonger)
            {
                return VideoComparison.secondFileBetter;
            }

            if (newFileIsMuchLonger && !incumbantFileIsBetterQuality)
            {
                return VideoComparison.secondFileBetter;
            }

            if (newFileContainsTerm)
            {
                return VideoComparison.secondFileBetter;
            }

            if (incumbantFileContainsTerm)
            {
                return VideoComparison.firstFileBetter;
            }

            return VideoComparison.similar;
        }

        public static int GetFrameHeight([NotNull] this FileInfo movieFile)
        {
            return GetMetaDetails(movieFile, o => o.Properties.System.Video.FrameHeight, Parse, "GetFrameHeight",
                wrapper => wrapper.Height, i => i);
        }

        private static int Parse(string returnValue, FileInfo sourceFile)
        {
            if (int.TryParse(returnValue, out int value))
            {
                return value;
            }

            return -1;
        }

        public static bool IsMovieFile([NotNull] this FileInfo file) => TVSettings.Instance.FileHasUsefulExtension(file, false);

        public static (bool found,string extension)  IsLanguageSpecificSubtitle(this FileInfo file)
        {
            foreach (string subExtension in TVSettings.Instance.subtitleExtensionsArray)
            {
                string regex = @"(?<ext>\.\w{2,3}\"+subExtension+")$";

                Match m = Regex.Match(file.Name, regex, RegexOptions.IgnoreCase);

                if (!m.Success)
                {
                    continue;
                }

                return (true,m.Groups["ext"].ToString());
            }

            return (false, string.Empty);
        }

        public static string UrlPathFullName([NotNull] this FileInfo baseFile)
        {
            //TODO - Should we remove or add characters here to appear in the export files?
            return baseFile.FullName;
        }

        [NotNull]
        public static FileInfo WithExtension([NotNull] this FileInfo baseFile, string extension) => new FileInfo(baseFile.RemoveExtension(true)+extension);

        private static int GetMetaDetails([NotNull] this FileInfo movieFile, Func<ShellObject, IShellProperty> extractMethod, Func<string,FileInfo,int> parseMethod,string operation, Func<MediaInfoWrapper,int> meExtractMethod,Func<int,int> meParseMethod)
        {
            try
            {
                string duration;
                using (ShellObject shell = ShellObject.FromParsingName(movieFile.FullName))
                {
                    duration = extractMethod(shell).FormatForDisplay(PropertyDescriptionFormatOptions.None);
                }

                if (!duration.IsNullOrWhitespace())
                {
                    int returnValue = parseMethod(duration, movieFile);

                    if (returnValue > 0)
                    {
                        return returnValue;
                    }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Logger.Warn($"Unable to find file as part of {operation} for {movieFile.FullName}");
                return -1;
            }
            catch (ArgumentException)
            {
                Logger.Warn(
                    $"Unable to use shell to access file (ArgumentException) as part of {operation} for {movieFile.FullName}");
            }
            catch (ShellException)
            {
                Logger.Warn($"Unable to use shell to access file as part of {operation} for {movieFile.FullName}");
            }
            catch (PlatformNotSupportedException pe)
            {
                Logger.Error(
                    $"Unable to use shell to access file as part of {operation} for {movieFile.FullName}. Platform is not supported: {pe.Message}");
            }

            MediaInfoWrapper mw = new MediaInfoWrapper(movieFile.FullName);
            int returnVal = meExtractMethod(mw);

            if (returnVal != 0)
            {
                return meParseMethod(returnVal);
            }

            Logger.Warn($"Could not {operation} for {movieFile.FullName} by using MediaInfo.");

            return -1;
        }

        public static int GetFilmLength([NotNull] this FileInfo movieFile)
        {
            return GetMetaDetails(movieFile, o => o.Properties.System.Media.Duration, ParseDuration,
                "GetFilmLength", wrapper => wrapper.Duration, ParseMwDuration);
        }

        private static int ParseMwDuration(int returnVal)
        {
            float ret = returnVal / 1000f;
            return (int)Math.Round(ret);
        }

        private static int ParseDuration(string? duration,FileInfo sourceFile)
        {
            try
            {
                // Duration should be formatted as "00:44:08"
                if (!string.IsNullOrWhiteSpace(duration))
                {
                    string[] timeParts = duration.Split(':');

                    return (int)( int.Parse(timeParts[0]).Hours().TotalSeconds
                                + int.Parse(timeParts[1]).Minutes().TotalSeconds
                                + int.Parse(timeParts[2]).Seconds().TotalSeconds);
                }
            }
            catch (FormatException)
            {
                //Need this section as we get random text back sometimes
                //              Unable to parse string Unbekannt as part of GetFilmLength System
                //              Unable to parse string Text hinzufügen as part of GetFilmLength System
                //              Unable to parse string Add text as part of GetFilmLength System
                //              Unable to parse string Unknown as part of GetFilmLength System
                //              Unable to parse string Okänt as part of GetFilmLength System
                //              Unable to parse string Lägg till text as part of GetFilmLength System

                Logger.Warn($"Unable to parse string '{duration}' as part of GetFilmLength for {sourceFile.FullName}");
            }

            return -1;
        }

        public static bool FileExistsCaseSensitive(string filename)
        {
            string name = Path.GetDirectoryName(filename);

            return name != null
                   && Array.Exists(Directory.GetFiles(name), s => s == Path.GetFullPath(filename));
        }

        public static bool SameDirectoryLocation([NotNull] this string directoryPath1, [NotNull] string directoryPath2)
        {
            // http://stackoverflow.com/questions/1794025/how-to-check-whether-2-directoryinfo-objects-are-pointing-to-the-same-directory
            try
            {
                return string.Compare(directoryPath1.NormalizePath().TrimEnd('\\'),
                           directoryPath2.NormalizePath().TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) ==
                       0;
            }
            catch (UriFormatException e)
            {
                Logger.Warn(e,$"Could not compare {directoryPath1} and {directoryPath2}, assuming they are not the same location");
                return false;
            }
        }

        [NotNull]
        private static string NormalizePath([NotNull] this string path)
        {
            //https://stackoverflow.com/questions/2281531/how-can-i-compare-directory-paths-in-c
            return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();
        }

        [NotNull]
        public static string RemoveExtension([NotNull] this FileInfo file) => RemoveExtension(file, false);

        [NotNull]
        public static string RemoveExtension([NotNull] this FileInfo file, bool useFullPath)
        {
            string root = useFullPath ? file.FullName : file.Name;
            return root.RemoveLast(file.Extension.Length);
        }

        [NotNull]
        public static string GetFilmDetails([NotNull] this FileInfo movieFile)
        {
            using (ShellPropertyCollection properties = new ShellPropertyCollection(movieFile.FullName))
            {
                StringBuilder sb = new StringBuilder();
                foreach (IShellProperty prop in properties)
                {
                    string value = prop.ValueAsObject is null
                        ? ""
                        : prop.FormatForDisplay(PropertyDescriptionFormatOptions.None);

                    sb.AppendLine($"{prop.CanonicalName} = {value}");
                }

                return sb.ToString();
            }
        }

        public static bool IsSubfolderOf(this string thisOne, string ofThat)
        {
            // need terminating slash, otherwise "c:\abc def" will match "c:\abc"
            if (!thisOne.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                thisOne += System.IO.Path.DirectorySeparatorChar.ToString();
            }

            if (!ofThat.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                ofThat += System.IO.Path.DirectorySeparatorChar.ToString();
            }

            int l = ofThat.Length;
            return thisOne.Length >= l && string.Equals(thisOne.Substring(0, l), ofThat, StringComparison.CurrentCultureIgnoreCase);
        }

        [NotNull]
        public static string TrimSlash([NotNull] this string s) // trim trailing slash
        {
            return s.TrimEnd(System.IO.Path.DirectorySeparatorChar);
        }

        // ReSharper disable once InconsistentNaming
        [NotNull]
        public static string GBMB(this long value) => GBMB(value, 2);

        // ReSharper disable once InconsistentNaming
        [NotNull]
        public static string GBMB(this long value, int decimalPlaces)
        {
            const long ONE_KB = 1024;
            const long ONE_MB = ONE_KB * 1024;
            const long ONE_GB = ONE_MB * 1024;
            const long ONE_TB = ONE_GB * 1024;

            double asTb = Math.Round((double)value / ONE_TB, decimalPlaces);
            double asGb = Math.Round((double)value / ONE_GB, decimalPlaces);
            double asMb = Math.Round((double)value / ONE_MB, decimalPlaces);
            double asKb = Math.Round((double)value / ONE_KB, decimalPlaces);
            double asB  = Math.Round((double)value, decimalPlaces);
            string chosenValue = asTb >= 1 ? $"{asTb:G3} TB"
                : asGb >= 1 ? $"{asGb:G3} GB"
                : asMb >= 1 ? $"{asMb:G3} MB"
                : asKb >= 1 ? $"{asKb:G3} KB"
                : $"{asB:G3} B";
            return chosenValue;
        }

        /// <summary>
        /// Gets the properties for this file system.
        /// </summary>
        /// <param name="volumeIdentifier">The path whose volume properties are to be queried.</param>
        /// <returns>A <see cref="FileSystemProperties"/> containing the properties for the specified file system.</returns>
        [NotNull]
        public static FileSystemProperties GetProperties(string volumeIdentifier)
        {
            if (NativeMethods.GetDiskFreeSpaceEx(volumeIdentifier, out ulong available, out ulong total, out ulong free))
            {
                return new FileSystemProperties((long)total, (long)free, (long)available);
            }
            return new FileSystemProperties(null, null, null);
        }

        public static void Rotate(string filenameBase)
        {
            if (File.Exists(filenameBase))
            {
                for (int i = 8; i >= 0; i--)
                {
                    string fn = filenameBase + "." + i;
                    if (File.Exists(fn))
                    {
                        string fn2 = filenameBase + "." + (i + 1);
                        if (File.Exists(fn2))
                        {
                            File.Delete(fn2);
                        }

                        File.Move(fn, fn2);
                    }
                }
                File.Copy(filenameBase, filenameBase + ".0");
            }
        }

        public static bool Same([NotNull] FileInfo a, [NotNull] FileInfo b)
        {
            return string.Compare(a.FullName, b.FullName, StringComparison.OrdinalIgnoreCase) == 0; // true->ignore case
        }

        public static bool Same([NotNull] DirectoryInfo a, [NotNull] DirectoryInfo b)
        {
            string n1 = a.FullName.EnsureEndsWithSeparator();
            string n2 = b.FullName.EnsureEndsWithSeparator();

            return string.Compare(n1, n2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        [NotNull]
        public static string EnsureEndsWithSeparator([NotNull] this string source)
        {
            if (!source.HasValue())
            {
                return Path.DirectorySeparatorChar.ToString();
            }
            if (source.Trim().EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return source.Trim();
            }
            return source.Trim() + Path.DirectorySeparatorChar;
        }

        [NotNull]
        public static FileInfo FileInFolder([NotNull] string dir, string fn) => new FileInfo(dir.EnsureEndsWithSeparator()+ fn);

        [NotNull]
        public static FileInfo FileInFolder([NotNull] DirectoryInfo di, string fn) => FileInFolder(di.FullName, fn);

        // see if showname is somewhere in filename
        public static bool SimplifyAndCheckFilename(string filename, string showName, bool simplifyfilename, bool simplifyshowname)
        {
            return Regex.Match(simplifyfilename ? Helpers.SimplifyName(filename) : filename, "\\b" + (simplifyshowname ? Helpers.SimplifyName(showName) : showName) + "\\b", RegexOptions.IgnoreCase).Success;
        }

        public static bool SimplifyAndCheckFilenameAtStart(string filename, string showName) =>
            SimplifyAndCheckFilenameAtStart(filename, showName,true,true);

        private static bool SimplifyAndCheckFilenameAtStart(string filename, string showName, bool simplifyFilename, bool simplifyShowName)
        {
            string showPattern = simplifyShowName ? Helpers.SimplifyName(showName) : showName;

            return (simplifyFilename ? Helpers.SimplifyName(filename) : filename).StartsWith( showPattern, StringComparison.CurrentCultureIgnoreCase);
        }
        public static bool SimplifyAndCheckFilename(string filename, string showName)
        {
            return SimplifyAndCheckFilename(filename, showName,true,true);
        }

        [NotNull]
        public static string MakeValidPath(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string directoryName = input;
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            foreach (char c in invalid)
            {
                directoryName = directoryName.Replace(c.ToString(), "");
            }
            return directoryName;
        }

        public static bool IgnoreFile([NotNull] this FileInfo fi)
        {
            if (!fi.IsMovieFile())
            {
                return true; // move on
            }

            if (TVSettings.Instance.IgnoreSamples &&
                Helpers.Contains(fi.FullName, "sample", StringComparison.OrdinalIgnoreCase) &&
                fi.Length / (1024 * 1024) < TVSettings.Instance.SampleFileMaxSizeMB)
            {
                return true;
            }

            if (fi.Name.StartsWith("-.", StringComparison.Ordinal) && fi.Length / 1024 < 10)
            {
                return true;
            }

            return false;
        }

        internal static bool FileExistsCaseSensitive([NotNull] IEnumerable<FileInfo> files, FileInfo newFile)
        {
            return files.Any(testFile => string.Equals(testFile.Name, newFile.Name, StringComparison.CurrentCulture));
        }

        public static bool IsValidDirectory(this string directoryName)
        {
            if (directoryName.ContainsAnyCharactersFrom(Path.GetInvalidPathChars()))
            {
                return false;
            }

            if (directoryName.ContainsAnyCharactersFrom("*?"))
            {
                return false;
            }

            return true;
        }
    }
}
