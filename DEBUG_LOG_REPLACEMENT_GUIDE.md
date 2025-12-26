# Debug.Log Replacement Guide

## Summary
This document provides a complete mapping for replacing all `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` calls in the StepQuest codebase with appropriate `Logger` calls using the correct categories.

## Files Already Completed ✓

### Dialogue/Social Files (Logger.LogCategory.DialogueLog)
- ✓ Assets/Scripts/Gameplay/Dialogue/DialogueManager.cs
- ✓ Assets/Scripts/UI/Panels/DialoguePanelUI.cs
- ✓ Assets/Scripts/UI/Panels/NPCInteractionPanel.cs
- ✓ Assets/Scripts/UI/Components/SocialAvatarCard.cs
- ✓ Assets/Scripts/UI/Panels/SocialSectionPanel.cs

### Inventory Files (Logger.LogCategory.InventoryLog)
- ✓ Assets/Scripts/UI/Panels/InventoryPanelUI.cs
- ✓ Assets/Scripts/UI/Panels/EquipmentPanelUI.cs
- ✓ Assets/Scripts/UI/Components/ItemActionPanel.cs
- ✓ Assets/Scripts/UI/Components/DraggedItemVisual.cs

### Activity Files - Partial (Logger.LogCategory.ActivityLog)
- ✓ Assets/Scripts/UI/Panels/ActivitiesSectionPanel.cs
- ⚠️ Assets/Scripts/UI/Panels/ActivityVariantsPanel.cs (partially complete)

## Files Still Needing Replacement

### 1. Activity Files (Logger.LogCategory.ActivityLog)
```
Assets/Scripts/UI/Panels/ActivityVariantsPanel.cs
  - Line 132: Debug.Log → Logger.LogInfo
  - Line 158: Debug.LogError → Logger.LogError
  - Line 176: Debug.Log → Logger.LogInfo
  - Line 187: Debug.LogError → Logger.LogError
  - Line 203: Debug.LogError → Logger.LogError
  - Line 216: Debug.Log → Logger.LogInfo
  - Line 229: Debug.Log → Logger.LogInfo
  - Line 234: Debug.Log → Logger.LogInfo
  - Line 240: Debug.Log → Logger.LogInfo
  - Line 244: Debug.LogWarning → Logger.LogWarning
  - Line 290: Debug.Log → Logger.LogInfo

Assets/Scripts/UI/Panels/VariantContainer.cs
  - Line 70: Debug.LogWarning → Logger.LogWarning
  - Line 92: Debug.Log → Logger.LogInfo
  - Line 165: Debug.LogError → Logger.LogError

Assets/Scripts/UI/Panels/VariantIconContainer.cs
  - Line 383: Debug.Log → Logger.LogInfo

Assets/Scripts/UI/Panels/IconContainer.cs
  - Line 195: Debug.LogWarning → Logger.LogWarning

Assets/Scripts/UI/Components/PrimaryActivityCard.cs
  - All Debug calls → Logger with ActivityLog category

Assets/Scripts/UI/Components/HarvestingActivityCard.cs
  - All Debug calls → Logger with ActivityLog category

Assets/Scripts/UI/Components/CraftingActivityCard.cs
  - All Debug calls → Logger with ActivityLog category
```

### 2. Combat Files (Logger.LogCategory.CombatLog)
```
Assets/Scripts/UI/Panels/CombatSectionPanel.cs
  - Line 53: Debug.LogWarning → Logger.LogWarning
  - Line 92: Debug.Log → Logger.LogInfo
  - Line 145: Debug.LogError → Logger.LogError
  - Line 151: Debug.LogError → Logger.LogError
  - Line 157: Debug.LogError → Logger.LogError
  - Line 168: Debug.LogWarning → Logger.LogWarning
  - Line 226: Debug.LogError → Logger.LogError
  - Line 240: Debug.Log → Logger.LogInfo

Assets/Scripts/UI/Components/EnemyCard.cs
  - All Debug calls → Logger with CombatLog category
```

### 3. XP/Progression Files (Logger.LogCategory.XpLog)
```
Assets/Scripts/Gameplay/Progression/XpEventHandler.cs
  - All Debug calls → Logger with XpLog category
```

### 4. Map Files (Logger.LogCategory.MapLog)
```
Assets/Scripts/Gameplay/World/POI.cs
  - All Debug calls → Logger with MapLog category

Assets/Scripts/Utils/MapToggleButton.cs
  - All Debug calls → Logger with MapLog category
```

### 5. UI Files (Logger.LogCategory.UILog)
```
Assets/Scripts/UI/Panels/ErrorPanel.cs
  - Line 154: Debug.LogWarning → Logger.LogWarning
```

### 6. Data/Registry Files (Logger.LogCategory.DataLog)
```
Assets/Scripts/Data/Registry/StatusEffectRegistry.cs
  - All Debug calls → Logger with DataLog category

Assets/Scripts/Data/Registry/AbilityRegistry.cs
  - All Debug calls → Logger with DataLog category
```

### 7. Debug Files (Logger.LogCategory.EditorLog)
```
Assets/Scripts/Debug/EquipmentDebugger.cs
Assets/Scripts/Debug/InventoryDataDebugger.cs
Assets/Scripts/Debug/PlayerDataDebugger.cs
Assets/Scripts/Debug/ActivityRegistryDebugger.cs
  - All Debug calls → Logger with EditorLog category
```

### 8. Editor Files (Logger.LogCategory.EditorLog)
```
Assets/Scripts/Editor/AbilityDebugWindow.cs
Assets/Scripts/Editor/AbilityManagerWindow.cs
Assets/Scripts/Editor/ActivityManagerWindow.cs
Assets/Scripts/Editor/ActivityVariantEditor.cs
Assets/Scripts/Editor/CombatContentCreator.cs
Assets/Scripts/Editor/CombatUIBuilder.cs
Assets/Scripts/Editor/CombatUIWirer.cs
Assets/Scripts/Editor/ConnectionManagerWindow.cs
Assets/Scripts/Editor/EditorStepSimulator.cs
Assets/Scripts/Editor/EnemyManagerWindow.cs
Assets/Scripts/Editor/GameDataResetter.cs
Assets/Scripts/Editor/ItemManagerWindow.cs
Assets/Scripts/Editor/NPCManagerWindow.cs
Assets/Scripts/Editor/POIEditor.cs
Assets/Scripts/Editor/PrefabScaleNormalizer.cs
Assets/Scripts/Editor/RegistryValidationDashboard.cs
Assets/Scripts/Editor/SampleDialogueCreator.cs
Assets/Scripts/Editor/StatusEffectCreator.cs
Assets/Scripts/Editor/StatusEffectManagerWindow.cs
Assets/Scripts/Editor/TestAbilityCreator.cs
  - All Debug calls → Logger with EditorLog category
```

## Replacement Pattern

### Basic Pattern
```csharp
// Before:
Debug.Log("Message");
Debug.LogWarning("Warning message");
Debug.LogError("Error message");

// After:
Logger.LogInfo("Message", Logger.LogCategory.XXX);
Logger.LogWarning("Warning message", Logger.LogCategory.XXX);
Logger.LogError("Error message", Logger.LogCategory.XXX);
```

### With String Interpolation
```csharp
// Before:
Debug.Log($"Found {count} items");

// After:
Logger.LogInfo($"Found {count} items", Logger.LogCategory.XXX);
```

### With Context Parameter (preserve as last parameter)
```csharp
// Before:
Debug.Log("Message", gameObject);

// After:
Logger.LogInfo("Message", Logger.LogCategory.XXX, gameObject);
```

## Files to SKIP (Already Using Logger)
- Assets/Scripts/Utils/Logger.cs
- Assets/Scripts/Utils/LoggerInitializer.cs
- Assets/Scripts/Utils/LoggerSettings.cs
- Assets/Scripts/Debug/LoggerDebugPanel.cs
- Assets/Scripts/Editor/LoggerControlWindow.cs

## Total Progress
- **Files Completed**: ~13/56
- **Files Remaining**: ~43/56
- **Percentage Complete**: ~23%

## Notes
- The Logger infrastructure files should keep their Debug.Log calls for internal debugging
- All LeanTween and TutorialInfo files should be excluded
- After replacement, test each category to ensure logs appear correctly in the LoggerDebugPanel
