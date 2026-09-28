using System.Collections.Generic;
using UnityEngine;

public enum HitboxLimb
{
    RightHand,
    LeftHand,
    RightFoot,
    LeftFoot,
    Head
}

/// <summary>
/// Emissor de dano (Hitbox). Anexado aos membros de ataque (mãos e pés).
/// Controla frames ativos, previne múltiplos hits no mesmo oponente por golpe
/// e utiliza triggers com amostragem contínua para zero tunnel-clipping.
/// Acompanha os ossos animados do Animator em tempo real.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class Hitbox : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Membro de ataque correspondente a esta hitbox.")]
    [SerializeField] private HitboxLimb limbType = HitboxLimb.RightHand;

    [Tooltip("Lutador dono desta hitbox.")]
    [SerializeField] private FighterController owner;

    [Tooltip("Quando ativado, vincula a hitbox ao osso do esqueleto do Animator correspondente ao membro.")]
    [SerializeField] private bool followAnimatedBone = true;

    [Header("Debug")]
    [Tooltip("Ativa a visualização da Hitbox na Scene View via Gizmos.")]
    [SerializeField] private bool showDebugGizmos = true;

    private Collider col;
    private SphereCollider sphereCol;
    private BoxCollider boxCol;
    private DamageData currentDamageData;
    private bool isActive;
    private Transform followBone;
    private Vector3 previousOverlapCenter;
    private bool hasPreviousOverlapCenter;

    // Buffer pré-alocado por instância — NÃO pode ser estático: múltiplos Hitbox no mesmo
    // frame sobrescreveriam o buffer um do outro, causando dano perdido silenciosamente
    private readonly Collider[] overlapResults = new Collider[24];

    // Conjunto de lutadores já atingidos nesta janela ativa (Zero GC por frame)
    private readonly HashSet<FighterController> hitFighters = new HashSet<FighterController>();

    public HitboxLimb LimbType => limbType;
    public FighterController Owner { get => owner; set => owner = value; }
    public bool IsActive => isActive;
    public bool FollowAnimatedBone { get => followAnimatedBone; set => followAnimatedBone = value; }
    public bool ShowDebugGizmos { get => showDebugGizmos; set => showDebugGizmos = value; }

    private void Awake()
    {
        EnsureComponents();
    }

    private void Start()
    {
        EnsureComponents();
        if (followAnimatedBone) BindBone();
    }

    private void EnsureComponents()
    {
        if (col == null)
        {
            col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.enabled = false;
            }
            sphereCol = col as SphereCollider;
            boxCol = col as BoxCollider;
        }

        if (owner == null)
        {
            owner = GetComponentInParent<FighterController>();
        }
    }

    /// <summary>
    /// Vincula a hitbox ao Transform do osso correspondente no Animator do lutador.
    /// Se o osso não for encontrado, emite um aviso claro via LogWarning em vez de falhar em silêncio.
    /// </summary>
    public void BindBone()
    {
        if (!followAnimatedBone) return;
        if (followBone != null) return;
        EnsureComponents();
        if (owner == null) return;

        Animator anim = owner.Animator != null ? owner.Animator : owner.GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            HumanBodyBones targetBone = limbType switch
            {
                HitboxLimb.RightHand => HumanBodyBones.RightHand,
                HitboxLimb.LeftHand => HumanBodyBones.LeftHand,
                HitboxLimb.RightFoot => HumanBodyBones.RightFoot,
                HitboxLimb.LeftFoot => HumanBodyBones.LeftFoot,
                HitboxLimb.Head => HumanBodyBones.Head,
                _ => HumanBodyBones.LastBone
            };

            if (targetBone != HumanBodyBones.LastBone)
            {
                followBone = anim.GetBoneTransform(targetBone);
            }
        }

        // Fallback: se não for Humanoid ou GetBoneTransform retornar null, busca na hierarquia
        if (followBone == null)
        {
            string expectedBoneName = limbType switch
            {
                HitboxLimb.RightHand => "RightHand",
                HitboxLimb.LeftHand => "LeftHand",
                HitboxLimb.RightFoot => "RightFoot",
                HitboxLimb.LeftFoot => "LeftFoot",
                HitboxLimb.Head => "Head",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(expectedBoneName))
            {
                followBone = FindBoneInHierarchy(owner.transform, expectedBoneName);
            }
        }

        // Se mesmo assim o osso não for encontrado, emite aviso explícito no console
        if (followBone == null)
        {
            Debug.LogWarning($"[Hitbox] O osso correspondente ao membro '{limbType}' não foi encontrado no modelo do lutador '{owner.gameObject.name}'. A hitbox permanecerá na posição relativa padrão.");
        }
    }

    private Transform FindBoneInHierarchy(Transform root, string boneName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string cleanName = child.name.Replace("mixamorig:", string.Empty).Replace("_", string.Empty);
            if (cleanName.IndexOf(boneName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return child;
            }
        }
        return null;
    }

    /// <summary>
    /// Sincroniza a posição e rotação da hitbox com a pose atual do osso animado.
    /// </summary>
    private void SyncBonePosition()
    {
        if (!followAnimatedBone) return;
        if (followBone == null)
        {
            BindBone();
        }

        if (followBone != null)
        {
            transform.position = followBone.position;
            transform.rotation = followBone.rotation;
        }
    }

    /// <summary>
    /// Ativa a hitbox com os parâmetros de impacto específicos do golpe.
    /// </summary>
    public void Activate(DamageData data)
    {
        EnsureComponents();
        currentDamageData = data;
        if (currentDamageData.attacker == null)
        {
            currentDamageData.attacker = owner;
        }

        hitFighters.Clear();
        isActive = true;
        if (col != null) col.enabled = true;

        // Atualiza a posição com o osso atual antes de checar sobreposição
        SyncBonePosition();

        // Guarda a primeira posição para que os frames seguintes possam testar
        // todo o trajeto do membro, e não apenas sua posição no fim do frame.
        if (TryGetSphereWorld(out Vector3 center, out _))
        {
            previousOverlapCenter = center;
            hasPreviousOverlapCenter = true;
        }
        else
        {
            hasPreviousOverlapCenter = false;
        }

        // Amostragem imediata no primeiro frame ativo
        CheckOverlaps();
    }

    /// <summary>
    /// Desativa a hitbox ao encerrar a janela ativa do golpe.
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        if (col != null) col.enabled = false;
        hitFighters.Clear();
        hasPreviousOverlapCenter = false;
    }

    private void Update()
    {
        if (isActive)
        {
            CheckOverlaps();
        }
    }

    private void LateUpdate()
    {
        // Garante que a hitbox acompanhe a pose final calculada pelo Animator
        SyncBonePosition();
    }

    /// <summary>
    /// Detecta sobreposição com Hurtboxes (suporta tanto triggers quanto non-triggers com QueryTriggerInteraction).
    /// </summary>
    private void CheckOverlaps()
    {
        if (!isActive) return;

        SyncBonePosition();

        int count = 0;

        if (TryGetSphereWorld(out Vector3 worldCenter, out float radius))
        {
            count = Physics.OverlapSphereNonAlloc(worldCenter, radius, overlapResults, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                ProcessHit(overlapResults[i]);
            }

            // Golpes rápidos (ex: chutes) podem deslocar o pé muito entre dois frames.
            // Varremos o segmento entre as duas poses do osso para manter a hitbox contínua.
            if (hasPreviousOverlapCenter && (worldCenter - previousOverlapCenter).sqrMagnitude > 0.0001f)
            {
                count = Physics.OverlapCapsuleNonAlloc(
                    previousOverlapCenter,
                    worldCenter,
                    radius,
                    overlapResults,
                    ~0,
                    QueryTriggerInteraction.Collide);
                for (int i = 0; i < count; i++)
                {
                    ProcessHit(overlapResults[i]);
                }
            }

            previousOverlapCenter = worldCenter;
            hasPreviousOverlapCenter = true;
            return;
        }

        if (boxCol != null)
        {
            Vector3 boxCenter = transform.TransformPoint(boxCol.center);
            Vector3 halfExtents = Vector3.Scale(boxCol.size * 0.5f, transform.lossyScale);
            count = Physics.OverlapBoxNonAlloc(boxCenter, halfExtents, overlapResults, transform.rotation, ~0, QueryTriggerInteraction.Collide);
        }

        for (int i = 0; i < count; i++)
        {
            ProcessHit(overlapResults[i]);
        }
    }

    private bool TryGetSphereWorld(out Vector3 worldCenter, out float radius)
    {
        worldCenter = default;
        radius = 0f;
        if (sphereCol == null) return false;

        worldCenter = transform.TransformPoint(sphereCol.center);
        float baseRadius = sphereCol.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        radius = Mathf.Max(0.40f, baseRadius);
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            ProcessHit(other);
        }
    }

    private void ProcessHit(Collider other)
    {
        if (!isActive || other == null) return;

        // Procura Hurtbox no objeto atingido
        var hurtbox = other.GetComponent<Hurtbox>();
        if (hurtbox == null || hurtbox.Owner == null) return;

        // Ignora colisão com o próprio lutador
        if (hurtbox.Owner == owner) return;

        // Previne múltiplos hits no mesmo oponente na mesma janela ativa
        if (hitFighters.Contains(hurtbox.Owner)) return;

        hitFighters.Add(hurtbox.Owner);

        // Repassa os dados de dano à Hurtbox
        hurtbox.ReceiveHit(currentDamageData, this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        if (col == null) col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        // Hitbox ativa: Vermelho vivo translúcido com contorno sólido
        if (isActive)
        {
            Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.65f);
            if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
        // Hitbox inativa: Laranja aramado na Scene View para validar o vínculo com o osso
        else
        {
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.45f);
            if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
#endif
}
