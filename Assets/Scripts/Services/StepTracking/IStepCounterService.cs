// Filepath: Assets/Scripts/Services/StepTracking/IStepCounterService.cs
using System; // For Action and DateTime

public interface IStepCounterService
{
    // Event to notify other systems when new steps are detected
    event Action<int> OnStepsUpdated;

    // Is the service ready and have we got permission?
    bool HasPermission { get; }
    bool IsInitialized { get; } // Maybe track initialization state

    // Start the service setup (check SDKs, maybe ask for permissions)
    void InitializeService(Action<bool> onInitialized); // Callback indicates success/failure

    // Explicitly request permissions if not already granted
    void RequestPermissions(Action<bool> onPermissionGranted); // Callback indicates success/failure

    // Get steps recorded between two points in time (for offline calculation)
    // This might be async depending on the native implementation! We'll start sync for simplicity.
    int GetHistoricalSteps(DateTime startTime, DateTime endTime);

    // Get steps counted *today* (useful for display, maybe less for tasks)
    int GetTodaysTotalSteps(); // Might also be async

    // --- Editor/Debug Helper ---
    // Method to manually trigger step updates in the editor
    void SimulateStepUpdate(int steps);
}