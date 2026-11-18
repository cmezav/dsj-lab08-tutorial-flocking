using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private bool m_IsStopped;
    [SerializeField] private float m_ResumeMargin = 0.5f;
    
    [SerializeField] private float m_Speed = 4f;
    [SerializeField] private float m_RotationSpeed = 8f;
    [SerializeField] private float m_StopDistance = 1.5f; // Distance at which the enemy stops following

    [Header("Boids")]
    [SerializeField] private float m_DetectionDistance = 1f;
    [SerializeField] private LayerMask m_EnemyMask;
    [SerializeField] private int m_NeighborsCount;

    [SerializeField] private float m_SeparationWeight = 1f;
    [SerializeField] private float m_AlignmentWeight = 1f;

    [SerializeField] private float m_CohesionWeight = 1f;

    // Parametros expuestos para el panel de pruebas durante Play.
    public Vector3 BoidWeights => new Vector3(m_SeparationWeight, m_AlignmentWeight, m_CohesionWeight);

    public void SetBoidWeights(float separation, float alignment, float cohesion)
    {
        m_SeparationWeight = Mathf.Clamp(separation, 0f, 10f);
        m_AlignmentWeight = Mathf.Clamp(alignment, 0f, 10f);
        m_CohesionWeight = Mathf.Clamp(cohesion, 0f, 10f);
    }



    private Vector3 m_SeparationForce;

    private Animator m_Animator;
    private Transform m_Target;

    private Vector3 m_Direction = Vector3.zero;
    private Quaternion m_TargetRotation;

    private float m_MovementSpeedBlend;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();

        PlayerController player = FindAnyObjectByType<PlayerController>();

        if (player != null)
            m_Target = player.transform;
        else
            Debug.LogError("No se encontrÃ³ un PlayerController en la escena.", this);
    }

    void Update()
    {
        if (m_Target != null)
        {
            FollowTarget();
        }
    }

    private void FollowTarget()
    {
        Collider[] neighbors = GetNeighbors();
        m_NeighborsCount = neighbors.Length;

        m_SeparationForce = CalculateSeparationForce(neighbors);
        m_SeparationForce += CalculateAlignmentForce(neighbors);
        m_SeparationForce += CalculateCohesionForce(neighbors);

        m_Direction = (m_Target.position - transform.position).WithNewY(0);
        float distance = m_Direction.magnitude;

        // Se detiene al entrar al círculo.
        if (!m_IsStopped && distance <= m_StopDistance)
            m_IsStopped = true;

        // Solo vuelve a avanzar cuando el jugador se aleja un poco más.
        if (m_IsStopped && distance > m_StopDistance + m_ResumeMargin)
            m_IsStopped = false;

        if (m_IsStopped)
        {
            StopMove();
        }
        else
        {
            MoveTowardsTarget();
            RotateTowardsTarget();
        }

    
    }

    private void MoveTowardsTarget()
    {
        Vector3 combinedDirection =
            (m_Direction.normalized + m_SeparationForce).normalized;

        transform.position += combinedDirection * m_Speed * Time.deltaTime;

        // Orienta al enemigo hacia su desplazamiento.
        if (combinedDirection.sqrMagnitude > 0.0001f)
            m_Direction = combinedDirection;

        m_MovementSpeedBlend = Mathf.Lerp(
            m_MovementSpeedBlend,
            1f,
            Time.deltaTime * m_Speed
        );

        m_Animator.SetFloat("Speed", m_MovementSpeedBlend);
    }

    private void StopMove()
    {
        m_MovementSpeedBlend = Mathf.Lerp(m_MovementSpeedBlend, 0, Time.deltaTime * m_Speed);
        m_Animator.SetFloat("Speed", m_MovementSpeedBlend);
    }

    private void RotateTowardsTarget()
    {
        if (m_Direction.sqrMagnitude < 0.0001f)
            return;

        m_TargetRotation = Quaternion.LookRotation(m_Direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            m_TargetRotation,
            Time.deltaTime * m_RotationSpeed
        );
    }

    private Collider[] GetNeighbors()
    {
        Collider[] detected = Physics.OverlapSphere(
            transform.position,
            m_DetectionDistance,
            m_EnemyMask
        );

        var neighbors = new System.Collections.Generic.List<Collider>();

        foreach (Collider candidate in detected)
        {
            EnemyController enemy = candidate.GetComponentInParent<EnemyController>();

            // No contamos al propio enemigo.
            if (enemy != null && enemy != this)
                neighbors.Add(candidate);
        }

        return neighbors.ToArray();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, m_DetectionDistance);
    }

    private Vector3 CalculateSeparationForce(Collider[] neighbors)
    {
        Vector3 force = Vector3.zero;

        foreach (Collider neighbor in neighbors)
        {
            EnemyController enemy = neighbor.GetComponentInParent<EnemyController>();

            Vector3 away = transform.position - enemy.transform.position;
            away.y = 0f;

            float distance = away.magnitude;

            if (distance > 0.001f)
                force += away.normalized / distance;
        }

        return force * m_SeparationWeight;
    }

    private Vector3 CalculateAlignmentForce(Collider[] neighbors)
    {
        if (neighbors.Length == 0)
            return Vector3.zero;

        Vector3 neighborsForward = Vector3.zero;

        foreach (Collider neighbor in neighbors)
        {
            EnemyController enemy =
                neighbor.GetComponentInParent<EnemyController>();

            neighborsForward += enemy.transform.forward;
        }

        neighborsForward.y = 0f;

        return neighborsForward.normalized * m_AlignmentWeight;
    }

    private Vector3 CalculateCohesionForce(Collider[] neighbors)
    {
        if (neighbors.Length == 0)
            return Vector3.zero;

        Vector3 averagePosition = Vector3.zero;

        foreach (Collider neighbor in neighbors)
        {
            EnemyController enemy =
                neighbor.GetComponentInParent<EnemyController>();

            averagePosition += enemy.transform.position;
        }

        averagePosition /= neighbors.Length;

        Vector3 directionToCenter = averagePosition - transform.position;
        directionToCenter.y = 0f;

        return directionToCenter.normalized * m_CohesionWeight;
    }

}
