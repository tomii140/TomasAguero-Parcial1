
using UnityEngine;
using TMPro;

public class Boid : Agent
{
    public FiniteStateMachine FSM { get; private set; }

    public BoidFlockingState FlockingState { get; private set; }
    public BoidEvadeState EvadeState { get; private set; }
    public BoidSeekObjectState SeekObjectState { get; private set; }
    public BoidConsumeState ConsumeState { get; private set; }
    public BoidDeadState DeadState { get; private set; }
    public BoidRespawnState RespawnState { get; private set; }

    public bool IsDead { get; private set; } = false;
    public bool IsRespawning { get; private set; } = false;
    public bool ShouldRespawn { get; private set; } = false;

    [Header("Health Configuration")]
    [SerializeField] private int maxHealth = 2;
    private int currentHealth;
    public int CurrentHealth => currentHealth;

    [Header("Flocking Configuration")]
    [SerializeField, Range(0.5f, 50f)] private float separationRadius = 2.0f;
    [SerializeField, Range(2f, 50f)] private float alignmentRadius = 12.0f;
    [SerializeField, Range(2f, 50f)] private float cohesionRadius = 12.0f;
    [SerializeField, Range(0f, 50f)] private float separationWeight = 2.0f;
    [SerializeField, Range(0f, 50f)] private float alignmentWeight = 1.0f;
    [SerializeField, Range(0f, 50f)] private float cohesionWeight = 1.5f;

    public float SeparationWeight => separationWeight;

    [Header("Perception Sensors")]
    [SerializeField, Range(1f, 50f)] private float hunterPerceptionRadius = 10f;
    [SerializeField, Range(1f, 50f)] private float fruitPerceptionRadius = 10f;
    [SerializeField, Range(0.5f, 50f)] private float fruitWeight = 2.0f;
    [SerializeField, Range(0.5f, 50f)] private float fruitArriveSlowingRadius = 3.0f;

    public float HunterPerceptionRadius => hunterPerceptionRadius;
    public float FruitWeight => fruitWeight;
    public float FruitArriveSlowingRadius => fruitArriveSlowingRadius;

    [Header("Respawn Configuration")]
    [SerializeField, Range(1f, 10f)] private float minRespawnTime = 3f;
    [SerializeField, Range(1f, 10f)] private float maxRespawnTime = 6f;

    public float MinRespawnTime => minRespawnTime;
    public float MaxRespawnTime => maxRespawnTime;

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip eatAudioClip;
    public AudioClip EatAudioClip => eatAudioClip;

    [Header("Feedback")]
    [SerializeField] private TextMeshPro stateTextUI;
    [SerializeField] private bool showGizmos = true;

    private MeshRenderer meshRenderer;
    private Animator animator;

    protected override void Start()
    {
        base.Start();

        meshRenderer = GetComponent<MeshRenderer>();
        animator = GetComponent<Animator>();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterBoid(this);

        currentHealth = maxHealth;

        InitializeFSM();

        RefreshUIFeedback();
    }

    private void OnDestroy()
    {
        if (FSM != null)
            FSM.OnStateChanged -= HandleStateChanged;

        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterBoid(this);
    }

    private void InitializeFSM()
    {
        FSM = new FiniteStateMachine();

        FlockingState = new BoidFlockingState(this);
        EvadeState = new BoidEvadeState(this);
        SeekObjectState = new BoidSeekObjectState(this);
        ConsumeState = new BoidConsumeState(this);
        DeadState = new BoidDeadState(this);
        RespawnState = new BoidRespawnState(this);

        FSM.OnStateChanged += HandleStateChanged;

        FSM.ChangeState(FlockingState);
    }

    public void Revive()
    {
        IsDead = false;
        IsRespawning = false;
        ShouldRespawn = false;

        currentHealth = maxHealth;

        SetColorFeedback(Color.green);

        Velocity = transform.forward * (maxSpeed * 0.5f);

        FSM.ChangeState(FlockingState);

        RefreshUIFeedback();
    }

    public void ReceiveDamage(int damage)
    {
        if (IsDead || IsRespawning) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            IsDead = true;
        }

        RefreshUIFeedback();
    }

    public void TriggerRespawnFlow()
    {
        IsRespawning = true;
        ShouldRespawn = true;
    }

    private void Update()
    {
        FSM.Update();

        ApplyPhysics();

        UpdateAnimator();
    }

    // SENSORES LOCALES POR OVERLAPSPHERE

    public HunterAI DetectHunterInRange()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            hunterPerceptionRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (var hit in hits)
        {
            HunterAI hunter = hit.GetComponent<HunterAI>();

            if (hunter != null)
            {
                float dist = Vector3.Distance(transform.position, hunter.transform.position);

                if (dist <= hunterPerceptionRadius)
                    return hunter;
            }
        }

        return null;
    }

    public Fruit FindNearestFruit()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            fruitPerceptionRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        Fruit nearest = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            Fruit fruit = hit.GetComponent<Fruit>();

            if (fruit != null)
            {
                float dist = Vector3.Distance(transform.position, fruit.transform.position);

                if (dist <= fruitPerceptionRadius && dist < minDistance)
                {
                    minDistance = dist;
                    nearest = fruit;
                }
            }
        }

        return nearest;
    }

    // CÁLCULOS DE FLOCKING (SEPARATION, ALIGNMENT, COHESION)

    public Vector3 CalculateFlocking()
    {
        return (GetSeparationForce() * separationWeight) +
               (GetAlignmentForce() * alignmentWeight) +
               (GetCohesionForce() * cohesionWeight);
    }

    public Vector3 GetSeparationForce()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            separationRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        Vector3 force = Vector3.zero;
        int count = 0;

        foreach (var hit in hits)
        {
            Boid other = hit.GetComponent<Boid>();

            if (other != null && other != this && !other.IsDead && !other.IsRespawning)
            {
                float dist = Vector3.Distance(transform.position, other.transform.position);

                if (dist <= separationRadius && dist > 0f)
                {
                    // Fuerza inversamente proporcional al cuadrado de la distancia para evitar superposiciones.
                    Vector3 repulse = transform.position - other.transform.position;

                    repulse /= (dist * dist);

                    force += repulse;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            force /= count;
            force = force.normalized * maxSpeed;
            force -= Velocity;
            force = Vector3.ClampMagnitude(force, maxForce);
        }

        return force;
    }

    public Vector3 GetAlignmentForce()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            alignmentRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        Vector3 sumVelocity = Vector3.zero;
        int count = 0;

        foreach (var hit in hits)
        {
            Boid other = hit.GetComponent<Boid>();

            if (other != null && other != this && !other.IsDead && !other.IsRespawning)
            {
                float dist = Vector3.Distance(transform.position, other.transform.position);

                if (dist > alignmentRadius)
                    continue;

                sumVelocity += other.Velocity;
                count++;
            }
        }

        if (count > 0)
        {
            sumVelocity /= count;
            sumVelocity = sumVelocity.normalized * maxSpeed;

            Vector3 steer = sumVelocity - Velocity;

            return Vector3.ClampMagnitude(steer, maxForce);
        }

        return Vector3.zero;
    }

    public Vector3 GetCohesionForce()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            cohesionRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        Vector3 sumPosition = Vector3.zero;
        int count = 0;

        foreach (var hit in hits)
        {
            Boid other = hit.GetComponent<Boid>();

            if (other != null && other != this && !other.IsDead && !other.IsRespawning)
            {
                float dist = Vector3.Distance(transform.position, other.transform.position);

                if (dist > cohesionRadius)
                    continue;

                sumPosition += other.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            sumPosition /= count;

            return Seek(sumPosition);
        }

        return Vector3.zero;
    }

    public void RestoreForwardMomentum()
    {
        Velocity = transform.forward * (maxSpeed * 0.5f);
    }

    public void SetRespawnVisibility(bool visible)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            renderer.enabled = visible;

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            col.enabled = visible;
    }

    public void SetColorFeedback(Color color)
    {
        if (meshRenderer != null)
            meshRenderer.material.color = color;
    }

    private void HandleStateChanged(IState newState)
    {
        RefreshUIFeedback();
    }

    private void RefreshUIFeedback()
    {
        if (stateTextUI == null || FSM == null || FSM.CurrentState == null)
            return;

        string stateName = FSM.CurrentState.GetType().Name
            .Replace("Boid", "")
            .Replace("State", "");

        stateTextUI.text = $"{stateName}\nHP: {currentHealth}/{maxHealth}";
    }

    private void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", Velocity.magnitude);
            animator.SetBool("IsDead", IsDead || IsRespawning);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hunterPerceptionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, fruitPerceptionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alignmentRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, cohesionRadius);
    }
}
