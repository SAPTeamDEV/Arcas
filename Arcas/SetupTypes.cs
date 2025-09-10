using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Arcas
{
    /// <summary>
    /// Architecture-specific enumeration for platform targeting
    /// </summary>
    public enum SetupArchitecture
    {
        Any,
        x86,
        x64,
        ARM64
    }

    /// <summary>
    /// Types of setup commands that can be executed
    /// </summary>
    public enum SetupCommandType
    {
        CopyFile,
        CopyDirectory,
        CreateShortcut,
        CreateDirectory,
        WriteRegistry,
        DeleteRegistry,
        RunExecutable,
        RunShellCommand,
        ExtractArchive,
        SetEnvironmentVariable,
        CreateFileAssociation,
        InstallService,
        UninstallService,
        Custom
    }

    /// <summary>
    /// When a command should be executed during the installation process
    /// </summary>
    public enum SetupCommandTiming
    {
        PreInstall,
        Install,
        PostInstall,
        Uninstall
    }

    /// <summary>
    /// Installation status enumeration
    /// </summary>
    public enum SetupStatus
    {
        NotStarted,
        Initializing,
        PreInstallation,
        Installing,
        PostInstallation,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Log levels for setup operations
    /// </summary>
    public enum SetupLogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Types of setup errors
    /// </summary>
    public enum SetupErrorType
    {
        Configuration,
        Validation,
        Permission,
        DiskSpace,
        FileSystem,
        Registry,
        Network,
        Dependency,
        Command,
        Unknown
    }

    /// <summary>
    /// Represents a single setup command with all its parameters
    /// </summary>
    public class SetupCommand
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public SetupCommandType Type { get; set; }
        public SetupCommandTiming Timing { get; set; } = SetupCommandTiming.Install;
        public bool Required { get; set; } = true;
        public int Order { get; set; } = 0;
        public bool RunAsAdmin { get; set; } = false;
        public SetupArchitecture TargetArchitecture { get; set; } = SetupArchitecture.Any;
        
        /// <summary>
        /// Command parameters - flexible dictionary for different command types
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        /// <summary>
        /// Conditions that must be met for this command to execute
        /// </summary>
        public List<SetupCondition> Conditions { get; set; } = new();
        
        /// <summary>
        /// Success criteria for determining if the command executed successfully
        /// </summary>
        public SetupSuccessCriteria? SuccessCriteria { get; set; }
    }

    /// <summary>
    /// Represents a condition that must be met for a command or component to be processed
    /// </summary>
    public class SetupCondition
    {
        public string Type { get; set; } = ""; // FileExists, RegistryKeyExists, EnvironmentVariable, etc.
        public string Target { get; set; } = "";
        public string ExpectedValue { get; set; } = "";
        public bool Negate { get; set; } = false;
    }

    /// <summary>
    /// Criteria for determining command success
    /// </summary>
    public class SetupSuccessCriteria
    {
        public int? ExpectedExitCode { get; set; }
        public string? ExpectedOutputContains { get; set; }
        public string? ExpectedFileExists { get; set; }
        public string? ExpectedRegistryValue { get; set; }
    }

    /// <summary>
    /// Represents an installable component with its associated commands
    /// </summary>
    public class SetupComponent
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Required { get; set; } = false;
        public bool DefaultSelected { get; set; } = false;
        public long SizeBytes { get; set; } = 0;
        public SetupArchitecture TargetArchitecture { get; set; } = SetupArchitecture.Any;
        public List<string> Dependencies { get; set; } = new(); // Component IDs this depends on
        public List<string> Conflicts { get; set; } = new(); // Component IDs this conflicts with
        public List<SetupCondition> Conditions { get; set; } = new();
        public List<SetupCommand> Commands { get; set; } = new();
        
        /// <summary>
        /// Architecture-specific overrides
        /// </summary>
        public Dictionary<SetupArchitecture, SetupComponentOverride> ArchitectureOverrides { get; set; } = new();
    }

    /// <summary>
    /// Architecture-specific overrides for components
    /// </summary>
    public class SetupComponentOverride
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? DefaultSelected { get; set; }
        public long? SizeBytes { get; set; }
        public List<SetupCommand>? AdditionalCommands { get; set; }
        public List<SetupCommand>? ReplacementCommands { get; set; }
    }

    /// <summary>
    /// Represents a setup page configuration
    /// </summary>
    public class SetupPageDefinition
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public int Order { get; set; } = 0;
        public string PageType { get; set; } = ""; // Welcome, License, Directory, Components, Progress, Completion, Custom
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<SetupCondition> ShowConditions { get; set; } = new();
    }

    /// <summary>
    /// Main setup definition - this is the configuration that defines the entire setup
    /// </summary>
    public class SetupDefinition
    {
        /// <summary>
        /// Application information
        /// </summary>
        public SetupAppInfo Application { get; set; } = new();
        
        /// <summary>
        /// Global setup settings
        /// </summary>
        public SetupGlobalSettings GlobalSettings { get; set; } = new();
        
        /// <summary>
        /// Architecture-specific settings that override global settings
        /// </summary>
        public Dictionary<SetupArchitecture, SetupGlobalSettings> ArchitectureSettings { get; set; } = new();
        
        /// <summary>
        /// License information (null if no license page needed)
        /// </summary>
        public SetupLicenseInfo? License { get; set; }
        
        /// <summary>
        /// Available components for installation
        /// </summary>
        public List<SetupComponent> Components { get; set; } = new();
        
        /// <summary>
        /// Page definitions and ordering
        /// </summary>
        public List<SetupPageDefinition> Pages { get; set; } = new();
        
        /// <summary>
        /// Global commands that run regardless of component selection
        /// </summary>
        public List<SetupCommand> GlobalCommands { get; set; } = new();
        
        /// <summary>
        /// Custom variables that can be used in command parameters
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new();
    }

    /// <summary>
    /// Application information
    /// </summary>
    public class SetupAppInfo
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string Description { get; set; } = "";
        public string Website { get; set; } = "";
        public string SupportUrl { get; set; } = "";
        public string IconPath { get; set; } = "";
        public Guid ApplicationId { get; set; } = Guid.NewGuid();
    }

    /// <summary>
    /// Global setup settings
    /// </summary>
    public class SetupGlobalSettings
    {
        public string DefaultInstallPath { get; set; } = "";
        public bool AllowCustomInstallPath { get; set; } = true;
        public bool CreateUninstaller { get; set; } = true;
        public bool RequireAdministrator { get; set; } = false;
        public long MinimumDiskSpace { get; set; } = 50 * 1024 * 1024; // 50 MB
        public List<string> SupportedArchitectures { get; set; } = new() { "Any" };
        public string UninstallerName { get; set; } = "uninstall.exe";
        public bool AddToControlPanel { get; set; } = true;
        public bool CreateStartMenuEntries { get; set; } = true;
        public bool CreateDesktopShortcut { get; set; } = false;
    }

    /// <summary>
    /// License information
    /// </summary>
    public class SetupLicenseInfo
    {
        public string Title { get; set; } = "License Agreement";
        public string Text { get; set; } = "";
        public string TextFilePath { get; set; } = ""; // Path to license text file
        public bool Required { get; set; } = true;
        public string AcceptText { get; set; } = "I accept the terms in the License Agreement";
        public string DeclineText { get; set; } = "I do not accept the terms in the License Agreement";
    }

    /// <summary>
    /// Runtime state of the setup process - this is NOT persisted to disk
    /// </summary>
    public class SetupState
    {
        /// <summary>
        /// Selected installation path
        /// </summary>
        public string InstallationPath { get; set; } = "";
        
        /// <summary>
        /// Whether the license was accepted (if license page is enabled)
        /// </summary>
        public bool LicenseAccepted { get; set; } = false;
        
        /// <summary>
        /// IDs of selected components
        /// </summary>
        public HashSet<string> SelectedComponentIds { get; set; } = new();
        
        /// <summary>
        /// Current architecture being installed for
        /// </summary>
        public SetupArchitecture CurrentArchitecture { get; set; } = SetupArchitecture.Any;
        
        /// <summary>
        /// When the installation process started
        /// </summary>
        public DateTime InstallationStartTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Current installation status
        /// </summary>
        public SetupStatus Status { get; set; } = SetupStatus.NotStarted;
        
        /// <summary>
        /// Whether this is a dry run (debug mode only)
        /// </summary>
        public bool IsDryRun { get; set; } = false;
        
        /// <summary>
        /// Installation progress (0-100)
        /// </summary>
        public int Progress { get; set; } = 0;
        
        /// <summary>
        /// Current operation being performed
        /// </summary>
        public string CurrentOperation { get; set; } = "";
        
        /// <summary>
        /// Log of installation operations
        /// </summary>
        public List<SetupLogEntry> Log { get; set; } = new();
        
        /// <summary>
        /// Resolved variables for this installation
        /// </summary>
        public Dictionary<string, string> ResolvedVariables { get; set; } = new();
        
        /// <summary>
        /// Executed commands and their results
        /// </summary>
        public List<SetupCommandResult> CommandResults { get; set; } = new();
        
        /// <summary>
        /// Errors encountered during installation
        /// </summary>
        public List<SetupError> Errors { get; set; } = new();
        
        /// <summary>
        /// Whether installation was completed successfully
        /// </summary>
        public bool InstallationCompleted { get; set; } = false;
        
        /// <summary>
        /// Whether user chose to launch application after installation
        /// </summary>
        public bool LaunchAfterInstall { get; set; } = true;
    }

    /// <summary>
    /// Log entry for installation operations
    /// </summary>
    public class SetupLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public SetupLogLevel Level { get; set; } = SetupLogLevel.Info;
        public string Message { get; set; } = "";
        public string? CommandId { get; set; }
        public string? ComponentId { get; set; }
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// Result of a setup command execution
    /// </summary>
    public class SetupCommandResult
    {
        public string CommandId { get; set; } = "";
        public string ComponentId { get; set; } = "";
        public SetupCommandType CommandType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public int? ExitCode { get; set; }
        public string Output { get; set; } = "";
        public string ErrorOutput { get; set; } = "";
        public Exception? Exception { get; set; }
        public bool WasSkipped { get; set; } = false;
        public string SkipReason { get; set; } = "";
    }

    /// <summary>
    /// Error information during setup
    /// </summary>
    public class SetupError
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public SetupErrorType Type { get; set; }
        public string Message { get; set; } = "";
        public string? ComponentId { get; set; }
        public string? CommandId { get; set; }
        public Exception? Exception { get; set; }
        public bool IsFatal { get; set; } = false;
    }

    /// <summary>
    /// Progress information for setup operations
    /// </summary>
    public class SetupProgressInfo
    {
        public string Operation { get; set; } = "";
        public int Percentage { get; set; }
        public string? Detail { get; set; }
    }

    /// <summary>
    /// Command with execution context
    /// </summary>
    internal class CommandWithContext
    {
        public SetupCommand Command { get; set; } = null!;
        public string? ComponentId { get; set; }
    }
}