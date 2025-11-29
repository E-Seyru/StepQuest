using UnityEngine;
using TMPro;

public class CombatPopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI popupText;

    [Header("Animation Settings")]
    [SerializeField] private float arcHeight = 150f;      // Height of the bounce arc
    [SerializeField] private float moveXDistance = 50f;   // How far it moves horizontally
    [SerializeField] private float duration = 1f;
    [SerializeField] private float startScale = 1.5f;
    [SerializeField] private float endScale = 1f;

    private RectTransform rectTransform;
    private Vector2 startPos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(float popupAmount, RectTransform characterImage)
    {
        // Set initial values
        popupText.text = popupAmount.ToString();

        // Generate random position within character image bounds
        float randomX = Random.Range(-characterImage.rect.width / 4, characterImage.rect.width / 4);
        float randomY = Random.Range((-characterImage.rect.height / 4) - 100, (characterImage.rect.height / 4) - 100);
        rectTransform.anchoredPosition = new Vector2(
            characterImage.anchoredPosition.x + randomX,
            characterImage.anchoredPosition.y + randomY
        );
        startPos = rectTransform.anchoredPosition;

        // Random horizontal direction
        moveXDistance *= Random.Range(0, 2) * 2 - 1; // Randomly go left or right

        // Initial scale
        transform.localScale = Vector3.one * startScale;

        // Start animations
        AnimatePopup();
    }

    private void AnimatePopup()
    {
        // Scale animation
        LeanTween.scale(gameObject, Vector3.one * endScale, duration)
            .setEaseOutBack();

        // Arc movement and fade
        LeanTween.value(gameObject, 0f, 1f, duration)
            .setOnUpdate((float value) =>
            {
                // Calculate arc movement
                float xPos = startPos.x + (moveXDistance * value);
                float yPos = startPos.y + (arcHeight * Mathf.Sin(value * Mathf.PI)); // Creates arc effect

                rectTransform.anchoredPosition = new Vector2(xPos, yPos);

                // Fade out near the end of animation
                Color textColor = popupText.color;
                textColor.a = 1 - (value * value); // Quadratic fade for smoother look
                popupText.color = textColor;
            })
            .setEaseOutQuad()
            .setOnComplete(() => {
                Destroy(gameObject);
            });
    }
}
