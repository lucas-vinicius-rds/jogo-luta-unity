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
    [SerializeField] private float baseDistance = 3.55f;

    [Tooltip("Altura da câmera em relação ao chão.")]
    [SerializeField] private float height = 2.15f;

    [Tooltip("Altura do ponto de foco (look-at) acima do chão.")]
    [SerializeField] private float lookAtHeight = 1.75f;

    [Tooltip("Fator de recuo de zoom conforme os lutadores se distanciam.")]
    [SerializeField] private float zoomFactor = 0.22f;

    [Header("Damping")]
    [Tooltip("Suavização do movimento da câmera (valores menores = mais suave).")]
    [SerializeField] private float followDamping = 8.0f;

    [Tooltip("Suavização da rotação da câmera.")]
    [SerializeField] private float rotationDamping = 10.0f;

    public Transform Fighter1 { get => fighter1; set => fighter1 = value; }
    public Transform Fighter2 { get => fighter2; set => fighter2 = value; }

    private void LateUpdate()
    {
        if (fighter1 == null || fighter2 == null) return;

        Camera camera = GetComponent<Camera>();

        // 1. Calcula o ponto médio entre os lutadores
        Vector3 p1 = fighter1.position;
        Vector3 p2 = fighter2.position;
        Vector3 midpoint = (p1 + p2) * 0.5f;

        // 2. Calcula a linha de combate no plano horizontal (XZ)
        // Câmera lateral fixa: a luta acontece apenas no eixo X.
        float fighterDistance = Mathf.Abs(p2.x - p1.x);
        float currentDistance = baseDistance + (fighterDistance * zoomFactor);
        Vector3 targetPosition = new Vector3(midpoint.x, midpoint.y + height, -currentDistance);
        targetPosition.y = midpoint.y + height;
        if (camera != null)
        {
            camera.orthographic = true;
            // Personagens ocupam a maior parte do quadro, como na referencia,
            // mas ainda ha area suficiente para salto e troca de lados.
            camera.orthographicSize = Mathf.Clamp(1.65f + fighterDistance * 0.06f, 1.85f, 2.45f);
        }

        // Interpolação suave de posição
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followDamping);

        // 4. Orientação da câmera olhando para o ponto médio dos lutadores
        Vector3 lookTarget = midpoint + Vector3.up * lookAtHeight;
        Vector3 directionToTarget = lookTarget - transform.position;

        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationDamping);
        }
    }
}
