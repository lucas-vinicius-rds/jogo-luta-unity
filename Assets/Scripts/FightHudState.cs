using System;
/// <summary>Health subscriptions independent of the rendering technology.</summary>
public sealed class FightHudState : IDisposable
{
    public float PlayerRatio { get; private set; }
    public float OpponentRatio { get; private set; }
    public string PlayerName { get; private set; }
    public string OpponentName { get; private set; }
    public event Action<bool> Damaged;
    HealthSystem player, opponent;
    public void Bind(FighterController p1,FighterController p2)
    {
        var a = p1 != null ? p1.HealthSystem : null; var b = p2 != null ? p2.HealthSystem : null;
        if(player == a && opponent == b) return;
        Dispose(); player = a; opponent = b;
        PlayerName=p1!=null?p1.name.Replace("Player1_","P1  "):"";
        OpponentName=p2!=null?p2.name.Replace("Player2_","P2  ").Replace("CPU_","CPU  "):"";
        if(player != null) { player.OnHealthChanged += PlayerChanged; player.OnDamageTaken += PlayerDamaged; PlayerChanged(player.CurrentHealth,player.MaxHealth); }
        if(opponent != null) { opponent.OnHealthChanged += OpponentChanged; opponent.OnDamageTaken += OpponentDamaged; OpponentChanged(opponent.CurrentHealth,opponent.MaxHealth); }
    }
    void PlayerChanged(float value,float maximum) => PlayerRatio = maximum > 0 ? value/maximum : 0;
    void OpponentChanged(float value,float maximum) => OpponentRatio = maximum > 0 ? value/maximum : 0;
    void PlayerDamaged(DamageData _) => Damaged?.Invoke(player.CurrentHealth <= 0);
    void OpponentDamaged(DamageData _) => Damaged?.Invoke(opponent.CurrentHealth <= 0);
    public void Dispose()
    {
        if(player != null) { player.OnHealthChanged -= PlayerChanged; player.OnDamageTaken -= PlayerDamaged; }
        if(opponent != null) { opponent.OnHealthChanged -= OpponentChanged; opponent.OnDamageTaken -= OpponentDamaged; }
        player = opponent = null;
    }
}
