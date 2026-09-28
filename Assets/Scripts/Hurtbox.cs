using UnityEngine;

/// <summary>
/// Receptor de dano (Hurtbox). Anexado ao corpo/esqueleto do lutador.
/// Possui um Collider Trigger e encaminha os impactos recebidos ao FighterController dono
/// acionando o método TakeDamage. Acompanha a altura e o centro do collider ao agachar.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class Hurtbox : MonoBehaviour
{
    [Header("Owner")]
    [Tooltip("Referência ao FighterController dono deste osso/hurtbox.")]
    [SerializeField] private FighterController owner;

    [Header("Debug")]
    [Tooltip("Ativa a visualização da Hurtbox na Scene View via Gizmos.")]
    [SerializeField] private bool showDebugGizmos = true;

    private Collider col;
    private CapsuleCollider capsuleCol;
    private Vector3 initialLocalPosition;
    private Vector3 initialCapsuleCenter;

    public FighterController Owner
    {
        get => owner;
        set => owner = value;
    }

    public bool ShowDebugGizmos
    {
        get => showDebugGizmos;
        set => showDebugGizmos = value;
    }

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
        capsuleCol = col as CapsuleCollider;

        initialLocalPosition = transform.localPosition;
        if (capsuleCol != null)
        {
            initialCapsuleCenter = capsuleCol.center;
        }

        if (owner == null)
        {
            owner = GetComponentInParent<FighterController>();
        }
    }

    /// <summary>
    /// Sincroniza a altura e o centro da Hurtbox com os valores do CharacterController (em pé ou agachado).
    /// </summary>
    public void SetBounds(float targetHeight, Vector3 targetCenter)
    {
        if (capsuleCol != null)
        {
            capsuleCol.height = targetHeight;

            // Se o GameObject da Hurtbox já possui um offset local (ex: Y=1.0f), movemos o transform local
            if (initialLocalPosition.sqrMagnitude > 0.001f)
            {
                transform.localPosition = new Vector3(initialLocalPosition.x, targetCenter.y, initialLocalPosition.z);
                capsuleCol.center = initialCapsuleCenter;
            }
            else
            {
                capsuleCol.center = targetCenter;
            }
        }
        else if (col is BoxCollider box)
        {
            box.size = new Vector3(box.size.x, targetHeight, box.size.z);
            box.center = targetCenter;
        }
    }

    /// <summary>
    /// Chamado pela Hitbox atacante ao colidir com esta Hurtbox.
    /// Aciona o TakeDamage no FighterController dono com os dados do impacto.
    /// </summary>
    public void ReceiveHit(DamageData data, Hitbox sourceHitbox)
    {
        if (owner == null) return;

        // Previne dano acidental contra si mesmo
        if (data.attacker == owner) return;

        owner.TakeDamage(data, sourceHitbox);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        if (col == null) col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0.35f, 0.45f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is CapsuleCollider capsule)
        {
            DrawCapsuleGizmo(capsule);
        }
        else if (col is BoxCollider box)
        {
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
    }

    private void DrawCapsuleGizmo(CapsuleCollider capsule)
    {
        Vector3 center = capsule.center;
        float radius = capsule.radius;
        float halfHeight = Mathf.Max(0f, (capsule.height * 0.5f) - radius);

        Vector3 top = center + Vector3.up * halfHeight;
        Vector3 bottom = center - Vector3.up * halfHeight;

        Gizmos.DrawWireSphere(top, radius);
        Gizmos.DrawWireSphere(bottom, radius);

        Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
        Gizmos.DrawLine(top - Vector3.forward * radius, bottom - Vector3.forward * radius);
        Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
        Gizmos.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);
    }
#endif
}
