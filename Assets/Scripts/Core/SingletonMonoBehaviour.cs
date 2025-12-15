// Purpose: Generic base class for MonoBehaviour singletons to eliminate boilerplate code
// Filepath: Assets/Scripts/Core/SingletonMonoBehaviour.cs
using UnityEngine;

/// <summary>
/// Generic base class for MonoBehaviour singletons.
/// Eliminates the need for repetitive singleton boilerplate code.
///
/// Usage:
/// public class MyManager : SingletonMonoBehaviour&lt;MyManager&gt;
/// {
///     protected override void OnAwakeInitialize() { /* custom init */ }
/// }
/// </summary>
/// <typeparam name="T">The type of the singleton class</typeparam>
public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
{
    public static T Instance { get; private set; }

    /// <summary>
    /// Override to make this singleton persist across scene loads.
    /// Default is false.
    /// </summary>
    protected virtual bool PersistAcrossScenes => false;

    /// <summary>
    /// Override to customize what happens when a duplicate is detected.
    /// Default behavior destroys the duplicate gameObject.
    /// </summary>
    protected virtual bool DestroyDuplicates => true;

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = (T)this;

            if (PersistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            OnAwakeInitialize();
        }
        else if (DestroyDuplicates)
        {
            Logger.LogWarning($"{typeof(T).Name}: Duplicate instance detected, destroying {gameObject.name}", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            OnSingletonDestroyed();
            Instance = null;
        }
    }

    /// <summary>
    /// Called after Instance is set during Awake.
    /// Override to add custom initialization logic.
    /// </summary>
    protected virtual void OnAwakeInitialize() { }

    /// <summary>
    /// Called when the singleton instance is being destroyed.
    /// Override to add cleanup logic.
    /// </summary>
    protected virtual void OnSingletonDestroyed() { }

    /// <summary>
    /// Check if the singleton instance exists.
    /// </summary>
    public static bool Exists => Instance != null;
}
