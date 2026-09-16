using System;
using UnityEngine;

/// <summary>
/// Sistema de gestão de saúde e integridade do lutador.
/// Controla pontos de vida, expõe eventos C# desacoplados para UI e áudio,
/// e sinaliza o FighterController ao sofrer nocaute (KO).
/// </summary>
[DisallowMultipleComponent]
public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Pontos de vida máximos do lutador.")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    [Header("Debug (Read Only)")]
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private bool isDead;

    private FighterController controller;

    // Eventos C# desacoplados para UI, SFX e câmeras
    public event Action<float, float> OnHealthChanged;      // (currentHealth, maxHealth)
    public event Action<DamageData> OnDamageTaken;          // dados detalhados do dano
    public event Action OnKnockout;                         // disparado quando HP atinge zero

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    public bool IsDead => isDead;

    public void ConfigureMaxHealth(float value, bool refill = true)
    {
        maxHealth = Mathf.Max(1f, value);
        if (refill)
        {
            isDead = false;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    private void Awake()
    {
        controller = GetComponent<FighterController>();
        currentHealth = maxHealth;
        isDead = false;
    }

    private void Start()
    {
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
            isDead = false;
        }

        // Notifica listeners no início com o estado inicial
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Aplica dano ao lutador reduzindo sua saúde e notificando os ouvintes.
    /// </summary>
    public void TakeDamage(DamageData data)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - data.damage);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke(data);

        // Checagem de Nocaute (KO)
        if (currentHealth <= 0f && !isDead)
        {
            isDead = true;
            OnKnockout?.Invoke();

            if (controller != null)
            {
                controller.TriggerKnockout();
            }
        }
    }

    /// <summary>
    /// Restaura vida do personagem sem ultrapassar o valor máximo.
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Reinicia a vida para o valor máximo e reseta a flag de morte.
    /// </summary>
    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (currentHealth <= 0f && maxHealth > 0f)
        {
            currentHealth = maxHealth;
        }
    }
#endif
}
