# Arcas Setup Wizard

This project implements a comprehensive, **highly configurable** setup wizard using Windows Forms and .NET 8. The setup wizard is designed to be **reusable** for any application through external configuration.

## ✅ **Completed Flexible Configuration System**

### **New Architecture (Highly Configurable)**
- **SetupDefinition**: External JSON configuration defining the entire setup
- **SetupState**: Runtime state management (not persisted)
- **SetupConfigurationManager**: Central configuration and state management
- **SetupCommandExecutor**: Flexible command execution system
- **SetupTypes**: All type definitions and enums

### **Key Features**

#### **🔧 Highly Configurable**
- **Application Information**: Name, version, publisher, description, website
- **Global Settings**: Install paths, permissions, disk space requirements
- **Architecture-Specific Settings**: x86, x64, ARM64 specific overrides
- **Modular License**: Optional license agreement page (can be disabled)
- **Custom Variables**: Expandable variables in commands and paths
- **Page Management**: Configurable page flow and conditional display

#### **📦 Flexible Component System**
- **Component Definitions**: Name, description, size, dependencies, conflicts
- **Architecture Support**: Per-architecture component availability
- **Conditional Installation**: Components with conditions (file exists, registry, etc.)
- **Architecture Overrides**: Different names/sizes/commands per architecture

#### **⚡ Advanced Command System**
- **Multiple Command Types**: Copy files/directories, create shortcuts, registry operations, run executables, environment variables, file associations, services
- **Command Timing**: PreInstall, Install, PostInstall, Uninstall phases
- **Parameter System**: Flexible parameters with variable expansion
- **Success Criteria**: Configurable success validation
- **Conditional Execution**: Commands with conditions
- **Dry Run Support**: Debug mode simulation

#### **🏗️ Future-Proof Design**
- **Plugin Architecture**: Custom command types can be added
- **JSON Configuration**: Easy to modify without recompilation
- **Embedded Resources**: Configuration can be embedded in executable
- **External Files**: Configuration can be loaded from external JSON files
- **Debug Configuration**: Automatic dummy configuration for testing

#### **🎛️ Debug Features**
- **Dry Run Mode**: Simulate installation without actual execution
- **Debug Configuration**: Automatic test configuration in debug builds
- **Comprehensive Logging**: Detailed operation logging with levels
- **Error Handling**: Robust error tracking and reporting

### **Setup Pages**
1. **Welcome Page** - Dynamic application branding
2. **License Agreement Page** - Configurable license (optional)
3. **Installation Directory Page** - Configurable default paths + **Debug Dry Run Option**
4. **Component Selection Page** - Dynamic components from configuration
5. **Installation Progress Page** - Real command execution with logging
6. **Completion Page** - Success confirmation with launch option

### **Configuration Sources (Priority Order)**
1. **Embedded Resource**: `setup.json` embedded in executable
2. **External File**: `setup.json` in application directory or config folder
3. **Debug Mode**: Automatic test configuration (debug builds only)

### **Usage Examples**

#### **Basic Setup Configuration (setup.json)**
```json
{
  "application": {
    "name": "MyApp",
    "version": "1.0.0",
    "publisher": "My Company",
    "description": "My awesome application"
  },
  "globalSettings": {
    "defaultInstallPath": "C:\\Program Files\\MyApp",
    "minimumDiskSpace": 104857600,
    "requireAdministrator": false
  },
  "license": {
    "title": "End User License Agreement",
    "textFilePath": "license.txt",
    "required": true
  },
  "components": [
    {
      "id": "core",
      "name": "Core Application",
      "description": "Main application files",
      "required": true,
      "defaultSelected": true,
      "sizeBytes": 52428800,
      "commands": [
        {
          "type": "CopyDirectory",
          "name": "Copy Application Files",
          "parameters": {
            "source": "app\\",
            "destination": "{InstallPath}\\",
            "recursive": true
          }
        }
      ]
    }
  ]
}
```

#### **Architecture-Specific Overrides**
```json
{
  "architectureSettings": {
    "x64": {
      "defaultInstallPath": "C:\\Program Files\\MyApp x64",
      "minimumDiskSpace": 209715200
    },
    "x86": {
      "defaultInstallPath": "C:\\Program Files (x86)\\MyApp",
      "minimumDiskSpace": 104857600
    }
  }
}
```

#### **Complex Command Examples**
```json
{
  "commands": [
    {
      "type": "CreateShortcut",
      "name": "Create Desktop Shortcut",
      "parameters": {
        "targetPath": "{InstallPath}\\MyApp.exe",
        "shortcutPath": "{Desktop}\\{AppName}.lnk",
        "description": "{AppDescription}",
        "workingDirectory": "{InstallPath}"
      }
    },
    {
      "type": "WriteRegistry",
      "name": "Register Application",
      "parameters": {
        "keyPath": "SOFTWARE\\MyCompany\\MyApp",
        "valueName": "InstallPath",
        "value": "{InstallPath}",
        "valueType": "String"
      }
    }
  ]
}
```

### **Variable Expansion**
Built-in variables: `{InstallPath}`, `{AppName}`, `{AppVersion}`, `{Desktop}`, `{StartMenu}`, `{ProgramFiles}`, etc.
Custom variables can be defined in configuration.

### **Migration from Old System**
- ✅ Old `SetupConfiguration` class is now a compatibility wrapper
- ✅ Runtime state is no longer persisted (configuration comes from external sources)
- ✅ All pages updated to use new configuration system
- ✅ Setup wizard automatically detects and uses available configuration sources

This creates a **professional, enterprise-grade** setup wizard that can be easily customized for any application without code changes, supporting complex installation scenarios with architecture-specific handling and flexible command execution.

## 🎨 **UI Design System**
- **Unified Design Language**: Consistent colors, typography, and spacing
- **Windows-Native Appearance**: Professional installer look and feel
- **Responsive Layouts**: Proper docking instead of absolute positioning
- **Modern Typography**: Clear hierarchy with Segoe UI font family
- **Accessibility**: High contrast colors and readable fonts