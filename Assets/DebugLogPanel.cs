using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugLogPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;         // Drag & drop ici votre DebugScrollView (ou son ScrollRect)
    [SerializeField] private TextMeshProUGUI logText;       // Drag & drop ici votre DebugLogText

    private void OnEnable()
    {
        // Enregistre le callback pour chaque log Unity
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        logText.text += condition + "\n";

        // Calcule la hauteur du texte
        float h = logText.preferredHeight;

        // Applique-la au RectTransform du Content
        scrollRect.content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            h
        );

        // Puis scroll en bas

    }
}
