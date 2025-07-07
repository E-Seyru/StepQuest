// Purpose: UI Card for Crafting Activities (Time-based activities) with red overlay and shake
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
    [SerializeField] private GameObject redOverlay; // Voile rouge par-dessus la carte

    [Header("Shake Animation")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeIntensity = 10f;

    [Header("Prefabs")]
    [SerializeField] private GameObject ingredientPrefab;

    // Data
    private ActivityVariant activityVariant;
    private List<GameObject> instantiatedIngredients = new List<GameObject>();
    private bool hasEnoughIngredients = true;

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

        Debug.Log($"CraftingActivityCard: Setup completed for {variant.VariantName}");
    }

    /// <summary>
    /// Vérifier si on a assez d'ingrédients et mettre à jour l'affichage
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
    }

    /// <summary>
    /// Mettre à jour l'état visuel de la carte (voile rouge)
    /// </summary>
    private void UpdateVisualState()
    {
        if (redOverlay != null)
        {
            redOverlay.SetActive(!hasEnoughIngredients);
        }
    }

    /// <summary>
    /// Animation de shake quand on clique sans avoir les ingrédients
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

        // Remettre à la position actuelle (pas l'originale)
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
            int level = activityVariant.UnlockRequirement > 0 ?
                activityVariant.UnlockRequirement : 1;
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
            // Si on n'a pas assez d'ingrédients, faire l'animation de shake
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
    /// Mettre à jour manuellement l'état des ingrédients (à appeler quand l'inventaire change)
    /// </summary>
    public void RefreshIngredientsState()
    {
        CheckIngredientsAvailability();
    }

    void OnDestroy()
    {
        ClearIngredients();
    }
}