---
name: add-content
description: Create new game content for StepQuest (locations, enemies, NPCs, items, abilities, activities, status effects). Use when adding new content to the game.
---

# Add Content Skill

Help the user create new game content for StepQuest by generating JSON specification files that get processed by the ClaudeContentCreator editor tool.

## Claude Content Creator Tool

**Location:** `Assets/Scripts/Editor/ClaudeContentCreator.cs`
**Menu:** `WalkAndRPG > Claude Content Creator`

This Unity editor tool processes JSON files to create ScriptableObjects automatically.

### Features
- Reads JSON files from `Assets/Editor/ContentQueue/`
- Creates Enemy, Ability, Item, and NPC ScriptableObjects
- Assigns content to locations (enemies, NPCs)
- Links abilities to enemies by ID
- Updates existing assets if they already exist
- Deletes processed JSON files after completion

### Supported Content Types
| Type | Creates | Can Assign to Location |
|------|---------|------------------------|
| Enemy | EnemyDefinition | Yes (with hidden/rarity) |
| Ability | AbilityDefinition | No |
| Item | ItemDefinition | No |
| NPC | NPCDefinition | Yes (with hidden/rarity) |

### Not Yet Supported (use Editor Windows)
- Locations (MapLocationDefinition)
- Activities (ActivityDefinition/ActivityVariant)
- Status Effects (StatusEffectDefinition)
- Loot tables on enemies

## How It Works

1. Gather requirements from the user
2. Generate a JSON file in `Assets/Editor/ContentQueue/`
3. User opens Unity and runs `WalkAndRPG > Claude Content Creator`
4. Click "Process All Files" to create the ScriptableObjects

## Step 1: Identify Content Type

Ask the user what type of content they want to create:

1. **Enemy** - A new enemy type (EnemyDefinition)
2. **NPC** - A new character (NPCDefinition)
3. **Item** - A new item (ItemDefinition) - Material, Consumable, or Equipment
4. **Ability** - A new combat ability (AbilityDefinition)
5. **Bundle** - Multiple related content (e.g., "new enemy with custom ability")

Note: Locations, Activities, and Status Effects still need manual creation via their Editor Windows.

## Step 2: Gather Requirements

For each content type, ask about required fields. Use snake_case for all IDs.

### Location
- ID (snake_case, e.g., `forest_clearing`)
- Display name
- Description
- Connections to other locations (and step costs)
- Available enemies (reference existing or create new)
- Available NPCs (reference existing or create new)
- Available activities (reference existing or create new)

### Enemy
- ID (snake_case, e.g., `forest_wolf`)
- Display name
- Base stats (HP, Attack, Defense, Speed)
- Abilities (reference existing or create new)
- Loot table (items, quantities, drop chances 0-1)
- Which locations should have this enemy

### NPC
- ID (snake_case, e.g., `village_blacksmith`)
- Name
- Description
- Avatar and Illustration (note: user provides images separately)
- Theme color (hex)
- Which locations should have this NPC

### Item
- ID (snake_case, e.g., `iron_ore`)
- Display name
- Description
- Item type: Material, Consumable, Equipment
- If Equipment: slot, stats, set (optional)
- Stack size (for materials/consumables)

### Ability
- ID (snake_case, e.g., `fireball`)
- Display name
- Description
- Effects (Damage, Heal, Shield, ApplyStatusEffect)
- Cooldown
- Weight (for equipment limit, default abilities are weight 1-2)
- Is it a player ability or enemy ability?

### Activity
- Activity type (Mining, Woodcutting, Fishing, Forging, Exploration, etc.)
- Variant ID (snake_case, e.g., `mine_iron`)
- Display name
- Is it step-based or time-based?
- Cost (steps or seconds)
- Required items (for crafting)
- Output items and quantities
- XP rewards (main skill + sub-skill)
- Which locations should have this activity

### Status Effect
- ID (snake_case, e.g., `burning`)
- Display name
- Description
- Effect type (Poison, Burn, Bleed, Stun, Regeneration, Shield, AttackBuff, etc.)
- Stacking behavior (Stacking, NoStacking)
- Decay behavior (None, Time, OnTick, OnHit)
- Duration, tick interval, potency as needed

## Step 3: Generate JSON File

Create a JSON file in `Assets/Editor/ContentQueue/` with the content specification.

### JSON Format

```json
{
  "enemies": [
    {
      "id": "rat",
      "name": "Rat",
      "filename": "Rat",
      "description": "Un rongeur agressif.",
      "level": 1,
      "maxHealth": 15,
      "xpReward": 3,
      "color": "8B7355",
      "abilityIds": ["basic_attack"],
      "victoryTitle": "Rat vaincu !",
      "victoryDescription": "Le rat s'enfuit dans l'ombre."
    }
  ],
  "abilities": [
    {
      "id": "bite",
      "name": "Morsure",
      "filename": "Bite",
      "description": "Une morsure rapide.",
      "cooldown": 1.5,
      "weight": 1,
      "color": "8B4513",
      "damage": 8,
      "isEnemyAbility": true
    }
  ],
  "items": [
    {
      "id": "rat_tail",
      "name": "Queue de Rat",
      "filename": "RatTail",
      "description": "Une queue de rat. Beurk.",
      "itemType": "Material",
      "maxStack": 99
    }
  ],
  "npcs": [
    {
      "id": "merchant_bob",
      "name": "Bob le Marchand",
      "filename": "MerchantBob",
      "description": "Un marchand jovial.",
      "color": "DAA520"
    }
  ],
  "locationAssignments": [
    {
      "locationId": "village_01",
      "enemyIds": [
        { "id": "rat", "isHidden": true, "rarity": 0 }
      ],
      "npcIds": [
        { "id": "merchant_bob", "isHidden": false, "rarity": 0 }
      ]
    }
  ]
}
```

### Rarity Values
- 0 = Common
- 1 = Uncommon
- 2 = Rare
- 3 = Epic
- 4 = Legendary

### File Naming
- Name the JSON file descriptively: `add_rat_enemy.json`, `new_forest_content.json`
- The `filename` field in each spec determines the .asset filename

## Step 4: Instruct User

After creating the JSON file, tell the user:

1. Open Unity
2. Go to `WalkAndRPG > Claude Content Creator`
3. Click "Process All Files"
4. The content will be created automatically

## Step 5: Offer Follow-ups

After creating the JSON, ask:
- "Want me to create any related content?" (e.g., abilities for the enemy, loot items)
- "Should I add more enemies/NPCs to this location?"
- "Want me to create a custom ability for this enemy?"

## Conventions Reminder

- All IDs use snake_case
- French display names are common in this project
- Colors are hex without the # prefix
- Reference existing abilities by their AbilityID (e.g., "basic_attack")
- XP values: weak enemies ~3-5, medium ~10-20, strong ~30-50
