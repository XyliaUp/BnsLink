using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


namespace BnsLink.Util
{
	public class Compress
    {
        public static readonly string InternalDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);


        public enum ParseState
        {
            Command,
            ArchiveFileName,
            FileNames
        }

        public enum ArchiveAction
        {
            None,
            Add,
            Extract,
            ExtractFull
        }

        static ArchiveAction archiveAction = ArchiveAction.None;

        static bool finished = false;
        static bool overwrite = false;

        public string password = null;

        /// 文件<summary>
        /// 存储路径
        /// </summary>
        public string archiveFileName = string.Empty;

        /// <summary>
        /// 输出文件夹路径
        /// </summary>
        public string outputPath = string.Empty;


        public List<string> fileNames = new List<string>();

        static DateTime timer;



       
        public void CompressFiles()
        {
            if (!fileNames.Any())
            {
                fileNames.Add(Directory.GetCurrentDirectory());
            }

            using (var archive = new pdj.tiny7z.Archive.SevenZipArchive(File.Create(archiveFileName), FileAccess.Write))

            using (var compressor = archive.Compressor())
            {
                compressor.CompressHeader = true;
                compressor.PreserveDirectoryStructure = true;
                //compressor.ProgressDelegate = ProgressEvent;
                compressor.Solid = true;

                foreach (var fn in fileNames)
                {
                    try
                    {
                        if (fn.IndexOfAny(new[] { '?', '*' }) != -1)
                        {
                            string path = Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(fn));
                            string pattern = Path.GetFileName(fn);

                            foreach (var file in Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly))
                            {
                                //Log.Information("Compressing {File}...", Path.GetFileName(file));
                                compressor.AddFile(file);
                            }
                        }
                        else
                        {
                            if (File.Exists(fn))
                            {
                                //Log.Information("Compressing {File}...", Path.GetFileName(fn));
                                compressor.AddFile(fn);
                            }
                            else if (Directory.Exists(fn))
                            {
                                var info = new DirectoryInfo(fn);
                                //Log.Information("Compressing contents of {Directory}...", Path.GetFileName(info.Name));
                                compressor.AddDirectory(info.FullName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log.Error(ex, "There was an error while building file list.");
                    }
                }

                var now = DateTime.Now;
                try
                {
                    compressor.Finalize();
                    Console.WriteLine();
                }
                catch (pdj.tiny7z.Archive.SevenZipFileAlreadyExistsException ex)
                {
                    //Log.Error(ex, "Output archive filename already exists.");
                }
                catch (Exception ex)
                {
                    //Log.Error(ex, "There was an error while compressing files.");
                }

                //Log.Information("Compression took {ela}ms.", DateTime.Now.Subtract(now).TotalMilliseconds);
            }
        }


        public void ExtractFiles(bool preserveDirectoryStructure,Action<int> act)
        {
            using (var archive = new pdj.tiny7z.Archive.SevenZipArchive(File.OpenRead(archiveFileName), FileAccess.Read))

            using (var extractor = archive.Extractor())
            {
                extractor.OverwriteExistingFiles = overwrite;
                extractor.Password = password;
                extractor.PreserveDirectoryStructure = preserveDirectoryStructure;
                extractor.SkipExistingFiles = false;

                if (string.IsNullOrWhiteSpace(outputPath)) outputPath = Directory.GetCurrentDirectory();


                var now = DateTime.Now;
                if (!fileNames.Any())
                {
                    extractor.ExtractArchive(outputPath, act);
                }
                else
                {
                    extractor.ExtractFiles(fileNames.ToArray(), outputPath);
                }
            }
        }



        public bool ProcessCommandLine(string[] args)
        {
            ParseState state = ParseState.Command;
            bool error = false;
            for (int i = 0; i < args.Length && !error; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("-"))
                {
                    switch (arg.ToLowerInvariant())
                    {
                        case "-o":
                            if (++i == args.Length)
                            {
                                //Log.Error("Invalid -o parameter. Missing output path.");
                                error = true;
                            }
                            else
                            {
                                outputPath = args[i];
                                if (!Directory.Exists(outputPath))
                                {
                                    //Log.Information("Creating output path: {OutputPath}", outputPath);
                                    Directory.CreateDirectory(outputPath);
                                }
                                else
                                {
                                    //Log.Information("Using \"{OutputPath}\" as output path.", outputPath);
                                }
                            }
                            break;

                        case "-p":
                            if (++i == args.Length)
                            {
                                //Log.Error("Invalid -p parameter. Missing password.");
                                error = true;
                            }
                            else
                            {
                                password = args[i];
                                //Log.Information("Using \"{Password}\" to decrypt pdj.tiny7z.Archive.", password);
                            }
                            break;

                        case "-x":
                            //Log.Information("-x Overwrite output archive or extracted files if it already exists.");
                            overwrite = true;
                            break;

                        default:
                            //Log.Error("Invalid parameter: {Arg}", arg);
                            error = true;
                            break;
                    }
                }
                else
                {
                    switch (state)
                    {
                        case ParseState.Command:
                            if (arg.Length == 1)
                            {
                                switch (arg.ToLower()[0])
                                {
                                    case 'a':
                                        //Log.Information("a  Adding files to pdj.tiny7z.Archive.");
                                        archiveAction = ArchiveAction.Add;
                                        break;
                                    case 'e':
                                        //Log.Information("e  Extracting files from pdj.tiny7z.Archive.");
                                        archiveAction = ArchiveAction.Extract;
                                        break;
                                    case 'x':
                                        //Log.Information("x  Extracting files from archive (full path).");
                                        archiveAction = ArchiveAction.ExtractFull;
                                        break;
                                    default:
                                        //Log.Error("Invalid command: {Arg}", arg);
                                        error = true;
                                        break;
                                }
                                state = ParseState.ArchiveFileName;
                            }
                            else
                            {
                                //Log.Error("Invalid command: {Arg}", arg);
                                error = true;
                            }
                            break;

                        case ParseState.ArchiveFileName:
                            archiveFileName = arg;
                            if (!archiveFileName.ToLowerInvariant().EndsWith(".7z"))
                            {
                                archiveFileName += ".7z";
                            }
                            //Log.Information("Using archive filename: {FileName}", archiveFileName);
                            state = ParseState.FileNames;
                            break;

                        case ParseState.FileNames:
                            //Log.Information("Including \"{Arg}\" in file list.", arg);
                            fileNames.Add(arg);
                            break;
                    }
                }
            }

            if (!error)
            {
                if (archiveAction == ArchiveAction.None)
                {
                    //Log.Error("No action requested.");
                    error = true;
                }
                else if (string.IsNullOrWhiteSpace(archiveFileName))
                {
                    //Log.Error("No archive file specified.");
                    error = true;
                }
                else if (archiveAction == ArchiveAction.Add && !overwrite && File.Exists(archiveFileName))
                {
                    //Log.Error("Archive filename \"{FileName}\" already exists. Cannot overwrite.", archiveFileName);
                    error = true;
                }
            }

            Console.WriteLine("");
            return !error;
        }
    }
}
