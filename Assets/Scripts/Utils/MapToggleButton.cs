using UnityEngine;
using UnityEngine.UI;

public class MapToggleButton : MonoBehaviour
{
    [Header("Button Sprites")]
    public Sprite mapSprite;
    public Sprite backSprite;

    [Header("References")]
    public Image buttonImage; // l'image du bouton (drag depuis l'éditeur)
    public PanelManager panelManager; // référence à ton PanelManager

    private void Start()
    {
        // S'assurer qu'on a la référence au PanelManager
        if (panelManager == null)
            panelManager = PanelManager.Instance;

        // S'abonner aux événements du PanelManager pour être notifié des changements d'état
        if (panelManager != null)
        {
            panelManager.OnMapStateChanged.AddListener(OnMapStateChanged);
        }

        // Initialiser le sprite selon l'état actuel
        UpdateButtonSprite();
    }

    private void OnDestroy()
    {
        // Se désabonner des événements pour éviter les erreurs
        if (panelManager != null)
        {
            panelManager.OnMapStateChanged.RemoveListener(OnMapStateChanged);
        }
    }

    /// <summary>
    /// Méthode appelée quand on clique sur le bouton
    /// </summary>
    public void OnClick()
    {
        if (panelManager == null)
        {
            Debug.LogWarning("MapToggleButton: PanelManager reference is null!");
            return;
        }

        // Utiliser la nouvelle logique centralisée du PanelManager
        if (panelManager.IsMapVisible)
        {
            // La carte est visible, on revient au panel précédent
            panelManager.HideMapAndReturnToPrevious();
        }
        else
        {
            // La carte est cachée, on l'affiche
            panelManager.ShowMap();
        }
    }

    /// <summary>
    /// Méthode appelée automatiquement quand l'état de la carte change
    /// </summary>
    /// <param name="mapIsVisible">True si la carte est visible, false sinon</param>
    private void OnMapStateChanged(bool mapIsVisible)
    {
        UpdateButtonSprite();
    }

    /// <summary>
    /// Met à jour le sprite du bouton selon l'état actuel de la carte
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
            // La carte est cachée, afficher le sprite "carte"
            buttonImage.sprite = mapSprite;
        }
    }
}