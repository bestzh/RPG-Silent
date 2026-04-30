// 玩家姿态枚举：表示一整套动画的"风格"。
// 每个姿态对应一份 AnimatorOverrideController，用来替换基础 AnimatorController 中的 AnimationClip。
// 想新增姿态时，先在这里加一项，再在 PlayerStanceController 里指派对应的 OverrideController 即可。
//
// 注意：枚举值显式指定数字，方便存档/网络同步时保持稳定。新增时追加在末尾，不要在中间插入。
public enum PlayerStance
{
    Relax = 0,
    Unarmed = 1,

    Armed = 10,
    ArmedShield = 11,

    OneHandDagger = 20,
    OneHandItem = 21,
    OneHandMace = 22,
    OneHandPistol = 23,
    OneHandSpear = 24,
    OneHandSword = 25,

    TwoHandAxe = 40,
    TwoHandBow = 41,
    TwoHandCrossbow = 42,
    TwoHandShooting = 43,
    TwoHandSpear = 44,
    TwoHandStaff = 45,
    TwoHandSword = 46,
}
