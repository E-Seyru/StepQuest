// Purpose: Provides a reliable source of time and calculates offline time progression.
// Filepath: Assets/Scripts/Services/TimeService.cs
using UnityEngine;
using System; // For DateTime

public class TimeService : MonoBehaviour
{
    // TODO: Implement Singleton pattern or make static?
    // TODO: Store last known time (e.g., in PlayerPrefs or save file)
    // TODO: Calculate time elapsed since last app session on startup
    // TODO: Provide current reliable time (potentially sync with NTP server if needed)

    private DateTime lastSavedTime;

    void Awake()
    {
        // TODO: Load last saved time
    }

    public TimeSpan GetOfflineTimeSpan()
    {
        // TODO: Calculate time difference between now and lastSavedTime
        return TimeSpan.Zero; // Placeholder
    }

    public DateTime GetCurrentTime()
    {
        // TODO: Return current time (DateTime.UtcNow recommended for consistency)
        return DateTime.UtcNow;
    }

    public void SaveTimestamp()
    {
        // TODO: Save the current time (e.g., PlayerPrefs.SetString("LastTime", GetCurrentTime().ToString("o"));)
        lastSavedTime = GetCurrentTime();
    }
}