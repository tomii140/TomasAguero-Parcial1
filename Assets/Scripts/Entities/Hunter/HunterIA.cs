
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HunterAI : Agent
{
    public FiniteStateMachine FSM { get; private set; }

    public HunterPatrolState PatrolState { get; private set; }
    public HunterAttackState AttackState { get; private set; }
    public HunterGatherState GatherState { get; private set; }
    public HunterRestState RestState { get; private set; }

    [Header("Detection & Combat")]
    [SerializeField, Range(1f, 80f)] private float visionRadius = 15f;
    [SerializeField, Range(1f, 50f)] private float rangeAttackRadius = 10f;
    [SerializeField, Range(0.5f, 25f)] private float meleeAttackRadius = 3f;
    [SerializeField, Range(0.1f, 10f)] private float timeBetweenAttacks = 2f;
    [SerializeField, Range(0.5f, 10f)] private float gatheringTime = 2f;
    [SerializeField, Range(0.5f, 5f)] private float captureRadius = 1.5f;

    [Header("Combat Damage")]
    [SerializeField, Min(1)] private int meleeDamage = 2;
    [SerializeField, Min(1)] private int rangedDamage = 1;

    [Header("Perception Optimization")]
    [SerializeField, Min(0.02f)] private float perceptionUpdateInterval = 0.2f;

    [Header("Energy & Rest")]
    [SerializeField, Range(0f, 500f)] private float maxEnergy = 100f;
    [SerializeField, Range(0.1f, 500f)] private float energyConsumptionRate = 15f;
    [SerializeField, Range(0.1f, 50f)] private float energyRecoveryRate = 25f;

    [Header("Energy Status")]
    [SerializeField] private float currentEnergy;

    [Header("Patrol")]
    [SerializeField, Range(0.1f, 10f)] private float waypointThreshold = 1f;
    [SerializeField, Range(0.5f, 10f)] private float waypointArriveSlowingRadius = 2f;
    [SerializeField] private Transform[] waypoints;

    [Header("Fruit Spawning")]
    [SerializeField] private GameObject fruitPrefab;
    [SerializeField, Range(1f, 10f)] private float fruitSpawnInterval = 4f;
    [SerializeField, Range(1f, 20f)] private float fruitSpawnRadius = 8f;
    [SerializeField, Range(1, 10)] private int maxFruitsInScene = 5;

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip rangedAttackSound;
    [SerializeField] private AudioClip meleeAttackSound;
    [SerializeField] private AudioClip detectBoidSound;
    [SerializeField] private AudioClip restSound;

    private AudioSource audioSource;

    [Header("Feedback")]
    [SerializeField] private TextMeshPro stateTextUI;
    [SerializeField] private bool showGizmos = true;

    [Header("Gathering Progress UI")]
    [SerializeField] private Slider gatheringProgressSlider;
    [SerializeField] private GameObject gatheringProgressCanvas;

    private Animator animator;
    private HunterPerception perception;
    private Boid targetBoid;

    public float TimerAttackCooldown { get; private set; }

    private Vector3 lastPosition;

    public float VisionRadius => visionRadius;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    public float TimeBetweenAttacks => timeBetweenAttacks;
    public float GatheringTime => gatheringTime;
    public float CaptureRadius => captureRadius;
    public float WaypointThreshold => waypointThreshold;
    public float WaypointArriveSlowingRadius => waypointArriveSlowingRadius;
    public float FruitSpawnInterval => fruitSpawnInterval;
    public float MaxEnergy => maxEnergy;
    public float CurrentEnergy => currentEnergy;
    public float EnergyConsumptionRate => energyConsumptionRate;
    public float EnergyRecoveryRate => energyRecoveryRate;
    public Transform[] Waypoints => waypoints;
    public AudioClip DetectBoidSound => detectBoidSound;
    public AudioClip RestSound => restSound;

    public int MeleeDamage => meleeDamage;
    public int RangedDamage => rangedDamage;

    protected override void Start()
    {
        base.Start();

        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
        }

        currentEnergy = maxEnergy;
        lastPosition = transform.position;

        HideGatheringProgress();

        if (gatheringProgressSlider != null)
        {
            gatheringProgressSlider.minValue = 0f;
            gatheringProgressSlider.maxValue = 1f;
            gatheringProgressSlider.value = 0f;
        }

        perception = new HunterPerception(this, perceptionUpdateInterval);
        perception.Refresh();

        PatrolState = new HunterPatrolState(this);
        AttackState = new HunterAttackState(this);
        GatherState = new HunterGatherState(this);
        RestState = new HunterRestState(this);

        FSM = new FiniteStateMachine();
        FSM.OnStateChanged += HandleStateChanged;
        FSM.ChangeState(PatrolState);

        RefreshUIFeedback();
    }

    private void OnDestroy()
    {
        if (FSM != null)
            FSM.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        perception.Update(Time.deltaTime);
        UpdateCooldowns();

        FSM.Update();

        ApplyPhysics();
        UpdateAnimator();
    }

    public void ShowGatheringProgress(float progress)
    {
        if (gatheringProgressCanvas != null)
            gatheringProgressCanvas.SetActive(true);

        if (gatheringProgressSlider != null)
            gatheringProgressSlider.value = Mathf.Clamp01(progress);
    }

    public void HideGatheringProgress()
    {
        if (gatheringProgressSlider != null)
            gatheringProgressSlider.value = 0f;

        if (gatheringProgressCanvas != null)
            gatheringProgressCanvas.SetActive(false);
    }

    private void UpdateCooldowns()
    {
        if (TimerAttackCooldown > 0f)
            TimerAttackCooldown -= Time.deltaTime;
    }

    public void ResetAttackCooldown()
    {
        TimerAttackCooldown = timeBetweenAttacks;
    }

    public void PlayAttackSound(bool isMelee)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = isMelee
            ? meleeAttackSound
            : rangedAttackSound;

        if (clipToPlay != null)
            audioSource.PlayOneShot(clipToPlay);
    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    public Boid GetTargetBoid()
    {
        return targetBoid;
    }

    public void SetTargetBoid(Boid boid)
    {
        targetBoid = boid;
    }

    public void ConsumeOrRecoverEnergy(float amount)
    {
        int previousDisplayedEnergy = Mathf.RoundToInt(currentEnergy);

        currentEnergy = Mathf.Clamp(
            currentEnergy + amount,
            0f,
            maxEnergy
        );

        if (Mathf.RoundToInt(currentEnergy) != previousDisplayedEnergy)
            RefreshUIFeedback();
    }

    public Boid FindBoidInVision(bool lookForDead)
    {
        return perception != null
            ? perception.FindBoidInVision(lookForDead)
            : null;
    }

    public void SpawnFruitIfAllowed()
    {
        if (fruitPrefab == null || GameManager.Instance == null)
            return;

        if (GameManager.Instance.ActiveFruits.Count >= maxFruitsInScene)
            return;

        Vector2 randomCircle = Random.insideUnitCircle * fruitSpawnRadius;

        Vector3 spawnPos = transform.position + new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        Instantiate(fruitPrefab, spawnPos, Quaternion.identity);
    }

    public void NotifyBoidCollected(Boid boid)
    {
        GameManager.Instance?.NotifyBoidDeath(boid);
    }

    private void HandleStateChanged(IState newState)
    {
        string stateName = newState.GetType().Name
            .Replace("Hunter", "")
            .Replace("State", "");

        Debug.Log($"[CAZADOR] Cambió su comportamiento: {stateName}.");

        RefreshUIFeedback();
    }

    private void RefreshUIFeedback()
    {
        if (stateTextUI == null || FSM == null || FSM.CurrentState == null)
            return;

        string currentStateName = FSM.CurrentState.GetType().Name
            .Replace("Hunter", "")
            .Replace("State", "");

        int displayedEnergy = Mathf.RoundToInt(currentEnergy);

        stateTextUI.text =
            $"State: {currentStateName}\nEnergy: {displayedEnergy}/{maxEnergy}";
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(lastPosition.x, 0f, lastPosition.z)
        ) / Time.deltaTime;

        lastPosition = transform.position;

        animator.SetFloat("Speed", speed);
        animator.SetBool("IsResting", FSM.CurrentState == RestState);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
}
