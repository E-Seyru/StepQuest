

// ===============================================
// SERVICE: Animation Management
// ===============================================
using UnityEngine;
using UnityEngine.UI;

public class AboveCanvasAnimationService
{
    private readonly AboveCanvasManager manager;
    private float lastProgressValue = -1f;
    private int currentAnimationId = -1;
    private int currentPulseId = -1;
    private int currentPopId = -1;        // Renomme pour le pop
    private Color originalFillColor;
    private Vector3 originalFillScale;
    private Vector3 originalRightIconScale;  // echelle originale de l'icône droite
    private Color originalRightIconColor;    // Couleur originale de l'icône droite

    // NOUVEAU : Positions originales pour les animations de slide
    private Vector3 activityBarOriginalPosition;
    private Vector3 idleBarOriginalPosition;
    private bool positionsSaved = false;

    // NOUVEAU : Variables pour l'animation de l'IdleBar
    private Vector3 idleBarImageOriginalScale;    // echelle originale de l'image IdleBar
    private Vector3 idleBarImageOriginalPosition; // Position originale de l'image IdleBar
    private int idleAnimationTimerId = -1;        // ID du timer pour repeter l'animation
    private int idleSnoreAnimationId = -1;        // ID de l'animation de ronflement
    private int idleShakeAnimationId = -1;        // ID de l'animation de vibration
    private bool isIdleAnimationActive = false;   // Flag pour savoir si l'animation est active

    public AboveCanvasAnimationService(AboveCanvasManager manager)
    {
        this.manager = manager;
    }

    public void Initialize()
    {
        // Configuration des animations peut se faire ici
        SaveOriginalPositions();
        SaveIdleBarImageOriginalValues();
    }

    private void SaveOriginalPositions()
    {
        if (positionsSaved) return;

        if (manager.ActivityBar != null)
        {
            activityBarOriginalPosition = manager.ActivityBar.transform.localPosition;
        }

        if (manager.IdleBar != null)
        {
            idleBarOriginalPosition = manager.IdleBar.transform.localPosition;
        }

        positionsSaved = true;
    }

    // NOUVEAU : Sauvegarder les valeurs originales de l'image IdleBar
    private void SaveIdleBarImageOriginalValues()
    {
        if (manager.IdleBarImage != null)
        {
            idleBarImageOriginalScale = manager.IdleBarImage.transform.localScale;
            idleBarImageOriginalPosition = manager.IdleBarImage.transform.localPosition;
        }
    }

    public void SetupProgressBar()
    {
        if (manager.FillBar != null)
        {
            // Configurer en mode Filled
            if (manager.FillBar.type != Image.Type.Filled)
            {
                manager.FillBar.type = Image.Type.Filled;
                manager.FillBar.fillMethod = Image.FillMethod.Horizontal;
            }

            // Sauvegarder les valeurs originales
            originalFillColor = manager.FillBar.color;
            originalFillScale = manager.FillBar.transform.localScale;
            manager.FillBar.fillAmount = 0f;
            lastProgressValue = 0f;
        }

        if (manager.BackgroundBar != null && manager.BackgroundBar.type == Image.Type.Filled)
        {
            manager.BackgroundBar.type = Image.Type.Simple;
        }

        // Sauvegarder les valeurs originales de l'icône droite pour l'animation de pop
        if (manager.RightIcon != null)
        {
            originalRightIconScale = manager.RightIcon.transform.localScale;
            originalRightIconColor = manager.RightIcon.color;
        }
    }

    public void AnimateProgressBar(float targetProgress)
    {
        if (manager.FillBar == null) return;
        if (Mathf.Approximately(targetProgress, lastProgressValue)) return;

        // Arrêter les animations precedentes
        if (currentAnimationId != -1)
        {
            LeanTween.cancel(currentAnimationId);
        }
        if (currentPulseId != -1)
        {
            LeanTween.cancel(currentPulseId);
        }

        // 1. Animer le remplissage de la barre
        currentAnimationId = LeanTween.value(manager.gameObject, lastProgressValue, targetProgress, manager.ProgressAnimationDuration)
            .setEase(manager.ProgressAnimationEase)
            .setOnUpdate((float val) =>
            {
                if (manager.FillBar != null)
                {
                    manager.FillBar.fillAmount = val;
                }
            })
            .setOnComplete(() =>
            {
                currentAnimationId = -1;
            }).id;

        // 2. IMMeDIATEMENT declencher le pulse en parallèle (pas a la fin !)
        PulseFillBarParallel();

        lastProgressValue = targetProgress;
    }

    private void PulseFillBarParallel()
    {
        if (manager.FillBar == null) return;

        // Pulse d'echelle EN MÊME TEMPS que l'animation de remplissage
        currentPulseId = LeanTween.scale(manager.FillBar.gameObject, originalFillScale * manager.PulseScaleAmount, manager.PulseDuration)
            .setEase(LeanTweenType.easeOutQuart)
            .setLoopPingPong(1)
            .setOnComplete(() =>
            {
                currentPulseId = -1;
                if (manager.FillBar != null)
                {
                    manager.FillBar.transform.localScale = originalFillScale;
                }
            }).id;

        // Animation de couleur EN PARALLÈLE (pas d'ID a stocker, courte duree)
        LeanTween.color(manager.FillBar.rectTransform, manager.PulseColor, manager.PulseDuration)
            .setEase(LeanTweenType.easeOutQuart)
            .setLoopPingPong(1)
            .setOnComplete(() =>
            {
                if (manager.FillBar != null)
                {
                    manager.FillBar.color = originalFillColor;
                }
            });
    }

    public void PulseFillBar()
    {
        // Methode separee pour les cas où on veut juste un pulse sans animation de remplissage
        PulseFillBarParallel();
    }

    public void ShakeRightIcon()
    {
        // NOUVEAU : Pop de recompense au lieu de shake d'erreur !
        PopRightIcon();
    }

    // NOUVEAU : Animation de slide pour les barres
    public void SlideInBar(GameObject bar)
    {
        if (bar == null) return;

        // Activer la barre d'abord
        bar.SetActive(true);

        // Determiner la position originale
        Vector3 originalPos;
        if (bar == manager.ActivityBar)
        {
            originalPos = activityBarOriginalPosition;
        }
        else if (bar == manager.IdleBar)
        {
            originalPos = idleBarOriginalPosition;
        }
        else
        {
            return; // Barre inconnue
        }

        // Position de depart (au-dessus, cachee)
        Vector3 startPos = originalPos;
        startPos.y += 100f; // Decaler vers le haut

        // Positionner la barre en position de depart
        bar.transform.localPosition = startPos;

        // Animer vers la position originale
        LeanTween.moveLocal(bar, originalPos, manager.SlideAnimationDuration)
            .setEase(manager.SlideAnimationEase);
    }

    public void HideBar(GameObject bar)
    {
        if (bar == null) return;

        bar.SetActive(false);

        // Remettre en position cachee pour la prochaine animation
        Vector3 originalPos;
        if (bar == manager.ActivityBar)
        {
            originalPos = activityBarOriginalPosition;
        }
        else if (bar == manager.IdleBar)
        {
            originalPos = idleBarOriginalPosition;
        }
        else
        {
            return; // Barre inconnue
        }

        Vector3 hiddenPos = originalPos;
        hiddenPos.y += 100f; // Position cachee au-dessus
        bar.transform.localPosition = hiddenPos;
    }

    private void PopRightIcon()
    {
        if (manager.RightIcon == null) return;

        // Arrêter le pop precedent
        if (currentPopId != -1)
        {
            LeanTween.cancel(currentPopId);
        }

        // Animation de POP satisfaisante :
        // 1. Grossit rapidement avec couleur plus lumineuse
        // 2. Retourne a la normale avec bounce

        // Scale + couleur en parallèle
        currentPopId = LeanTween.scale(manager.RightIcon.gameObject, originalRightIconScale * manager.PopScaleAmount, manager.PopDuration * 0.4f)
            .setEase(LeanTweenType.easeOutQuart)
            .setOnComplete(() =>
            {
                // Phase 2: Retour a la normale avec bounce satisfaisant
                LeanTween.scale(manager.RightIcon.gameObject, originalRightIconScale, manager.PopDuration * 0.6f)
                    .setEase(manager.PopEaseType) // easeOutBack pour l'effet bounce
                    .setOnComplete(() =>
                    {
                        currentPopId = -1;
                    });
            }).id;

        // Animation de couleur en parallèle (illumination)
        LeanTween.color(manager.RightIcon.rectTransform, manager.PopBrightColor, manager.PopDuration * 0.4f)
            .setEase(LeanTweenType.easeOutQuart)
            .setOnComplete(() =>
            {
                // Retour couleur normale
                LeanTween.color(manager.RightIcon.rectTransform, originalRightIconColor, manager.PopDuration * 0.6f)
                    .setEase(LeanTweenType.easeOutQuart);
            });
    }

    // ===============================================
    // NOUVEAU : GESTION DE L'ANIMATION IDLE BAR
    // ===============================================

    /// <summary>
    /// Demarre l'animation repetitive de ronflement de l'IdleBar
    /// </summary>
    public void StartIdleBarAnimation()
    {
        if (manager.IdleBarImage == null || isIdleAnimationActive) return;

        isIdleAnimationActive = true;
        Logger.LogInfo("AboveCanvasManager: Starting idle bar snore animation", Logger.LogCategory.General);

        // Demarrer immediatement le premier ronflement
        PlayIdleSnoreAnimation();

        // Puis programmer les repetitions
        ScheduleNextIdleAnimation();
    }

    /// <summary>
    /// Arrête l'animation repetitive de ronflement de l'IdleBar
    /// </summary>
    public void StopIdleBarAnimation()
    {
        if (!isIdleAnimationActive) return;

        isIdleAnimationActive = false;
        Logger.LogInfo("AboveCanvasManager: Stopping idle bar snore animation", Logger.LogCategory.General);

        // Annuler le timer de repetition
        if (idleAnimationTimerId != -1)
        {
            LeanTween.cancel(idleAnimationTimerId);
            idleAnimationTimerId = -1;
        }

        // Annuler les animations en cours
        if (idleSnoreAnimationId != -1)
        {
            LeanTween.cancel(idleSnoreAnimationId);
            idleSnoreAnimationId = -1;
        }

        if (idleShakeAnimationId != -1)
        {
            LeanTween.cancel(idleShakeAnimationId);
            idleShakeAnimationId = -1;
        }

        // Remettre l'image a ses valeurs originales
        if (manager.IdleBarImage != null)
        {
            manager.IdleBarImage.transform.localScale = idleBarImageOriginalScale;
            manager.IdleBarImage.transform.localPosition = idleBarImageOriginalPosition;
        }
    }

    /// <summary>
    /// Programme la prochaine animation de ronflement
    /// </summary>
    private void ScheduleNextIdleAnimation()
    {
        if (!isIdleAnimationActive) return;

        // Programmer le prochain ronflement après l'intervalle defini
        idleAnimationTimerId = LeanTween.delayedCall(manager.IdleAnimationInterval, () =>
        {
            if (isIdleAnimationActive) // Verifier qu'on n'a pas arrête entre temps
            {
                PlayIdleSnoreAnimation();
                ScheduleNextIdleAnimation(); // Programmer la suivante (recursion)
            }
        }).id;
    }

    /// <summary>
    /// Joue une animation de "ronflement" sur l'image de l'IdleBar
    /// Phase 1: Inspiration (grossissement lent)
    /// Phase 2: Expiration avec vibration (retrecissement + shake)
    /// </summary>
    private void PlayIdleSnoreAnimation()
    {
        if (manager.IdleBarImage == null || !isIdleAnimationActive) return;

        float inflateDuration = manager.IdleSnoreDuration * 0.7f;  // 70% du temps pour l'inspiration
        float deflateDuration = manager.IdleSnoreDuration * 0.3f;  // 30% du temps pour l'expiration + shake

        // Phase 1 : Inspiration (grossissement lent et profond)
        idleSnoreAnimationId = LeanTween.scale(manager.IdleBarImage.gameObject, idleBarImageOriginalScale * manager.IdleSnoreScale, inflateDuration)
            .setEase(manager.IdleInflateEase)  // easeInSine pour une inspiration progressive
            .setOnComplete(() =>
            {
                if (manager.IdleBarImage != null && isIdleAnimationActive)
                {
                    // Phase 2 : Expiration avec shake (ronflement!)
                    PlaySnoreDeflateWithShake(deflateDuration);
                }
            }).id;
    }

    /// <summary>
    /// Joue l'animation d'expiration avec vibration (la partie "ronflement")
    /// </summary>
    private void PlaySnoreDeflateWithShake(float duration)
    {
        if (manager.IdleBarImage == null || !isIdleAnimationActive) return;

        // Animation de retrecissement avec bounce (comme un ronflement qui "expire")
        idleSnoreAnimationId = LeanTween.scale(manager.IdleBarImage.gameObject, idleBarImageOriginalScale, duration)
            .setEase(manager.IdleDeflateEase)  // easeOutBounce pour l'effet ronflement
            .setOnComplete(() =>
            {
                idleSnoreAnimationId = -1;
            }).id;

        // EN PARALLÈLE : Animation de shake/vibration pour simuler le ronflement
        PlaySnoreShakeEffect(duration);
    }

    /// <summary>
    /// Cree l'effet de vibration pendant l'expiration (simule le bruit du ronflement)
    /// VRAIES vibrations continues et rapides !
    /// </summary>
    private void PlaySnoreShakeEffect(float duration)
    {
        if (manager.IdleBarImage == null || !isIdleAnimationActive) return;

        // VRAIE vibration : mouvement rapide et continu en X (horizontal)
        // Vibration rapide de gauche a droite pendant toute la duree
        float vibrateFrequency = 15f; // Hz - très rapide pour effet vibration
        float totalCycles = duration * vibrateFrequency;

        idleShakeAnimationId = LeanTween.value(manager.IdleBarImage.gameObject, 0f, totalCycles * 2f * Mathf.PI, duration)
            .setOnUpdate((float value) =>
            {
                if (manager.IdleBarImage != null && isIdleAnimationActive)
                {
                    // Calculer l'intensite qui diminue progressivement
                    float progress = value / (totalCycles * 2f * Mathf.PI);
                    float currentIntensity = manager.IdleShakeIntensity * (1f - progress * 0.7f); // Diminue de 70%

                    // Position oscillante rapide (vibration)
                    float xOffset = Mathf.Sin(value) * currentIntensity;
                    Vector3 vibratePosition = idleBarImageOriginalPosition + new Vector3(xOffset, 0f, 0f);

                    manager.IdleBarImage.transform.localPosition = vibratePosition;
                }
            })
            .setOnComplete(() =>
            {
                idleShakeAnimationId = -1;
                // Remettre en position originale
                if (manager.IdleBarImage != null)
                {
                    manager.IdleBarImage.transform.localPosition = idleBarImageOriginalPosition;
                }
            }).id;
    }

    public void Cleanup()
    {
        // Arrêter l'animation de l'IdleBar
        StopIdleBarAnimation();

        // Version securisee : annuler par GameObject plutôt que par ID
        // evite les "orphan tweens" quand l'objet est detruit
        if (manager.FillBar != null)
        {
            LeanTween.cancel(manager.FillBar.gameObject);
        }

        if (manager.RightIcon != null)
        {
            LeanTween.cancel(manager.RightIcon.gameObject);
        }

        if (manager.IdleBarImage != null)
        {
            LeanTween.cancel(manager.IdleBarImage.gameObject);
        }

        // Reset des IDs pour securite
        currentAnimationId = -1;
        currentPulseId = -1;
        currentPopId = -1;
        idleAnimationTimerId = -1;
        idleSnoreAnimationId = -1;
        idleShakeAnimationId = -1;
    }
}