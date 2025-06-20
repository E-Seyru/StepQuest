using UnityEngine;
using UnityEngine.UI;

public class MapToggleButton : MonoBehaviour
{
    [Header("Button Sprites")]
    public Sprite mapSprite;
    public Sprite backSprite;

    [Header("References")]
    public Image buttonImage; // l'image du bouton (drag depuis l'editeur)
    public PanelManager panelManager; // reference a ton PanelManager

    private void Start()
    {
        // S'assurer qu'on a la reference au PanelManager
        if (panelManager == null)
            panelManager = PanelManager.Instance;

        // S'abonner aux evenements du PanelManager pour être notifie des changements d'etat
        if (panelManager != null)
        {
            panelManager.OnMapStateChanged.AddListener(OnMapStateChanged);
        }

        // Initialiser le sprite selon l'etat actuel
        UpdateButtonSprite();
    }

    private void OnDestroy()
    {
        // Se desabonner des evenements pour eviter les erreurs
        if (panelManager != null)
        {
            panelManager.OnMapStateChanged.RemoveListener(OnMapStateChanged);
        }
    }

    /// <summary>
    /// Methode appelee quand on clique sur le bouton
    /// </summary>
    public void OnClick()
    {
        if (panelManager == null)
        {
            Debug.LogWarning("MapToggleButton: PanelManager reference is null!");
            return;
        }

        // Utiliser la nouvelle logique centralisee du PanelManager
        if (panelManager.IsMapVisible)
        {
            // La carte est visible, on revient au panel precedent
            panelManager.HideMapAndReturnToPrevious();
        }
        else
        {
            // La carte est cachee, on l'affiche
            panelManager.ShowMap();
        }
    }

    /// <summary>
    /// Methode appelee automatiquement quand l'etat de la carte change
    /// </summary>
    /// <param name="mapIsVisible">True si la carte est visible, false sinon</param>
    private void OnMapStateChanged(bool mapIsVisible)
    {
        UpdateButtonSprite();
    }

    /// <summary>
    /// Met a jour le sprite du bouton selon l'etat actuel de la carte
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
            // La carte est cachee, afficher le sprite "carte"
            buttonImage.sprite = mapSprite;
        }
    }
}