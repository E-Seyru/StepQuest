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

    [Header("Double Click Protection")]
    [SerializeField] private float clickCooldown = 0.5f; // Temps d'attente entre les clics (en secondes)

    private float lastClickTime = 0f; // Temps du dernier clic
    private bool isProcessingClick = false; // Pour eviter les clics pendant le traitement

    private void Start()
    {
        // S'assurer qu'on a la reference au PanelManager
        if (panelManager == null)
            panelManager = PanelManager.Instance;

        // S'abonner aux evenements du PanelManager pour etre notifie des changements d'etat
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
        // Verification 1: Le PanelManager existe-t-il ?
        if (panelManager == null)
        {
            Debug.LogWarning("MapToggleButton: PanelManager reference is null!");
            return;
        }

        // Verification 2: Sommes-nous deja en train de traiter un clic ?
        if (isProcessingClick)
        {
            Debug.Log("MapToggleButton: Clic ignore - traitement en cours");
            return;
        }

        // Verification 3: Le cooldown est-il respecte ?
        float currentTime = Time.time;
        if (currentTime - lastClickTime < clickCooldown)
        {
            Debug.Log($"MapToggleButton: Clic ignore - cooldown actif ({clickCooldown}s)");
            return;
        }

        // Tous les checks sont passes, on peut traiter le clic
        ProcessClick(currentTime);
    }

    /// <summary>
    /// Traite le clic de maniere securisee
    /// </summary>
    /// <param name="clickTime">Le temps auquel le clic a eu lieu</param>
    private void ProcessClick(float clickTime)
    {
        // Marquer qu'on est en train de traiter le clic
        isProcessingClick = true;
        lastClickTime = clickTime;

        try
        {
            // Utiliser la nouvelle logique centralisee du PanelManager
            if (panelManager.IsMapVisible)
            {
                // La carte est visible, on revient au panel precedent
                Debug.Log("MapToggleButton: Masquage de la carte");
                panelManager.HideMapAndReturnToPrevious();
            }
            else
            {
                // La carte est cachee, on l'affiche
                Debug.Log("MapToggleButton: Affichage de la carte");
                panelManager.ShowMap();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MapToggleButton: Erreur lors du traitement du clic - {e.Message}");
        }
        finally
        {
            // Remettre le flag a false apres un court delai pour etre sur que l'operation est terminee
            Invoke(nameof(ResetClickFlag), 0.1f);
        }
    }

    /// <summary>
    /// Remet le flag de traitement a false
    /// </summary>
    private void ResetClickFlag()
    {
        isProcessingClick = false;
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