// ManagersPersistence.cs - À attacher sur le GameObject "Managers"
using UnityEngine;

public class ManagersPersistence : MonoBehaviour
{
    public static ManagersPersistence Instance { get; private set; }

    void Awake()
    {
        // Singleton pour éviter les doublons de managers
        if (Instance != null && Instance != this)
        {
            Logger.LogWarning("ManagersPersistence: Duplicate Managers detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
            return;
        }

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persiste tout le conteneur "Managers"

        }
    }
}