using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugLogPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI logText;

    // Color definitions for different log types
    private readonly string infoColor = "#3399FF";     // Blue
    private readonly string warningColor = "#FFCC00";  // Yellow
    private readonly string errorColor = "#FF3333";    // Red

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        string coloredText = condition;

        // Apply color based on the log type or message content
        if (condition.Contains("[Info]"))
        {
            coloredText = condition.Replace("[Info]", $"<color={infoColor}>[Info]</color>");
        }
        else if (condition.Contains("[Warning]"))
        {
            coloredText = condition.Replace("[Warning]", $"<color={warningColor}>[Warning]</color>");
        }
        else if (condition.Contains("[Error]"))
        {
            coloredText = condition.Replace("[Error]", $"<color={errorColor}>[Error]</color>");
        }
        else if (type == LogType.Warning)
        {
            coloredText = $"<color={warningColor}>{condition}</color>";
        }
        else if (type == LogType.Error || type == LogType.Exception)
        {
            coloredText = $"<color={errorColor}>{condition}</color>";
        }

        logText.text += coloredText + "\n";

        // Update scroll position
        float h = logText.preferredHeight;
        scrollRect.content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            h
        );
        scrollRect.verticalNormalizedPosition = 0.0f;
    }
}