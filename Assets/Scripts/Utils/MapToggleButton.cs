using UnityEngine;
using UnityEngine.UI;

public class MapToggleButton : MonoBehaviour
{
    public Sprite mapSprite;
    public Sprite backSprite;
    public Image buttonImage; // l'image du bouton (drag depuis l'éditeur)
    public PanelManager panelManager; // référence à ton PanelManager

    private bool showingMap = false;

    public void OnClick()
    {
        if (showingMap)
        {
            panelManager.ShowAndHideMapPanel(); // Fermer la carte
            buttonImage.sprite = mapSprite;
        }
        else
        {
            panelManager.ShowAndHideMapPanel(); // Ouvrir la carte
            buttonImage.sprite = backSprite;
        }
        showingMap = !showingMap;
    }
}
