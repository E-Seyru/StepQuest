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
    private Vector2 currentOffset; // Offset calculé (peut être différent de poiOffset si décalé)

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
        // S'assurer que le panel est masqué au départ
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
        // Si le panel est affiché et qu'on a une référence au POI, 
        // recalculer la position en continu pour suivre les mouvements de caméra
        if (isDisplaying && currentPOI != null)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// Affiche le panel d'erreur avec le message spécifié
    /// </summary>
    public void ShowError(string message, Transform poiTransform = null)
    {
        if (isDisplaying) return;

        if (errorText != null)
        {
            errorText.text = message;
        }

        // Stocker la référence au POI pour le suivi en continu
        currentPOI = poiTransform;

        // Calculer l'offset adapté selon la position du POI (seulement au moment du clic)
        CalculateAdaptedOffset();

        // Positionner près du POI
        UpdatePosition();

        // Préparer l'animation d'entrée
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
                // Nettoyer la référence au POI seulement quand l'animation est terminée
                currentPOI = null;
            });
    }

    /// <summary>
    /// Calcule l'offset adapté selon la position du POI pour éviter de sortir de l'écran
    /// </summary>
    private void CalculateAdaptedOffset()
    {
        // Commencer avec l'offset par défaut
        currentOffset = poiOffset;

        if (currentPOI == null)
        {
            return;
        }

        // Trouver la caméra principale
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        // Récupérer la vraie largeur du panel
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        float panelWidth = rectTransform.rect.width;

        // Convertir la position du POI en position écran
        Vector3 poiScreenPosition = mainCamera.WorldToScreenPoint(currentPOI.position);

        // Calculer la position cible avec l'offset normal
        Vector3 targetScreenPosition = poiScreenPosition + new Vector3(poiOffset.x, poiOffset.y, 0f);

        // Calculer combien de pixels du panel seraient cachés à droite
        float hiddenWidth = (targetScreenPosition.x + panelWidth) - Screen.width;

        // Changer de côté seulement si 25% ou plus du panel serait caché
        if (hiddenWidth > 0 && hiddenWidth > panelWidth * 0.25f)
        {
            // Décaler vers la gauche : utiliser un offset négatif
            currentOffset.x = -Mathf.Abs(poiOffset.x);
        }
    }

    /// <summary>
    /// Met à jour la position du panel par rapport au POI actuel
    /// </summary>
    private void UpdatePosition()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null) return;

        if (currentPOI == null)
        {
            // Position par défaut si pas de POI spécifié
            rectTransform.anchoredPosition = new Vector2(0f, 200f);
            return;
        }

        // Trouver la caméra principale
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("ErrorPanel: Main camera not found!");
            rectTransform.anchoredPosition = new Vector2(0f, 200f);
            return;
        }

        // Convertir la position du POI (world space) en position écran
        Vector3 poiScreenPosition = mainCamera.WorldToScreenPoint(currentPOI.position);

        // Utiliser l'offset adapté (calculé au moment du clic)
        Vector3 targetScreenPosition = poiScreenPosition + new Vector3(currentOffset.x, currentOffset.y, 0f);

        // Convertir la position écran en position UI locale pour notre RectTransform
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
            // Fallback : utiliser la position écran directement
            rectTransform.position = targetScreenPosition;
        }
    }

    void OnDestroy()
    {
        // Nettoyer les animations LeanTween
        LeanTween.cancel(gameObject);

        // Nettoyer la référence au POI
        currentPOI = null;
    }
}