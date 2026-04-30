using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ComboAttackProfile
{
    [Min(1)] public int ComboIndex = 1;
    public AttackProfile Profile;
}

[Serializable]
public class StanceAttackProfileSet
{
    public PlayerStance Stance;
    public List<ComboAttackProfile> ComboProfiles = new List<ComboAttackProfile>();
}

public class PlayerAttackLoadout : MonoBehaviour
{
    [SerializeField] private PlayerStanceController stanceController;
    [SerializeField] private AttackProfile fallbackProfile;
    [SerializeField] private List<StanceAttackProfileSet> stanceProfiles = new List<StanceAttackProfileSet>();

    private void Awake()
    {
        if (stanceController == null)
        {
            stanceController = GetComponent<PlayerStanceController>();
        }
    }

    public AttackProfile GetProfile(int comboIndex)
    {
        PlayerStance stance = stanceController != null ? stanceController.CurrentStance : PlayerStance.Relax;
        return GetProfile(stance, comboIndex);
    }

    public AttackProfile GetProfile(PlayerStance stance, int comboIndex)
    {
        foreach (StanceAttackProfileSet profileSet in stanceProfiles)
        {
            if (profileSet == null || profileSet.Stance != stance)
            {
                continue;
            }

            AttackProfile profile = GetComboProfile(profileSet, comboIndex);
            if (profile != null)
            {
                return profile;
            }
        }

        return fallbackProfile;
    }

    private static AttackProfile GetComboProfile(StanceAttackProfileSet profileSet, int comboIndex)
    {
        foreach (ComboAttackProfile comboProfile in profileSet.ComboProfiles)
        {
            if (comboProfile != null && comboProfile.ComboIndex == comboIndex)
            {
                return comboProfile.Profile;
            }
        }

        return null;
    }
}
