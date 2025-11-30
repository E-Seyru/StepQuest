// Purpose: Scales card content proportionally when parent size changes (e.g., in GridLayoutGroup)
// Filepath: Assets/Scripts/UI/Components/CardScaler.cs
using UnityEngine;

[ExecuteAlways]
public class CardScaler : MonoBehaviour
{
    [Tooltip("La taille originale pour laquelle la carte a été designée")]
    [SerializeField] private Vector2 designedSize = new Vector2(300, 400);

    [Tooltip("Scaler tous les enfants directs au lieu d'un seul content")]
    [SerializeField] private bool scaleAllChildren = true;

    [Tooltip("Le contenu à scaler (utilisé seulement si scaleAllChildren est false)")]
    [SerializeField] private RectTransform content;

    private RectTransform rectTransform;
    private float lastScale = 1f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (rectTransform == null) return;

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // Évite les divisions par zéro
        if (width <= 0 || height <= 0 || designedSize.x <= 0 || designedSize.y <= 0) return;

        // Calcule le ratio entre la taille actuelle et la taille designée
        float scaleX = width / designedSize.x;
        float scaleY = height / designedSize.y;
        float scale = Mathf.Min(scaleX, scaleY); // Garde le ratio pour éviter la déformation

        // Évite de recalculer si le scale n'a pas changé
        if (Mathf.Approximately(scale, lastScale)) return;
        lastScale = scale;

        if (scaleAllChildren)
        {
            // Scale tous les enfants directs
            foreach (Transform child in transform)
            {
                child.localScale = new Vector3(scale, scale, 1f);
            }
        }
        else if (content != null)
        {
            content.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
