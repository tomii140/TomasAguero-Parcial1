using UnityEngine;
using System.Collections;

public class HunterAI : Agent
{
    public FiniteStateMachine FSM { get; private set; }

    [Header("Configuración de Ataque (Variables Consigna)")]
    public float visionRadius = 8f;
    public float RangeAttackRadius = 5f;
    public float MeleeAttackRadius = 2f;
    public float TBA = 3f;
    
    public float TimerTBA { get; private set; }

    [Header("Manzanas Spawn")]
    public GameObject applePrefab;
    public float tiempoSpawnApple = 3f;
    public float radioSpawnApple = 6f;
    private float timerApple = 0f;

    [Header("Navegación por Waypoints")]
    public Transform[] waypoints;

    private Boid victimaTarget;

    protected override void Start()
    {
        base.Start();
        TimerTBA = 0f;

        FSM = new FiniteStateMachine();
        FSM.ChangeState(new HunterPatrolState(this));
    }

    void Update()
    {
        if (TimerTBA > 0)
            TimerTBA -= Time.deltaTime;

        ManejarSpawnManzanas();
        FSM.Update();
        AplicarFisicas();
    }

    void ManejarSpawnManzanas()
    {
        timerApple += Time.deltaTime;

        if (timerApple >= tiempoSpawnApple)
        {
            timerApple = 0f;
            Fruit[] manzanasActuales = Object.FindObjectsByType<Fruit>(FindObjectsSortMode.None);
            if (manzanasActuales.Length < 5)
            {
                Vector2 p = (Vector2)transform.position + Random.insideUnitCircle * radioSpawnApple;
                Instantiate(applePrefab, p, Quaternion.identity);
            }
        }
    }

    public void SetColorFeedback(Color c)
    {
        if (miRender != null) miRender.color = c;
    }

    public void SetVictimaTarget(Boid b) => victimaTarget = b;
    public Boid GetVictimaTarget() => victimaTarget;
    public void ResetTBA() => TimerTBA = TBA;

    public void IniciarCorrutinaRecoleccion() => StartCoroutine(RutinaRecoleccionBoid());

    public Boid BuscarBoidEntorno(bool buscarMuerto)
    {
        Boid[] grupo = Object.FindObjectsByType<Boid>(FindObjectsSortMode.None);
        Boid masCercano = null;
        float dMin = float.MaxValue;

        foreach (var b in grupo)
        {
            if (b.isDead == buscarMuerto)
            {
                float d = Vector2.Distance(transform.position, b.transform.position);
                if (d < dMin) { dMin = d; masCercano = b; }
            }
        }
        return masCercano;
    }

    private IEnumerator RutinaRecoleccionBoid()
    {
        velocity = Vector2.zero;
        aceleracion = Vector2.zero;
        yield return new WaitForSeconds(2.0f);

        if (victimaTarget != null)
        {
            if (UIManager.Instance) UIManager.Instance.SumarBoidAtrapado();
            if (GameManager.Instance) GameManager.Instance.NotificarMuerteBoidExistente(victimaTarget);
        }

        victimaTarget = null;
        FSM.ChangeState(new HunterPatrolState(this));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, visionRadius);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, RangeAttackRadius);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, MeleeAttackRadius);
    }
}