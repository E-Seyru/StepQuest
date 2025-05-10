// Purpose: Stores and provides access to all available crafting recipes.
// Filepath: Assets/Scripts/Gameplay/Crafting/RecipeRegistry.cs
using UnityEngine;
using System.Collections.Generic; // For Dictionary

public class RecipeRegistry : MonoBehaviour
{
    // TODO: Use ScriptableObjects for Recipe definitions? (Recommended)
    // Store a list of RecipeDefinition ScriptableObjects assigned in the Inspector
    // public List<RecipeDefinition> allRecipes;

    // TODO: Or load recipes from configuration files (JSON, XML)

    // TODO: Store recipes in a Dictionary for quick lookup by ID
    // private Dictionary<string, RecipeDefinition> recipeMap;

    void Awake()
    {
        // TODO: Populate the recipeMap from the loaded recipes (ScriptableObjects or files)
        // recipeMap = new Dictionary<string, RecipeDefinition>();
        // foreach (var recipe in allRecipes) {
        //     if (recipe != null && !recipeMap.ContainsKey(recipe.RecipeID)) {
        //         recipeMap.Add(recipe.RecipeID, recipe);
        //     }
        // }
        Debug.Log("RecipeRegistry: Initialized (Placeholder - Load recipes)");
    }

    public /* RecipeDefinition */ object GetRecipe(string recipeId)
    {
        // TODO: Look up recipe in the map
        // recipeMap.TryGetValue(recipeId, out RecipeDefinition recipe);
        // return recipe; // Return null if not found
        Debug.Log($"RecipeRegistry: GetRecipe {recipeId} (Placeholder)");
        return null; // Placeholder
    }

    public List</* RecipeDefinition */ object> GetAllRecipes()
    {
        // TODO: Return a list of all loaded recipe definitions
        // return new List<RecipeDefinition>(recipeMap.Values);
        return new List<object>(); // Placeholder
    }

    public List</* RecipeDefinition */ object> GetRecipesForSkill(SkillType skill)
    {
        // TODO: Filter recipes based on the required crafting skill
        return new List<object>(); // Placeholder
    }
}

// Example ScriptableObject structure (Create -> WalkAndRPG -> Recipe Definition)
// [CreateAssetMenu(fileName = "NewRecipe", menuName = "WalkAndRPG/Recipe Definition")]
// public class RecipeDefinition : ScriptableObject
// {
//     public string RecipeID; // Unique identifier
//     public string CraftedItemID; // Reference to ItemDefinition ID
//     public int CraftedQuantity = 1;
//
//     public List<Ingredient> RequiredIngredients;
//     public float CraftingTimeSeconds;
//
//     public SkillType RequiredSkill; // e.g., Alchemy, Blacksmithing
//     public int RequiredSkillLevel = 1;
//     public float ExperienceGranted; // XP for the crafting skill
// }

// [System.Serializable]
// public struct Ingredient
// {
//     public string ItemID; // Reference to ItemDefinition ID
//     public int Quantity;
// }