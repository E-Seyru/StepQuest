// Purpose: Script pour débugger et nettoyer les données d'inventaire corrompues
// Filepath: Assets/Scripts/Debug/InventoryDataDebugger.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class InventoryDataDebugger : EditorWindow
{
    [MenuItem("StepQuest/Debug Inventory Data")]
    public static void ShowWindow()
    {
        InventoryDataDebugger window = GetWindow<InventoryDataDebugger>();
        window.titleContent = new GUIContent("Inventory Data Debugger");
        window.Show();
    }

    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("Inventory Data Debugger", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Ce script va analyser et nettoyer les données d'inventaire corrompues", MessageType.Info);

        if (GUILayout.Button("Analyser les données d'inventaire"))
        {
            AnalyzeInventoryData();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Nettoyer les items corrompus"))
        {
            CleanCorruptedItems();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Migrer les IDs vers la nouvelle casse"))
        {
            MigrateItemIDs();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset complet de l'inventaire"))
        {
            if (EditorUtility.DisplayDialog("Attention",
                "Cela va supprimer TOUT ton inventaire. Es-tu sûr ?",
                "Oui, vider l'inventaire", "Annuler"))
            {
                ResetInventory();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Info:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DisplayInventoryDebugInfo();
        EditorGUILayout.EndScrollView();
    }

    private void AnalyzeInventoryData()
    {
        Debug.Log("=== ANALYSE DES DONNÉES D'INVENTAIRE ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("Lance le jeu en mode Play pour analyser l'inventaire !");
            EditorUtility.DisplayDialog("Mode Play requis",
                "Tu dois être en mode Play pour analyser l'inventaire.", "OK");
            return;
        }

        var inventoryManager = InventoryManager.Instance;
        var dataManager = DataManager.Instance;

        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager non trouvé !");
            return;
        }

        if (dataManager?.LocalDatabase == null)
        {
            Debug.LogError("DataManager ou LocalDatabase non trouvé !");
            return;
        }

        // Analyser les conteneurs
        var containers = dataManager.LocalDatabase.LoadAllInventoryContainers();

        Debug.Log($"=== {containers.Count} CONTENEUR(S) TROUVÉ(S) ===");

        foreach (var containerData in containers)
        {
            Debug.Log($"📦 Conteneur: {containerData.ContainerID} ({containerData.ContainerType})");

            var container = containerData.ToInventoryContainer();
            var nonEmptySlots = container.GetNonEmptySlots();

            Debug.Log($"   Slots utilisés: {nonEmptySlots.Count}/{container.MaxSlots}");

            foreach (var slot in nonEmptySlots)
            {
                bool itemExists = inventoryManager.GetItemRegistry()?.HasItem(slot.ItemID) == true;

                if (itemExists)
                {
                    Debug.Log($"   ✅ {slot.ItemID} x{slot.Quantity}");
                }
                else
                {
                    Debug.LogError($"   ❌ {slot.ItemID} x{slot.Quantity} - ITEM INTROUVABLE DANS LE REGISTRY !");

                    // Chercher une variante avec une casse différente
                    string possibleMatch = FindItemWithDifferentCase(slot.ItemID, inventoryManager.GetItemRegistry());
                    if (!string.IsNullOrEmpty(possibleMatch))
                    {
                        Debug.LogWarning($"      💡 Variante trouvée: '{possibleMatch}' (casse différente)");
                    }
                }
            }
        }

        // Analyser le registry des items
        Debug.Log("=== ITEMS DISPONIBLES DANS LE REGISTRY ===");
        var itemRegistry = inventoryManager.GetItemRegistry();
        if (itemRegistry?.AllItems != null)
        {
            foreach (var item in itemRegistry.AllItems)
            {
                if (item != null)
                {
                    Debug.Log($"📋 Registry: {item.ItemID} - {item.ItemName}");
                }
            }
        }
    }

    private void CleanCorruptedItems()
    {
        Debug.Log("=== NETTOYAGE DES ITEMS CORROMPUS ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("Lance le jeu en mode Play !");
            EditorUtility.DisplayDialog("Mode Play requis",
                "Tu dois être en mode Play pour nettoyer l'inventaire.", "OK");
            return;
        }

        var inventoryManager = InventoryManager.Instance;
        var dataManager = DataManager.Instance;

        if (inventoryManager == null || dataManager?.LocalDatabase == null)
        {
            Debug.LogError("Managers non trouvés !");
            return;
        }

        var containers = dataManager.LocalDatabase.LoadAllInventoryContainers();
        int cleanedCount = 0;

        foreach (var containerData in containers)
        {
            var container = containerData.ToInventoryContainer();
            bool containerChanged = false;

            for (int i = 0; i < container.Slots.Count; i++)
            {
                var slot = container.Slots[i];

                if (!slot.IsEmpty())
                {
                    bool itemExists = inventoryManager.GetItemRegistry()?.HasItem(slot.ItemID) == true;

                    if (!itemExists)
                    {
                        Debug.Log($"🗑️ Suppression de l'item corrompu: {slot.ItemID} x{slot.Quantity} du conteneur {container.ContainerID}");
                        slot.Clear();
                        containerChanged = true;
                        cleanedCount++;
                    }
                }
            }

            if (containerChanged)
            {
                // Sauvegarder le conteneur nettoyé
                var cleanedData = InventoryContainerData.FromInventoryContainer(container);
                dataManager.LocalDatabase.SaveInventoryContainer(cleanedData);
            }
        }

        Debug.Log($"✅ Nettoyage terminé ! {cleanedCount} item(s) corrompu(s) supprimé(s)");
        EditorUtility.DisplayDialog("Nettoyage terminé",
            $"{cleanedCount} item(s) corrompu(s) ont été supprimés de l'inventaire.", "OK");
    }

    private void MigrateItemIDs()
    {
        Debug.Log("=== MIGRATION DES IDs VERS LA NOUVELLE CASSE ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("Lance le jeu en mode Play !");
            EditorUtility.DisplayDialog("Mode Play requis",
                "Tu dois être en mode Play pour migrer les IDs.", "OK");
            return;
        }

        var inventoryManager = InventoryManager.Instance;
        var dataManager = DataManager.Instance;

        if (inventoryManager == null || dataManager?.LocalDatabase == null)
        {
            Debug.LogError("Managers non trouvés !");
            return;
        }

        var containers = dataManager.LocalDatabase.LoadAllInventoryContainers();
        int migratedCount = 0;

        // Définir les migrations connues
        var migrations = new Dictionary<string, string>
        {
            { "cuivre", "Cuivre" },
            { "fer", "Fer" },
            { "charbon", "Charbon" },
            { "pin", "Pin" }
            // Ajoute d'autres migrations si nécessaire
        };

        foreach (var containerData in containers)
        {
            var container = containerData.ToInventoryContainer();
            bool containerChanged = false;

            for (int i = 0; i < container.Slots.Count; i++)
            {
                var slot = container.Slots[i];

                if (!slot.IsEmpty() && migrations.ContainsKey(slot.ItemID))
                {
                    string oldId = slot.ItemID;
                    string newId = migrations[oldId];

                    // Vérifier que le nouvel ID existe dans le registry
                    if (inventoryManager.GetItemRegistry()?.HasItem(newId) == true)
                    {
                        Debug.Log($"🔄 Migration: {oldId} → {newId} (x{slot.Quantity}) dans {container.ContainerID}");
                        slot.ItemID = newId;
                        containerChanged = true;
                        migratedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Impossible de migrer {oldId} → {newId} : le nouvel ID n'existe pas dans le registry");
                    }
                }
            }

            if (containerChanged)
            {
                // Sauvegarder le conteneur migré
                var migratedData = InventoryContainerData.FromInventoryContainer(container);
                dataManager.LocalDatabase.SaveInventoryContainer(migratedData);
            }
        }

        Debug.Log($"✅ Migration terminée ! {migratedCount} item(s) migré(s)");
        EditorUtility.DisplayDialog("Migration terminée",
            $"{migratedCount} item(s) ont été migrés vers les nouveaux IDs.", "OK");
    }

    private void ResetInventory()
    {
        Debug.Log("=== RESET COMPLET DE L'INVENTAIRE ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("Lance le jeu en mode Play !");
            EditorUtility.DisplayDialog("Mode Play requis",
                "Tu dois être en mode Play pour reset l'inventaire.", "OK");
            return;
        }

        var inventoryManager = InventoryManager.Instance;
        var dataManager = DataManager.Instance;

        if (inventoryManager == null || dataManager?.LocalDatabase == null)
        {
            Debug.LogError("Managers non trouvés !");
            return;
        }

        // Vider tous les conteneurs
        var playerContainer = inventoryManager.GetContainer("player");
        var bankContainer = inventoryManager.GetContainer("bank");

        if (playerContainer != null)
        {
            playerContainer.Clear();
        }

        if (bankContainer != null)
        {
            bankContainer.Clear();
        }

        // Forcer la sauvegarde
        inventoryManager.ForceSave();

        Debug.Log("✅ Inventaire complètement vidé !");
        EditorUtility.DisplayDialog("Reset terminé",
            "L'inventaire a été complètement vidé.", "OK");
    }

    private void DisplayInventoryDebugInfo()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.LabelField("Lance le jeu pour voir les infos de debug", EditorStyles.miniLabel);
            return;
        }

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            EditorGUILayout.LabelField(inventoryManager.GetDebugInfo(), EditorStyles.wordWrappedMiniLabel);
        }
    }

    private string FindItemWithDifferentCase(string itemId, ItemRegistry registry)
    {
        if (registry?.AllItems == null) return null;

        foreach (var item in registry.AllItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.ItemID))
            {
                // Comparer en ignorant la casse
                if (string.Equals(item.ItemID, itemId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return item.ItemID;
                }
            }
        }

        return null;
    }
}
#endif