namespace RPGSilent.Domain
{
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(int damage);
    }
}
