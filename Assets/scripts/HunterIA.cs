using UnityEngine;
using System.Collections;

public class HunterAI : Agent
{
    public FiniteStateMachine FSM { get; private set; }

    [Header("Attack Settings")]
    public float visionRadius= 8f;
    public float RangeAttackRadius= 5f;
    public float MeleeAttackRadius= 2f;
    public float TBA= 3f;

    public float TimerTBA { get; private set; }

    [Header("Fruit Spawn")]
    public GameObject fruitPrefab;
    public float fruitSpawnInterval= 3f;
    public float fruitSpawnRadius= 6f;
    private float fruitTimer= 0f;

    [Header("Navigation")]
    public Transform[] waypoints;

    private Boid targetBoid;

    protected override void Start()
    {
        base.Start();
        TimerTBA= 0f;

        FSM= new FiniteStateMachine();
        FSM.ChangeState(new HunterPatrolState(this));
    }

    void Update()
    {
        if (TimerTBA> 0)
            TimerTBA -= Time.deltaTime;

        HandleFruitSpawning();
        FSM.Update();
        ApplyPhysics();
    }

    void HandleFruitSpawning()
    {
        fruitTimer += Time.deltaTime;

        if (fruitTimer>= fruitSpawnInterval)
        {
            fruitTimer= 0f;
            Fruit[] currentFruits= Object.FindObjectsByType<Fruit>(FindObjectsSortMode.None);
            if (currentFruits.Length< 5)
            {
                Vector2 p= (Vector2)transform.position + Random.insideUnitCircle* fruitSpawnRadius;
                Instantiate(fruitPrefab, p, Quaternion.identity);
            }
        }
    }

    public void SetColorFeedback(Color c)
    {
        if (myRenderer!= null) myRenderer.color= c;
    }

    public void SetTargetBoid(Boid b) => targetBoid= b;
    public Boid GetTargetBoid() => targetBoid;
    public void ResetTBA() => TimerTBA= TBA;

    public void StartGatheringRoutine() => StartCoroutine(GatheringRoutine());

    public Boid FindBoidInVision(bool lookForDead)
    {
        Boid[] group= Object.FindObjectsByType<Boid>(FindObjectsSortMode.None);
        Boid nearest= null;
        float minDist= float.MaxValue;

        foreach (var b in group)
        {
            if (b.isDead== lookForDead)
            {
                float d= Vector2.Distance(transform.position, b.transform.position);
                if (d< minDist) { minDist= d; nearest= b; }
            }
        }
        return nearest;
    }

    private IEnumerator GatheringRoutine()
    {
        velocity= Vector2.zero;
        acceleration= Vector2.zero;
        yield return new WaitForSeconds(2.0f);

        if (targetBoid != null)
        {
            if (UIManager.Instance) UIManager.Instance.AddCapturedBoid();
            if (GameManager.Instance) GameManager.Instance.NotifyBoidDeath(targetBoid);
        }

        targetBoid= null;
        FSM.ChangeState(new HunterPatrolState(this));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color= Color.yellow; Gizmos.DrawWireSphere(transform.position, visionRadius);
        Gizmos.color= Color.cyan; Gizmos.DrawWireSphere(transform.position, RangeAttackRadius);
        Gizmos.color= Color.red; Gizmos.DrawWireSphere(transform.position, MeleeAttackRadius);
    }
}