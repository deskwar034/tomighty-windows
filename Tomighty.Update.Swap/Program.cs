using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using Tomighty.Windows;

namespace Tomighty.Update.Swap
{
    static class Program
    {
        private static readonly Logger logger = new Logger("swap");

        [STAThread]
        static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length != 5)
            {
                logger.Error($"Wrong number of arguments: {args.Length}, exiting now");
                return;
            }

            var processId = args[1];
            var exePath = args[2];
            var sourcePackage = args[3];
            var restart = args[4];

            if (!IsInteger(processId))
            {
                logger.Error($"Invalid process id: {processId}");
                return;
            }

            if (!File.Exists(exePath))
            {
                logger.Error($"File not found: {exePath}");
                return;
            }

            if (!File.Exists(sourcePackage))
            {
                logger.Error($"File not found: {sourcePackage}");
                return;
            }

            var targetDir = Path.GetDirectoryName(exePath);

            try
            {
                Swap(int.Parse(processId), exePath, sourcePackage, targetDir, restart == "true");
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        private static void Swap(int processId, string exePath, string sourcePackage, string targetDir, bool restart)
        {
            try
            {
                logger.Info($"Waiting for process {processId} to exit");
                Process.GetProcessById(processId).WaitForExit();

                logger.Info($"Process {processId} has exited");
            }
            catch (ArgumentException)
            {
                logger.Info($"Process {processId} doesn't exist, probably exited");
            }

            Thread.Sleep(500);
            var stagedDir = Path.Combine(Path.GetTempPath(), "tomighty-swap-stage-" + Guid.NewGuid().ToString("N"));
            var backupDir = Path.Combine(Path.GetTempPath(), "tomighty-swap-backup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stagedDir);
            Directory.CreateDirectory(backupDir);

            try
            {
                logger.Info($"Extracting files from `{sourcePackage}` into `{stagedDir}`");
                ZipFile.ExtractToDirectory(sourcePackage, stagedDir);
                ValidateExtractedInstallation(stagedDir, exePath);

                logger.Info($"Backing up current installation from `{targetDir}`");
                MoveDirectoryContents(targetDir, backupDir);

                try
                {
                    logger.Info($"Applying extracted files into `{targetDir}`");
                    MoveDirectoryContents(stagedDir, targetDir);
                }
                catch (Exception applyEx)
                {
                    logger.Error($"Failed to apply update, attempting rollback: {applyEx.Message}");
                    TryRestoreBackup(backupDir, targetDir);
                    throw;
                }
            }
            finally
            {
                TryDeleteDirectory(stagedDir);
                TryDeleteDirectory(backupDir);
            }

            if (restart)
            {
                logger.Info($"Starting process {exePath}");
                Process.Start(exePath);
            }

            StartupEventFlags.Flags.TurnOn(StartupEventFlags.AppUpdatedFlag);

            logger.Info($"Done");
        }

        private static void MoveDirectoryContents(string fromDir, string toDir)
        {
            foreach (var file in Directory.GetFiles(fromDir))
            {
                var targetFile = Path.Combine(toDir, Path.GetFileName(file));
                if (File.Exists(targetFile))
                    File.Delete(targetFile);
                File.Move(file, targetFile);
            }

            foreach (var dir in Directory.GetDirectories(fromDir))
            {
                var targetPath = Path.Combine(toDir, Path.GetFileName(dir));
                if (Directory.Exists(targetPath))
                    Directory.Delete(targetPath, true);
                Directory.Move(dir, targetPath);
            }
        }

        private static void ValidateExtractedInstallation(string stagedDir, string exePath)
        {
            var expectedExeName = Path.GetFileName(exePath);
            var expectedExeInRoot = Path.Combine(stagedDir, expectedExeName);
            var matchingFiles = Directory.GetFiles(stagedDir, expectedExeName, SearchOption.AllDirectories);

            if (!File.Exists(expectedExeInRoot) && matchingFiles.Length == 0)
            {
                throw new InvalidOperationException($"Invalid update package: `{expectedExeName}` not found in extracted files.");
            }
        }

        private static void TryRestoreBackup(string backupDir, string targetDir)
        {
            try
            {
                var failedDir = targetDir + ".failed-" + Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(failedDir);
                MoveDirectoryContents(targetDir, failedDir);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to move partial target files before rollback: {ex.Message}");
            }

            MoveDirectoryContents(backupDir, targetDir);
            logger.Info("Rollback completed");
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to delete temporary directory `{path}`: {ex.Message}");
            }
        }

        private static bool IsInteger(string s)
        {
            return s != null && Regex.IsMatch(s, @"^\d+$");
        }
    }
}
