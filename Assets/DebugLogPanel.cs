using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class DebugLogPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private TMP_InputField categoryFilterInput;

    // Color definitions for different log types
    private readonly string infoColor = "#3399FF";     // Blue
    private readonly string warningColor = "#FFCC00";  // Yellow
    private readonly string errorColor = "#FF3333";    // Red

    public struct LogMessage
    {
        public string fullText;
        public string condition;
        public string stackTrace;
        public LogType type;
        public string category;
    }

    private List<LogMessage> allLogs = new List<LogMessage>();
    // Regex to extract category: matches characters between the first ']' and the next '['
    // e.g., "[LogLevel][CategoryName] message" -> "CategoryName"
    private static readonly Regex categoryRegex = new Regex(@"\]\[(.*?)\]", RegexOptions.Compiled);

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        if (categoryFilterInput != null)
        {
            categoryFilterInput.onValueChanged.AddListener(delegate { ApplyFilterAndRedrawLogs(); });
        }
        ApplyFilterAndRedrawLogs(); // Initial draw
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        if (categoryFilterInput != null)
        {
            categoryFilterInput.onValueChanged.RemoveListener(delegate { ApplyFilterAndRedrawLogs(); });
        }
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        string coloredText = condition;
        string category = null;

        // Try to parse category
        Match categoryMatch = categoryRegex.Match(condition);
        if (categoryMatch.Success && categoryMatch.Groups.Count > 1)
        {
            category = categoryMatch.Groups[1].Value;
        }

        // Apply color based on the log type or message content
        // This logic ensures the category tag is also colored if it's part of the condition string
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
        else if (type == LogType.Warning) // Fallback for types if no keyword found
        {
            coloredText = $"<color={warningColor}>{condition}</color>";
        }
        else if (type == LogType.Error || type == LogType.Exception)
        {
            coloredText = $"<color={errorColor}>{condition}</color>";
        }
        // If it's a debug log or info without the tag, it remains default color or needs specific handling if desired.

        allLogs.Add(new LogMessage
        {
            fullText = coloredText,
            condition = condition,
            stackTrace = stackTrace,
            type = type,
            category = category
        });

        ApplyFilterAndRedrawLogs();
    }

    private void ApplyFilterAndRedrawLogs()
    {
        if (logText == null) return; // Guard clause if logText is not set

        logText.text = "";
        string filterText = categoryFilterInput != null ? categoryFilterInput.text : "";

        for (int i = 0; i < allLogs.Count; i++)
        {
            LogMessage logMessage = allLogs[i];
            bool passesFilter = string.IsNullOrEmpty(filterText);

            if (!passesFilter && !string.IsNullOrEmpty(logMessage.category))
            {
                if (logMessage.category.ToLowerInvariant().Contains(filterText.ToLowerInvariant()))
                {
                    passesFilter = true;
                }
            }
            
            if(passesFilter)
            {
                logText.text += logMessage.fullText + "\n";
            }
        }

        // Update scroll position
        if (scrollRect != null && logText.preferredHeight > 0) // Check preferredHeight to avoid issues if text is empty
        {
            // It's better to update content size after all text has been set
            LayoutRebuilder.ForceRebuildLayoutImmediate(logText.rectTransform);
            scrollRect.content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                logText.preferredHeight
            );
            // Scroll to bottom only if the scrollbar is near the bottom or at the bottom.
            // This prevents auto-scrolling if the user has scrolled up to view older logs.
            // However, for simplicity here, we'll always scroll to bottom.
            // A more advanced version might check scrollRect.verticalNormalizedPosition before forcing it.
            scrollRect.verticalNormalizedPosition = 0.0f;
        }
    }
}