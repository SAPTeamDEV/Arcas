using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using IWshRuntimeLibrary;
using System.IO.Compression;

namespace Arcas
{
    /// <summary>
    /// Executes setup commands with support for different command types
    /// </summary>
    public class SetupCommandExecutor
    {
        private readonly SetupState _state;
        private readonly IProgress<SetupProgressInfo>? _progress;

        public SetupCommandExecutor(SetupState state, IProgress<SetupProgressInfo>? progress = null)
        {
            _state = state;
            _progress = progress;
        }

        /// <summary>
        /// Execute all commands for selected components
        /// </summary>
        public async Task<bool> ExecuteInstallationAsync()
        {
            try
            {
                _state.Status = SetupStatus.Initializing;
                SetupConfigurationManager.Log(SetupLogLevel.Info, "Starting installation process");

                // Get all commands to execute
                var commands = GetCommandsToExecute();
                var totalCommands = commands.Count;
                var completedCommands = 0;

                ReportProgress("Initializing installation...", 0);

                _state.Status = SetupStatus.PreInstallation;

                // Execute pre-install commands
                var preInstallCommands = commands.Where(c => c.Command.Timing == SetupCommandTiming.PreInstall).OrderBy(c => c.Command.Order).ToList();
                foreach (var cmd in preInstallCommands)
                {
                    if (!await ExecuteCommandAsync(cmd))
                    {
                        _state.Status = SetupStatus.Failed;
                        return false;
                    }
                    completedCommands++;
                    ReportProgress($"Pre-installation: {cmd.Command.Name}", (completedCommands * 100) / totalCommands);
                }

                _state.Status = SetupStatus.Installing;

                // Execute main install commands
                var installCommands = commands.Where(c => c.Command.Timing == SetupCommandTiming.Install).OrderBy(c => c.Command.Order).ToList();
                foreach (var cmd in installCommands)
                {
                    if (!await ExecuteCommandAsync(cmd))
                    {
                        _state.Status = SetupStatus.Failed;
                        return false;
                    }
                    completedCommands++;
                    ReportProgress($"Installing: {cmd.Command.Name}", (completedCommands * 100) / totalCommands);
                }

                _state.Status = SetupStatus.PostInstallation;

                // Execute post-install commands
                var postInstallCommands = commands.Where(c => c.Command.Timing == SetupCommandTiming.PostInstall).OrderBy(c => c.Command.Order).ToList();
                foreach (var cmd in postInstallCommands)
                {
                    if (!await ExecuteCommandAsync(cmd))
                    {
                        _state.Status = SetupStatus.Failed;
                        return false;
                    }
                    completedCommands++;
                    ReportProgress($"Post-installation: {cmd.Command.Name}", (completedCommands * 100) / totalCommands);
                }

                _state.Status = SetupStatus.Completed;
                _state.InstallationCompleted = true;
                ReportProgress("Installation completed successfully!", 100);

                SetupConfigurationManager.Log(SetupLogLevel.Info, "Installation completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _state.Status = SetupStatus.Failed;
                SetupConfigurationManager.Log(SetupLogLevel.Critical, "Installation failed", exception: ex);
                _state.Errors.Add(new SetupError
                {
                    Type = SetupErrorType.Unknown,
                    Message = ex.Message,
                    Exception = ex,
                    IsFatal = true
                });
                return false;
            }
        }

        /// <summary>
        /// Get all commands that need to be executed
        /// </summary>
        private List<CommandWithContext> GetCommandsToExecute()
        {
            var commands = new List<CommandWithContext>();

            // Add global commands
            foreach (var command in SetupConfigurationManager.Definition.GlobalCommands)
            {
                if (SetupConfigurationManager.EvaluateConditions(command.Conditions))
                {
                    commands.Add(new CommandWithContext { Command = command, ComponentId = null });
                }
            }

            // Add component commands
            var availableComponents = SetupConfigurationManager.GetAvailableComponents();
            foreach (var component in availableComponents.Where(c => _state.SelectedComponentIds.Contains(c.Id)))
            {
                foreach (var command in component.Commands)
                {
                    if (SetupConfigurationManager.EvaluateConditions(command.Conditions))
                    {
                        commands.Add(new CommandWithContext { Command = command, ComponentId = component.Id });
                    }
                }
            }

            return commands.OrderBy(c => c.Command.Timing).ThenBy(c => c.Command.Order).ToList();
        }

        /// <summary>
        /// Execute a single command
        /// </summary>
        private async Task<bool> ExecuteCommandAsync(CommandWithContext cmdContext)
        {
            var command = cmdContext.Command;
            var result = new SetupCommandResult
            {
                CommandId = command.Id,
                ComponentId = cmdContext.ComponentId ?? "",
                CommandType = command.Type,
                StartTime = DateTime.Now
            };

            try
            {
                SetupConfigurationManager.Log(SetupLogLevel.Info, $"Executing command: {command.Name}", command.Id, cmdContext.ComponentId);

                if (_state.IsDryRun)
                {
                    SetupConfigurationManager.Log(SetupLogLevel.Info, $"DRY RUN: Would execute {command.Type} command", command.Id, cmdContext.ComponentId);
                    result.Success = true;
                    result.WasSkipped = true;
                    result.SkipReason = "Dry run mode";
                }
                else
                {
                    result.Success = await ExecuteCommandByTypeAsync(command);
                }

                result.EndTime = DateTime.Now;
                _state.CommandResults.Add(result);

                if (!result.Success && command.Required)
                {
                    SetupConfigurationManager.Log(SetupLogLevel.Error, $"Required command failed: {command.Name}", command.Id, cmdContext.ComponentId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
                result.EndTime = DateTime.Now;
                _state.CommandResults.Add(result);

                SetupConfigurationManager.Log(SetupLogLevel.Error, $"Command execution failed: {command.Name} - {ex.Message}", command.Id, cmdContext.ComponentId, ex);

                if (command.Required)
                {
                    return false;
                }

                return true; // Non-required command failure is not fatal
            }
        }

        /// <summary>
        /// Execute command based on its type
        /// </summary>
        private async Task<bool> ExecuteCommandByTypeAsync(SetupCommand command)
        {
            return command.Type switch
            {
                SetupCommandType.CopyFile => await ExecuteCopyFileAsync(command),
                SetupCommandType.CopyDirectory => await ExecuteCopyDirectoryAsync(command),
                SetupCommandType.CreateShortcut => await ExecuteCreateShortcutAsync(command),
                SetupCommandType.CreateDirectory => await ExecuteCreateDirectoryAsync(command),
                SetupCommandType.WriteRegistry => await ExecuteWriteRegistryAsync(command),
                SetupCommandType.DeleteRegistry => await ExecuteDeleteRegistryAsync(command),
                SetupCommandType.RunExecutable => await ExecuteRunExecutableAsync(command),
                SetupCommandType.RunShellCommand => await ExecuteRunShellCommandAsync(command),
                SetupCommandType.ExtractArchive => await ExecuteExtractArchiveAsync(command),
                SetupCommandType.SetEnvironmentVariable => await ExecuteSetEnvironmentVariableAsync(command),
                SetupCommandType.CreateFileAssociation => await ExecuteCreateFileAssociationAsync(command),
                SetupCommandType.InstallService => await ExecuteInstallServiceAsync(command),
                SetupCommandType.UninstallService => await ExecuteUninstallServiceAsync(command),
                SetupCommandType.Custom => await ExecuteCustomCommandAsync(command),
                _ => throw new NotSupportedException($"Command type {command.Type} is not supported")
            };
        }

        #region Command Implementations

        private async Task<bool> ExecuteCopyFileAsync(SetupCommand command)
        {
            var source = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Source"));
            var destination = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Destination"));
            var overwrite = GetParameter(command, "Overwrite", true);

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Copying file from '{source}' to '{destination}'");

            // Ensure destination directory exists
            var destDir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            await Task.Run(() => System.IO.File.Copy(source, destination, overwrite));
            return true;
        }

        private async Task<bool> ExecuteCopyDirectoryAsync(SetupCommand command)
        {
            var source = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Source"));
            var destination = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Destination"));
            var recursive = GetParameter(command, "Recursive", true);

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Copying directory from '{source}' to '{destination}'");

            await Task.Run(() => CopyDirectory(source, destination, recursive));
            return true;
        }

        private async Task<bool> ExecuteCreateShortcutAsync(SetupCommand command)
        {
            var targetPath = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "TargetPath"));
            var shortcutPath = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "ShortcutPath"));
            var description = SetupConfigurationManager.ExpandVariables(GetParameter(command, "Description", ""));
            var workingDirectory = SetupConfigurationManager.ExpandVariables(GetParameter(command, "WorkingDirectory", ""));

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Creating shortcut '{shortcutPath}' -> '{targetPath}'");

            await Task.Run(() => CreateShortcut(targetPath, shortcutPath, description, workingDirectory));
            return true;
        }

        private async Task<bool> ExecuteCreateDirectoryAsync(SetupCommand command)
        {
            var path = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Path"));

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Creating directory '{path}'");

            await Task.Run(() => Directory.CreateDirectory(path));
            return true;
        }

        private async Task<bool> ExecuteWriteRegistryAsync(SetupCommand command)
        {
            var keyPath = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "KeyPath"));
            var valueName = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "ValueName"));
            var value = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Value"));
            var valueType = GetParameter(command, "ValueType", "String");

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Writing registry value '{keyPath}\\{valueName}' = '{value}'");

            await Task.Run(() => WriteRegistryValue(keyPath, valueName, value, valueType));
            return true;
        }

        private async Task<bool> ExecuteDeleteRegistryAsync(SetupCommand command)
        {
            var keyPath = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "KeyPath"));
            var valueName = SetupConfigurationManager.ExpandVariables(GetParameter(command, "ValueName", ""));

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Deleting registry '{keyPath}\\{valueName}'");

            await Task.Run(() => DeleteRegistryValue(keyPath, valueName));
            return true;
        }

        private async Task<bool> ExecuteRunExecutableAsync(SetupCommand command)
        {
            var executable = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Executable"));
            var arguments = SetupConfigurationManager.ExpandVariables(GetParameter(command, "Arguments", ""));
            var waitForExit = GetParameter(command, "WaitForExit", true);
            var workingDirectory = SetupConfigurationManager.ExpandVariables(GetParameter(command, "WorkingDirectory", ""));

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Running executable '{executable}' with arguments '{arguments}'");

            return await RunProcessAsync(executable, arguments, workingDirectory, waitForExit);
        }

        private async Task<bool> ExecuteRunShellCommandAsync(SetupCommand command)
        {
            var commandText = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Command"));
            var waitForExit = GetParameter(command, "WaitForExit", true);

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Running shell command '{commandText}'");

            return await RunProcessAsync("cmd.exe", $"/c {commandText}", "", waitForExit);
        }

        private async Task<bool> ExecuteExtractArchiveAsync(SetupCommand command)
        {
            var archivePath = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "ArchivePath"));
            var destination = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Destination"));

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Extracting archive '{archivePath}' to '{destination}'");

            await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, destination));
            return true;
        }

        private async Task<bool> ExecuteSetEnvironmentVariableAsync(SetupCommand command)
        {
            var name = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Name"));
            var value = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Value"));
            var target = GetParameter(command, "Target", "User");

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Setting environment variable '{name}' = '{value}'");

            var envTarget = target.ToLowerInvariant() switch
            {
                "machine" => EnvironmentVariableTarget.Machine,
                "user" => EnvironmentVariableTarget.User,
                _ => EnvironmentVariableTarget.Process
            };

            await Task.Run(() => Environment.SetEnvironmentVariable(name, value, envTarget));
            return true;
        }

        private async Task<bool> ExecuteCreateFileAssociationAsync(SetupCommand command)
        {
            var extension = GetParameter<string>(command, "Extension");
            var progId = GetParameter<string>(command, "ProgId");
            var description = SetupConfigurationManager.ExpandVariables(GetParameter(command, "Description", ""));
            var executable = SetupConfigurationManager.ExpandVariables(GetParameter<string>(command, "Executable"));

            SetupConfigurationManager.Log(SetupLogLevel.Debug, $"Creating file association '{extension}' -> '{progId}'");

            await Task.Run(() => CreateFileAssociation(extension, progId, description, executable));
            return true;
        }

        private async Task<bool> ExecuteInstallServiceAsync(SetupCommand command)
        {
            // This would require additional service installation logic
            SetupConfigurationManager.Log(SetupLogLevel.Warning, "Service installation not implemented in this example");
            await Task.Delay(100); // Simulate work
            return true;
        }

        private async Task<bool> ExecuteUninstallServiceAsync(SetupCommand command)
        {
            // This would require additional service uninstallation logic
            SetupConfigurationManager.Log(SetupLogLevel.Warning, "Service uninstallation not implemented in this example");
            await Task.Delay(100); // Simulate work
            return true;
        }

        private async Task<bool> ExecuteCustomCommandAsync(SetupCommand command)
        {
            // Custom commands would be implemented by derived classes or plugins
            SetupConfigurationManager.Log(SetupLogLevel.Warning, $"Custom command '{command.Name}' not implemented");
            await Task.Delay(100); // Simulate work
            return true;
        }

        #endregion

        #region Helper Methods

        private T GetParameter<T>(SetupCommand command, string key)
        {
            if (command.Parameters.TryGetValue(key, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            throw new ArgumentException($"Required parameter '{key}' not found in command '{command.Name}'");
        }

        private T GetParameter<T>(SetupCommand command, string key, T defaultValue)
        {
            if (command.Parameters.TryGetValue(key, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            return defaultValue;
        }

        private void CopyDirectory(string sourceDir, string destDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                file.CopyTo(Path.Combine(destDir, file.Name), true);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    CopyDirectory(subDir.FullName, Path.Combine(destDir, subDir.Name), true);
                }
            }
        }

        private void CreateShortcut(string targetPath, string shortcutPath, string description, string workingDirectory)
        {
            var shell = new WshShell();
            var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.Description = description;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.Save();
        }

        private void WriteRegistryValue(string keyPath, string valueName, string value, string valueType)
        {
            using var key = Registry.LocalMachine.CreateSubKey(keyPath);
            var regValueKind = valueType.ToLowerInvariant() switch
            {
                "dword" => RegistryValueKind.DWord,
                "qword" => RegistryValueKind.QWord,
                "binary" => RegistryValueKind.Binary,
                _ => RegistryValueKind.String
            };
            key?.SetValue(valueName, value, regValueKind);
        }

        private void DeleteRegistryValue(string keyPath, string valueName)
        {
            using var key = Registry.LocalMachine.OpenSubKey(keyPath, true);
            if (string.IsNullOrEmpty(valueName))
            {
                Registry.LocalMachine.DeleteSubKey(keyPath, false);
            }
            else
            {
                key?.DeleteValue(valueName, false);
            }
        }

        private async Task<bool> RunProcessAsync(string fileName, string arguments, string workingDirectory, bool waitForExit)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            if (waitForExit)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }

            return true;
        }

        private void CreateFileAssociation(string extension, string progId, string description, string executable)
        {
            // Register the ProgID
            using var progIdKey = Registry.ClassesRoot.CreateSubKey(progId);
            progIdKey?.SetValue("", description);

            using var commandKey = Registry.ClassesRoot.CreateSubKey($"{progId}\\shell\\open\\command");
            commandKey?.SetValue("", $"\"{executable}\" \"%1\"");

            // Associate the extension
            using var extKey = Registry.ClassesRoot.CreateSubKey(extension);
            extKey?.SetValue("", progId);
        }

        private void ReportProgress(string operation, int percentage)
        {
            _state.CurrentOperation = operation;
            _state.Progress = percentage;
            
            _progress?.Report(new SetupProgressInfo
            {
                Operation = operation,
                Percentage = percentage
            });
        }

        #endregion
    }
}