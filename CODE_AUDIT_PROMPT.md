# StepQuest Codebase Audit Prompt

You are a senior software architect tasked with auditing a Unity C# codebase for a mobile RPG called StepQuest. Your goal is to identify code health issues, redundancies, and architectural inefficiencies.

## Project Context

- **Type**: Unity mobile RPG (Android) using step counting as core mechanic
- **Language**: C# (.NET Standard 2.1)
- **Architecture**: Singleton managers, EventBus pattern, ScriptableObjects for data
- **Key Folders**:
  - `Assets/Scripts/Core/` - GameManager, EventBus, GameEvents
  - `Assets/Scripts/Gameplay/` - Player, Combat, World, NPC, Exploration systems
  - `Assets/Scripts/Services/` - Step tracking, platform services
  - `Assets/Scripts/Data/` - DataManager, PlayerData, models
  - `Assets/Scripts/UI/` - All UI panels and components
  - `Assets/Scripts/Debug/` - Debug tools
  - `Assets/Scripts/Editor/` - Unity editor windows

## Audit Categories

### 1. Dead Code Detection

Identify code that is never called or used:

- **Unused public methods**: Methods declared but never invoked anywhere
- **Unused private methods**: Helper methods that lost their callers
- **Unused fields/properties**: Variables declared but never read
- **Unused classes**: Entire classes that are never instantiated or referenced
- **Unused events**: Events declared but never subscribed to or published
- **Commented-out code blocks**: Old code left in comments that should be deleted
- **Unused ScriptableObject fields**: Fields in SO definitions never used by runtime code
- **Orphaned event handlers**: Methods subscribed to events that no longer exist

For each finding, report:
- File path and line number
- The unused element
- Confidence level (definitely unused vs. potentially unused via reflection/Unity callbacks)

### 2. Duplicate/Redundant Code

Find code that does the same thing in multiple places:

- **Copy-pasted logic**: Similar code blocks across different files
- **Redundant utility methods**: Multiple implementations of the same helper
- **Duplicate validation logic**: Same checks repeated in multiple places
- **Repeated event patterns**: Same subscribe/unsubscribe patterns that could be abstracted
- **Duplicate UI setup code**: Similar panel initialization across UI classes
- **Redundant null checks**: Excessive defensive coding that could be centralized
- **Similar service classes**: Services that overlap significantly in functionality

For each finding, report:
- All locations where the duplication occurs
- What the duplicated logic does
- Suggested refactoring approach

### 3. Forgotten/Underutilized Methods

Find existing methods that should be used but aren't:

- **Reinvented wheels**: New code that duplicates existing utility methods
- **Underused helper classes**: Utility classes with useful methods nobody calls
- **Ignored convenience methods**: Methods designed to simplify common patterns but bypassed
- **Unused EventBus events**: Events defined in GameEvents.cs that nothing publishes
- **Unused registry methods**: Lookup/query methods on registries never called
- **Unused PlayerData methods**: Data access methods that could simplify other code

For each finding, report:
- The underutilized method and its location
- Places where it should be used instead of current approach
- Impact of adopting the existing method

### 4. Circular Dependencies & Unnecessary Coupling

Find architectural issues:

- **Manager cross-dependencies**: Managers that call each other in circular patterns
- **Tight coupling**: Classes that know too much about each other's internals
- **Event bypassing**: Direct method calls where events would be more appropriate
- **Service leakage**: Internal services exposed or accessed directly
- **Singleton abuse**: Places using singletons that could use dependency injection
- **Data model coupling**: UI directly manipulating PlayerData instead of going through managers

For each finding, report:
- The circular or tightly coupled relationship
- Why it's problematic
- Suggested decoupling approach

### 5. Inconsistent Patterns

Find places where the codebase doesn't follow its own conventions:

- **Inconsistent event usage**: Some systems use EventBus, others use direct calls
- **Inconsistent error handling**: Some methods throw, others return null/false
- **Inconsistent logging**: Varied use of Logger categories
- **Inconsistent null handling**: Mix of null checks, null-conditional, and no checks
- **Inconsistent naming**: snake_case vs PascalCase vs camelCase inconsistencies
- **Inconsistent async patterns**: Mix of coroutines, async/await, and callbacks

For each finding, report:
- The inconsistency and examples
- What the established pattern should be
- Which occurrences should be changed

### 6. Performance Concerns

Find potential performance issues:

- **Repeated expensive operations**: Lookups in Update() that could be cached
- **Unnecessary allocations**: String concatenation, LINQ in hot paths
- **Dictionary access patterns**: Repeated TryGetValue when value is already held
- **Event subscription leaks**: Missing Unsubscribe calls in OnDestroy
- **Excessive logging**: Debug logs that should be removed or guarded
- **Heavy operations on main thread**: Blocking calls that should be async

For each finding, report:
- Location and the performance concern
- Estimated impact (critical, moderate, minor)
- Suggested fix

## Output Format

Organize your findings into these sections:

```
## Executive Summary
- Total issues found per category
- Critical issues requiring immediate attention
- Quick wins (easy fixes with high impact)

## Critical Issues
[Issues that could cause bugs, crashes, or significant problems]

## Dead Code
[List all dead code with locations and confidence levels]

## Duplications
[List all duplications with locations and refactoring suggestions]

## Underutilized Methods
[List methods that exist but aren't being used where they should be]

## Architectural Issues
[Circular dependencies, coupling problems, pattern violations]

## Performance Issues
[Performance concerns ordered by impact]

## Recommendations
- Priority-ordered action items
- Suggested refactoring order
- Estimated effort levels (small/medium/large)
```

## Special Attention Areas

Based on project history, pay extra attention to:

1. **DataManager and its services** - Complex data layer, possible redundancy
2. **MapManager and travel services** - Multiple services, potential overlap
3. **ActivityManager** - Handles both step-based and time-based, may have dead paths
4. **Combat system** - Recently implemented, may have unused scaffolding
5. **UI panels** - Many similar patterns, likely duplication
6. **Event system** - GameEvents.cs may have unused events
7. **Exploration system** - Recently added, may not be fully integrated

## What NOT to Flag

- Unity lifecycle methods (Awake, Start, Update) even if they look unused
- Public methods on MonoBehaviours that might be called via Unity Events
- ScriptableObject fields used by Unity serialization
- Editor-only code (#if UNITY_EDITOR blocks)
- Test/debug code in Debug/ and Editor/ folders (unless it references production code that's dead)

## Deliverable

Provide a thorough, actionable report that a developer can use to:
1. Safely delete dead code
2. Consolidate duplicate logic
3. Better utilize existing methods
4. Improve architectural consistency
5. Address performance concerns

Be specific with file paths and line numbers. Prioritize findings by impact and effort required to fix.
