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
- **ExplorationManager** (`Assets/Scripts/Gameplay/Exploration/ExplorationManager.cs`) - Handles exploration activities, discovery rolls, and progress tracking

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
- `ExplorationEvents` - Discovery events for hidden content

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
- LocationDiscoveries (Dictionary<string, HashSet<string>>) - Exploration discoveries per location
- DiscoveredNPCs (List<string>) - NPCs the player has met
- NPCRelationships (Dictionary<string, int>) - Affinity points per NPC
- DialogueFlags (Dictionary<string, bool>) - Story/dialogue progress flags

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
- **NPCManager** - Singleton handling NPC discovery (via PlayerData.DiscoveredNPCs) and interaction events

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

**Current Test Content (all IDs use snake_case):**
- Locations: village_01, mine_01, foret_01, cabane_de_pecheur
- Enemies: slime, goblin, wolf
- Abilities: basic_attack, heal, poison_strike, venom_strike, wolf_bite, wolf_howl
- Status Effects: poison, burn, stun, regeneration, attack_up, defense_down

## Development Notes

### Thread Safety Considerations
`DataManager.SaveGameAsync()` runs saves on a background thread. The current implementation creates a JSON snapshot of PlayerData before saving, which is safe. However, be aware:
- `DataManager.Instance.PlayerData` is publicly accessible and could be read/modified while a save is in progress
- Dictionary properties in PlayerData (Skills, SubSkills, etc.) deserialize from JSON on each access - concurrent access could cause issues
- Most gameplay code runs on Unity's main thread, so this is currently not a problem in practice
- If adding more async operations in the future, consider using the existing `playerDataLock` consistently

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
- `ExplorationManagerWindow` - Configure hidden content and test discoveries at runtime

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
- Core travel system (step-based with multi-segment pathfinding)
- Activity system (step-based harvesting, time-based crafting with offline progress)
- Full inventory and equipment system
- Combat system with auto-battler mechanics
- Generic status effect system (DoT, HoT, buffs, debuffs, stuns)
- Ability ownership and equipment with weight limits
- XP/Skill progression system (main skills + sub-skills)
- Event-driven architecture (EventBus pattern)
- NPC system with discovery tracking and interaction UI
- Exploration system with rarity-based discoveries
- Specialized activity panels (Gathering, Crafting, Exploration, Bank)

### In Progress / Next Steps:
- NPC Talk/Gift functionality (relationship points system exists, UI needs work)
- MerchantPanel for buy/sell activities
- Ability weight enforcement (system exists, not yet balanced)
- XP curve balancing and rewards tuning
- Expand content (abilities, enemies, locations, NPCs)
- Looped/offline combat simulation

### Exploration System (Implemented)
Step-based activity for discovering hidden content at locations.

**Core Components:**
- **ExplorationManager** - Singleton handling discovery logic, rolls, and progress tracking
- **ExplorationPanelUI** - Shows discoverable content, progress, and discovery chances
- **DiscoverableItemUI** - Individual item display with rarity colors and discovery status
- **ExplorationManagerWindow** - Editor tool for configuring and testing discoveries

**Discovery System:**
- **5 Rarity Tiers**: Common, Uncommon, Rare, Epic, Legendary
- Each rarity has base discovery chance (configured in `GameConstants`)
- Discovery chance modified by player's Exploration skill level (+2% per level, capped at +100%)
- Bonus XP awarded on discovery, scaling with rarity

**Discoverable Content Types (DiscoverableType enum):**
- `Enemy` - Hidden monsters that appear after discovery
- `NPC` - Hidden characters revealed through exploration
- `Activity` - Hidden activities (special gathering spots, etc.)
- `Dungeon` - Sub-areas within the location (placeholder for future)

**Location Integration:**
- `LocationEnemy.IsHidden`, `LocationEnemy.Rarity` - Enemy discovery settings
- `LocationNPC.IsHidden`, `LocationNPC.Rarity` - NPC discovery settings
- `LocationActivity.IsHidden`, `LocationActivity.Rarity` - Activity discovery settings
- All have `GetDiscoveryID()`, `GetDiscoveryBonusXP()`, `GetBaseDiscoveryChance()` methods

**PlayerData Discovery Tracking:**
- `PlayerData.LocationDiscoveries` - Dictionary<string, HashSet<string>> tracking discoveries per location
- Methods: `HasDiscoveredAtLocation()`, `AddDiscoveryAtLocation()`, `GetDiscoveryCountAtLocation()`, `ClearDiscoveriesAtLocation()`

**Events (ExplorationEvents namespace):**
- `ExplorationStartedEvent` - When exploration begins at a location
- `ExplorationTickEvent` - Each exploration tick
- `ExplorationEndedEvent` - When exploration stops
- `ExplorationDiscoveryEvent` - When something is discovered (triggers UI refresh)
- `ExplorationProgressChangedEvent` - When discovery count changes

**UI Behavior:**
- Hidden content filtered from display until discovered
- `CombatSectionPanel`, `SocialSectionPanel`, `LocationDetailsPanel` subscribe to `ExplorationDiscoveryEvent`
- Panels refresh automatically when relevant content is discovered

### Activity Panels (Implemented)
Specialized panels per activity type, routed via `LocationDetailsPanel.OnActivitySelected()`:

**Implemented Panels:**
- **ExplorationPanelUI** - For exploration activities (discovery rolls, progress tracking)
- **GatheringPanel** - For step-based harvesting activities (mining, woodcutting, fishing)
- **CraftingPanel** - For time-based crafting activities (forging, cooking)
- **BankPanel** - For bank/storage activities

**Pending Panels:**
- **MerchantPanel** - For buying/selling with NPCs (merchant-type activities)

### Expedition System (Future - Not Yet Designed)
Long-distance step-based activity with tiered goals and rewards. Concept notes:
- Large step requirements (e.g., 2k / 10k / 25k steps)
- Fixed rewards at tier completion + random rewards along the way
- Permanent stat bonuses as incentive for first completion
- Possibly guarded by monsters at higher tiers
- Needs more design work to make it engaging (avoid "boring progress bar" feel)

---

## Planned Features (Not Yet Implemented)

### Day/Night Cycle System
Step-based time system affecting gameplay.

**Core Mechanics:**
- Cycle toggles between Day and Night every ~5000 steps (tunable)
- Tracked in PlayerData, persists across sessions
- Triggers `DayNightChangedEvent` when cycle changes

**Gameplay Effects:**
- **NPC Schedules**: Some NPCs only available during day or night (e.g., blacksmith by day, witch by night)
- **Enemy Variations**: Different/additional enemies spawn at night (nocturnal creatures, harder variants)
- **Visual Changes**: UI theme or location art could reflect time of day (future polish)

**Integration Points:**
- NPCDefinition may need `AvailableDuring` field (Day, Night, Both)
- LocationEnemy may need `SpawnTime` field (Day, Night, Both)
- StepManager triggers cycle check on step updates

---

### In-Game Calendar System
Calendar for tracking time and triggering events.

**Core Mechanics:**
- Calendar advances based on day/night cycles (1 full cycle = 1 in-game day)
- Tracks current day, week, season, year
- Used for scheduling events, story beats, seasonal content

**Future Uses:**
- Seasonal events (festivals, special merchants)
- Story events triggered on specific dates
- Calendar-locked content (certain dungeons open only during full moon, etc.)

---

### Hired Workers System
Passive income through NPC workers who gather resources automatically.

**Core Mechanics:**
- Player can hire workers (Woodcutter, Miner, Gatherer, Fisher, etc.)
- Each worker assigned to a specific location/activity
- Workers deposit gathered resources to player's bank at each day/night cycle transition
- Workers have a daily wage (gold cost) deducted automatically

**Progression:**
- Workers can be trained/certified to access higher-tier materials
- Training costs gold and possibly materials
- Higher-tier workers = better resources but higher wages
- Worker efficiency may scale with player's own skill level in that profession

**Data Model Considerations:**
- `HiredWorker`: WorkerType, AssignedLocation, CertificationLevel, DailyWage
- `PlayerData.HiredWorkers`: List of active workers
- Bank receives deposits on `DayNightChangedEvent`

---

### Caravan Fast Travel System
Alternative travel method using gold and time instead of steps.

**Core Mechanics:**
- Player can build/unlock caravan checkpoints at locations
- Once checkpoints exist at two locations, player can fast travel between them
- Fast travel costs gold + real time (not steps)
- Cannot fast travel while activity is in progress

**Progression:**
- Checkpoints require materials/gold to build
- Higher-tier checkpoints = faster travel times or reduced costs
- Some locations may require story/reputation unlock before checkpoint can be built

**Design Notes:**
- Complements step-based travel, doesn't replace it
- Useful for returning to distant locations without grinding steps
- Gold sink for late-game players

---

### Equipment Set Bonuses
Bonus stats for wearing matching equipment pieces.

**Core Mechanics:**
- Equipment grouped into sets (e.g., "Miner's Set", "Wolf Hunter Set")
- Wearing 2/3/4 pieces grants cumulative bonuses
- Bonuses can be stat boosts, skill efficiency, or special effects

**Implementation Notes:**
- `EquipmentSetDefinition` ScriptableObject: SetID, RequiredPieces, Bonuses per tier
- `ItemDefinition` gets optional `SetID` field
- `EquipmentManager` or `InventoryManager` calculates active set bonuses
- Lower priority - requires more equipment content first

---

### The Book (Journal/Compendium UI)
Central UI for tracking game knowledge and statistics.

**Planned Sections:**
- **Bestiary**: Monster lore, kill counts, discovered drops, weaknesses
- **Statistics**: Steps walked, resources gathered, enemies defeated, activities completed
- **NPC Relationships**: Affinity levels, gift history, unlocked dialogue
- **Achievements**: Milestones and accomplishments (if implemented)
- **World Lore**: Discovered secrets, location histories, story fragments

**Design Notes:**
- Single entry point in main navigation or accessible from multiple places
- Tabs or pages for each section
- Content populates as player discovers/accomplishes things
- Lower priority - UI framework, implement when systems need a home

---

### Dynamic Location Events
Temporary events that add variety to locations.

**Core Mechanics:**
- Events spawn randomly or on schedule at locations
- Examples: "Merchant caravan arrived", "Wolf pack sighted", "Rare ore vein discovered"
- Events last for a duration (steps or real time) then disappear
- Events can add temporary NPCs, enemies, activities, or shop inventory

**Discovery:**
- Player can hear about active events at taverns or from NPCs
- Creates reason to visit tavern locations and talk to NPCs
- Events could be tied to calendar system

**Implementation Notes:**
- `LocationEvent` ScriptableObject: EventType, Duration, Effects, SpawnConditions
- `EventManager` handles spawning, tracking, expiring events
- Lower priority - content enrichment feature

---

### Community Events & Leaderboards (Online Features)
Shared progress and competitive elements via Firebase.

**Community Events:**
- Server-side aggregate tracking (e.g., "Community gathered 1,000,000 mushrooms")
- All players contribute to shared goal
- Rewards unlock for everyone when goal is reached
- Simple Firebase Realtime Database document for totals

**Leaderboards:**
- Track: Total steps, monsters killed, skill levels, achievements
- Firebase Firestore or Google Play Games Services
- Query top players, show player's rank

**Security Considerations:**
- Client-side data can be manipulated - accept some trust or implement server validation
- Focus on cooperative events over competitive to reduce cheating incentive

**Implementation Notes:**
- Requires Firebase setup (already planned for cloud saves)
- Lower priority - post-core-gameplay feature
- Could tie into seasonal calendar events

---

### Legacy Steps System (From MasterPlan)
Long-term rewards for cumulative walking.

**Core Mechanics:**
- Every X steps (e.g., 10,000), player earns a Legacy Point
- Points spent in passive skill tree for permanent bonuses
- Bonuses: stat increases, efficiency boosts, unlock perks

**Design Notes:**
- Rewards long-term engagement with core walking mechanic
- Tree should have meaningful choices, not just linear upgrades
- Visual representation of progress (total lifetime steps)

---

### Combat Stat Allocation (From MasterPlan)
Manual stat point distribution for combat builds.

**Core Mechanics:**
- Leveling Combat Proficiency grants Stat Points
- Player allocates points to: Strength, Intelligence, Stamina, etc.
- Stats affect combat damage, HP, ability effectiveness

**Design Notes:**
- Allows build customization beyond just equipment
- Respec option (costly) for changing builds
- Interacts with equipment stats

---

### Looped Combat / Offline Grinding (From MasterPlan)
Automated combat that continues while app is closed.

**Core Mechanics:**
- Player sets combat to "loop" at a location
- Combat continues in background/offline
- On return, game calculates results via simulation
- Player defines consumable usage rules (use potion at X% HP)

**Simulation Considerations:**
- Must be fast (can't simulate 8 hours of combat in real-time)
- Risk-aware: chance of death, potion depletion
- Results: XP gained, loot collected, potions used, final state

**Implementation Notes:**
- Complex feature - needs careful design
- Core to the "idle RPG" aspect of the game
- Medium-high priority once combat is more fleshed out

---

### Quest System (From MasterPlan)
Structured objectives with rewards.

**Core Mechanics:**
- Quests have objectives (kill X, gather Y, travel to Z, talk to NPC)
- Completion grants rewards (XP, items, currency, unlocks)
- Quest chains for story progression

**Types:**
- **Main Quests**: Story-driven, unlock major content
- **Side Quests**: Optional content, NPC-given
- **Repeatable Quests**: Daily/weekly objectives

**Implementation Notes:**
- Requires dialogue system for quest giving/turning in
- Story content creation is separate from system implementation
- Medium priority - system before content

---

### Story & Dialogue System (From MasterPlan)
Narrative delivery through NPC conversations.

**Core Mechanics:**
- Dialogue trees with player choices
- Choices can affect story outcomes, NPC relationships, unlocks
- Dialogue displayed in UI panels (text-based, no voice)

**Integration:**
- NPCs have dialogue based on relationship level, quest state, story flags
- `DialogueDefinition` ScriptableObject for conversation trees
- `StoryManager` tracks flags and branching state

**Implementation Notes:**
- System implementation separate from writing content
- Can start simple (linear dialogue) and add branching later
- Lower priority until other systems are stable
