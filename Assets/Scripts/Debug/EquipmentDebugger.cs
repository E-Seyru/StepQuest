// Script temporaire pour debugger le probleme d'equipement
// a placer sur un GameObject temporaire avec un bouton dans l'UI ou appele via console
using UnityEngine;

public class EquipmentDebugger : MonoBehaviour
{
    [Header("Debug Buttons - Click in Inspector")]
    [SerializeField] private bool debugEquipmentIssue;
    [SerializeField] private bool forceAddEpee;
    [SerializeField] private bool tryEquipEpee;

    void Update()
    {
        // Verification des boutons dans l'inspector
        if (debugEquipmentIssue)
        {
            debugEquipmentIssue = false;
            DebugEquipmentIssue();
        }
        if (forceAddEpee)
        {
            forceAddEpee = false;
            ForceAddEpeeDeFer();
        }
        if (tryEquipEpee)
        {
            tryEquipEpee = false;
            TryEquipEpee();
        }

        // Raccourcis clavier
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugEquipmentIssue();
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ForceAddEpeeDeFer();
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            TryEquipEpee();
        }
    }

    [ContextMenu("Debug Equipment Issue")]
    public void DebugEquipmentIssue()
    {
        Debug.Log("=== DEBUGGING EQUIPMENT ISSUE ===");

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager introuvable!");
            return;
        }

        var playerContainer = inventoryManager.GetContainer("player");
        if (playerContainer == null)
        {
            Debug.LogError("Conteneur 'player' introuvable!");
            return;
        }

        var itemRegistry = inventoryManager.GetItemRegistry();
        if (itemRegistry == null)
        {
            Debug.LogError("ItemRegistry introuvable!");
            return;
        }

        Debug.Log("--- CONTENU DE L'INVENTAIRE JOUEUR ---");
        var nonEmptySlots = playerContainer.GetNonEmptySlots();
        foreach (var slot in nonEmptySlots)
        {
            Debug.Log($"Slot: '{slot.ItemID}' x{slot.Quantity}");

            // Verifier si l'item existe dans le registry
            var itemDef = itemRegistry.GetItem(slot.ItemID);
            if (itemDef != null)
            {
                Debug.Log($"  [OK] Item trouve dans registry: {itemDef.ItemName} (Type: {itemDef.Type})");
                if (itemDef.IsEquipment())
                {
                    Debug.Log($"  [EQUIP] C'est un equipement pour slot: {itemDef.EquipmentSlot}");
                }
            }
            else
            {
                Debug.LogError($"  [ERROR] Item '{slot.ItemID}' INTROUVABLE dans le registry!");
            }
        }

        Debug.Log("--- RECHERCHE DE L'EPEE DE FER ---");
        string[] possibleIDs = {
            "epee_de_fer",
            "Epee de fer",
            "Epee_de_fer",
            "epee_de_fer",
            "epee de fer"
        };

        foreach (string id in possibleIDs)
        {
            // Test du registry
            var itemDef = itemRegistry.GetItem(id);
            if (itemDef != null)
            {
                Debug.Log($"[OK] Registry contient: '{id}' -> {itemDef.ItemName}");
            }
            else
            {
                Debug.Log($"[NO] Registry ne contient pas: '{id}'");
            }

            // Test de l'inventaire
            bool hasInInventory = playerContainer.HasItem(id);
            Debug.Log($"Inventaire contient '{id}': {hasInInventory}");
        }

        Debug.Log("--- TOUS LES ITEMS DU REGISTRY ---");
        if (itemRegistry.AllItems != null)
        {
            foreach (var item in itemRegistry.AllItems)
            {
                if (item != null)
                {
                    Debug.Log($"Registry: '{item.ItemID}' -> {item.ItemName} (Type: {item.Type})");
                }
            }
        }
    }

    [ContextMenu("Force Add Epee de fer")]
    public void ForceAddEpeeDeFer()
    {
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null) return;

        // Essayer avec l'ID exact du ScriptableObject
        bool success = inventoryManager.AddItem("player", "epee_de_fer", 1);
        Debug.Log($"Ajout force 'epee_de_fer': {success}");
    }

    [ContextMenu("Try Equip Epee")]
    public void TryEquipEpee()
    {
        var equipmentPanel = EquipmentPanelUI.Instance;
        if (equipmentPanel == null)
        {
            Debug.LogError("EquipmentPanelUI introuvable!");
            return;
        }

        bool success = equipmentPanel.TryEquipItem("epee_de_fer");
        Debug.Log($"Tentative d'equipement: {success}");
    }
}