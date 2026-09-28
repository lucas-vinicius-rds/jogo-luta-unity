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

    [Tooltip("Use apenas em rigs com ossos consistentes. Por padrão a hitbox usa o volume local configurado à frente do lutador.")]
    [SerializeField] private bool followAnimatedBone = false;

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
    }

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
        // Garante que a hitbox siga a posição exata do osso após o Animator avaliar as poses
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

            // Golpes de K, principalmente chutes voadores, deslocam o pé muito
            // entre dois Updates. A esfera isolada podia passar pelo corpo do
            // alvo sem nunca registrar uma sobreposição. Varremos o segmento
            // entre as duas poses do osso para manter a hitbox contínua.
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
        if (!isActive) return;
        if (col == null) col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.55f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
        else if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
        }
    }
#endif
}
