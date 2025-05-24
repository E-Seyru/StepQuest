// Purpose: Animates a sprite that moves along the travel path during journey
// Filepath: Assets/Scripts/UI/Components/TravelSpriteAnimator.cs
using UnityEngine;

public class TravelSpriteAnimator : MonoBehaviour
{
    [Header("Sprite Settings")]
    [SerializeField] private GameObject travelSprite; // Le sprite qui se d�place
    [SerializeField] private float spriteScale = 1f; // Taille du sprite
    [SerializeField] private Vector3 spriteOffset = Vector3.zero; // D�calage par rapport au trajet

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f; // Dur�e des mouvements LeanTween
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeInOutQuad;
    [SerializeField] private bool bounceAnimation = true; // Animation de rebond pendant le mouvement
    [SerializeField] private float bounceHeight = 0.2f; // Hauteur du rebond
    [SerializeField] private float bounceSpeed = 2f; // Vitesse du rebond

    [Header("Debug")]
    [SerializeField] private bool showDebugPath = false;
    [SerializeField] private Color debugPathColor = Color.yellow;

    // R�f�rences
    private MapManager mapManager;
    private DataManager dataManager;
    private LocationRegistry locationRegistry;

    // �tat du voyage
    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool isAnimating = false;
    private int currentBounceId = -1;

    public static TravelSpriteAnimator Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Logger.LogWarning("TravelSpriteAnimator: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.MapLog);
            Destroy(gameObject);
            return;
        }

        // Le sprite (personnage) reste toujours visible
        if (travelSprite != null)
        {
            travelSprite.SetActive(true);
        }
    }

    void Start()
    {
        // Obtenir les r�f�rences
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance;

        if (mapManager != null)
        {
            locationRegistry = mapManager.LocationRegistry;

            // S'abonner aux �v�nements de voyage
            mapManager.OnTravelStarted += OnTravelStarted;
            mapManager.OnTravelCompleted += OnTravelCompleted;
            mapManager.OnTravelProgress += OnTravelProgress;
            mapManager.OnLocationChanged += OnLocationChanged; // NOUVEAU
        }

        // Validation
        if (travelSprite == null)
        {
            Logger.LogError("TravelSpriteAnimator: travelSprite not assigned!", Logger.LogCategory.MapLog);
        }

        // Positionner le personnage � sa location actuelle au d�marrage
        StartCoroutine(PositionPlayerAfterDelay());
    }

    /// <summary>
    /// Positionne le joueur apr�s un petit d�lai pour s'assurer que tout est initialis�
    /// </summary>
    private System.Collections.IEnumerator PositionPlayerAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // Attendre que tout soit charg�
        PositionPlayerAtCurrentLocation();
    }

    /// <summary>
    /// NOUVEAU: Appel� quand le joueur change de location (m�me sans voyage)
    /// </summary>
    private void OnLocationChanged(MapLocationDefinition newLocation)
    {
        // Si le joueur n'est pas en voyage, s'assurer qu'il est � la bonne position
        if (!dataManager.PlayerData.IsCurrentlyTraveling())
        {
            PositionPlayerAtCurrentLocation();
        }
    }

    void OnDestroy()
    {
        // Se d�sabonner des �v�nements
        if (mapManager != null)
        {
            mapManager.OnTravelStarted -= OnTravelStarted;
            mapManager.OnTravelCompleted -= OnTravelCompleted;
            mapManager.OnTravelProgress -= OnTravelProgress;
            mapManager.OnLocationChanged -= OnLocationChanged; // NOUVEAU
        }

        // Arr�ter les animations en cours
        StopAllAnimations();
    }

    /// <summary>
    /// Appel� quand un voyage commence
    /// </summary>
    private void OnTravelStarted(string destinationId)
    {
        SetupTravelPath(destinationId);
        PositionPlayerAtStart();

        Logger.LogInfo($"TravelSpriteAnimator: Travel animation started to {destinationId}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Appel� pendant le voyage pour mettre � jour la position
    /// </summary>
    private void OnTravelProgress(string destinationId, int currentSteps, int requiredSteps)
    {
        if (travelSprite != null && travelSprite.activeSelf)
        {
            float progress = requiredSteps > 0 ? (float)currentSteps / requiredSteps : 0f;
            progress = Mathf.Clamp01(progress);

            UpdateSpritePosition(progress);
        }
    }

    /// <summary>
    /// Appel� quand un voyage se termine
    /// </summary>
    private void OnTravelCompleted(string arrivedLocationId)
    {
        // Le personnage reste � sa nouvelle position (pas de disparition)
        AnimateToDestination();
        StopBounceAnimation(); // Arr�te le rebond une fois arriv�

        Logger.LogInfo($"TravelSpriteAnimator: Player arrived at {arrivedLocationId}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Configure le chemin de voyage entre deux locations
    /// </summary>
    private void SetupTravelPath(string destinationId)
    {
        if (mapManager?.CurrentLocation == null || locationRegistry == null)
        {
            Logger.LogError("TravelSpriteAnimator: Cannot setup travel path - missing references", Logger.LogCategory.MapLog);
            return;
        }

        // Trouver les positions des POI dans le monde
        startPosition = FindPOIPosition(mapManager.CurrentLocation.LocationID);
        endPosition = FindPOIPosition(destinationId);

        // Ajouter les offsets
        startPosition += spriteOffset;
        endPosition += spriteOffset;

        Logger.LogInfo($"TravelSpriteAnimator: Path setup from {startPosition} to {endPosition}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Trouve la position d'un POI dans le monde par son LocationID
    /// </summary>
    private Vector3 FindPOIPosition(string locationId)
    {
        // Chercher tous les POI dans la sc�ne
        POI[] allPOIs = FindObjectsOfType<POI>();

        foreach (POI poi in allPOIs)
        {
            if (poi.LocationID == locationId)
            {
                return poi.transform.position;
            }
        }

        // Si on ne trouve pas le POI, retourner une position par d�faut
        Logger.LogWarning($"TravelSpriteAnimator: POI not found for location {locationId}", Logger.LogCategory.MapLog);
        return Vector3.zero;
    }

    /// <summary>
    /// Positionne le personnage � sa location actuelle au d�marrage
    /// </summary>
    private void PositionPlayerAtStart()
    {
        if (travelSprite == null) return;

        travelSprite.SetActive(true);
        travelSprite.transform.position = startPosition;
        travelSprite.transform.localScale = Vector3.one * spriteScale;

        // D�marrer l'animation de rebond pendant le voyage
        if (bounceAnimation)
        {
            StartBounceAnimation();
        }
    }

    /// <summary>
    /// Positionne le personnage � sa location actuelle (au d�marrage du jeu)
    /// </summary>
    public void PositionPlayerAtCurrentLocation()
    {
        if (mapManager?.CurrentLocation == null || travelSprite == null)
        {
            return;
        }

        Vector3 currentPos = FindPOIPosition(mapManager.CurrentLocation.LocationID);
        currentPos += spriteOffset;

        travelSprite.transform.position = currentPos;
        travelSprite.SetActive(true);

        Logger.LogInfo($"TravelSpriteAnimator: Player positioned at {mapManager.CurrentLocation.DisplayName}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Met � jour la position du sprite bas�e sur le progr�s
    /// </summary>
    private void UpdateSpritePosition(float progress)
    {
        if (travelSprite == null) return;

        Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, progress);

        // Animation fluide vers la nouvelle position
        if (!isAnimating)
        {
            LeanTween.move(travelSprite, currentPosition, animationDuration)
                .setEase(easeType)
                .setOnStart(() => isAnimating = true)
                .setOnComplete(() => isAnimating = false);
        }
    }

    /// <summary>
    /// Anime le sprite jusqu'� la destination finale
    /// </summary>
    private void AnimateToDestination()
    {
        if (travelSprite == null) return;

        LeanTween.move(travelSprite, endPosition, animationDuration)
            .setEase(easeType)
            .setOnComplete(() =>
            {
                // Le personnage reste � sa nouvelle position
                Logger.LogInfo("TravelSpriteAnimator: Player positioned at destination", Logger.LogCategory.MapLog);
            });
    }

    // SUPPRIM�: HideTravelSprite() car le personnage ne dispara�t jamais

    /// <summary>
    /// D�marre l'animation de rebond
    /// </summary>
    private void StartBounceAnimation()
    {
        if (travelSprite == null) return;

        StopBounceAnimation(); // S'assurer qu'il n'y a pas d'animation en cours

        Vector3 originalPos = travelSprite.transform.position;
        Vector3 bouncePos = originalPos + Vector3.up * bounceHeight;

        currentBounceId = LeanTween.moveY(travelSprite, bouncePos.y, 1f / bounceSpeed)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong()
            .id;
    }

    /// <summary>
    /// Arr�te l'animation de rebond
    /// </summary>
    private void StopBounceAnimation()
    {
        if (currentBounceId >= 0)
        {
            LeanTween.cancel(currentBounceId);
            currentBounceId = -1;
        }
    }

    /// <summary>
    /// Arr�te toutes les animations
    /// </summary>
    private void StopAllAnimations()
    {
        if (travelSprite != null)
        {
            LeanTween.cancel(travelSprite);
        }
        StopBounceAnimation();
        isAnimating = false;
    }

    /// <summary>
    /// Force une mise � jour de position (utile pour le debug)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceUpdatePosition()
    {
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            long currentTotalSteps = dataManager.PlayerData.TotalSteps;
            long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
            int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;

            float progress = requiredSteps > 0 ? (float)progressSteps / requiredSteps : 0f;
            progress = Mathf.Clamp01(progress);

            if (travelSprite != null)
            {
                Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, progress);
                travelSprite.transform.position = currentPosition;
            }
        }
    }

    /// <summary>
    /// Debug : dessiner le chemin dans la Scene View
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugPath) return;

        Gizmos.color = debugPathColor;

        if (startPosition != Vector3.zero && endPosition != Vector3.zero)
        {
            Gizmos.DrawLine(startPosition, endPosition);
            Gizmos.DrawWireSphere(startPosition, 0.5f);
            Gizmos.DrawWireSphere(endPosition, 0.5f);
        }
    }
}