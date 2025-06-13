// Purpose: Simple error panel that appears when travel is not possible
// Filepath: Assets/Scripts/UI/Panels/ErrorPanel.cs
using TMPro;
using UnityEngine;

public class ErrorPanel : MonoBehaviour
{
    public static ErrorPanel Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Display Settings")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private LeanTweenType fadeInEase = LeanTweenType.easeOutQuad;
    [SerializeField] private LeanTweenType fadeOutEase = LeanTweenType.easeInQuad;

    [Header("Position Settings")]
    [SerializeField] private Vector2 poiOffset = new Vector2(100f, 50f);
    [Tooltip("Offset en pixels par rapport au POI (X = droite, Y = haut)")]

    private bool isDisplaying = false;
    private Transform currentPOI = null; // Pour suivre le POI pendant l'affichage
    private Vector2 currentOffset; // Offset calcul� (peut �tre diff�rent de poiOffset si d�cal�)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // S'assurer que le panel est masqu� au d�part
        gameObject.SetActive(false);

        // Initialiser le CanvasGroup
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    void Update()
    {
        // Si le panel est affich� et qu'on a une r�f�rence au POI, 
        // recalculer la position en continu pour suivre les mouvements de cam�ra
        if (isDisplaying && currentPOI != null)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// Affiche le panel d'erreur avec le message sp�cifi�
    /// </summary>
    public void ShowError(string message, Transform poiTransform = null)
    {
        if (isDisplaying) return;

        if (errorText != null)
        {
            errorText.text = message;
        }

        // Stocker la r�f�rence au POI pour le suivi en continu
        currentPOI = poiTransform;

        // Calculer l'offset adapt� selon la position du POI (seulement au moment du clic)
        CalculateAdaptedOffset();

        // Positionner pr�s du POI
        UpdatePosition();

        // Pr�parer l'animation d'entr�e
        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        isDisplaying = true;

        // Annuler toute animation en cours
        LeanTween.cancel(gameObject);

        // Animation de fade in
        LeanTween.alphaCanvas(canvasGroup, 1f, fadeInDuration)
            .setEase(fadeInEase)
            .setOnComplete(() =>
            {
                // Programmer la fermeture automatique
                LeanTween.delayedCall(displayDuration, () =>
                {
                    HideError();
                });
            });
    }

    /// <summary>
    /// Masque le panel d'erreur avec animation
    /// </summary>
    public void HideError()
    {
        if (!isDisplaying) return;

        // Annuler toute animation en cours
        LeanTween.cancel(gameObject);

        // Animation de fade out
        LeanTween.alphaCanvas(canvasGroup, 0f, fadeOutDuration)
            .setEase(fadeOutEase)
            .setOnComplete(() =>
            {
                gameObject.SetActive(false);
                isDisplaying = false;
                // Nettoyer la r�f�rence au POI seulement quand l'animation est termin�e
                currentPOI = null;
            });
    }

    /// <summary>
    /// Calcule l'offset adapt� selon la position du POI pour �viter de sortir de l'�cran
    /// </summary>
    private void CalculateAdaptedOffset()
    {
        // Commencer avec l'offset par d�faut
        currentOffset = poiOffset;

        if (currentPOI == null)
        {
            return;
        }

        // Trouver la cam�ra principale
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        // R�cup�rer la vraie largeur du panel
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        float panelWidth = rectTransform.rect.width;

        // Convertir la position du POI en position �cran
        Vector3 poiScreenPosition = mainCamera.WorldToScreenPoint(currentPOI.position);

        // Calculer la position cible avec l'offset normal
        Vector3 targetScreenPosition = poiScreenPosition + new Vector3(poiOffset.x, poiOffset.y, 0f);

        // Calculer combien de pixels du panel seraient cach�s � droite
        float hiddenWidth = (targetScreenPosition.x + panelWidth) - Screen.width;

        // Changer de c�t� seulement si 25% ou plus du panel serait cach�
        if (hiddenWidth > 0 && hiddenWidth > panelWidth * 0.25f)
        {
            // D�caler vers la gauche : utiliser un offset n�gatif
            currentOffset.x = -Mathf.Abs(poiOffset.x);
        }
    }

    /// <summary>
    /// Met � jour la position du panel par rapport au POI actuel
    /// </summary>
    private void UpdatePosition()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null) return;

        if (currentPOI == null)
        {
            // Position par d�faut si pas de POI sp�cifi�
            rectTransform.anchoredPosition = new Vector2(0f, 200f);
            return;
        }

        // Trouver la cam�ra principale
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("ErrorPanel: Main camera not found!");
            rectTransform.anchoredPosition = new Vector2(0f, 200f);
            return;
        }

        // Convertir la position du POI (world space) en position �cran
        Vector3 poiScreenPosition = mainCamera.WorldToScreenPoint(currentPOI.position);

        // Utiliser l'offset adapt� (calcul� au moment du clic)
        Vector3 targetScreenPosition = poiScreenPosition + new Vector3(currentOffset.x, currentOffset.y, 0f);

        // Convertir la position �cran en position UI locale pour notre RectTransform
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                targetScreenPosition,
                parentCanvas.worldCamera,
                out localPosition
            );

            rectTransform.anchoredPosition = localPosition;
        }
        else
        {
            // Fallback : utiliser la position �cran directement
            rectTransform.position = targetScreenPosition;
        }
    }

    void OnDestroy()
    {
        // Nettoyer les animations LeanTween
        LeanTween.cancel(gameObject);

        // Nettoyer la r�f�rence au POI
        currentPOI = null;
    }
}