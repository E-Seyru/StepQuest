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
        Logger.LogInfo("=== DEBUGGING EQUIPMENT ISSUE ===", Logger.LogCategory.EditorLog);

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Logger.LogError("InventoryManager introuvable!", Logger.LogCategory.EditorLog);
            return;
        }

        var playerContainer = inventoryManager.GetContainer("player");
        if (playerContainer == null)
        {
            Logger.LogError("Conteneur 'player' introuvable!", Logger.LogCategory.EditorLog);
            return;
        }

        var itemRegistry = inventoryManager.GetItemRegistry();
        if (itemRegistry == null)
        {
            Logger.LogError("ItemRegistry introuvable!", Logger.LogCategory.EditorLog);
            return;
        }

        Logger.LogInfo("--- CONTENU DE L'INVENTAIRE JOUEUR ---", Logger.LogCategory.EditorLog);
        var nonEmptySlots = playerContainer.GetNonEmptySlots();
        foreach (var slot in nonEmptySlots)
        {
            Logger.LogInfo($"Slot: '{slot.ItemID}' x{slot.Quantity}", Logger.LogCategory.EditorLog);

            // Verifier si l'item existe dans le registry
            var itemDef = itemRegistry.GetItem(slot.ItemID);
            if (itemDef != null)
            {
                Logger.LogInfo($"  [OK] Item trouve dans registry: {itemDef.ItemName} (Type: {itemDef.Type}, Logger.LogCategory.EditorLog)");
                if (itemDef.IsEquipment())
                {
                    Logger.LogInfo($"  [EQUIP] C'est un equipement pour slot: {itemDef.EquipmentSlot}", Logger.LogCategory.EditorLog);
                }
            }
            else
            {
                Logger.LogError($"  [ERROR] Item '{slot.ItemID}' INTROUVABLE dans le registry!", Logger.LogCategory.EditorLog);
            }
        }

        Logger.LogInfo("--- RECHERCHE DE L'EPEE DE FER ---", Logger.LogCategory.EditorLog);
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
                Logger.LogInfo($"[OK] Registry contient: '{id}' -> {itemDef.ItemName}", Logger.LogCategory.EditorLog);
            }
            else
            {
                Logger.LogInfo($"[NO] Registry ne contient pas: '{id}'", Logger.LogCategory.EditorLog);
            }

            // Test de l'inventaire
            bool hasInInventory = playerContainer.HasItem(id);
            Logger.LogInfo($"Inventaire contient '{id}': {hasInInventory}", Logger.LogCategory.EditorLog);
        }

        Logger.LogInfo("--- TOUS LES ITEMS DU REGISTRY ---", Logger.LogCategory.EditorLog);
        if (itemRegistry.AllItems != null)
        {
            foreach (var item in itemRegistry.AllItems)
            {
                if (item != null)
                {
                    Logger.LogInfo($"Registry: '{item.ItemID}' -> {item.ItemName} (Type: {item.Type}, Logger.LogCategory.EditorLog)");
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
        Logger.LogInfo($"Ajout force 'epee_de_fer': {success}", Logger.LogCategory.EditorLog);
    }

    [ContextMenu("Try Equip Epee")]
    public void TryEquipEpee()
    {
        var equipmentPanel = EquipmentPanelUI.Instance;
        if (equipmentPanel == null)
        {
            Logger.LogError("EquipmentPanelUI introuvable!", Logger.LogCategory.EditorLog);
            return;
        }

        bool success = equipmentPanel.TryEquipItem("epee_de_fer");
        Logger.LogInfo($"Tentative d'equipement: {success}", Logger.LogCategory.EditorLog);
    }
}