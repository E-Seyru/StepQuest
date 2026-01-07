# StepQuest Code Audit Report

**Date:** January 7, 2026
**Auditor:** Claude Code (Opus 4.5)
**Codebase:** StepQuest Unity Mobile RPG
**Total Files Analyzed:** 166 C# files

---

## Executive Summary

| Category | Issues Found | Critical | High | Medium | Low |
|----------|-------------|----------|------|--------|-----|
| Dead Code | 32 | 0 | 6 | 12 | 14 |
| Duplicate Code | 18 | 0 | 3 | 8 | 7 |
| Unused Events | 14 | 0 | 2 | 7 | 5 |
| Performance Issues | 8 | 1 | 2 | 3 | 2 |
| Pattern Inconsistencies | 11 | 0 | 1 | 5 | 5 |
| Architectural Issues | 6 | 0 | 1 | 3 | 2 |
| **TOTAL** | **89** | **1** | **15** | **38** | **35** |

### Quick Wins (Easy Fixes, High Impact)
1. Add Dictionary cache to `LocationRegistry` - eliminates O(n) lookups
2. Fix missing `EventBus.Unsubscribe()` in `GatheringPanel.OnDestroy()`
3. Remove 6 dead methods from `DataManager`
4. Replace `OrderBy().First()` with `Min` pattern in `MapPathfindingService`

### Critical Issues Requiring Immediate Attention
1. **Performance:** LINQ `OrderBy().First()` in Dijkstra loop (MapPathfindingService:180)

---

## Critical Issues

### 1. LINQ in Hot Path - Dijkstra Algorithm
**File:** `Assets/Scripts/Gameplay/World/MapPathfindingService.cs:180`

```csharp
// PROBLEM: O(n log n) operation inside while loop
string currentLocationId = unvisited.OrderBy(id => distances[id]).First();
```

**Impact:** HIGH - Called on every pathfinding iteration. With many locations, this creates significant performance overhead and GC pressure.

**Fix:** Use `Aggregate` for O(n) min-finding or implement a proper priority queue:
```csharp
string currentLocationId = unvisited.Aggregate((min, id) =>
    distances[id] < distances[min] ? id : min);
```

---

## Dead Code

### DataManager Dead Methods
**File:** `Assets/Scripts/Data/DataManager.cs`

| Method | Line | Status | Confidence |
|--------|------|--------|------------|
| `SaveGameAsync()` | 78-81 | Never called | Definite |
| `ForceSave()` | 93-97 | Never called | Definite |
| `CompleteTravel()` | 165-171 | Never called externally | Definite |
| `CancelTravel()` | 176-182 | Never called | Definite |
| `GetCurrentState()` | 229-237 | Never called | Definite |
| `SetCurrentLocation()` | 254-260 | Never called (MapManager handles directly) | Definite |

**Recommendation:** Remove all 6 methods. `SaveGame()` (sync) is used 31 times; `SaveGameAsync()` is never used.

---

### Combat System Dead Methods
**File:** `Assets/Scripts/Gameplay/Combat/`

| Method | Location | Status |
|--------|----------|--------|
| `CombatManager.SetPlayerAbilities()` | CombatManager.cs:355 | Never called |
| `CombatManager.SetPlayerMaxHealth()` | CombatManager.cs:363 | Never called |
| `CombatAbilityService.CanUseAbilities()` | CombatAbilityService.cs:163 | Redundant - stun check embedded in ability loop |
| `CombatData.DamagePlayer()` | CombatData.cs:154 | Legacy - replaced by ICombatant |
| `CombatData.DamageEnemy()` | CombatData.cs:182 | Legacy - replaced by ICombatant |
| `CombatData.HealPlayer()` | CombatData.cs:210 | Legacy - replaced by ICombatant |
| `CombatData.HealEnemy()` | CombatData.cs:221 | Legacy - replaced by ICombatant |
| `Combatant.SetMaxHealth()` | Combatant.cs:232 | Never called |
| `Combatant.SetHealth()` | Combatant.cs:224 | Never called |
| `Combatant.SetBaseStats()` | Combatant.cs:244 | Never called |

---

### MapManager Dead Methods
**File:** `Assets/Scripts/Gameplay/World/MapManager.cs`

| Method | Line | Status |
|--------|------|--------|
| `GetPathDetails()` | 101 | Defined but never called |
| `DebugPath()` | (debug only) | Only context menu usage |
| `MapValidationService.GetTravelBlockReason()` | ~200 | Never called |
| `MapSaveService.CheckAndSaveProgress()` | 976 | Never called |

---

### ActivityManager Dead/Redundant Methods
**File:** `Assets/Scripts/Gameplay/Player/ActivityManager.cs`

| Method | Line | Issue |
|--------|------|-------|
| `ShouldBlockTravel()` | 148-151 | Redundant wrapper - just calls `HasActiveActivity()` |
| `GetDebugInfo()` | 153-156 | Only called by ActivityDisplayPanel (debug panel) |
| `StartTimedActivity()` locationId parameter | 163 | Always null/overwritten |

---

### Unused Events (Published but Never Subscribed)
**File:** `Assets/Scripts/Core/Events/GameEvents.cs`

| Event | Namespace | Published In | Subscribers |
|-------|-----------|--------------|-------------|
| `CombatStunAppliedEvent` | CombatEvents:479 | CombatEventService | **NONE** |
| `CombatStunEndedEvent` | CombatEvents:500 | CombatEventService | **NONE** |
| `AbilityAcquiredEvent` | AbilityEvents:525 | AbilityManager | **NONE** |
| `AbilityEquippedEvent` | AbilityEvents:545 | AbilityManager | **NONE** |
| `AbilityUnequippedEvent` | AbilityEvents:565 | AbilityManager | **NONE** |
| `EquippedAbilitiesChangedEvent` | AbilityEvents:585 | AbilityManager | **NONE** |
| `OwnedAbilitiesChangedEvent` | AbilityEvents:607 | AbilityManager | **NONE** |
| `NPCDiscoveredEvent` | NPCEvents:631 | NPCManager | **NONE** |
| `NPCInteractionStartedEvent` | NPCEvents:649 | NPCManager | **NONE** |
| `DialogueChoiceMadeEvent` | DialogueEvents:722 | DialogueManager | **NONE** |
| `ExplorationStartedEvent` | ExplorationEvents:815 | ExplorationManager | **NONE** |
| `ExplorationTickEvent` | ExplorationEvents:835 | ExplorationManager | **NONE** |
| `ExplorationEndedEvent` | ExplorationEvents:888 | ExplorationManager | **NONE** |
| `ExplorationProgressChangedEvent` | ExplorationEvents:916 | ExplorationManager | **NONE** |

**Note:** `ExplorationDiscoveryEvent` IS properly subscribed (by SocialSectionPanel, CombatSectionPanel, LocationDetailsPanel).

**Recommendation:** Either wire up UI subscribers for these events or remove them to reduce code noise.

---

## Duplicate Code

### 1. UI Panel Singleton Pattern (14+ panels)
**Impact:** ~200-300 lines of duplicate code

All panels implement identical singleton pattern:
```csharp
void Awake() {
    if (Instance == null) {
        Instance = this;
    } else {
        Destroy(gameObject);
        return;
    }
}
```

**Affected Files:**
- GatheringPanel.cs (68-82)
- CraftingPanel.cs (80-91)
- ExplorationPanelUI.cs (52-63)
- BankPanel.cs (67-78)
- InventoryPanelUI.cs (45-56)
- ActivitiesSectionPanel.cs (42-53)
- And 8+ more panels

**Recommendation:** Create `SingletonPanel<T>` base class.

---

### 2. Object Pooling Pattern (3 panels)
**Impact:** ~100-150 lines of duplicate code

**Files:**
- ActivitiesSectionPanel.cs (376-409): `RecycleActivityCards()`, `GetPooledActivityCard()`
- CombatSectionPanel.cs (353-382): `RecycleEnemyCards()`, `GetPooledEnemyCard()`
- SocialSectionPanel.cs (344-373): `RecycleSocialActivityCards()`, `GetPooledSocialActivityCard()`

All three implement identical pooling logic with only naming differences.

**Recommendation:** Create `GenericObjectPool<T>` utility class.

---

### 3. Card/Slot Creation Loop (5+ panels)
**Impact:** ~150-200 lines of duplicate code

Identical pattern in:
- GatheringPanel.cs (376-402): `CreateVariantCard()`
- CraftingPanel.cs (471-497): `CreateVariantCard()`
- BankPanel.cs (226-241, 244-259): Slot creation loops

**Recommendation:** Extract generic `CreateActivityCard<T>()` method.

---

### 4. Travel Validation Logic (3 locations)
**Impact:** Same validation repeated in 3 places

- `MapValidationService.CanTravelTo()` (172-232)
- `MapTravelService.StartTravel()` (310-343)
- `POI.HandleClick()` (183-213)

Each repeats: direct connection check, pathfinding check, activity blocking check.

**Recommendation:** Single source of truth - always go through `MapValidationService`.

---

### 5. Discovery Check Pattern (3 section panels)

**Files:**
- CombatSectionPanel.cs (284-329): `GetVisibleEnemyCount()`, `IsEnemyDiscovered()`
- SocialSectionPanel.cs (275-320): `GetVisibleNPCCount()`, `IsNPCDiscovered()`
- ExplorationPanelUI.cs (358-468): `GetDiscoverableEnemies()`, etc.

**Recommendation:** Create `DiscoveryVisibilityService`.

---

## Underutilized Methods

### 1. EventBus Events Without Subscribers
As listed in Dead Code section - 14 events are published but never subscribed to.

### 2. DataManager Thread-Safe Methods
`DataManager` has thread-safe methods that are **bypassed**:

| Method | Intended Use | Actual Usage |
|--------|-------------|--------------|
| `SetCurrentLocation()` | Update player location | MapManager modifies PlayerData directly |
| `CompleteTravel()` | Complete travel | MapManager uses its own method |
| `CancelTravel()` | Cancel travel | Never called |

**Impact:** Thread safety guarantees are inconsistent.

---

## Architectural Issues

### 1. Indirect Circular Dependencies (Safe but Tight Coupling)

**Cycle 1: Activity ↔ Combat**
```
ActivityManager → CombatManager → ActivityManager.StopActivity()
```
- Location: CombatExecutionService calls `ActivityManager.Instance?.StopActivity()`
- Risk: LOW (only at combat end, null-guarded)

**Cycle 2: Map → Activity → Exploration → Map**
```
MapManager → ActivityManager → ExplorationManager → MapManager.LocationRegistry
```
- Risk: LOW (read-only access to LocationRegistry)

---

### 2. GameManager Initialization Has No Timeout
**File:** `Assets/Scripts/Core/GameManager.cs`

```csharp
while (DataManager.Instance == null ||
       MapManager.Instance == null ||
       ActivityManager.Instance == null)
{
    yield return new WaitForSeconds(0.1f);
}
// No timeout - infinite loop if manager missing!
```

**Risk:** Application hangs if any manager fails to initialize.

**Recommendation:** Add timeout constant in `GameConstants` and abort/show error after N seconds.

---

### 3. Mixed Event Patterns (EventBus vs Direct Events)

**Inconsistent:** NPCManager uses both:
```csharp
// EventBus (good)
EventBus.Publish(new NPCDiscoveredEvent(npcId));

// Direct event (inconsistent)
OnNPCDiscovered?.Invoke(npcId);
```

**Files using direct events instead of EventBus:**
- `NPCManager.cs:24` - `OnNPCDiscovered`
- `GatheringPanel.cs:63` - `OnVariantSelected`
- `CraftingPanel.cs:54` - `OnVariantSelected`

**Recommendation:** Remove direct events; use EventBus exclusively.

---

### 4. PlayerData Access Pattern Inconsistency

**Three different access styles for the same data:**

| Style | Example | Used By |
|-------|---------|---------|
| Direct property assignment | `playerData.Skills = skills;` | XpManager, AbilityManager |
| Helper methods | `playerData.AddDiscoveredNPC(npcId);` | NPCManager, ExplorationManager |
| Direct field manipulation | `playerData.TravelDestinationId = null;` | Debug tools |

**Recommendation:** Choose one pattern and enforce it. Add helper methods for all complex types or remove them entirely.

---

## Performance Issues

### 1. CRITICAL: LINQ OrderBy in Dijkstra Loop
**File:** `MapPathfindingService.cs:180`
- **Impact:** HIGH
- **Fix:** Replace with `Aggregate` or priority queue

### 2. HIGH: Registry Lookups Using FirstOrDefault
**File:** `LocationRegistry.cs:29, 77-78`

```csharp
// O(n) scan on every lookup
var location = AllLocations.FirstOrDefault(loc => loc.LocationID == locationId);
```

**Impact:** Every travel request, activity start, NPC lookup performs O(n) scan.

**Fix:** Add `Dictionary<string, MapLocationDefinition>` cache initialized in `Initialize()`.

**Affected Registries:**
- LocationRegistry
- AbilityRegistry
- ItemRegistry
- NPCRegistry

---

### 3. HIGH: Missing EventBus Unsubscribe
**File:** `GatheringPanel.cs`

Subscribes at line 106:
```csharp
EventBus.Subscribe<ActivityStartedEvent>(OnActivityStarted);
```

**No `OnDestroy()` method with matching unsubscribe!**

**Risk:** Memory leak, potential null reference after scene unload.

**Other files to verify:**
- CraftingPanel.cs
- ActivityDisplayPanel.cs

---

### 4. MEDIUM: JSON Deserialization on Every Property Access
**File:** `PlayerData.cs:226-245`

```csharp
public Dictionary<string, SkillData> Skills
{
    get
    {
        // Deserializes JSON on EVERY access!
        return JsonConvert.DeserializeObject<Dictionary<string, SkillData>>(_skillsJson);
    }
}
```

**Impact:** Repeated deserializations when iterating skills.

**Recommendation:** Add caching layer or use lazy loading pattern.

---

## Pattern Inconsistencies

### 1. Logger Category Usage
**Inconsistent:** NPCManager and EventBus use `Logger.LogCategory.General` instead of specific categories.

**Files:**
- `NPCManager.cs` - Should use a `SocialLog` or `NPCLog` category
- `EventBus.cs` - Uses `General` for all logging

**Recommendation:** Add `NPCLog` category to Logger enum.

---

### 2. Async Patterns Mixed
**File:** DataManager uses `async/await`:
```csharp
public async Task SaveGameAsync() { ... }
```

**File:** ActivityManager uses `Invoke`:
```csharp
Invoke(nameof(ProcessOfflineProgressInvoke), 1f);
```

**File:** UI panels use coroutines:
```csharp
StartCoroutine(PlayOpenAnimation());
```

**Recommendation:** Standardize on one async pattern (coroutines for Unity, async/await for data operations).

---

### 3. Null Handling Strategies
**Inconsistent strategies:**

| Pattern | Used By | Behavior |
|---------|---------|----------|
| Return null, log error | Registries | Silent failure |
| Return tuple with nulls | ActivityManager | `return (null, null);` |
| Return null, no log | MapManager | Silent null return |

**Recommendation:** Define clear policy: throw for critical errors, return null for lookups, use null coalescing operators.

---

## TODO Comments and Incomplete Features

### High Priority TODOs (Feature Blockers)

| File | Line | TODO | Impact |
|------|------|------|--------|
| ItemActionPanel.cs | 410-670 | Shop/trade system not implemented | 11 TODOs - major feature gap |
| ExplorationManager.cs | 125, 263 | XP awards not implemented | Exploration gives no XP |
| CombatExecutionService.cs | 145 | Combat XP not awarded | Combat gives no XP |
| NPCInteractionPanel.cs | 247 | Gift functionality not implemented | NPC relationship system incomplete |

### Medium Priority TODOs

| File | Line | TODO |
|------|------|------|
| EquipmentPanelUI.cs | 235, 287, 313 | Equipment stats not calculated, uses PlayerPrefs |
| UniversalSlotUI.cs | 423, 434 | Tooltip system not implemented |
| TravelConfirmationPopup.cs | 163 | Step cost validation not implemented |

---

## Recommendations

### Priority 1: Immediate (Critical/High Impact)
1. **Fix Dijkstra performance** - Replace `OrderBy().First()` with `Aggregate` (15 min)
2. **Add Registry caching** - Dictionary cache in `LocationRegistry.Initialize()` (30 min)
3. **Fix GatheringPanel memory leak** - Add `OnDestroy()` with unsubscribe (5 min)
4. **Remove DataManager dead methods** - 6 unused methods (10 min)

### Priority 2: High (Architecture Cleanup)
5. **Create `SingletonPanel<T>` base class** - Eliminates 200+ duplicate lines (1-2 hours)
6. **Create `GenericObjectPool<T>` utility** - Consolidates 3 pooling implementations (1 hour)
7. **Wire up or remove unused events** - 14 events published but never subscribed (30 min)
8. **Add GameManager initialization timeout** (15 min)

### Priority 3: Medium (Code Quality)
9. **Remove Combat system legacy methods** - 10 dead methods (30 min)
10. **Standardize event patterns** - Remove direct events, use EventBus only (1 hour)
11. **Create `DiscoveryVisibilityService`** - Consolidate discovery checks (1-2 hours)
12. **Standardize Logger categories** - Add NPCLog, use consistently (30 min)

### Priority 4: Low (Future Consideration)
13. **Add PlayerData caching** - Avoid repeated JSON deserialization (2-3 hours)
14. **Unify PlayerData access patterns** - Helper methods vs direct access (2-3 hours)
15. **Document initialization order** - Prevent future issues (30 min)

---

## Estimated Effort Summary

| Priority | Items | Estimated Time |
|----------|-------|----------------|
| Priority 1 | 4 | 1 hour |
| Priority 2 | 4 | 4-5 hours |
| Priority 3 | 4 | 3-4 hours |
| Priority 4 | 3 | 5-6 hours |
| **Total** | **15** | **13-16 hours** |

---

## Files Most Impacted by Issues

| File | Issues | Types |
|------|--------|-------|
| `DataManager.cs` | 6 | Dead code, thread safety |
| `MapPathfindingService.cs` | 2 | Performance, dead code |
| `GatheringPanel.cs` | 4 | Memory leak, duplicates |
| `CraftingPanel.cs` | 4 | Duplicates, direct events |
| `GameEvents.cs` | 14 | Unused events |
| `CombatManager.cs` | 3 | Dead code |
| `LocationRegistry.cs` | 2 | Performance |
| `ActivityManager.cs` | 3 | Dead code, redundant methods |

---

## Conclusion

The StepQuest codebase is generally well-structured with good separation of concerns. The main issues are:

1. **Dead code accumulation** from iterative development (scaffolded features never completed)
2. **Duplicate patterns** in UI panels that could be abstracted
3. **Performance anti-patterns** in pathfinding and registry lookups
4. **Inconsistent patterns** for event handling and data access

The codebase shows good practices in:
- EventBus pattern for decoupling
- Service pattern for manager internals
- ScriptableObjects for content
- Thread-safety awareness (even if inconsistently applied)

Most issues are LOW to MEDIUM severity and represent technical debt rather than critical bugs. The Priority 1 items should be addressed promptly to prevent performance issues and memory leaks.
