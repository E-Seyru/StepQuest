// Purpose: Creates sample dialogue assets for testing
// Filepath: Assets/Scripts/Editor/SampleDialogueCreator.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to create sample dialogues for testing the dialogue system.
/// </summary>
public class SampleDialogueCreator : EditorWindow
{
    [MenuItem("WalkAndRPG/Social/Create Sample Dialogues")]
    public static void CreateSampleDialogues()
    {
        // Find Homeless_Child NPC
        NPCDefinition homelessChild = FindNPC("homeless_child");
        if (homelessChild == null)
        {
            Debug.LogWarning("SampleDialogueCreator: Could not find Homeless_Child NPC. Creating dialogues without NPC assignment.");
        }

        // Create folder
        string folderPath = "Assets/ScriptableObjects/Dialogues/Homeless_Child";
        CreateFolderRecursive(folderPath);

        // Create dialogues
        var introDialogue = CreateIntroDialogue(folderPath);
        var hungryDialogue = CreateHungryDialogue(folderPath);
        var defaultDialogue = CreateDefaultDialogue(folderPath);

        // Assign to NPC
        if (homelessChild != null)
        {
            if (homelessChild.Dialogues == null)
                homelessChild.Dialogues = new List<DialogueDefinition>();

            homelessChild.Dialogues.Clear();
            homelessChild.Dialogues.Add(introDialogue);
            homelessChild.Dialogues.Add(hungryDialogue);
            homelessChild.Dialogues.Add(defaultDialogue);

            EditorUtility.SetDirty(homelessChild);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("SampleDialogueCreator: Created 3 sample dialogues for Homeless_Child");
        EditorUtility.DisplayDialog("Sample Dialogues Created",
            "Created 3 dialogues:\n" +
            "• HC_Intro (first meeting)\n" +
            "• HC_Hungry (before being fed)\n" +
            "• HC_Default (fallback)\n\n" +
            "Assigned to Homeless_Child NPC.",
            "OK");
    }

    private static DialogueDefinition CreateIntroDialogue(string folderPath)
    {
        var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
        dialogue.DialogueID = "hc_intro";
        dialogue.DialogueName = "First Meeting";
        dialogue.Priority = 100;  // High priority for first meeting
        dialogue.DeveloperNotes = "Plays only when HC_HasMet flag is not set";

        // Condition: Must NOT have met before
        dialogue.Conditions = new List<DialogueCondition>
        {
            new DialogueCondition
            {
                Type = ConditionType.Flag,
                Key = "HC_HasMet",
                Operator = ComparisonOperator.Equals,
                Value = 0  // Flag must be false (not set)
            }
        };

        // Lines
        dialogue.Lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*L'enfant leve les yeux vers vous, l'air mefiant*",
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Vous... vous etes pas comme les autres. Vous me regardez vraiment.",
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "La plupart des gens passent sans meme me voir...",
                Choices = new List<DialogueChoice>
                {
                    DialogueChoice.CreateWithEffects("Comment t'appelles-tu ?", null, 1),
                    DialogueChoice.CreateWithEffects("Tu as l'air d'avoir faim.", null, 1),
                    DialogueChoice.Create("Je dois y aller.")
                }
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*Un faible sourire apparait sur son visage*",
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Merci de m'avoir parle. Ca fait du bien...",
                Choices = new List<DialogueChoice>()
            }
        };

        // Set flag after completion
        dialogue.FlagsToSetOnCompletion = new List<string> { "HC_HasMet" };

        string path = $"{folderPath}/HC_Intro.asset";
        AssetDatabase.CreateAsset(dialogue, path);
        return dialogue;
    }

    private static DialogueDefinition CreateHungryDialogue(string folderPath)
    {
        var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
        dialogue.DialogueID = "hc_hungry";
        dialogue.DialogueName = "Hungry Child";
        dialogue.Priority = 50;  // Medium priority
        dialogue.DeveloperNotes = "Plays after meeting, before feeding";

        // Conditions: Has met, but hasn't been fed
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
                Key = "HC_WasFed",
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
                Text = "*L'enfant vous reconnait et sourit faiblement*",
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Vous etes revenu... Mon ventre grogne un peu...",
                Choices = new List<DialogueChoice>
                {
                    DialogueChoice.CreateWithEffects("Tiens, prends ca.", "HC_WasFed", 2),
                    DialogueChoice.Create("Je n'ai rien a te donner pour l'instant.")
                }
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Merci d'etre passe me voir quand meme.",
                Choices = new List<DialogueChoice>()
            }
        };

        string path = $"{folderPath}/HC_Hungry.asset";
        AssetDatabase.CreateAsset(dialogue, path);
        return dialogue;
    }

    private static DialogueDefinition CreateDefaultDialogue(string folderPath)
    {
        var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
        dialogue.DialogueID = "hc_default";
        dialogue.DialogueName = "Default Chat";
        dialogue.Priority = 0;  // Lowest priority - fallback
        dialogue.DeveloperNotes = "Fallback dialogue when no other conditions are met";

        // No conditions - always available as fallback
        dialogue.Conditions = new List<DialogueCondition>();

        // Lines
        dialogue.Lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*L'enfant vous regarde avec curiosite*",
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Bonjour, voyageur. C'est gentil de passer me voir.",
                Choices = new List<DialogueChoice>()
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "Les journees sont longues ici, mais ca va.",
                Choices = new List<DialogueChoice>
                {
                    DialogueChoice.CreateWithRelationship("Je reviendrai te voir.", 1),
                    DialogueChoice.Create("Prends soin de toi.")
                }
            },
            new DialogueLine
            {
                Speaker = "Enfant sans-abri",
                Text = "*L'enfant hoche la tete*",
                Choices = new List<DialogueChoice>()
            }
        };

        string path = $"{folderPath}/HC_Default.asset";
        AssetDatabase.CreateAsset(dialogue, path);
        return dialogue;
    }

    private static NPCDefinition FindNPC(string npcId)
    {
        string[] guids = AssetDatabase.FindAssets("t:NPCDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var npc = AssetDatabase.LoadAssetAtPath<NPCDefinition>(path);
            if (npc != null && npc.NPCID.ToLower() == npcId.ToLower())
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
