using UnityEngine;

/// <summary>
/// Câmera dinâmica estilo jogos de luta 3D (Tekken / Soulcalibur).
/// Mantém os dois lutadores enquadrados, calculando o ponto médio e
/// orbitando perpendicularmente à linha de combate com zoom dinâmico.
/// </summary>
public class TekkenCamera : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Lutador 1 (Jogador)")]
    [SerializeField] private Transform fighter1;

    [Tooltip("Lutador 2 (Oponente)")]
    [SerializeField] private Transform fighter2;

    [Header("Framing Settings")]
    [Tooltip("Distância base da câmera em relação ao ponto médio.")]
    [SerializeField] private float baseDistance = 5.0f;

    [Tooltip("Altura da câmera em relação ao chão.")]
    [SerializeField] private float height = 1.6f;

    [Tooltip("Altura do ponto de foco (look-at) acima do chão.")]
    [SerializeField] private float lookAtHeight = 1.1f;

    [Header("Presentation Scale")]
    [Tooltip("Multiplicador da altura existente da câmera. 1.5 corresponde a +50%.")]
    [SerializeField, Min(0.1f)] private float presentationHeightMultiplier = 1.5f;

    [Tooltip("Multiplicador do tamanho ortográfico calculado. 0.6 corresponde a aproximar 40%.")]
    [SerializeField, Range(0.1f, 1f)] private float presentationZoomMultiplier = 0.6f;

    [Tooltip("Limite mínimo de tamanho ortográfico; o ajuste 0.6x continua ativo no enquadramento padrão.")]
    [SerializeField, Min(0.1f)] private float safeMinimumOrthographicSize = 1.92f;

    [Tooltip("Margem de segurança aplicada aos limites horizontal e vertical dos lutadores.")]
    [SerializeField, Range(1f, 1.5f)] private float framingSafeMargin = 1.12f;

    [Tooltip("Fator de recuo de zoom conforme os lutadores se distanciam.")]
    [SerializeField] private float zoomFactor = 0.6f;

    [Header("Damping")]
    [Tooltip("Suavização do movimento da câmera (valores menores = mais suave).")]
    [SerializeField] private float followDamping = 8.0f;

    [Tooltip("Suavização da rotação da câmera.")]
    [SerializeField] private float rotationDamping = 10.0f;

    public Transform Fighter1 { get => fighter1; set => fighter1 = value; }
    public Transform Fighter2 { get => fighter2; set => fighter2 = value; }

    private Camera targetCamera;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (targetCamera != null)
        {
            targetCamera.orthographic = true;
        }
    }

    private void LateUpdate()
    {
        if (fighter1 == null || fighter2 == null) return;

        // 1. Calcula o ponto médio entre os lutadores
        Vector3 p1 = fighter1.position;
        Vector3 p2 = fighter2.position;
        Vector3 midpoint = (p1 + p2) * 0.5f;

        // 2. Calcula a linha de combate no plano horizontal (XZ)
        // Câmera lateral fixa: a luta acontece apenas no eixo X.
        float fighterDistance = Mathf.Abs(p2.x - p1.x);
        float currentDistance = baseDistance + (fighterDistance * zoomFactor);
        float raisedHeight = height * presentationHeightMultiplier;
        Vector3 targetPosition = new Vector3(midpoint.x, midpoint.y + raisedHeight, -currentDistance);
        Vector3 lookTarget = midpoint + Vector3.up * lookAtHeight * presentationHeightMultiplier;
        if (targetCamera != null)
        {
            float baseSize = Mathf.Clamp(3.2f + fighterDistance * 0.22f, 3.2f, 6.5f);
            float requestedSize = baseSize * presentationZoomMultiplier;

            // OrthoSize mede metade da altura. Garanta ainda que a distância
            // entre os lutadores cabe na largura, inclusive em telas 16:9.
            float aspect = Mathf.Max(0.01f, targetCamera.aspect);
            float horizontalFitSize = fighterDistance * framingSafeMargin / (2f * aspect);
            Quaternion framingRotation = Quaternion.LookRotation(lookTarget - targetPosition);
            float verticalFitSize = Mathf.Max(
                RequiredVerticalSize(fighter1, lookTarget, framingRotation * Vector3.up),
                RequiredVerticalSize(fighter2, lookTarget, framingRotation * Vector3.up));
            float targetSize = Mathf.Max(
                Mathf.Max(requestedSize, safeMinimumOrthographicSize),
                Mathf.Max(horizontalFitSize, verticalFitSize));
            if (Mathf.Abs(targetCamera.orthographicSize - targetSize) > 0.001f)
            {
                targetCamera.orthographicSize = targetSize;
            }
        }

        // Interpolação suave de posição
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followDamping);

        // 4. Orientação da câmera olhando para o ponto médio dos lutadores
        Vector3 directionToTarget = lookTarget - transform.position;

        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationDamping);
        }
    }

    private float RequiredVerticalSize(Transform fighter, Vector3 lookTarget, Vector3 cameraUp)
    {
        CharacterController body = fighter.GetComponent<CharacterController>();
        if (body == null) return 0f;

        Bounds bounds = body.bounds;
        Vector3 offset = bounds.center - lookTarget;
        Vector3 extents = bounds.extents;
        float projectedCenter = Mathf.Abs(Vector3.Dot(offset, cameraUp));
        float projectedExtent = Mathf.Abs(cameraUp.x) * extents.x +
                                Mathf.Abs(cameraUp.y) * extents.y +
                                Mathf.Abs(cameraUp.z) * extents.z;
        return (projectedCenter + projectedExtent + 0.12f) * framingSafeMargin;
    }
}
