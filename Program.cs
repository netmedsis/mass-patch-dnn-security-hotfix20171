using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MassPatchDnnSecurityHotFix20171
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                return;
            }

            string websitesRootFolder = args[0];
            string hotfixFolder = string.Format(@"{0}\_DnnSecurityHotFix20171", websitesRootFolder);
            string backupRootFolder = string.Format(@"{0}\Backup-{1}", hotfixFolder, DateTime.Now.ToString("yyyy-MM-dd"));
            string logfile = Path.Combine(hotfixFolder, "logfile.txt");

            string webConfig = "web.config";
            var filesForBackup = new List<string>()
                {
                    webConfig,
                    "Telerik.Web.UI.dll",
                    "Telerik.Web.UI.Skins.dll"
                };


            using (var cc = new ConsoleCopy(logfile))
            {
                Console.WriteLine("********************************************************************************");
                Console.WriteLine(string.Format("***** START             : {0}", DateTime.Now));
                Console.WriteLine(string.Format("***** websitesRootFolder: {0}", websitesRootFolder));
                Console.WriteLine(string.Format("***** hotfixFolder      : {0}", hotfixFolder));
                Console.WriteLine(string.Format("***** backupRootFolder  : {0}", backupRootFolder));
                Console.WriteLine(string.Format("***** logfile           : {0}", logfile));
                Console.WriteLine("********************************************************************************");
                Console.WriteLine(" ");

                foreach (var topFolder in GetTopFolders(websitesRootFolder))
                {

                    Console.WriteLine("********************************************************************************");
                    Console.WriteLine("*** FOLDER: " + topFolder);
                    Console.WriteLine("********************************************************************************");

                    try
                    {
                        List<Dll> dllovi = new List<Dll>();

                        foreach (string file in filesForBackup)
                        {
                            FindTargetFileAndBackupAll(file, topFolder, websitesRootFolder, backupRootFolder, ref dllovi);
                        }

                        foreach (Dll dll in dllovi)
                        {
                            if (dll.PacthStatus == PacthStatus.ForPatching)
                            {
                                // TODO: patch
                                string patchFile = Path.Combine(hotfixFolder, Path.GetFileName(dll.FullName));
                                try
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("................................................................................");
                                    Console.WriteLine("patch file     : " + dll.FullName);

                                    File.Copy(patchFile, dll.FullName, true);
                                    dll.PacthStatus = PacthStatus.Patched;
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    Console.WriteLine(" - error       : UnauthorizedAccessException");
                                    FileInfo fileInfo = new FileInfo(dll.FullName);
                                    if (fileInfo.IsReadOnly)
                                    {
                                        fileInfo.IsReadOnly = false;
                                        Console.WriteLine(" - remove readonly");
                                        File.Copy(patchFile, dll.FullName, true);
                                        dll.PacthStatus = PacthStatus.Patched;
                                    }
                                    else
                                    {
                                        Console.Beep();
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine(" - patching failed :(");
                                        //Console.ResetColor();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.Beep();
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(" - error       : " + ex.Message);
                                    Console.WriteLine(" - patching failed :(");
                                    //Console.ResetColor();
                                    //throw;
                                }

                                Console.WriteLine(" - patch status: " + dll.PacthStatus);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("................................................................................");
                                Console.ResetColor();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Beep();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(" - error       : " + ex.Message);
                        Console.ResetColor();
                        //throw;
                    }
                }

                Console.WriteLine(" ");
                Console.WriteLine("********************************************************************************");
                Console.WriteLine(string.Format("***** END: {0}", DateTime.Now));
                Console.WriteLine("********************************************************************************");

                //Console.WriteLine("Press any key...");
                //Console.ReadKey();
            }
        }

        private static void FindTargetFileAndBackupAll(string targetFile, string topFolder, string websitesRootFolder, string backupRootFolder, ref List<Dll> dllovi)
        {
            List<string> files = GetAllFoldersWithFile(topFolder, targetFile);

            foreach (string file in files)
            {
                string backupFile = file.Replace(websitesRootFolder, backupRootFolder);
                string backupFolder = Path.GetDirectoryName(backupFile);
                Directory.CreateDirectory(backupFolder);

                Console.WriteLine("backup file    : " + file);

                if (Path.GetExtension(file) == ".dll")
                {
                    Dll dll = new Dll();
                    dll.FullName = file;
                    dll.Version = GetAssemblyVersion(file);

                    if (dll.Version == "2013.2.717.40")
                    {
                        dll.PacthStatus = PacthStatus.ForPatching;
                    }
                    else
                    {
                        dll.PacthStatus = PacthStatus.NotCompatible;
                    }

                    Console.WriteLine(" - version     : " + dll.Version);
                    Console.WriteLine(" - patch status: " + dll.PacthStatus);

                    dllovi.Add(dll);
                }

                try
                {
                    File.Copy(file, backupFile, true);
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine(" - error       : UnauthorizedAccessException");
                    FileInfo fileInfo = new FileInfo(backupFile);
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                        Console.WriteLine(" - remove readonly");
                        File.Copy(file, backupFile, true);
                    }
                    else
                    {
                        Console.Beep();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(" - backup failed :(");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    Console.Beep();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(" - error       : " + ex.Message);
                    Console.WriteLine(" - backup failed :(");
                    Console.ResetColor();
                    //throw;
                }
            }
        }

        static private List<string> GetTopFolders(string startFolder)
        {
            List<string> folders = 
                new DirectoryInfo(startFolder)
                    .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                    .Where(d => (d.Name.IndexOf("_") != 0))
                    .Select(d => d.FullName).ToList();

            return folders;
        }

        static private List<string> GetAllFoldersWithFile(string startFolder, string fileName)
        {
            List<string> folders =
                new DirectoryInfo(startFolder)
                .EnumerateFiles(fileName, SearchOption.AllDirectories)
                .Select(d => d.FullName).ToList();

            return folders;
        }

        static private string GetFileVersion(string fullName)
        {
            return FileVersionInfo.GetVersionInfo(fullName).FileVersion;
        }

        static private string GetAssemblyVersion(string fullName)
        {
            return AssemblyName.GetAssemblyName(fullName).Version.ToString();
        }

        enum PacthStatus
        {
            NotCompatible = 0,
            ForPatching = 1,
            Patched = 2,
        }

        class Dll
        {
            public string FullName { get; set; }
            public string Version { get; set; }
            public PacthStatus PacthStatus { get; set; }
        }
    }
}
