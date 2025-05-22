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
        string messageColor = infoColor; // Default to info color
        string displayMessage = condition;
        string category = null;

        // Determine color based on LogType
        switch (type)
        {
            case LogType.Warning:
                messageColor = warningColor;
                break;
            case LogType.Error:
            case LogType.Exception:
                messageColor = errorColor;
                break;
            case LogType.Log: // Typically info
            default:
                messageColor = infoColor;
                break;
        }

        string actualMessage = condition;

        // Try to parse category and simultaneously strip it and any preceding log level tag from actualMessage
        Match categoryMatch = categoryRegex.Match(actualMessage);
        if (categoryMatch.Success && categoryMatch.Groups.Count > 1)
        {
            category = categoryMatch.Groups[1].Value;
            // The message is what's after the full category match (e.g., after "[LogLevel][Category]")
            int msgStartIndex = actualMessage.IndexOf(categoryMatch.Value) + categoryMatch.Value.Length;
            actualMessage = actualMessage.Substring(msgStartIndex).TrimStart();
        }
        else // No category found by regex, try stripping common log level prefixes
        {
            if (actualMessage.StartsWith("[Info]"))
            {
                actualMessage = actualMessage.Substring("[Info]".Length).TrimStart();
            }
            else if (actualMessage.StartsWith("[Warning]"))
            {
                actualMessage = actualMessage.Substring("[Warning]".Length).TrimStart();
            }
            else if (actualMessage.StartsWith("[Error]"))
            {
                actualMessage = actualMessage.Substring("[Error]".Length).TrimStart();
            }
            // If no prefix is found, actualMessage remains the original condition.
        }

        // Now, construct the displayMessage using the processed actualMessage
        if (!string.IsNullOrEmpty(category))
        {
            displayMessage = $"[{category}] {actualMessage}";
        }
        else
        {
            displayMessage = actualMessage;
        }

        string coloredText = $"<color={messageColor}>{displayMessage}</color>";

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