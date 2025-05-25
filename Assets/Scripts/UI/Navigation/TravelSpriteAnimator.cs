// Purpose: Animates a sprite that moves along the travel path during journey
// Filepath: Assets/Scripts/UI/Components/TravelSpriteAnimator.cs
using UnityEngine;

public class TravelSpriteAnimator : MonoBehaviour
{
    [Header("Sprite Settings")]
    [SerializeField] private GameObject travelSprite; // Le sprite qui se déplace
    [SerializeField] private float spriteScale = 1f; // Taille du sprite
    [SerializeField] private Vector3 spriteOffset = Vector3.zero; // Décalage par rapport au trajet
    [SerializeField] private Transform spriteVisual;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f; // Durée des mouvements LeanTween
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeInOutQuad;
    [SerializeField] private bool bounceAnimation = true; // Animation de rebond pendant le mouvement
    [SerializeField] private float bounceHeight = 0.2f; // Hauteur du rebond
    [SerializeField] private float bounceSpeed = 2f; // Vitesse du rebond

    [Header("Debug")]
    [SerializeField] private bool showDebugPath = false;
    [SerializeField] private Color debugPathColor = Color.yellow;

    // Références
    private MapManager mapManager;
    private DataManager dataManager;
    private LocationRegistry locationRegistry;

    // État du voyage
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
        // Obtenir les références
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance;

        if (mapManager != null)
        {
            locationRegistry = mapManager.LocationRegistry;

            // S'abonner aux événements de voyage
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

        // Positionner le personnage à sa location actuelle au démarrage
        StartCoroutine(PositionPlayerAfterDelay());
    }

    /// <summary>
    /// Positionne le joueur après un petit délai pour s'assurer que tout est initialisé
    /// </summary>
    private System.Collections.IEnumerator PositionPlayerAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // Attendre que tout soit chargé
        PositionPlayerAtCurrentLocation();
    }

    /// <summary>
    /// NOUVEAU: Appelé quand le joueur change de location (même sans voyage)
    /// </summary>
    private void OnLocationChanged(MapLocationDefinition newLocation)
    {
        // Si le joueur n'est pas en voyage, s'assurer qu'il est à la bonne position
        if (!dataManager.PlayerData.IsCurrentlyTraveling())
        {
            PositionPlayerAtCurrentLocation();
        }
    }

    void OnDestroy()
    {
        // Se désabonner des événements
        if (mapManager != null)
        {
            mapManager.OnTravelStarted -= OnTravelStarted;
            mapManager.OnTravelCompleted -= OnTravelCompleted;
            mapManager.OnTravelProgress -= OnTravelProgress;
            mapManager.OnLocationChanged -= OnLocationChanged; // NOUVEAU
        }

        // Arrêter les animations en cours
        StopAllAnimations();
    }

    /// <summary>
    /// Appelé quand un voyage commence
    /// </summary>
    private void OnTravelStarted(string destinationId)
    {
        SetupTravelPath(destinationId);
        PositionPlayerAtStart();

        Logger.LogInfo($"TravelSpriteAnimator: Travel animation started to {destinationId}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Appelé pendant le voyage pour mettre à jour la position
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
    /// Appelé quand un voyage se termine
    /// </summary>
    private void OnTravelCompleted(string arrivedLocationId)
    {
        // Le personnage reste à sa nouvelle position (pas de disparition)
        AnimateToDestination();
        StopBounceAnimation(); // Arrête le rebond une fois arrivé

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

        // Trouver les positions des POI dans le monde (en utilisant les points de départ personnalisés)
        startPosition = FindPOITravelStartPosition(mapManager.CurrentLocation.LocationID);
        endPosition = FindPOITravelStartPosition(destinationId);

        // Ajouter les offsets
        startPosition += spriteOffset;
        endPosition += spriteOffset;

        Logger.LogInfo($"TravelSpriteAnimator: Path setup from {startPosition} to {endPosition}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// MODIFIÉ: Trouve la position de départ pour le voyage d'un POI dans le monde par son LocationID
    /// Utilise maintenant GetTravelPathStartPosition() au lieu de la position du transform
    /// </summary>
    private Vector3 FindPOITravelStartPosition(string locationId)
    {
        // Chercher tous les POI dans la scène
        POI[] allPOIs = FindObjectsOfType<POI>();

        Logger.LogInfo($"TravelSpriteAnimator: Looking for POI with LocationID '{locationId}' among {allPOIs.Length} POIs", Logger.LogCategory.MapLog);

        foreach (POI poi in allPOIs)
        {
            if (poi.LocationID == locationId)
            {
                // NOUVEAU: Utilise la méthode GetTravelPathStartPosition() du POI
                Vector3 startPos = poi.GetTravelPathStartPosition();
                Logger.LogInfo($"TravelSpriteAnimator: Found POI '{locationId}' at travel start position {startPos}", Logger.LogCategory.MapLog);
                return startPos;
            }
        }

        // Si on ne trouve pas le POI, retourner une position par défaut
        Logger.LogWarning($"TravelSpriteAnimator: POI not found for location {locationId}. Available POIs: {string.Join(", ", System.Array.ConvertAll(allPOIs, p => p.LocationID))}", Logger.LogCategory.MapLog);
        return Vector3.zero;
    }

    /// <summary>
    /// ANCIEN: Trouve la position d'un POI dans le monde par son LocationID (garde pour compatibilité si besoin)
    /// </summary>
    private Vector3 FindPOIPosition(string locationId)
    {
        // Chercher tous les POI dans la scène
        POI[] allPOIs = FindObjectsOfType<POI>();

        foreach (POI poi in allPOIs)
        {
            if (poi.LocationID == locationId)
            {
                return poi.transform.position;
            }
        }

        // Si on ne trouve pas le POI, retourner une position par défaut
        Logger.LogWarning($"TravelSpriteAnimator: POI not found for location {locationId}", Logger.LogCategory.MapLog);
        return Vector3.zero;
    }

    /// <summary>
    /// Positionne le personnage à sa location actuelle au démarrage
    /// </summary>
    private void PositionPlayerAtStart()
    {
        if (travelSprite == null) return;

        travelSprite.SetActive(true);
        travelSprite.transform.position = startPosition;
        travelSprite.transform.localScale = Vector3.one * spriteScale;

        // Démarrer l'animation de rebond pendant le voyage
        if (bounceAnimation)
        {
            StartBounceAnimation();
        }
    }

    /// <summary>
    /// MODIFIÉ: Positionne le personnage à sa location actuelle (au démarrage du jeu)
    /// Utilise maintenant le point de départ personnalisé
    /// </summary>
    public void PositionPlayerAtCurrentLocation()
    {
        if (mapManager?.CurrentLocation == null || travelSprite == null)
        {
            Logger.LogWarning("TravelSpriteAnimator: Cannot position player - mapManager.CurrentLocation or travelSprite is null", Logger.LogCategory.MapLog);
            return;
        }

        // MODIFIÉ: Utilise FindPOITravelStartPosition au lieu de FindPOIPosition
        Vector3 currentPos = FindPOITravelStartPosition(mapManager.CurrentLocation.LocationID);

        // Vérifier si on a trouvé une position valide
        if (currentPos == Vector3.zero)
        {
            Logger.LogError($"TravelSpriteAnimator: Could not find valid position for location '{mapManager.CurrentLocation.LocationID}'. Trying fallback method.", Logger.LogCategory.MapLog);
            // Fallback vers l'ancienne méthode
            currentPos = FindPOIPosition(mapManager.CurrentLocation.LocationID);
        }

        currentPos += spriteOffset;

        Logger.LogInfo($"TravelSpriteAnimator: Positioning player at {currentPos} for location {mapManager.CurrentLocation.DisplayName}", Logger.LogCategory.MapLog);

        travelSprite.transform.position = currentPos;
        travelSprite.SetActive(true);

        Logger.LogInfo($"TravelSpriteAnimator: Player positioned at {mapManager.CurrentLocation.DisplayName}. Sprite active: {travelSprite.activeSelf}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Met à jour la position du sprite basée sur le progrès
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
    /// Anime le sprite jusqu'à la destination finale
    /// </summary>
    private void AnimateToDestination()
    {
        if (travelSprite == null) return;

        LeanTween.move(travelSprite, endPosition, animationDuration)
            .setEase(easeType)
            .setOnComplete(() =>
            {
                // Le personnage reste à sa nouvelle position
                Logger.LogInfo("TravelSpriteAnimator: Player positioned at destination", Logger.LogCategory.MapLog);
            });
    }

    // SUPPRIMÉ: HideTravelSprite() car le personnage ne disparaît jamais

    /// <summary>
    /// Démarre l'animation de rebond
    /// </summary>
    private void StartBounceAnimation()
    {
        if (spriteVisual == null) return;

        // stoppe toute animation résiduelle
        LeanTween.cancel(spriteVisual.gameObject);
        spriteVisual.localPosition = Vector3.zero;    // point de repos

        currentBounceId = LeanTween
            .moveLocalY(spriteVisual.gameObject,       // le child, pas le parent !
                        bounceHeight,
                        0.5f / bounceSpeed)           // demi-période
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong()
            .id;
    }

    /// <summary>
    /// Arrête l'animation de rebond
    /// </summary>
    private void StopBounceAnimation()
    {
        if (currentBounceId >= 0)
            LeanTween.cancel(currentBounceId);

        currentBounceId = -1;
        if (spriteVisual != null)                     // remet au repos
            spriteVisual.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Arrête toutes les animations
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
    /// Force une mise à jour de position (utile pour le debug)
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