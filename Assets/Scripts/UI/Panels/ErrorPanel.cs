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
    private Vector2 currentOffset; // Offset calcule (peut etre different de poiOffset si decale)

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
        // S'assurer que le panel est masque au depart
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
        // Si le panel est affiche et qu'on a une reference au POI, 
        // recalculer la position en continu pour suivre les mouvements de camera
        if (isDisplaying && currentPOI != null)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// Affiche le panel d'erreur avec le message specifie
    /// </summary>
    public void ShowError(string message, Transform poiTransform = null)
    {
        if (isDisplaying) return;

        if (errorText != null)
        {
            errorText.text = message;
        }

        // Stocker la reference au POI pour le suivi en continu
        currentPOI = poiTransform;

        // Calculer l'offset adapte selon la position du POI (seulement au moment du clic)
        CalculateAdaptedOffset();

        // Positionner pres du POI
        UpdatePosition();

        // Preparer l'animation d'entree
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
                // Nettoyer la reference au POI seulement quand l'animation est terminee
                currentPOI = null;
            });
    }

    /// <summary>
    /// Calcule l'offset adapte selon la position du POI pour eviter de sortir de l'ecran
    /// </summary>
    private void CalculateAdaptedOffset()
    {
        // Commencer avec l'offset par defaut
        currentOffset = poiOffset;

        if (currentPOI == null)
        {
            return;
        }

        // Trouver la camera principale
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        // Recuperer la vraie largeur du panel
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        float panelWidth = rectTransform.rect.width;

        // Convertir la position du POI en position ecran
        Vector3 poiScreenPosition = mainCamera.WorldToScreenPoint(currentPOI.position);

        // Calculer la position cible avec l'offset normal
        Vector3 targetScreenPosition = poiScreenPosition + new Vector3(poiOffset.x, poiOffset.y, 0f);

        // Calculer combien de pixels du panel seraient caches a droite
        float hiddenWidth = (targetScreenPosition.x + panelWidth) - Screen.width;

        // Changer de côte seulement si 25% ou plus du panel serait cache
        if (hiddenWidth > 0 && hiddenWidth > panelWidth * 0.25f)
        {
            // Decaler vers la gauche : utiliser un offset negatif
            currentOffset.x = -Mathf.Abs(poiOffset.x);
        }
    }

    /// <summary>
    /// Met a jour la position du panel par rapport au POI actuel
    /// </summary>
    private void UpdatePosition()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null) return;

        if (currentPOI == null)
        {
            // Position par defaut si pas de POI specifie
            rectTransform.anchoredPosition = new Vector2(0f, 200f);
            return;
        }

        // Trouver la camera principale
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("ErrorPanel: Main camera not found!");
            rectTransform.anchoredPosition = new Vector2(0f, 200f);
            return;
        }

        // Convertir la position du POI (world space) en position ecran
        Vector3 poiScreenPosition = mainCamera.WorldToScreenPoint(currentPOI.position);

        // Utiliser l'offset adapte (calcule au moment du clic)
        Vector3 targetScreenPosition = poiScreenPosition + new Vector3(currentOffset.x, currentOffset.y, 0f);

        // Convertir la position ecran en position UI locale pour notre RectTransform
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
            // Fallback : utiliser la position ecran directement
            rectTransform.position = targetScreenPosition;
        }
    }

    void OnDestroy()
    {
        // Nettoyer les animations LeanTween
        LeanTween.cancel(gameObject);

        // Nettoyer la reference au POI
        currentPOI = null;
    }
}