using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Skill/Create New Skill Data")]
public class SkillData : ScriptableObject
{
    public string SkillName;           // 技能名称（比如 "Fireball"）
    public AnimationData Animation; // 播放的动作
    public float CastTime = 0f;         // 施法时间
    [Header("冷却相关")] 
    public float CooldownTime = 5f;     // 冷却时间
    public bool CanBeInterrupted = true;// 是否可以被打断

     [Header("技能特效")]
    public GameObject EffectPrefab;    // 技能特效预制体
    public Vector3 EffectOffset;       // 特效位置偏移

    [Header("命中特效")]
    public GameObject HitEffectPrefab;
    public Vector3 HitEffectOffset;

    [Header("连锁特效")]
    public bool HasChainEffect = false;     // 是否有连锁
    public int ChainCount = 3;              // 连锁次数
    public float ChainRadius = 5f;           // 连锁搜索半径
    public GameObject ChainEffectPrefab;    // 连锁特效Prefab

    [Header("Combo配置")]
    public bool CanCombo = false;         // 这个技能能不能连Combo
    public SkillData NextComboSkill;   // 连招的下一个技能
    public float ComboInputWindow = 0.5f; // 连招判定时间（单位秒）

    [Header("能量槽系统")]
    public float maxEnergy = 100f; // 最大能量
    public float currentEnergy = 0f; // 当前能量
    public float energyRecoveryRate = 10f; // 每秒回复能量速度

    [Header("能量消耗")]
    public float EnergyCost = 20f; // 释放这个技能需要消耗的能量
    
    
}
