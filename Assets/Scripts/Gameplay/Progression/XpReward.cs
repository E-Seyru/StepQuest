// Purpose: Enhanced data structure for XP rewards from activities
// Filepath: Assets/Scripts/Gameplay/Progression/XpReward.cs
using System;
using UnityEngine;

/// <summary>
/// Structure de donnees pour les recompenses d'XP provenant des activites
/// Version amelioree avec validation et metadonnees
/// </summary>
[Serializable]
public class XPReward
{
    [Header("Main Skill")]
    [SerializeField] private string mainSkillId = "";
    [SerializeField] private int mainSkillXP = 0;

    [Header("Sub Skill")]
    [SerializeField] private string subSkillId = "";
    [SerializeField] private int subSkillXP = 0;

    [Header("Metadata")]
    [SerializeField] private string sourceActivity = "";
    [SerializeField] private float bonusMultiplier = 1.0f;

    #region Properties

    public string MainSkillId
    {
        get => mainSkillId;
        set => mainSkillId = ValidateSkillId(value);
    }

    public string SubSkillId
    {
        get => subSkillId;
        set => subSkillId = ValidateSkillId(value);
    }

    public int MainSkillXP
    {
        get => mainSkillXP;
        set => mainSkillXP = Mathf.Max(0, value);
    }

    public int SubSkillXP
    {
        get => subSkillXP;
        set => subSkillXP = Mathf.Max(0, value);
    }

    public string SourceActivity
    {
        get => sourceActivity;
        set => sourceActivity = value ?? "";
    }

    public float BonusMultiplier
    {
        get => bonusMultiplier;
        set => bonusMultiplier = Mathf.Max(0.1f, value);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructeur par defaut
    /// </summary>
    public XPReward()
    {
        mainSkillId = "";
        subSkillId = "";
        mainSkillXP = 0;
        subSkillXP = 0;
        sourceActivity = "";
        bonusMultiplier = 1.0f;
    }

    /// <summary>
    /// Constructeur de base avec validation
    /// </summary>
    public XPReward(string mainSkillId, string subSkillId, int mainSkillXP, int subSkillXP)
    {
        this.mainSkillId = ValidateSkillId(mainSkillId);
        this.subSkillId = ValidateSkillId(subSkillId);
        this.mainSkillXP = Mathf.Max(0, mainSkillXP);
        this.subSkillXP = Mathf.Max(0, subSkillXP);
        this.sourceActivity = "";
        this.bonusMultiplier = 1.0f;
    }

    /// <summary>
    /// Constructeur complet avec metadonnees
    /// </summary>
    public XPReward(string mainSkillId, string subSkillId, int mainSkillXP, int subSkillXP,
                   string sourceActivity, float bonusMultiplier = 1.0f)
    {
        this.mainSkillId = ValidateSkillId(mainSkillId);
        this.subSkillId = ValidateSkillId(subSkillId);
        this.mainSkillXP = Mathf.Max(0, mainSkillXP);
        this.subSkillXP = Mathf.Max(0, subSkillXP);
        this.sourceActivity = sourceActivity ?? "";
        this.bonusMultiplier = Mathf.Max(0.1f, bonusMultiplier);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Verifier si cette recompense contient de l'XP
    /// </summary>
    public bool HasAnyXP()
    {
        return mainSkillXP > 0 || subSkillXP > 0;
    }

    /// <summary>
    /// Obtenir le total d'XP (main + sub)
    /// </summary>
    public int GetTotalXP()
    {
        return mainSkillXP + subSkillXP;
    }

    /// <summary>
    /// Appliquer un multiplicateur de bonus a cette recompense
    /// </summary>
    public XPReward ApplyBonus(float multiplier)
    {
        if (multiplier <= 0) return this;

        return new XPReward(
            mainSkillId,
            subSkillId,
            Mathf.RoundToInt(mainSkillXP * multiplier),
            Mathf.RoundToInt(subSkillXP * multiplier),
            sourceActivity,
            bonusMultiplier * multiplier
        );
    }

    /// <summary>
    /// Combiner cette recompense avec une autre
    /// </summary>
    public XPReward CombineWith(XPReward other)
    {
        if (other == null) return this;

        // Si les skills correspondent, on additionne
        if (mainSkillId == other.mainSkillId && subSkillId == other.subSkillId)
        {
            return new XPReward(
                mainSkillId,
                subSkillId,
                mainSkillXP + other.mainSkillXP,
                subSkillXP + other.subSkillXP,
                sourceActivity + " + " + other.sourceActivity,
                (bonusMultiplier + other.bonusMultiplier) / 2f
            );
        }

        // Sinon, on retourne une copie de cette recompense
        return new XPReward(mainSkillId, subSkillId, mainSkillXP, subSkillXP, sourceActivity, bonusMultiplier);
    }

    /// <summary>
    /// Creer une copie de cette recompense
    /// </summary>
    public XPReward Clone()
    {
        return new XPReward(mainSkillId, subSkillId, mainSkillXP, subSkillXP, sourceActivity, bonusMultiplier);
    }

    /// <summary>
    /// Verifier si cette recompense est valide
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(mainSkillId) || !string.IsNullOrEmpty(subSkillId);
    }

    /// <summary>
    /// Obtenir un resume formate de cette recompense
    /// </summary>
    public string GetFormattedSummary()
    {
        if (!HasAnyXP()) return "No XP reward";

        string summary = "";

        if (mainSkillXP > 0)
        {
            summary += $"{mainSkillId}: +{mainSkillXP} XP";
        }

        if (subSkillXP > 0)
        {
            if (!string.IsNullOrEmpty(summary)) summary += ", ";
            summary += $"{subSkillId}: +{subSkillXP} XP";
        }

        if (bonusMultiplier != 1.0f)
        {
            summary += $" (Bonus: x{bonusMultiplier:F1})";
        }

        return summary;
    }

    /// <summary>
    /// Obtenir l'ID normalise d'une variante d'activite
    /// Cette methode assure la coherence entre le systeme d'XP, l'UI et les sauvegardes
    /// </summary>
    public string GetVariantId(ActivityVariant variant)
    {
        if (variant == null) return "";
        return ValidateSkillId(variant.GetSubSkillId());
    }

    #endregion

    #region Static Methods

    /// <summary>
    /// Creer une recompense vide
    /// </summary>
    public static XPReward Empty => new XPReward();

    /// <summary>
    /// Creer une recompense uniquement pour la competence principale
    /// </summary>
    public static XPReward ForMainSkill(string skillId, int xp, string sourceActivity = "")
    {
        return new XPReward(skillId, "", xp, 0, sourceActivity);
    }

    /// <summary>
    /// Creer une recompense uniquement pour la sous-competence
    /// </summary>
    public static XPReward ForSubSkill(string skillId, int xp, string sourceActivity = "")
    {
        return new XPReward("", skillId, 0, xp, sourceActivity);
    }

    /// <summary>
    /// Creer une recompense pour une variante d'activite specifique
    /// Utilise l'ID normalise de la variante pour assurer la coherence
    /// </summary>
    public static XPReward ForVariant(ActivityVariant variant, int xp, string sourceActivity = "")
    {
        if (variant == null) return Empty;
        return new XPReward("", ValidateSkillId(variant.GetSubSkillId()), 0, xp, sourceActivity);
    }

    /// <summary>
    /// Combiner plusieurs recompenses en une seule
    /// </summary>
    public static XPReward Combine(params XPReward[] rewards)
    {
        if (rewards == null || rewards.Length == 0) return Empty;

        var result = rewards[0].Clone();

        for (int i = 1; i < rewards.Length; i++)
        {
            result = result.CombineWith(rewards[i]);
        }

        return result;
    }

    /// <summary>
    /// NOUVELLE MeTHODE PUBLIQUE : Valider et nettoyer un ID de competence
    /// Convertit les espaces en underscores et assure la coherence du format
    /// Cette methode peut etre utilisee par d'autres classes pour standardiser les IDs
    /// </summary>
    public static string ValidateSkillId(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId)) return "";

        // Nettoyer l'ID : supprimer les espaces de debut/fin, convertir espaces internes en underscores
        return skillId.Trim().Replace(" ", "_");
    }

    #endregion

    #region Validation (Methode privee conservee pour compatibilite)

    /// <summary>
    /// Methode privee qui utilise la methode statique publique
    /// </summary>
    private string ValidateSkillId_Private(string skillId)
    {
        return ValidateSkillId(skillId);  // Delegue a la methode statique publique
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Representation string amelioree
    /// </summary>
    public override string ToString()
    {
        if (!HasAnyXP()) return "XP Reward: Empty";

        string result = "XP Reward: ";

        if (mainSkillXP > 0)
        {
            result += $"{mainSkillId} +{mainSkillXP}";
        }

        if (subSkillXP > 0)
        {
            if (mainSkillXP > 0) result += ", ";
            result += $"{subSkillId} +{subSkillXP}";
        }

        if (!string.IsNullOrEmpty(sourceActivity))
        {
            result += $" (from {sourceActivity})";
        }

        return result;
    }

    /// <summary>
    /// Comparaison d'egalite
    /// </summary>
    public override bool Equals(object obj)
    {
        if (obj is XPReward other)
        {
            return mainSkillId == other.mainSkillId &&
                   subSkillId == other.subSkillId &&
                   mainSkillXP == other.mainSkillXP &&
                   subSkillXP == other.subSkillXP;
        }
        return false;
    }

    /// <summary>
    /// Hash code pour les collections
    /// </summary>
    public override int GetHashCode()
    {
        return (mainSkillId + subSkillId + mainSkillXP + subSkillXP).GetHashCode();
    }

    #endregion

    #region Operators

    /// <summary>
    /// Operateur d'addition pour combiner des recompenses
    /// </summary>
    public static XPReward operator +(XPReward a, XPReward b)
    {
        if (a == null) return b?.Clone() ?? Empty;
        if (b == null) return a.Clone();

        return a.CombineWith(b);
    }

    /// <summary>
    /// Operateur de multiplication pour appliquer des bonus
    /// </summary>
    public static XPReward operator *(XPReward reward, float multiplier)
    {
        return reward?.ApplyBonus(multiplier) ?? Empty;
    }

    /// <summary>
    /// Operateur de multiplication (ordre inverse)
    /// </summary>
    public static XPReward operator *(float multiplier, XPReward reward)
    {
        return reward * multiplier;
    }

    #endregion
}