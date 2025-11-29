# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

StepQuest is a Unity mobile RPG (Android) that uses real-world step counting as a core game mechanic. Players walk in real life to travel between locations on a world map and perform activities (mining, gathering, crafting, fishing).

**Unity Version**: Uses Unity 2022+ with Mobile 2D template
**Target Platform**: Android (uses Health Connect API for step counting)
**Language**: C# with .NET Standard 2.1

## Architecture

### Core Managers (Singleton Pattern)
All managers use `Instance` singleton pattern and are initialized at game start:

- **GameManager** (`Assets/Scripts/Core/GameManager.cs`) - Central state machine managing game states (Loading, Idle, Traveling, DoingActivity, InCombat, Paused)
- **DataManager** (`Assets/Scripts/Data/DataManager.cs`) - Facade for all data operations with internal services for database, validation, and save/load
- **MapManager** (`Assets/Scripts/Gameplay/World/MapManager.cs`) - Handles world map, locations, pathfinding, and travel logic
- **ActivityManager** (`Assets/Scripts/Gameplay/Player/ActivityManager.cs`) - Manages step-based and time-based activities (harvesting, crafting)
- **StepManager** (`Assets/Scripts/Services/StepTracking/StepManager.cs`) - Handles step counting via Health Connect API on device, simplified polling in editor
- **InventoryManager** (`Assets/Scripts/Gameplay/Player/InventoryManager.cs`) - Manages player inventory and equipment

### Event System
The codebase uses a custom **EventBus** (`Assets/Scripts/Core/Events/EventBus.cs`) for decoupled communication:

```csharp
// Subscribe
EventBus.Subscribe<TravelStartedEvent>(OnTravelStarted);

// Publish
EventBus.Publish(new TravelStartedEvent(destinationId, currentLocation, stepCost));

// Unsubscribe (in OnDestroy)
EventBus.Unsubscribe<TravelStartedEvent>(OnTravelStarted);
```

Event types are defined in `Assets/Scripts/Core/Events/GameEvents.cs` under namespaces:
- `GameEvents` - Game state changes
- `MapEvents` - Travel and location events
- `ActivityEvents` - Activity progress and completion

### Data Layer

**ScriptableObjects** define game content:
- `MapLocationDefinition` - Location data (ID, display name, connections)
- `ActivityDefinition` - Activity types (mining, crafting, etc.)
- `ActivityVariant` - Specific variants with resources and costs
- `ItemDefinition` - Item data

**Registries** aggregate ScriptableObjects:
- `LocationRegistry` - All map locations and connections
- `ActivityRegistry` - All activities and their variants
- `ItemRegistry` - All items

**PlayerData** (`Assets/Scripts/Data/Models/PlayerData.cs`) stores:
- Step counts (TotalSteps, DailySteps)
- Current location and travel state
- Active activity state
- Inventory and equipment

**LocalDatabase** uses SQLite for persistent storage on device.

### Service Pattern
Managers delegate to internal service classes for separation of concerns:
- `DataManagerDatabaseService`, `DataManagerValidationService`, `DataManagerSaveLoadService`
- `MapTravelService`, `MapLocationService`, `MapValidationService`, `MapEventService`, `MapSaveService`
- `ActivityExecutionService`, `ActivityProgressService`, `ActivityTimeService`, `ActivityPersistenceService`

### UI Architecture
- **UIManager** coordinates UI panels
- **PanelManager** handles panel navigation
- **AboveCanvasManager** manages overlay UI with services for events, animations, and display

## Key Game Mechanics

### Travel System
- Travel costs are measured in steps
- Pathfinding supports multi-segment routes between non-adjacent locations
- Travel state persists across app close/open
- Progress tracked via `PlayerData.TravelStartSteps` and `TravelRequiredSteps`

### Activity System
- **Step-based activities**: Progress by walking (harvesting resources)
- **Time-based activities**: Progress by real-time (crafting)
- Activities can loop automatically if materials are available
- Offline progress is processed on app resume

### Step Counting
- **On Device**: Uses Android Health Connect Recording API + direct sensor
- **In Editor**: Simplified mode using `EditorStepSimulator` to manually add steps

### Combat System (NEW - Added Nov 2024)
Auto-battler combat system where abilities trigger automatically on cooldown.

**Core Components:**
- **CombatManager** (`Assets/Scripts/Gameplay/Combat/CombatManager.cs`) - Singleton managing combat with coroutine-based ability cycling
- **AbilityDefinition** (`Assets/Scripts/Data/ScriptableObjects/AbilityDefinition.cs`) - ScriptableObject defining abilities (Damage, Heal, Poison, Shield effects)
- **EnemyDefinition** (`Assets/Scripts/Data/ScriptableObjects/EnemyDefinition.cs`) - ScriptableObject defining enemies with stats, abilities, and loot
- **CombatData** (`Assets/Scripts/Data/Models/CombatData.cs`) - Runtime combat state

**Combat Events** (in `GameEvents.cs` under `CombatEvents` namespace):
- `CombatStartedEvent`, `CombatEndedEvent`, `CombatFledEvent`
- `CombatHealthChangedEvent`, `CombatAbilityUsedEvent`, `CombatPoisonTickEvent`

**UI Components** (`Assets/Scripts/UI/Combat/`):
- `CombatPanelUI.cs` - Main combat display
- `EnemySelectionUI.cs` - Enemy selection at locations
- `CombatAbilityUI.cs` - Ability display with cooldown

**Location Integration:**
- `MapLocationDefinition` has `AvailableEnemies` list
- `LocationEnemy` class wraps enemy references per location

**Test Content:**
- Abilities: `Assets/ScriptableObjects/Abilities/` (BasicAttack, Heal, PoisonStrike, ShieldBash, Bite)
- Enemies: `Assets/ScriptableObjects/Enemies/` (Slime, Goblin, Wolf)

**Editor Tools:**
- `WalkAndRPG > Combat Content Creator` - Create test abilities/enemies
- `WalkAndRPG > Combat Tester` - Test combat in Play Mode

#### SETUP TODO (Resume Point):
1. Create empty GameObject "CombatManager" → Add `CombatManager` component
2. In Inspector, drag to "Player Abilities" list:
   - `Assets/ScriptableObjects/Abilities/BasicAttack.asset`
   - `Assets/ScriptableObjects/Abilities/Heal.asset`
3. Enter Play Mode
4. Menu: `WalkAndRPG > Combat Tester`
5. Drag `Assets/ScriptableObjects/Enemies/Slime.asset` → Click "Start Combat!"

#### NEXT STEPS:
- Build CombatPanel UI prefab in scene
- Create Abilities Inventory & Equipment system (weight-based slots)
- Equipment granting stats + auto-equipping abilities

## Development Notes

### Editor vs Device Behavior
The codebase uses `#if UNITY_EDITOR` extensively. StepManager has completely different paths:
- Editor: Polls DataManager for step changes (set by EditorStepSimulator)
- Device: Uses Health Connect API and direct sensor listener

### Logging
Use the custom `Logger` class with categories:
```csharp
Logger.LogInfo("Message", Logger.LogCategory.General);
Logger.LogInfo("Message", Logger.LogCategory.MapLog);
Logger.LogInfo("Message", Logger.LogCategory.StepLog);
```

### ScriptableObject Locations
- Activities: `Assets/ScriptableObjects/Activities/`
- Activity Variants: `Assets/ScriptableObjects/Activities/ActivitiesVariant/`
- Items: `Assets/ScriptableObjects/Ressources/`
- Map Locations: `Assets/ScriptableObjects/MapLocation/`
- Registries: Root of respective ScriptableObject folders

### Editor Windows
Custom editor tools in `Assets/Scripts/Editor/`:
- `ActivityManagerWindow` - Manage activities
- `ItemManagerWindow` - Manage items
- `EditorStepSimulator` - Simulate steps in editor
- `GameDataResetter` - Reset player data

## Build Commands

Open project in Unity and use standard Unity build workflow:
- **Play in Editor**: Press Play button or Ctrl+P
- **Build Android**: File > Build Settings > Android > Build
- **Refresh Assets**: Ctrl+R to reimport modified assets
