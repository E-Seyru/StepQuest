// Purpose: Creates sample dialogue assets and abilities for testing
// Filepath: Assets/Scripts/Editor/SampleDialogueCreator.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to create sample dialogues and abilities for testing the dialogue system.
/// Creates a complete story progression for the Homeless Child NPC.
/// </summary>
public class SampleDialogueCreator : EditorWindow
{
    [MenuItem("WalkAndRPG/Social/Create Homeless Child Content")]
    public static void CreateHomelessChildContent()
    {
        // Create ThrowRock ability first
        var throwRockAbility = CreateThrowRockAbility();

        // Find Homeless_Child NPC
        NPCDefinition homelessChild = FindNPC("homeless_child");
        if (homelessChild == null)
        {
            Logger.LogWarning("SampleDialogueCreator: Could not find Homeless_Child NPC. Creating dialogues without NPC assignment.", Logger.LogCategory.EditorLog);
        }

        // Create dialogue folder
        string folderPath = "Assets/ScriptableObjects/Dialogues/Homeless_Child";
        CreateFolderRecursive(folderPath);

        // Delete existing dialogues in folder
        DeleteExistingDialogues(folderPath);

        // Create all dialogues
        var introDialogue = CreateIntroDialogue(folderPath);
        var buildingTrustDialogue = CreateBuildingTrustDialogue(folderPath);
        var playRocksDialogue = CreatePlayRocksDialogue(folderPath, throwRockAbility);
        var friendDialogue = CreateFriendDialogue(folderPath);
        var defaultDialogue = CreateDefaultDialogue(folderPath);

        // Assign to NPC
        if (homelessChild != null)
        {
            if (homelessChild.Dialogues == null)
                homelessChild.Dialogues = new List<DialogueDefinition>();

            homelessChild.Dialogues.Clear();
            homelessChild.Dialogues.Add(introDialogue);
            homelessChild.Dialogues.Add(buildingTrustDialogue);
            homelessChild.Dialogues.Add(playRocksDialogue);
            homelessChild.Dialogues.Add(friendDialogue);
            homelessChild.Dialogues.Add(defaultDialogue);

            EditorUtility.SetDirty(homelessChild);
        }

        // Register ability if registry exists
        RegisterAbility(throwRockAbility);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Logger.LogInfo("SampleDialogueCreator: Created ThrowRock ability and 5 dialogues for Homeless_Child", Logger.LogCategory.EditorLog);
        EditorUtility.DisplayDialog("Homeless Child Content Created",
            "Created:\n\n" +
            "ABILITY:\n" +
            "• ThrowRock (damage ability)\n\n" +
            "DIALOGUES:\n" +
            "• HC_Intro (first meeting)\n" +
            "• HC_BuildingTrust (second meeting)\n" +
            "• HC_PlayRocks (offers to play, grants ability!)\n" +
            "• HC_Friend (after playing together)\n" +
            "• HC_Default (fallback)\n\n" +
            "Story: First meeting -> Build trust -> Play rocks -> Friends!",
            "OK");
    }

    [MenuItem("WalkAndRPG/Social/Reset Homeless Child Flags (Debug)")]
    public static void ResetHomelessChildFlags()
    {
        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null)
        {
            Logger.LogWarning("Cannot reset flags: PlayerData not available (game not running?)", Logger.LogCategory.EditorLog);
            return;
        }

        // Log current values before reset
        Logger.LogInfo($"Before reset - HC_HasMet: {playerData.GetDialogueFlag("HC_HasMet")}, " +
                  $"HC_TalkedTwice: {playerData.GetDialogueFlag("HC_TalkedTwice")}, " +
                  $"HC_PlayedRocks: {playerData.GetDialogueFlag("HC_PlayedRocks")}, " +
                  $"Relationship: {playerData.GetNPCRelationship("homeless_child")}", Logger.LogCategory.EditorLog);

        // Reset dialogue flags (directly modify the dictionary for consistency)
        var dialogueFlags = playerData.DialogueFlags;
        dialogueFlags["HC_HasMet"] = false;
        dialogueFlags["HC_TalkedTwice"] = false;
        dialogueFlags["HC_PlayedRocks"] = false;
        playerData.DialogueFlags = dialogueFlags;

        // Reset NPC relationship (directly modify the dictionary for consistency)
        var relationships = playerData.NPCRelationships;
        relationships["homeless_child"] = 0;
        playerData.NPCRelationships = relationships;

        // Log values after reset (before save)
        Logger.LogInfo($"After reset - HC_HasMet: {playerData.GetDialogueFlag("HC_HasMet")}, " +
                  $"HC_TalkedTwice: {playerData.GetDialogueFlag("HC_TalkedTwice")}, " +
                  $"HC_PlayedRocks: {playerData.GetDialogueFlag("HC_PlayedRocks")}, " +
                  $"Relationship: {playerData.GetNPCRelationship("homeless_child")}", Logger.LogCategory.EditorLog);

        // Remove ThrowRock ability if owned (directly from PlayerData)
        bool abilityRemoved = false;
        var ownedAbilities = playerData.OwnedAbilities;
        if (ownedAbilities.Contains("throw_rock"))
        {
            ownedAbilities.Remove("throw_rock");
            playerData.OwnedAbilities = ownedAbilities;
            abilityRemoved = true;

            // Also remove from equipped if it was equipped
            var equippedAbilities = playerData.EquippedAbilities;
            if (equippedAbilities.Contains("throw_rock"))
            {
                equippedAbilities.Remove("throw_rock");
                playerData.EquippedAbilities = equippedAbilities;
            }
        }

        DataManager.Instance.SaveGame();

        Logger.LogInfo("Reset all Homeless Child dialogue flags, relationship, and ability", Logger.LogCategory.EditorLog);
        EditorUtility.DisplayDialog("Flags Reset",
            "Reset the following:\n" +
            "• HC_HasMet = false\n" +
            "• HC_TalkedTwice = false\n" +
            "• HC_PlayedRocks = false\n" +
            "• Relationship = 0\n" +
            (abilityRemoved ? "• ThrowRock ability removed" : "• ThrowRock ability (not owned)"),
            "OK");
    }

    // === ABILITY CREATION ===

    private static AbilityDefinition CreateThrowRockAbility()
    {
        string folderPath = "Assets/ScriptableObjects/Abilities";
        CreateFolderRecursive(folderPath);

        string path = $"{folderPath}/ThrowRock.asset";

        // Check if already exists
        var existing = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
        if (existing != null)
        {
            Logger.LogInfo("ThrowRock ability already exists, updating it", Logger.LogCategory.EditorLog);
            // Update existing
            existing.AbilityID = "throw_rock";
            existing.AbilityName = "Lancer de Pierre";
            existing.Description = "Lance une pierre sur l'ennemi. Un cadeau d'un ami special.";
            existing.Cooldown = 3f;
            existing.Weight = 1;
            existing.AbilityColor = new Color(0.6f, 0.5f, 0.4f); // Brownish stone color

            existing.Effects = new List<AbilityEffect>
            {
                new AbilityEffect
                {
                    Type = AbilityEffectType.Damage,
                    Value = 8f,
                    TargetsSelf = false
                }
            };

            existing.DeveloperNotes = "Obtained from Homeless Child after playing rocks together";
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // Create new
        var ability = ScriptableObject.CreateInstance<AbilityDefinition>();
        ability.AbilityID = "throw_rock";
        ability.AbilityName = "Lancer de Pierre";
        ability.Description = "Lance une pierre sur l'ennemi. Un cadeau d'un ami special.";
        ability.Cooldown = 3f;
        ability.Weight = 1;
        ability.AbilityColor = new Color(0.6f, 0.5f, 0.4f);

        ability.Effects = new List<AbilityEffect>
        {
            new AbilityEffect
            {
                Type = AbilityEffectType.Damage,
                Value = 8f,
                TargetsSelf = false
            }
        };

        ability.DeveloperNotes = "Obtained from Homeless Child after playing rocks together";

        AssetDatabase.CreateAsset(ability, path);
        Logger.LogInfo($"Created ThrowRock ability at {path}", Logger.LogCategory.EditorLog);
        return ability;
    }

    private static void RegisterAbility(AbilityDefinition ability)
    {
        // Find AbilityRegistry
        string[] guids = AssetDatabase.FindAssets("t:AbilityRegistry");
        if (guids.Length == 0)
        {
            Logger.LogWarning("No AbilityRegistry found. Please manually add ThrowRock to the registry.", Logger.LogCategory.EditorLog);
            return;
        }

        string registryPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var registry = AssetDatabase.LoadAssetAtPath<AbilityRegistry>(registryPath);

        if (registry != null && registry.AllAbilities != null)
        {
            // Check if already registered
            bool found = false;
            for (int i = 0; i < registry.AllAbilities.Count; i++)
            {
                if (registry.AllAbilities[i] != null && registry.AllAbilities[i].AbilityID == ability.AbilityID)
                {
                    registry.AllAbilities[i] = ability;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                registry.AllAbilities.Add(ability);
            }

            EditorUtility.SetDirty(registry);
            Logger.LogInfo("Registered ThrowRock ability in AbilityRegistry", Logger.LogCategory.EditorLog);
        }
    }

    // === DIALOGUE CREATION ===

    /// <summary>
    /// First meeting - introduces the child
    /// Priority: 100 (highest)
    /// Condition: HC_HasMet = false
    /// Sets: HC_HasMet = true
    /// </summary>
    private static DialogueDefinition CreateIntroDialogue(string folderPath)
    {
        var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
        dialogue.DialogueID = "hc_intro";
        dialogue.DialogueName = "Premiere Rencontre";
        dialogue.Priority = 100;
        dialogue.DeveloperNotes = "First meeting. Sets HC_HasMet flag.";

        // Condition: Must NOT have met before
        dialogue.Conditions = new List<DialogueCondition>
        {
            new DialogueCondition
            {
                Type = ConditionType.Flag,
                Key = "HC_HasMet",
                Operator = ComparisonOperator.Equals,
                Value = 0
            }
        };

        // Lines
        dialogue.Lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*L'enfant leve les yeux vers vous, surpris*",
                Emotion = NPCEmotion.Surprise,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Oh... Bonjour. Les gens passent d'habitude sans me voir.",
                Emotion = NPCEmotion.Sadness,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Vous... vous voulez me parler ?",
                Emotion = NPCEmotion.Curiosity,
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice
                    {
                        ChoiceText = "Oui, comment tu t'appelles ?",
                        RelationshipChange = 1
                    },
                    new DialogueChoice
                    {
                        ChoiceText = "Je passais juste par la.",
                        RelationshipChange = 0
                    }
                }
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*Un petit sourire apparait sur son visage*",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Merci de m'avoir regarde. Revenez me voir si vous voulez !",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>()
            }
        };

        dialogue.FlagsToSetOnCompletion = new List<string> { "HC_HasMet" };

        string path = $"{folderPath}/HC_Intro.asset";
        AssetDatabase.CreateAsset(dialogue, path);
        return dialogue;
    }

    /// <summary>
    /// Second meeting - building trust
    /// Priority: 80
    /// Condition: HC_HasMet = true AND HC_TalkedTwice = false
    /// Sets: HC_TalkedTwice = true
    /// </summary>
    private static DialogueDefinition CreateBuildingTrustDialogue(string folderPath)
    {
        var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
        dialogue.DialogueID = "hc_building_trust";
        dialogue.DialogueName = "Confiance Naissante";
        dialogue.Priority = 80;
        dialogue.DeveloperNotes = "Second meeting. Child starts warming up.";

        dialogue.Conditions = new List<DialogueCondition>
        {
            new DialogueCondition
            {
                Type = ConditionType.Flag,
                Key = "HC_HasMet",
                Operator = ComparisonOperator.Equals,
                Value = 1
            },
            new DialogueCondition
            {
                Type = ConditionType.Flag,
                Key = "HC_TalkedTwice",
                Operator = ComparisonOperator.Equals,
                Value = 0
            }
        };

        dialogue.Lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*L'enfant vous reconnait et s'illumine*",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Vous etes revenu ! Je... je pensais pas que vous reviendriez.",
                Emotion = NPCEmotion.Surprise,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Vous savez, j'aime bien ramasser des cailloux. Y en a des jolis par ici.",
                Emotion = NPCEmotion.Curiosity,
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice
                    {
                        ChoiceText = "Ah oui ? Tu me montres ?",
                        RelationshipChange = 2
                    },
                    new DialogueChoice
                    {
                        ChoiceText = "C'est interessant.",
                        RelationshipChange = 1
                    }
                }
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*Il sort quelques cailloux de sa poche et les regarde avec fierte*",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Peut-etre qu'un jour... on pourrait jouer ensemble ?",
                Emotion = NPCEmotion.Embarrassment,
                Choices = new List<DialogueChoice>()
            }
        };

        dialogue.FlagsToSetOnCompletion = new List<string> { "HC_TalkedTwice" };

        string path = $"{folderPath}/HC_BuildingTrust.asset";
        AssetDatabase.CreateAsset(dialogue, path);
        return dialogue;
    }

    /// <summary>
    /// Third meeting - offer to play rocks, grants ThrowRock ability!
    /// Priority: 60
    /// Condition: HC_TalkedTwice = true AND HC_PlayedRocks = false
    /// Sets: HC_PlayedRocks = true (via choice)
    /// Reward: ThrowRock ability (via choice)
    /// </summary>
    private static DialogueDefinition CreatePlayRocksDialogue(string folderPath, AbilityDefinition throwRockAbility)
    {
        var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
        dialogue.DialogueID = "hc_play_rocks";
        dialogue.DialogueName = "Jouer aux Cailloux";
        dialogue.Priority = 60;
        dialogue.DeveloperNotes = "Child offers to play! Grants ThrowRock ability if player agrees.";

        dialogue.Conditions = new List<DialogueCondition>
        {
            new DialogueCondition
            {
                Type = ConditionType.Flag,
                Key = "HC_TalkedTwice",
                Operator = ComparisonOperator.Equals,
                Value = 1
            },
            new DialogueCondition
            {
                Type = ConditionType.Flag,
                Key = "HC_PlayedRocks",
                Operator = ComparisonOperator.Equals,
                Value = 0
            }
        };

        dialogue.Lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*L'enfant sautille sur place en vous voyant*",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Vous savez quoi ? J'ai trouve plein de beaux cailloux !",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "On... on pourrait jouer a les lancer ensemble ? S'il vous plait !",
                Emotion = NPCEmotion.Curiosity,
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice
                    {
                        ChoiceText = "Oui, jouons ensemble !",
                        FlagToSet = "HC_PlayedRocks",
                        RelationshipChange = 3,
                        AbilityToGrant = "throw_rock",
                        NextLineIndex = 3  // Jump to playing scene
                    },
                    new DialogueChoice
                    {
                        ChoiceText = "Pas maintenant, desole.",
                        RelationshipChange = -1,
                        NextLineIndex = 7  // Jump to sad response
                    }
                }
            },
            // === PLAYED ROCKS PATH (lines 3-6) ===
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*Ses yeux s'illuminent de bonheur*",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Genial ! Regardez, on vise ce tronc la-bas !",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "",  // Narration
                Text = "*Vous passez un bon moment a lancer des cailloux ensemble. L'enfant vous montre sa technique.*",
                Emotion = NPCEmotion.Neutral,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Vous etes doue ! Tenez, gardez ces cailloux. Ils sont speciaux, comme notre amitie !",
                Emotion = NPCEmotion.Love,
                Choices = new List<DialogueChoice>(),
                ShowReward = true,   // Show the ability reward on this line
                EndsDialogue = true  // End dialogue after this line
            },
            // === DECLINED PATH (lines 7-8) ===
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*Son sourire s'efface un peu*",
                Emotion = NPCEmotion.Sadness,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Oh... d'accord. Peut-etre une autre fois alors.",
                Emotion = NPCEmotion.Sadness,
                Choices = new List<DialogueChoice>()
            }
        };

        string path = $"{folderPath}/HC_PlayRocks.asset";
        AssetDatabase.CreateAsset(dialogue, path);
        return dialogue;
    }

    /// <summary>
    /// After playing - now friends!
    /// Priority: 40
    /// Condition: HC_PlayedRocks = true
    /// </summary>
    private static DialogueDefinition CreateFriendDialogue(string folderPath)
    {
        var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
        dialogue.DialogueID = "hc_friend";
        dialogue.DialogueName = "Ami pour Toujours";
        dialogue.Priority = 40;
        dialogue.DeveloperNotes = "After playing rocks together. They are friends now!";

        dialogue.Conditions = new List<DialogueCondition>
        {
            new DialogueCondition
            {
                Type = ConditionType.Flag,
                Key = "HC_PlayedRocks",
                Operator = ComparisonOperator.Equals,
                Value = 1
            }
        };

        dialogue.Lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*L'enfant vous fait un grand signe de la main*",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Mon ami ! Vous utilisez toujours les cailloux que je vous ai donnes ?",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice
                    {
                        ChoiceText = "Bien sur ! Ils sont tres utiles.",
                        RelationshipChange = 1
                    },
                    new DialogueChoice
                    {
                        ChoiceText = "Je les garde precieusement.",
                        RelationshipChange = 1
                    }
                }
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*Il sourit de toutes ses dents*",
                Emotion = NPCEmotion.Joy,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Je suis content d'avoir un ami comme vous. Revenez vite !",
                Emotion = NPCEmotion.Love,
                Choices = new List<DialogueChoice>()
            }
        };

        string path = $"{folderPath}/HC_Friend.asset";
        AssetDatabase.CreateAsset(dialogue, path);
        return dialogue;
    }

    /// <summary>
    /// Default fallback dialogue
    /// Priority: 0 (lowest)
    /// No conditions
    /// </summary>
    private static DialogueDefinition CreateDefaultDialogue(string folderPath)
    {
        var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
        dialogue.DialogueID = "hc_default";
        dialogue.DialogueName = "Discussion Simple";
        dialogue.Priority = 0;
        dialogue.DeveloperNotes = "Fallback dialogue. Should rarely play due to other dialogues.";

        // No conditions - always available as fallback
        dialogue.Conditions = new List<DialogueCondition>();

        dialogue.Lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*L'enfant vous regarde avec curiosite*",
                Emotion = NPCEmotion.Curiosity,
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Bonjour voyageur. C'est gentil de passer me voir.",
                Emotion = NPCEmotion.Neutral,
                Choices = new List<DialogueChoice>()
            }
        };

        string path = $"{folderPath}/HC_Default.asset";
        AssetDatabase.CreateAsset(dialogue, path);
        return dialogue;
    }

    // === UTILITY METHODS ===

    private static void DeleteExistingDialogues(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath)) return;

        string[] guids = AssetDatabase.FindAssets("t:DialogueDefinition", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static NPCDefinition FindNPC(string npcId)
    {
        string[] guids = AssetDatabase.FindAssets("t:NPCDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var npc = AssetDatabase.LoadAssetAtPath<NPCDefinition>(path);
            if (npc != null && npc.NPCID != null && npc.NPCID.ToLower() == npcId.ToLower())
            {
                return npc;
            }
        }
        return null;
    }

    private static void CreateFolderRecursive(string path)
    {
        string[] parts = path.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string newPath = currentPath + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(newPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }
            currentPath = newPath;
        }
    }
}
#endif
