# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

StepQuest is a Unity mobile RPG (Android) that uses real-world step counting as a core game mechanic. Players walk in real life to travel between locations on a world map and perform activities (mining, gathering, crafting, fishing, combat).

**Unity Version**: Unity 2022+ with Mobile 2D template
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
- **CombatManager** (`Assets/Scripts/Gameplay/Combat/CombatManager.cs`) - Auto-battler combat with coroutine-based ability cycling
- **AbilityManager** (`Assets/Scripts/Gameplay/Player/AbilityManager.cs`) - Manages player's owned and equipped abilities with weight-based limits
- **XpManager** (`Assets/Scripts/Gameplay/Progression/XpManager.cs`) - Handles skill/subskill XP progression and level-up mechanics
- **NPCManager** (`Assets/Scripts/Gameplay/NPC/NPCManager.cs`) - Manages NPC discovery and interactions

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
- `CombatEvents` - Combat flow, abilities, health, status effects
- `AbilityEvents` - Ability acquisition and equipment
- `NPCEvents` - NPC discovery and interaction events

### Data Layer

**ScriptableObjects** define game content:
- `MapLocationDefinition` - Location data (ID, display name, connections, available enemies)
- `ActivityDefinition` - Activity types (mining, crafting, etc.)
- `ActivityVariant` - Specific variants with resources, costs, and XP rewards
- `ItemDefinition` - Item data (materials, consumables, equipment)
- `AbilityDefinition` - Combat abilities with effects, cooldowns, and weight
- `EnemyDefinition` - Enemy stats, abilities, loot tables
- `StatusEffectDefinition` - DoT, HoT, buffs, debuffs, control effects
- `NPCDefinition` - NPC data (ID, name, description, avatar, illustration)

**Registries** aggregate ScriptableObjects:
- `LocationRegistry` - All map locations (`Assets/ScriptableObjects/MapLocation/LocationRegistry.asset`)
- `ActivityRegistry` - All activities (`Assets/ScriptableObjects/Activities/ActivityRegistry.asset`)
- `ItemRegistry` - All items (`Assets/ScriptableObjects/Ressources/Materials/MainItemRegistry.asset`)
- `AbilityRegistry` - All abilities (`Assets/ScriptableObjects/AbilityRegistry.asset`)
- `StatusEffectRegistry` - All status effects (`Assets/ScriptableObjects/Registries/StatusEffectRegistry.asset`)
- `NPCRegistry` - All NPCs (`Assets/ScriptableObjects/NPCs/NPCRegistry.asset`)

**PlayerData** (`Assets/Scripts/Data/Models/PlayerData.cs`) stores:
- Step counts (TotalSteps, DailySteps)
- Current location and travel state
- Active activity state
- Inventory and equipment
- Skills and SubSkills (Dictionary<string, SkillData>)
- OwnedAbilities and EquippedAbilities (List<string>)

**LocalDatabase** uses SQLite for persistent storage on device.

### Service Pattern
Managers delegate to internal service classes for separation of concerns:
- `DataManagerDatabaseService`, `DataManagerValidationService`, `DataManagerSaveLoadService`
- `MapTravelService`, `MapLocationService`, `MapValidationService`, `MapEventService`, `MapSaveService`, `MapPathfindingService`
- `ActivityExecutionService`, `ActivityProgressService`, `ActivityTimeService`, `ActivityPersistenceService`
- `CombatExecutionService`, `CombatAbilityService`, `CombatEventService`, `CombatStatusEffectService`

### UI Architecture
- **UIManager** coordinates UI panels
- **PanelManager** handles panel navigation
- **AboveCanvasManager** manages overlay UI with services for events, animations, and display
- Combat UI: `CombatPanelUI`, `CombatAbilityUI`, `CombatAbilityDisplay`, `StatusEffectUI`, `CombatPopup`
- Ability UI: `AbilitySlotUI`, `AbilitiesInventoryContainer`, `EquippedAbilitiesContainer`
- Activity UI: `PrimaryActivityCard`, `HarvestingActivityCard`, `CraftingActivityCard`
- Inventory UI: `UniversalSlotUI`, `EquipmentSlotUI`, `DraggedItemVisual`
- Social UI: `SocialSectionPanel`, `SocialAvatarCard`, `NPCInteractionPanel`, `HeartDisplay`

## Key Game Mechanics

### Travel System
- Travel costs are measured in steps
- Pathfinding supports multi-segment routes between non-adjacent locations (`MapPathfindingService`)
- Travel state persists across app close/open
- Progress tracked via `PlayerData.TravelStartSteps` and `TravelRequiredSteps`

### Activity System
- **Step-based activities**: Progress by walking (harvesting resources like mining, woodcutting, fishing)
- **Time-based activities**: Progress by real-time (crafting - forging bars, equipment)
- Activities can loop automatically if materials are available
- Offline progress is processed on app resume
- XP rewards for main skill and sub-skill per activity variant

### Step Counting
- **On Device**: Uses Android Health Connect Recording API (`RecordingAPIStepCounter`) + direct sensor
- **In Editor**: Simplified mode using `EditorStepSimulator` to manually add steps

### Skill System
- **Main Skills**: Mining, Woodcutting, Fishing, Forging, etc.
- **Sub-Skills**: Specific variants (e.g., "Mine Iron", "Couper du Saule")
- Level progression with exponential XP curve
- Efficiency bonuses based on level tiers (Apprenti, Competent, Expert, Maitre, Legendaire)

### Combat System
Auto-battler combat where abilities trigger automatically on cooldown.

**Core Components:**
- **CombatManager** - Singleton managing combat with coroutine-based ability cycling
- **AbilityDefinition** - ScriptableObject defining abilities with:
  - Effects (Damage, Heal, Shield, ApplyStatusEffect)
  - Weight (for equipment limit)
  - Cooldown
- **EnemyDefinition** - ScriptableObject defining enemies with stats, abilities, and loot tables
- **StatusEffectDefinition** - Generic system for DoT, HoT, buffs, debuffs, stuns

**Status Effect System:**
- Types: Poison, Burn, Bleed, Stun, Regeneration, Shield, AttackBuff/Debuff, DefenseBuff/Debuff, SpeedBuff/Debuff
- Configurable stacking behavior (Stacking vs NoStacking)
- Configurable decay behavior (None, Time, OnTick, OnHit)
- Effect behaviors: DamageOverTime, HealOverTime, StatModifier, ControlEffect

**Ability Equipment System:**
- Players own abilities (acquired from loot, progression)
- Weight-based equipment limit (default: 6 weight, max: 12)
- Abilities managed via `AbilityManager`

**Combat Events:**
- `CombatStartedEvent`, `CombatEndedEvent`, `CombatFledEvent`
- `CombatHealthChangedEvent`, `CombatAbilityUsedEvent`
- `StatusEffectAppliedEvent`, `StatusEffectTickEvent`, `StatusEffectRemovedEvent`
- `CombatStunAppliedEvent`, `CombatStunEndedEvent`

**Location Integration:**
- `MapLocationDefinition.AvailableEnemies` - List of enemies at each location
- `MapLocationDefinition.AvailableNPCs` - List of NPCs at each location

**Loot System:**
- `EnemyDefinition.LootTable` - List of `LootDropEntry` (item, min/max quantity, drop chance 0-1)
- `EnemyDefinition.GenerateLoot()` - Rolls loot on victory
- Loot automatically added to inventory via `InventoryManager.AddItem()`

### NPC System
NPCs can be placed at locations and interacted with by players.

**Core Components:**
- **NPCDefinition** - ScriptableObject defining NPC with:
  - NPCID, NPCName, Description
  - Avatar (small image for cards/lists)
  - Illustration (large image for interaction panel)
  - ThemeColor, IsActive
- **NPCRegistry** - Central registry with fast lookup cache
- **NPCManager** - Singleton handling NPC discovery and interaction events

**UI Components:**
- **SocialSectionPanel** - Displays NPC cards at a location (similar to CombatSectionPanel for enemies)
- **SocialAvatarCard** - Individual NPC card with avatar and name
- **NPCInteractionPanel** - Overlay popup when clicking an NPC (illustration, description, hearts, Talk/Gift buttons)
- **HeartDisplay** - Shows relationship level as 5 hearts (0-10 scale, half-heart increments)

**NPC Events:**
- `NPCDiscoveredEvent` - Fired when player first meets an NPC
- `NPCInteractionStartedEvent` - Fired when player interacts with an NPC

**Location Integration:**
- `MapLocationDefinition.AvailableNPCs` - List of NPCs at each location
- `MapLocationDefinition.GetAvailableNPCs()` - Returns valid, active NPCs

### Content Organization

**ScriptableObject Locations:**
- Activities: `Assets/ScriptableObjects/Activities/`
- Activity Variants: `Assets/ScriptableObjects/Activities/ActivitiesVariant/`
- Items: `Assets/ScriptableObjects/Ressources/` (Materials/, Consumables/, Equipment/)
- Map Locations: `Assets/ScriptableObjects/MapLocation/`
- Abilities: `Assets/ScriptableObjects/Abilities/` (includes `Enemies Abilities/` subfolder)
- Enemies: `Assets/ScriptableObjects/Enemies/`
- Status Effects: `Assets/ScriptableObjects/StatusEffects/`
- NPCs: `Assets/ScriptableObjects/NPCs/`
- Registries: Root of respective folders or `Assets/ScriptableObjects/Registries/`

**Current Test Content:**
- Locations: Village_01, Mine_01, Foret_01, Cabane de pecheur
- Enemies: Slime, Goblin, Wolf
- Abilities: BasicAttack, Heal, PoisonStrike, VenomStrike, Bite, Howl
- Status Effects: Poison, Burn, Stun, Regeneration, AttackUp, DefenseDown

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

### Editor Windows
Custom editor tools in `Assets/Scripts/Editor/`:
- `ActivityManagerWindow` - Manage activities
- `ItemManagerWindow` - Manage items
- `AbilityManagerWindow` - Manage abilities
- `EnemyManagerWindow` - Manage enemies
- `StatusEffectManagerWindow` - Manage status effects
- `EditorStepSimulator` - Simulate steps in editor
- `GameDataResetter` - Reset player data
- `CombatContentCreator` - Create test combat content
- `RegistryValidationDashboard` - Validate all registries
- `ConnectionManagerWindow` - Manage location connections
- `NPCManagerWindow` - Manage NPCs with bidirectional location sync

### Debug Tools
Debug scripts in `Assets/Scripts/Debug/`:
- `PlayerDataDebugger` - Inspect player data at runtime
- `InventoryDataDebugger` - Inspect inventory
- `EquipmentDebugger` - Inspect equipment
- `ActivityRegistryDebugger` - Inspect activity registry
- `RuntimePlayerDataFixer` - Fix corrupted player data

## Build Commands

Open project in Unity and use standard Unity build workflow:
- **Play in Editor**: Press Play button or Ctrl+P
- **Build Android**: File > Build Settings > Android > Build
- **Refresh Assets**: Ctrl+R to reimport modified assets

## Current Development Status

### Implemented:
- Core travel system (step-based)
- Activity system (step-based harvesting, time-based crafting)
- Full inventory and equipment system
- Combat system with auto-battler mechanics
- Generic status effect system
- Ability ownership and equipment with weight limits
- XP/Skill progression system
- Event-driven architecture
- NPC system with discovery and interaction UI

### In Progress / Next Steps:
- NPC relationship system (tracking points per NPC)
- NPC Talk/Gift functionality
- Expand enemy loot tables with test items
- Balance combat and progression
- Add more content (abilities, enemies, locations, NPCs)
- UI polish for ability equipment in InventoryPanel
- Looped/offline combat simulation
