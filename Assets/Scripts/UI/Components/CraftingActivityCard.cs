// Purpose: UI Card for Crafting Activities (Time-based activities) with red overlay and gray overlay
// Filepath: Assets/Scripts/UI/Components/CraftingActivityCard.cs
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingActivityCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI levelRequiredText;
    [SerializeField] private TextMeshProUGUI timeRequiredText;
    [SerializeField] private Transform ingredientsContainer;
    [SerializeField] private Button cardButton;

    [Header("Red Overlay")]
    [SerializeField] private GameObject redOverlay; // Voile rouge pour ressources manquantes

    [Header("Gray Overlay")]
    [SerializeField] private GameObject grayOverlay; // Voile gris pour niveau insuffisant

    [Header("Level Background Colors")]
    [SerializeField] private Image levelBackground; // Background du texte de niveau
    [SerializeField] private Color normalLevelBackgroundColor = Color.white;
    [SerializeField] private Color insufficientLevelBackgroundColor = Color.red;

    [Header("Shake Animation")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeIntensity = 10f;

    [Header("Prefabs")]
    [SerializeField] private GameObject ingredientPrefab;

    // Data
    private ActivityVariant activityVariant;
    private List<GameObject> instantiatedIngredients = new List<GameObject>();
    private bool hasEnoughIngredients = true;
    private bool hasRequiredLevel = true;

    // Events
    public System.Action<ActivityVariant> OnCardClicked;

    void Start()
    {
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardButtonClicked);
        }
    }

    /// <summary>
    /// Setup the card with activity variant data
    /// </summary>
    public void Setup(ActivityVariant variant)
    {
        if (variant == null)
        {
            Debug.LogWarning("CraftingActivityCard: Cannot setup with null variant!");
            return;
        }

        if (!variant.IsTimeBased)
        {
            Debug.LogWarning($"CraftingActivityCard: Variant '{variant.VariantName}' is not time-based!");
            return;
        }

        activityVariant = variant;

        SetupBasicInfo();
        SetupRequirements();
        SetupIngredients();
        CheckIngredientsAvailability();
        CheckLevelRequirement();

        Debug.Log($"CraftingActivityCard: Setup completed for {variant.VariantName}");
    }

    /// <summary>
    /// Verifier si on a assez d'ingredients et mettre a jour l'affichage
    /// </summary>
    private void CheckIngredientsAvailability()
    {
        hasEnoughIngredients = true;

        if (activityVariant.RequiredMaterials != null && activityVariant.RequiredQuantities != null)
        {
            var inventoryManager = InventoryManager.Instance;
            if (inventoryManager != null)
            {
                for (int i = 0; i < activityVariant.RequiredMaterials.Length; i++)
                {
                    var material = activityVariant.RequiredMaterials[i];
                    var requiredQuantity = activityVariant.RequiredQuantities[i];

                    if (material != null)
                    {
                        var container = inventoryManager.GetContainer("player");
                        int availableQuantity = container?.GetItemQuantity(material.ItemID) ?? 0;
                        if (availableQuantity < requiredQuantity)
                        {
                            hasEnoughIngredients = false;
                            break;
                        }
                    }
                }
            }
        }

        UpdateVisualState();
        UpdateLevelTextColor(); // Mettre à jour la couleur du texte de niveau
        UpdateIngredientsTextColors(); // Mettre à jour les couleurs des ingrédients
    }

    /// <summary>
    /// Verifier le niveau requis
    /// </summary>
    private void CheckLevelRequirement()
    {
        hasRequiredLevel = true;

        if (activityVariant != null && activityVariant.UnlockRequirement > 0)
        {
            // Obtenir le niveau de la compétence principale
            string mainSkillId = activityVariant.GetMainSkillId();

            if (!string.IsNullOrEmpty(mainSkillId) && XpManager.Instance != null)
            {
                int playerLevel = XpManager.Instance.GetPlayerSkill(mainSkillId).Level;
                hasRequiredLevel = playerLevel >= activityVariant.UnlockRequirement;

                if (!hasRequiredLevel)
                {
                    Debug.Log($"CraftingActivityCard: Level insufficient for {activityVariant.VariantName}. Required: {activityVariant.UnlockRequirement}, Player: {playerLevel}");
                }
            }
        }

        UpdateVisualState();
    }

    /// <summary>
    /// Mettre a jour l'etat visuel de la carte (overlays)
    /// </summary>
    private void UpdateVisualState()
    {
        // Priorité : Niveau > Ressources
        // Si pas le bon niveau -> overlay gris seulement
        // Sinon si pas assez de ressources -> overlay rouge seulement
        // Sinon aucun overlay

        if (grayOverlay != null)
        {
            grayOverlay.SetActive(!hasRequiredLevel);
        }

        if (redOverlay != null)
        {
            // Seulement afficher le rouge si on a le niveau requis mais pas les ressources
            redOverlay.SetActive(hasRequiredLevel && !hasEnoughIngredients);
        }

        // Mettre à jour les couleurs des textes
        UpdateLevelTextColor();
        UpdateIngredientsTextColors();
    }

    /// <summary>
    /// Animation de shake quand on clique sans avoir les ressources
    /// </summary>
    private IEnumerator ShakeAnimation()
    {
        // Capturer la position actuelle au moment du shake
        Vector3 currentPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-shakeIntensity, shakeIntensity);
            float y = Random.Range(-shakeIntensity, shakeIntensity);

            transform.localPosition = currentPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Remettre a la position actuelle (pas l'originale)
        transform.localPosition = currentPosition;
    }

    /// <summary>
    /// Setup basic card information (title, icon)
    /// </summary>
    private void SetupBasicInfo()
    {
        // Title
        if (titleText != null)
        {
            titleText.text = activityVariant.GetDisplayName();
        }

        // Icon
        if (iconImage != null && activityVariant.VariantIcon != null)
        {
            iconImage.sprite = activityVariant.VariantIcon;
        }
    }

    /// <summary>
    /// Setup requirements (level, time)
    /// </summary>
    private void SetupRequirements()
    {
        // Level requirement (using UnlockRequirement for now)
        if (levelRequiredText != null)
        {
            int level = activityVariant.UnlockRequirement > 0 ? activityVariant.UnlockRequirement : 1;
            levelRequiredText.text = $"Lvl : {level}";
        }

        // Time requirement
        if (timeRequiredText != null)
        {
            float timeInSeconds = activityVariant.CraftingTimeMs / 1000f;
            if (timeInSeconds >= 60f)
            {
                float minutes = timeInSeconds / 60f;
                timeRequiredText.text = $"{minutes:F1}min";
            }
            else
            {
                timeRequiredText.text = $"{timeInSeconds:F0}s";
            }
        }
    }

    /// <summary>
    /// Setup ingredients grid
    /// </summary>
    private void SetupIngredients()
    {
        // Clear existing ingredients
        ClearIngredients();

        if (activityVariant.RequiredMaterials == null || activityVariant.RequiredQuantities == null)
        {
            Debug.Log($"CraftingActivityCard: No ingredients for {activityVariant.VariantName}");
            return;
        }

        int materialCount = Mathf.Min(activityVariant.RequiredMaterials.Length, activityVariant.RequiredQuantities.Length);

        for (int i = 0; i < materialCount; i++)
        {
            var material = activityVariant.RequiredMaterials[i];
            var quantity = activityVariant.RequiredQuantities[i];

            if (material != null)
            {
                CreateIngredientSlot(material, quantity);
            }
        }

        Debug.Log($"CraftingActivityCard: Created {instantiatedIngredients.Count} ingredient slots");
    }

    /// <summary>
    /// Create an ingredient slot
    /// </summary>
    private void CreateIngredientSlot(ItemDefinition material, int quantity)
    {
        if (ingredientPrefab == null || ingredientsContainer == null)
        {
            Debug.LogWarning("CraftingActivityCard: IngredientPrefab or IngredientsContainer not assigned!");
            return;
        }

        GameObject ingredientSlot = Instantiate(ingredientPrefab, ingredientsContainer);
        instantiatedIngredients.Add(ingredientSlot);

        // Setup ingredient data (assuming the prefab has Image and TextMeshProUGUI components)
        Image iconImage = ingredientSlot.GetComponentInChildren<Image>();
        TextMeshProUGUI quantityText = ingredientSlot.GetComponentInChildren<TextMeshProUGUI>();

        if (iconImage != null && material.ItemIcon != null)
        {
            iconImage.sprite = material.ItemIcon;
        }

        if (quantityText != null)
        {
            quantityText.text = $"x{quantity}";

            // Stocker la référence au material pour la vérification des couleurs
            var ingredientData = ingredientSlot.GetComponent<IngredientSlotData>();
            if (ingredientData == null)
            {
                ingredientData = ingredientSlot.AddComponent<IngredientSlotData>();
            }
            ingredientData.Material = material;
            ingredientData.RequiredQuantity = quantity;
            ingredientData.QuantityText = quantityText;
        }

        Debug.Log($"CraftingActivityCard: Created ingredient slot for {material.GetDisplayName()} x{quantity}");
    }

    /// <summary>
    /// Clear all instantiated ingredients
    /// </summary>
    private void ClearIngredients()
    {
        foreach (var ingredient in instantiatedIngredients)
        {
            if (ingredient != null)
            {
                Destroy(ingredient);
            }
        }
        instantiatedIngredients.Clear();
    }

    /// <summary>
    /// Handle card button click
    /// </summary>
    private void OnCardButtonClicked()
    {
        if (activityVariant != null)
        {
            // Vérifier d'abord le niveau - PAS d'animation si niveau insuffisant
            if (!hasRequiredLevel)
            {
                Debug.Log($"CraftingActivityCard: Level requirement not met for {activityVariant.VariantName}");
                return; // Pas d'animation, juste ignorer le clic
            }

            // Puis vérifier les ressources - Animation seulement pour les ressources manquantes
            if (!hasEnoughIngredients)
            {
                Debug.Log($"CraftingActivityCard: Not enough ingredients for {activityVariant.VariantName}, shaking card");
                StartCoroutine(ShakeAnimation());
                return;
            }

            Debug.Log($"CraftingActivityCard: Card clicked for {activityVariant.VariantName}");
            OnCardClicked?.Invoke(activityVariant);
        }
    }

    /// <summary>
    /// Get the activity variant this card represents
    /// </summary>
    public ActivityVariant GetActivityVariant()
    {
        return activityVariant;
    }

    /// <summary>
    /// Mettre a jour manuellement l'etat des ingredients (a appeler quand l'inventaire change)
    /// </summary>
    public void RefreshIngredientsState()
    {
        CheckIngredientsAvailability();
    }

    /// <summary>
    /// Mettre a jour manuellement l'etat du niveau (a appeler quand le niveau change)
    /// </summary>
    public void RefreshLevelState()
    {
        CheckLevelRequirement();
    }

    /// <summary>
    /// Mettre a jour tous les etats (niveau et ingredients)
    /// </summary>
    public void RefreshAllStates()
    {
        CheckLevelRequirement();
        CheckIngredientsAvailability();
    }

    /// <summary>
    /// Mettre à jour la couleur du texte de niveau
    /// </summary>
    private void UpdateLevelTextColor()
    {
        if (levelRequiredText != null)
        {
            levelRequiredText.color = hasRequiredLevel ? Color.white : Color.red;
        }

        // Mettre à jour la couleur du background du niveau
        if (levelBackground != null)
        {
            levelBackground.color = hasRequiredLevel ? normalLevelBackgroundColor : insufficientLevelBackgroundColor;
        }
    }

    /// <summary>
    /// Mettre à jour les couleurs des textes d'ingrédients
    /// </summary>
    private void UpdateIngredientsTextColors()
    {
        if (activityVariant == null) return;

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null) return;

        foreach (var ingredientSlot in instantiatedIngredients)
        {
            if (ingredientSlot == null) continue;

            var ingredientData = ingredientSlot.GetComponent<IngredientSlotData>();
            if (ingredientData == null || ingredientData.Material == null || ingredientData.QuantityText == null) continue;

            // Vérifier si on a assez de cette ressource
            var container = inventoryManager.GetContainer("player");
            int availableQuantity = container?.GetItemQuantity(ingredientData.Material.ItemID) ?? 0;
            bool hasEnoughOfThisIngredient = availableQuantity >= ingredientData.RequiredQuantity;

            // Mettre à jour la couleur
            ingredientData.QuantityText.color = hasEnoughOfThisIngredient ? Color.white : Color.red;
        }
    }

    void OnDestroy()
    {
        ClearIngredients();
    }
}

/// <summary>
/// Composant helper pour stocker les données d'un slot d'ingrédient
/// </summary>
public class IngredientSlotData : MonoBehaviour
{
    public ItemDefinition Material;
    public int RequiredQuantity;
    public TextMeshProUGUI QuantityText;
}