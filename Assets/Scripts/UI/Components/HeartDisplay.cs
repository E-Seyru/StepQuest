// Purpose: Displays relationship hearts (0-10 scale with half-heart increments)
// Filepath: Assets/Scripts/UI/Components/HeartDisplay.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays relationship level as hearts (5 hearts total, 0-10 points scale).
/// Each heart represents 2 points. Half hearts are supported.
/// Assign heart Image components in the Inspector.
/// </summary>
public class HeartDisplay : MonoBehaviour
{
    [Header("Heart Sprites")]
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private Sprite fullHeartSprite;

    [Header("Heart Images")]
    [Tooltip("Assign the 5 heart Image components here")]
    [SerializeField] private List<Image> heartImages = new List<Image>();

    [Header("Colors")]
    [SerializeField] private Color heartColor = Color.white;
    [SerializeField] private Color emptyHeartColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    // Runtime
    private int currentPoints = 0;
    private const int MAX_POINTS = 10;

    /// <summary>
    /// Set the relationship points (0-10 scale)
    /// </summary>
    public void SetPoints(int points)
    {
        currentPoints = Mathf.Clamp(points, 0, MAX_POINTS);
        UpdateHeartDisplay();
    }

    /// <summary>
    /// Get current points
    /// </summary>
    public int GetPoints()
    {
        return currentPoints;
    }

    /// <summary>
    /// Update the visual display of hearts based on current points
    /// </summary>
    private void UpdateHeartDisplay()
    {
        // Each heart = 2 points
        // 0 points = empty, 1 point = half, 2 points = full
        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] == null) continue;

            int pointsForThisHeart = currentPoints - (i * 2);

            if (pointsForThisHeart >= 2)
            {
                // Full heart
                if (fullHeartSprite != null)
                    heartImages[i].sprite = fullHeartSprite;
                heartImages[i].color = heartColor;
            }
            else if (pointsForThisHeart == 1)
            {
                // Half heart
                if (halfHeartSprite != null)
                    heartImages[i].sprite = halfHeartSprite;
                heartImages[i].color = heartColor;
            }
            else
            {
                // Empty heart
                if (emptyHeartSprite != null)
                    heartImages[i].sprite = emptyHeartSprite;
                heartImages[i].color = emptyHeartColor;
            }
        }
    }

    /// <summary>
    /// Refresh the display
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateHeartDisplay();
    }
}
