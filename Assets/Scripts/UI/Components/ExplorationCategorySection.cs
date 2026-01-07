// Purpose: Reusable section component for exploration panel categories
// Filepath: Assets/Scripts/UI/Components/ExplorationCategorySection.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A category section for the exploration panel (Activities, Enemies, or NPCs).
/// Contains a header with title/counter and a container for discoverable items.
/// </summary>
public class ExplorationCategorySection : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject emptyStateText;

    [Header("Configuration")]
    [SerializeField] private string categoryName = "Category";
    [SerializeField] private string emptyMessage = "Aucun element cache ici";

    [Header("Item Prefab")]
    [SerializeField] private GameObject discoverableItemPrefab;

    // Instantiated items for cleanup
    private List<GameObject> instantiatedItems = new List<GameObject>();

    /// <summary>
    /// Populate this section with discoverable items
    /// </summary>
    public void Populate(List<DiscoverableInfo> items)
    {
        ClearItems();

        int total = items.Count;
        int discovered = 0;
        foreach (var item in items)
        {
            if (item.IsDiscovered) discovered++;
        }

        // Update header
        UpdateHeader(discovered, total);

        // Show/hide empty state
        bool hasItems = total > 0;
        if (emptyStateText != null)
        {
            emptyStateText.SetActive(!hasItems);
        }

        if (!hasItems) return;

        // Sort: rarer first, then undiscovered first
        var sortedItems = new List<DiscoverableInfo>(items);
        sortedItems.Sort((a, b) =>
        {
            int rarityCompare = ((int)b.Rarity).CompareTo((int)a.Rarity);
            if (rarityCompare != 0) return rarityCompare;
            return a.IsDiscovered.CompareTo(b.IsDiscovered);
        });

        // Create items
        foreach (var item in sortedItems)
        {
            CreateItem(item);
        }

        // Force layout rebuild
        if (itemsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsContainer.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// Clear all instantiated items
    /// </summary>
    public void ClearItems()
    {
        foreach (var item in instantiatedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        instantiatedItems.Clear();
    }

    /// <summary>
    /// Update the header text with discovered/total count
    /// </summary>
    private void UpdateHeader(int discovered, int total)
    {
        if (headerText == null) return;

        if (total == 0)
        {
            headerText.text = $"{categoryName} - {emptyMessage}";
        }
        else
        {
            headerText.text = $"{categoryName} ({discovered}/{total})";
        }
    }

    /// <summary>
    /// Create a single discoverable item
    /// </summary>
    private void CreateItem(DiscoverableInfo info)
    {
        if (discoverableItemPrefab == null || itemsContainer == null) return;

        GameObject item = Instantiate(discoverableItemPrefab, itemsContainer);
        instantiatedItems.Add(item);

        var itemUI = item.GetComponent<DiscoverableItemUI>();
        if (itemUI != null)
        {
            itemUI.Setup(info);
        }
    }

    /// <summary>
    /// Check if this section has any content
    /// </summary>
    public bool HasContent()
    {
        return instantiatedItems.Count > 0;
    }

    /// <summary>
    /// Show or hide the entire section
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    void OnDestroy()
    {
        ClearItems();
    }
}
