using UnityEngine;
using UnityEngine.UI;

public class MapToggleButton : MonoBehaviour
{
    [Header("Button Sprites")]
    public Sprite mapSprite;
    public Sprite backSprite;

    [Header("References")]
    public Image buttonImage; // l'image du bouton (drag depuis l'�diteur)
    public PanelManager panelManager; // r�f�rence � ton PanelManager

    private void Start()
    {
        // S'assurer qu'on a la r�f�rence au PanelManager
        if (panelManager == null)
            panelManager = PanelManager.Instance;

        // S'abonner aux �v�nements du PanelManager pour �tre notifi� des changements d'�tat
        if (panelManager != null)
        {
            panelManager.OnMapStateChanged.AddListener(OnMapStateChanged);
        }

        // Initialiser le sprite selon l'�tat actuel
        UpdateButtonSprite();
    }

    private void OnDestroy()
    {
        // Se d�sabonner des �v�nements pour �viter les erreurs
        if (panelManager != null)
        {
            panelManager.OnMapStateChanged.RemoveListener(OnMapStateChanged);
        }
    }

    /// <summary>
    /// M�thode appel�e quand on clique sur le bouton
    /// </summary>
    public void OnClick()
    {
        if (panelManager == null)
        {
            Debug.LogWarning("MapToggleButton: PanelManager reference is null!");
            return;
        }

        // Utiliser la nouvelle logique centralis�e du PanelManager
        if (panelManager.IsMapVisible)
        {
            // La carte est visible, on revient au panel pr�c�dent
            panelManager.HideMapAndReturnToPrevious();
        }
        else
        {
            // La carte est cach�e, on l'affiche
            panelManager.ShowMap();
        }
    }

    /// <summary>
    /// M�thode appel�e automatiquement quand l'�tat de la carte change
    /// </summary>
    /// <param name="mapIsVisible">True si la carte est visible, false sinon</param>
    private void OnMapStateChanged(bool mapIsVisible)
    {
        UpdateButtonSprite();
    }

    /// <summary>
    /// Met � jour le sprite du bouton selon l'�tat actuel de la carte
    /// </summary>
    private void UpdateButtonSprite()
    {
        if (buttonImage == null || panelManager == null) return;

        if (panelManager.IsMapVisible)
        {
            // La carte est visible, afficher le sprite "retour"
            buttonImage.sprite = backSprite;
        }
        else
        {
            // La carte est cach�e, afficher le sprite "carte"
            buttonImage.sprite = mapSprite;
        }
    }
}