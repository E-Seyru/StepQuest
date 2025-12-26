# Logger System - Usage Guide

## Overview

Your StepQuest project now has a professional, categorized logging system that solves the "too many logs" problem by allowing you to filter by category and log level.

## Categories

The following log categories are available:

| Category | Used For |
|----------|----------|
| **General** | General game flow, uncategorized systems |
| **StepLog** | Step counting, RecordingAPI, step tracking |
| **MapLog** | Map system, POI, travel, pathfinding |
| **CombatLog** | Combat system, enemies, abilities, status effects |
| **InventoryLog** | Inventory, equipment, items |
| **ActivityLog** | Activities (harvesting, crafting), activity progression |
| **UILog** | General UI panels and components |
| **DialogueLog** | NPC dialogues, conversations, social interactions |
| **XpLog** | XP gains, skill progression, level-ups |
| **DataLog** | Data persistence, database, save/load, registries |
| **EditorLog** | Editor tools, debug windows, validators |

## How to Use

### In Editor (Recommended for Development)

1. Open **Tools > Logger Control**
2. Use quick preset buttons to focus on specific systems:
   - Click **"Combat"** to see only combat logs
   - Click **"Activity"** to see only activity logs
   - Click **"All Categories"** to see everything
   - Click **"No Categories"** to disable all logs
3. Adjust log level (Debug, Info, Warning, Error)
4. Toggle individual categories as needed
5. Settings automatically persist between sessions

**Important**: When you uncheck all categories, logs will be completely disabled until you re-enable at least one category. This is useful for temporarily silencing all logs.

### At Runtime (For Device Testing)

1. Add `LoggerDebugPanel` component to a UI Canvas
2. Press **F12** to toggle the debug panel (configurable)
3. Control same settings as editor window

### Via LoggerSettings ScriptableObject

1. Create: **Assets > Create > Logger Settings**
2. Configure your preferred defaults
3. Attach `LoggerInitializer` to a GameObject in your startup scene
4. Assign the LoggerSettings asset

### Via Code

```csharp
// Focus on combat logs only
Logger.EnableCategoryFiltering(Logger.LogCategory.CombatLog);

// Focus on multiple categories
Logger.EnableCategoryFiltering(
    Logger.LogCategory.ActivityLog,
    Logger.LogCategory.XpLog
);

// See everything again
Logger.DisableCategoryFiltering();

// Change log level
Logger.SetLogLevel(Logger.LogLevel.Debug);

// Disable all logging
Logger.SetEnabled(false);

// Check if a category is enabled
if (Logger.IsCategoryEnabled(Logger.LogCategory.CombatLog))
{
    // Do expensive debug work
}
```

## Writing Logs

Use the appropriate method based on severity:

```csharp
// Debug - Detailed development info (hidden in production)
Logger.LogDebug("Detailed trace info", Logger.LogCategory.CombatLog);

// Info - General informational messages
Logger.LogInfo("Activity started successfully", Logger.LogCategory.ActivityLog);

// Warning - Something unexpected but recoverable
Logger.LogWarning("Missing optional component", Logger.LogCategory.UILog);

// Error - Something went wrong
Logger.LogError("Failed to load data!", Logger.LogCategory.DataLog);
```

## Build Behavior

- **Editor builds**: Default to `LogLevel.Info` (shows Debug and Info)
- **Development builds**: Default to `LogLevel.Info`
- **Production builds**: Default to `LogLevel.Warning` (only Warnings and Errors)

This can be customized in LoggerSettings.

## Performance Tips

1. **Use category filtering** instead of lowering log level when debugging - this keeps important warnings/errors visible from all systems
2. **Don't do expensive work** before the log call - check `IsCategoryEnabled()` first if needed
3. **Use appropriate log levels** - too many Info logs defeat the purpose

## Common Workflows

### Debugging Combat Issues
1. Open **Tools > Logger Control**
2. Click **"Combat"** preset
3. Set log level to **Debug**
4. Enter play mode and test combat

### Debugging Activity System
1. Click **"Activity"** preset in Logger Control
2. You'll now see only activity-related logs
3. Errors/warnings from other systems still appear

### Preparing for Production Build
1. Verify LoggerSettings has `productionLogLevel = Warning`
2. Test with production settings by manually setting `Logger.SetLogLevel(LogLevel.Warning)`
3. Ensure critical errors still appear

### Debugging on Device
1. Build with Development Build flag enabled
2. Use LoggerDebugPanel (F12) to adjust categories at runtime
3. Use `adb logcat` to view logs from device

## Examples from Your Codebase

```csharp
// Activity logging
Logger.LogInfo($"Starting activity: {variant.VariantName}", Logger.LogCategory.ActivityLog);

// Combat logging
Logger.LogInfo($"Combat started vs {enemy.GetDisplayName()}", Logger.LogCategory.CombatLog);

// Step tracking
Logger.LogInfo($"Steps updated: {steps}", Logger.LogCategory.StepLog);

// Dialogue system
Logger.LogInfo($"Starting dialogue: {dialogue.DialogueID}", Logger.LogCategory.DialogueLog);

// XP/Progression
Logger.LogInfo($"Level up! {skillName} is now level {newLevel}", Logger.LogCategory.XpLog);

// Data operations
Logger.LogError("Failed to save player data!", Logger.LogCategory.DataLog);
```

## Persistence

Logger settings are automatically saved to PlayerPrefs and persist between sessions:
- Log level
- Enabled/disabled state
- Category filtering state
- Which categories are enabled

This means your logging configuration survives:
- Entering/exiting play mode
- Closing/reopening Unity
- Building to device

To reset to defaults, use the **"Reset to Defaults"** button in the Logger Control window.

## Troubleshooting

**Q: I don't see any logs!**
- Check that logger is enabled: `Logger.SetEnabled(true)`
- Check log level isn't too restrictive
- Check if category filtering is active and excluding your category
- If ALL categories are unchecked, nothing will log (by design)

**Q: Too many logs in console!**
- Use category filtering to focus on one system at a time
- Raise the log level to Warning to see only issues
- Uncheck "All Categories" to silence everything temporarily

**Q: Logs disappeared after filtering!**
- Click "All Categories" in Logger Control to reset
- Or call `Logger.DisableCategoryFiltering()` in code
- Or click "Reset to Defaults" to restore default settings

**Q: Settings don't persist / keep resetting!**
- This should now work automatically via PlayerPrefs
- If issues persist, click "Reset to Defaults" and reconfigure

**Q: I unchecked everything but still see logs!**
- This is now fixed - when all categories are unchecked, no logs will appear
- Use this to completely silence logging when needed

**Q: How do I see logs on Android device?**
- Use `adb logcat -s Unity` to view Unity logs
- Or attach LoggerDebugPanel to your UI and use F12

## Migration Complete

All 1,170+ Debug.Log calls across 56 files have been replaced with Logger calls using appropriate categories. The codebase is now using a professional logging system!
