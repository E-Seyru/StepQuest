# StepQuest Cleanup Todo List
*Generated from code audit session - December 2024*

---

## YOUR TASK - Assign SerializeField References in Unity Inspector

The code changes are done, but you need to assign these references in the Inspector to get the full performance benefit (otherwise it falls back to FindObjectOfType):

### POI.cs (all POI GameObjects in your scene)
- [ ] Assign `Travel Popup` field → drag your TravelConfirmationPopup from the scene

### EnemySelectionUI.cs
- [x] **DELETED** - Duplicate of CombatSectionPanel (which is actually used by LocationDetailsPanel)

### Optional (already have fallbacks, but good to verify):
- [ ] `DragDropManager.cs` → verify `Drag Canvas` is assigned
- [ ] `VariantContainer.cs` → verify `Activity Registry` is assigned
- [ ] `ActivityXpContainer.cs` → verify `Activity Registry` is assigned

---

## COMPLETED - Quick Wins (Code Changes Done by Claude)

### Unused Code Removed
- [x] `DataManager.CheckAndResetDailySteps()` - Obsolete method, functionality moved to StepManager
- [x] `GameManager.SetGamePaused()` - Never called, pause feature not implemented
- [x] `GameManager.GetGameStateInfo()` - Unused debug method
- [x] `MapManager.ApplyLeftoverSteps()` - Dead code, logic already inline in CompleteCurrentSegment()
- [x] `BeforeGameStateChangeEvent` in GameEvents.cs - Never published or subscribed
- [x] `CombatTester.OnHealthChanged()` empty handler - Removed subscription

### Performance: Cached FindObjectOfType() Calls
- [x] `POI.cs` - Added [SerializeField] for TravelConfirmationPopup with fallback
- [x] ~~`EnemySelectionUI.cs`~~ - **DELETED** (was duplicate of CombatSectionPanel)

### Performance: Cached Camera.main
- [x] `InventoryPanelUI.cs` - Added cachedMainCamera + cachedRectTransform
- [x] `BankPanelUI.cs` - Added cachedMainCamera + cachedRectTransform

### Already Properly Implemented (Verified OK)
- [x] `DragDropManager.cs` - Already has [SerializeField] with fallback for Canvas
- [x] `VariantContainer.cs` - Already has [SerializeField] with fallback for ActivityRegistry
- [x] `ActivityXpContainer.cs` - Already has [SerializeField] with fallback for ActivityRegistry
- [x] `StepManager.cs` - Already cleans up Instance in OnDestroy()
- [x] `CombatManager.cs` - Already cleans up Instance in OnDestroy()

---

## PENDING - Medium Effort Tasks (For Claude)

### Performance: Convert Update() Polling to Event-Driven
Files that poll input every frame unnecessarily:
- [ ] `InventoryPanelUI.cs` - CheckForDeselection() polls Input every frame
- [ ] `BankPanelUI.cs` - Same CheckForDeselection() pattern (duplicate code)
- [ ] `DragDropManager.cs` - Input polling during drag
- [ ] `VariantContainer.cs` - DetectClickOutside() every frame

**Suggested fix:** Use OnPointerDown/OnPointerExit events instead of Update() polling

### Code Duplication: Extract ContainerPanelUI Base Class
- [x] **COMPLETED** - Created `ContainerPanelUI` base class
- [x] Refactored `InventoryPanelUI` to inherit from base (550 → 215 lines)
- [x] Refactored `BankPanelUI` to inherit from base (472 → 180 lines)
- [x] Eliminated ~300 lines of duplicate code

### Architecture: Standardize Event Systems
Mixed usage of EventBus and C# events creates confusion:
- [ ] `InventoryManager.cs` - Uses C# events (OnContainerChanged, OnItemAdded, OnItemRemoved)
- [ ] `AbilityManager.cs` - Uses C# events (OnOwnedAbilitiesChanged, OnEquippedAbilitiesChanged)
- [ ] Other managers use EventBus consistently

**Suggested fix:** Create InventoryEvents and AbilityEvents in GameEvents.cs, migrate to EventBus

### Thread Safety
- [ ] `CombatManager._abilityInstanceCounts` dictionary - Accessed without locks
- [ ] `InventoryManager` services - No thread protection on container access

---

## PENDING - Larger Refactors (For Claude)

### Singleton Boilerplate (28 files)
- [x] **CREATED** `SingletonMonoBehaviour<T>` base class in `Assets/Scripts/Core/`
- [x] Refactored `UIManager` to use base class
- [x] Refactored `ErrorPanel` to use base class
- [ ] Remaining 26 singletons can be refactored using the same pattern

**Usage example:**
```csharp
public class MyManager : SingletonMonoBehaviour<MyManager>
{
    protected override void OnAwakeInitialize() { /* custom init */ }
    protected override void OnSingletonDestroyed() { /* cleanup */ }
    protected override bool PersistAcrossScenes => true; // optional
}
```

### Editor Window Boilerplate (7 files)
All editor windows in Assets/Scripts/Editor/ have nearly identical structure:
- ActivityManagerWindow.cs
- ItemManagerWindow.cs
- AbilityManagerWindow.cs
- EnemyManagerWindow.cs
- ConnectionManagerWindow.cs
- StatusEffectManagerWindow.cs
- AbilityDebugWindow.cs

**Suggested fix:** Create `BaseEditorWindow<T>` with common UI patterns

### Hardcoded UI Strings (Localization)
Mixed French and English strings throughout UI:
- `BankPanelUI.cs:294` - "Coffre de Banque"
- `InventoryPanelUI.cs:459` - Mixed "Inventaire" / "Abilities"
- `ItemActionPanel.cs:175` - "Aucune description disponible"

**Suggested fix:** Create StringConstants class or implement localization system

---

## UNUSED ASSETS (For Manual Review)

### Unused Items (in registry but not used by activities)
- [ ] `Carpe.asset` - Fish item, no activity produces it
- [ ] `Truite.asset` - Fish item, no activity produces it
- [ ] `Hareng.asset` - Fish item, no activity produces it

### Unused Enemies (created but not assigned to locations)
- [ ] `Slime.asset` - Not in any location's AvailableEnemies
- [ ] `Goblin.asset` - Not in any location's AvailableEnemies
- [ ] `Wolf.asset` - USED (assigned to Foret_01)

### Unused Abilities (registered but no enemy uses them)
- [ ] `Heal.asset` - In AbilityRegistry but unused
- [ ] `VenomStrike.asset` - In AbilityRegistry but unused

---

## DEBUG SCRIPTS (Keep or Remove?)

These are in `Assets/Scripts/Debug/` - intentional debug tools:
- [ ] `ActivityRegistryDebugger.cs` - Editor-only registry debugger
- [ ] `InventoryDataDebugger.cs` - Editor-only inventory analyzer
- [ ] `PlayerDataDebugger.cs` - Editor-only player data diagnostics
- [ ] `EquipmentDebugger.cs` - Runtime debug script (might be obsolete)
- [ ] `RuntimePlayerDataFixer.cs` - On-device debug panel

---

## KEPT FOR FUTURE USE

These were identified as unused but intentionally kept:
- `CombatManager.SetPlayerMaxHealth()` - Will be needed for equipment-based health bonuses
- `AbilityManager.GetRemainingWeight()` - Could be useful for ability equipment UI

---

## HOW TO CONTINUE

When resuming this cleanup work with Claude:
1. Reference this file: `TODO_CLEANUP.md`
2. Pick a section (Medium Effort or Larger Refactors)
3. Ask Claude to implement specific items

Example prompts:
- "Continue the cleanup from TODO_CLEANUP.md - extract ContainerPanelUI base class"
- "Continue the cleanup - convert InventoryPanelUI Update() to event-driven"
- "Continue the cleanup - create SingletonManager<T> base class"
