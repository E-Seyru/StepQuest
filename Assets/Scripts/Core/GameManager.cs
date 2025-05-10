// Filepath: Assets/Scripts/Core/GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Makes the GameManager persist across scenes
        }
        else
        {
            Destroy(gameObject); // Ensures there's only one instance
        }

        // TODO: Initialize core services (DataManager, TaskManager, etc.)
        // TODO: Manage game states (MainMenu, Playing, Paused)
    }

    void Start()
    {
        // TODO: Initialization logic
        Debug.Log("GameManager started!");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // TODO: Handle pausing (potentially trigger save, offline calcs)
    }

    void OnApplicationQuit()
    {
        // TODO: Handle quitting (ensure save)
    }
}