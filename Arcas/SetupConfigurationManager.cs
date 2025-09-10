using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arcas
{
    /// <summary>
    /// Central manager for setup configuration and state
    /// </summary>
    public static class SetupConfigurationManager
    {
        private static SetupDefinition? _definition;
        private static SetupState? _state;
        private static readonly object _lock = new object();

        /// <summary>
        /// Current setup definition (configuration)
        /// </summary>
        public static SetupDefinition Definition
        {
            get
            {
                lock (_lock)
                {
                    _definition ??= LoadDefinition();
                    return _definition;
                }
            }
        }

        /// <summary>
        /// Current runtime state
        /// </summary>
        public static SetupState State
        {
            get
            {
                lock (_lock)
                {
                    _state ??= new SetupState
                    {
                        CurrentArchitecture = DetectCurrentArchitecture(),
                        InstallationPath = GetDefaultInstallPath()
                    };
                    return _state;
                }
            }
        }

        /// <summary>
        /// Reset state (for new installation or testing)
        /// </summary>
        public static void ResetState()
        {
            lock (_lock)
            {
                _state = new SetupState
                {
                    CurrentArchitecture = DetectCurrentArchitecture(),
                    InstallationPath = GetDefaultInstallPath()
                };
            }
        }

        /// <summary>
        /// Load setup definition from various sources
        /// </summary>
        private static SetupDefinition LoadDefinition()
        {
            // Try to load from embedded resource first
            var definition = LoadFromEmbeddedResource();
            if (definition != null) return definition;

            // Try to load from external file
            definition = LoadFromExternalFile();
            if (definition != null) return definition;

            // In debug mode, create a dummy configuration for testing
            #if DEBUG
            definition = CreateDebugConfiguration();
            if (definition != null) return definition;
            #endif

            throw new InvalidOperationException("No setup configuration found. Please provide a setup.json file or embed configuration as a resource.");
        }

        /// <summary>
        /// Load configuration from embedded resource
        /// </summary>
        private static SetupDefinition? LoadFromEmbeddedResource()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(x => x.EndsWith("setup.json", StringComparison.OrdinalIgnoreCase));

                if (resourceName == null) return null;

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return null;

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                
                return JsonSerializer.Deserialize<SetupDefinition>(json, GetJsonOptions());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load embedded configuration: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load configuration from external file
        /// </summary>
        private static SetupDefinition? LoadFromExternalFile()
        {
            try
            {
                var configPaths = new[]
                {
                    "setup.json",
                    "config\\setup.json",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "setup.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "setup.json")
                };

                foreach (var configPath in configPaths)
                {
                    if (File.Exists(configPath))
                    {
                        var json = File.ReadAllText(configPath);
                        return JsonSerializer.Deserialize<SetupDefinition>(json, GetJsonOptions());
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load external configuration: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create debug configuration for testing
        /// </summary>
        #if DEBUG
        private static SetupDefinition CreateDebugConfiguration()
        {
            return new SetupDefinition
            {
                Application = new SetupAppInfo
                {
                    Name = "Arcas Debug Setup",
                    Version = "1.0.0-debug",
                    Publisher = "Debug Publisher",
                    Description = "Debug configuration for testing setup wizard",
                    Website = "https://example.com"
                },
                GlobalSettings = new SetupGlobalSettings
                {
                    DefaultInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Arcas Debug"),
                    AllowCustomInstallPath = true,
                    CreateUninstaller = true,
                    RequireAdministrator = false,
                    MinimumDiskSpace = 50 * 1024 * 1024
                },
                License = new SetupLicenseInfo
                {
                    Title = "Debug License Agreement",
                    Text = GetDebugLicenseText(),
                    Required = true
                },
                Components = new List<SetupComponent>
                {
                    new SetupComponent
                    {
                        Id = "core",
                        Name = "Core Application",
                        Description = "Main application files and core functionality (Required)",
                        Required = true,
                        DefaultSelected = true,
                        SizeBytes = 25 * 1024 * 1024,
                        Commands = new List<SetupCommand>
                        {
                            new SetupCommand
                            {
                                Id = "copy-core-files",
                                Name = "Copy Core Files",
                                Type = SetupCommandType.CopyDirectory,
                                Parameters = new Dictionary<string, object>
                                {
                                    ["Source"] = "bin\\",
                                    ["Destination"] = "{InstallPath}\\",
                                    ["Recursive"] = true
                                }
                            }
                        }
                    },
                    new SetupComponent
                    {
                        Id = "documentation",
                        Name = "Documentation",
                        Description = "User manual, help files, and API documentation",
                        Required = false,
                        DefaultSelected = true,
                        SizeBytes = 15 * 1024 * 1024,
                        Commands = new List<SetupCommand>
                        {
                            new SetupCommand
                            {
                                Id = "copy-docs",
                                Name = "Copy Documentation",
                                Type = SetupCommandType.CopyDirectory,
                                Parameters = new Dictionary<string, object>
                                {
                                    ["Source"] = "docs\\",
                                    ["Destination"] = "{InstallPath}\\Documentation\\",
                                    ["Recursive"] = true
                                }
                            }
                        }
                    },
                    new SetupComponent
                    {
                        Id = "desktop-shortcut",
                        Name = "Desktop Shortcut",
                        Description = "Create a shortcut on the desktop",
                        Required = false,
                        DefaultSelected = true,
                        SizeBytes = 0,
                        Commands = new List<SetupCommand>
                        {
                            new SetupCommand
                            {
                                Id = "create-desktop-shortcut",
                                Name = "Create Desktop Shortcut",
                                Type = SetupCommandType.CreateShortcut,
                                Parameters = new Dictionary<string, object>
                                {
                                    ["TargetPath"] = "{InstallPath}\\{AppName}.exe",
                                    ["ShortcutPath"] = "{Desktop}\\{AppName}.lnk",
                                    ["Description"] = "{AppDescription}",
                                    ["WorkingDirectory"] = "{InstallPath}"
                                }
                            }
                        }
                    },
                    new SetupComponent
                    {
                        Id = "start-menu",
                        Name = "Start Menu Shortcuts",
                        Description = "Create shortcuts in the Start menu",
                        Required = false,
                        DefaultSelected = true,
                        SizeBytes = 0,
                        Commands = new List<SetupCommand>
                        {
                            new SetupCommand
                            {
                                Id = "create-start-menu-folder",
                                Name = "Create Start Menu Folder",
                                Type = SetupCommandType.CreateDirectory,
                                Parameters = new Dictionary<string, object>
                                {
                                    ["Path"] = "{StartMenu}\\Programs\\{AppName}"
                                }
                            },
                            new SetupCommand
                            {
                                Id = "create-start-menu-shortcut",
                                Name = "Create Start Menu Shortcut",
                                Type = SetupCommandType.CreateShortcut,
                                Order = 1,
                                Parameters = new Dictionary<string, object>
                                {
                                    ["TargetPath"] = "{InstallPath}\\{AppName}.exe",
                                    ["ShortcutPath"] = "{StartMenu}\\Programs\\{AppName}\\{AppName}.lnk",
                                    ["Description"] = "{AppDescription}",
                                    ["WorkingDirectory"] = "{InstallPath}"
                                }
                            }
                        }
                    }
                },
                Pages = new List<SetupPageDefinition>
                {
                    new SetupPageDefinition { Id = "welcome", Title = "Welcome", PageType = "Welcome", Order = 1 },
                    new SetupPageDefinition { Id = "license", Title = "License Agreement", PageType = "License", Order = 2 },
                    new SetupPageDefinition { Id = "directory", Title = "Installation Directory", PageType = "Directory", Order = 3 },
                    new SetupPageDefinition { Id = "components", Title = "Select Components", PageType = "Components", Order = 4 },
                    new SetupPageDefinition { Id = "progress", Title = "Installing", PageType = "Progress", Order = 5 },
                    new SetupPageDefinition { Id = "completion", Title = "Setup Complete", PageType = "Completion", Order = 6 }
                },
                Variables = new Dictionary<string, string>
                {
                    ["AppName"] = "Arcas Debug",
                    ["AppDescription"] = "Debug configuration for testing setup wizard"
                }
            };
        }

        private static string GetDebugLicenseText()
        {
            return @"DEBUG LICENSE AGREEMENT

This is a debug license agreement for testing purposes only.

1. GRANT OF LICENSE
This debug version grants you unlimited rights for testing purposes.

2. NO WARRANTIES
This debug version is provided AS-IS for testing only.

3. TESTING PURPOSES
This license is only for testing the setup wizard functionality.

By selecting ""I accept"", you acknowledge this is a debug configuration.";
        }
        #endif

        /// <summary>
        /// Get JSON serialization options
        /// </summary>
        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        /// <summary>
        /// Detect current system architecture
        /// </summary>
        public static SetupArchitecture DetectCurrentArchitecture()
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            return arch switch
            {
                Architecture.X86 => SetupArchitecture.x86,
                Architecture.X64 => SetupArchitecture.x64,
                Architecture.Arm64 => SetupArchitecture.ARM64,
                _ => SetupArchitecture.Any
            };
        }

        /// <summary>
        /// Get default installation path based on current configuration
        /// </summary>
        public static string GetDefaultInstallPath()
        {
            var settings = GetEffectiveSettings();
            if (!string.IsNullOrEmpty(settings.DefaultInstallPath))
            {
                return ExpandVariables(settings.DefaultInstallPath);
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            return Path.Combine(programFiles, Definition.Application.Name);
        }

        /// <summary>
        /// Get effective settings (global + architecture-specific overrides)
        /// </summary>
        public static SetupGlobalSettings GetEffectiveSettings()
        {
            var settings = Definition.GlobalSettings;
            
            // Apply architecture-specific overrides
            if (Definition.ArchitectureSettings.TryGetValue(State.CurrentArchitecture, out var archSettings))
            {
                // Create a copy and apply overrides
                settings = new SetupGlobalSettings
                {
                    DefaultInstallPath = archSettings.DefaultInstallPath ?? settings.DefaultInstallPath,
                    AllowCustomInstallPath = archSettings.AllowCustomInstallPath,
                    CreateUninstaller = archSettings.CreateUninstaller,
                    RequireAdministrator = archSettings.RequireAdministrator,
                    MinimumDiskSpace = archSettings.MinimumDiskSpace,
                    SupportedArchitectures = archSettings.SupportedArchitectures,
                    UninstallerName = archSettings.UninstallerName ?? settings.UninstallerName,
                    AddToControlPanel = archSettings.AddToControlPanel,
                    CreateStartMenuEntries = archSettings.CreateStartMenuEntries,
                    CreateDesktopShortcut = archSettings.CreateDesktopShortcut
                };
            }

            return settings;
        }

        /// <summary>
        /// Get components filtered by architecture and conditions
        /// </summary>
        public static List<SetupComponent> GetAvailableComponents()
        {
            var components = new List<SetupComponent>();

            foreach (var component in Definition.Components)
            {
                // Check architecture compatibility
                if (component.TargetArchitecture != SetupArchitecture.Any && 
                    component.TargetArchitecture != State.CurrentArchitecture)
                    continue;

                // Check conditions
                if (!EvaluateConditions(component.Conditions))
                    continue;

                // Apply architecture-specific overrides
                var effectiveComponent = ApplyArchitectureOverrides(component);
                components.Add(effectiveComponent);
            }

            return components;
        }

        /// <summary>
        /// Apply architecture-specific overrides to a component
        /// </summary>
        private static SetupComponent ApplyArchitectureOverrides(SetupComponent component)
        {
            if (!component.ArchitectureOverrides.TryGetValue(State.CurrentArchitecture, out var overrides))
                return component;

            // Create a copy with overrides applied
            var effectiveComponent = new SetupComponent
            {
                Id = component.Id,
                Name = overrides.Name ?? component.Name,
                Description = overrides.Description ?? component.Description,
                Required = component.Required,
                DefaultSelected = overrides.DefaultSelected ?? component.DefaultSelected,
                SizeBytes = overrides.SizeBytes ?? component.SizeBytes,
                TargetArchitecture = component.TargetArchitecture,
                Dependencies = component.Dependencies,
                Conflicts = component.Conflicts,
                Conditions = component.Conditions,
                Commands = overrides.ReplacementCommands ?? component.Commands.Concat(overrides.AdditionalCommands ?? new List<SetupCommand>()).ToList(),
                ArchitectureOverrides = component.ArchitectureOverrides
            };

            return effectiveComponent;
        }

        /// <summary>
        /// Evaluate conditions for components or commands
        /// </summary>
        public static bool EvaluateConditions(List<SetupCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return true;

            foreach (var condition in conditions)
            {
                var result = EvaluateCondition(condition);
                if (!result)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Evaluate a single condition
        /// </summary>
        private static bool EvaluateCondition(SetupCondition condition)
        {
            bool result = condition.Type.ToLowerInvariant() switch
            {
                "fileexists" => File.Exists(ExpandVariables(condition.Target)),
                "directoryexists" => Directory.Exists(ExpandVariables(condition.Target)),
                "registrykey" => CheckRegistryKey(condition.Target),
                "environmentvariable" => CheckEnvironmentVariable(condition.Target, condition.ExpectedValue),
                "architecture" => State.CurrentArchitecture.ToString().Equals(condition.ExpectedValue, StringComparison.OrdinalIgnoreCase),
                "dryrun" => State.IsDryRun.ToString().Equals(condition.ExpectedValue, StringComparison.OrdinalIgnoreCase),
                _ => true
            };

            return condition.Negate ? !result : result;
        }

        /// <summary>
        /// Check if registry key exists
        /// </summary>
        private static bool CheckRegistryKey(string keyPath)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check environment variable
        /// </summary>
        private static bool CheckEnvironmentVariable(string variable, string expectedValue)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrEmpty(expectedValue))
                return !string.IsNullOrEmpty(value);

            return string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Expand variables in strings
        /// </summary>
        public static string ExpandVariables(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = input;

            // Expand built-in variables
            result = result.Replace("{InstallPath}", State.InstallationPath);
            result = result.Replace("{AppName}", Definition.Application.Name);
            result = result.Replace("{AppVersion}", Definition.Application.Version);
            result = result.Replace("{AppDescription}", Definition.Application.Description);
            result = result.Replace("{AppPublisher}", Definition.Application.Publisher);
            result = result.Replace("{Desktop}", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            result = result.Replace("{StartMenu}", Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));
            result = result.Replace("{ProgramFiles}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            result = result.Replace("{SystemRoot}", Environment.GetFolderPath(Environment.SpecialFolder.System));
            result = result.Replace("{TempPath}", Path.GetTempPath());

            // Expand custom variables
            foreach (var variable in Definition.Variables)
            {
                result = result.Replace($"{{{variable.Key}}}", variable.Value);
            }

            // Expand resolved variables
            foreach (var variable in State.ResolvedVariables)
            {
                result = result.Replace($"{{{variable.Key}}}", variable.Value);
            }

            return result;
        }

        /// <summary>
        /// Get enabled pages based on configuration and conditions
        /// </summary>
        public static List<SetupPageDefinition> GetEnabledPages()
        {
            return Definition.Pages
                .Where(p => p.Enabled && EvaluateConditions(p.ShowConditions))
                .OrderBy(p => p.Order)
                .ToList();
        }

        /// <summary>
        /// Log a message to the setup log
        /// </summary>
        public static void Log(SetupLogLevel level, string message, string? commandId = null, string? componentId = null, Exception? exception = null)
        {
            State.Log.Add(new SetupLogEntry
            {
                Level = level,
                Message = message,
                CommandId = commandId,
                ComponentId = componentId,
                Exception = exception
            });

            // Also write to debug output
            System.Diagnostics.Debug.WriteLine($"[{level}] {message}");
        }
    }
}