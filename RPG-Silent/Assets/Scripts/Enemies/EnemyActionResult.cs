namespace RPGSilent.Enemies
{
    // 能力命令的结果，不承担行为树遍历；持续动作由 EnemyController 管理。
    public enum EnemyActionResult { Failure, Success, Running }
}
