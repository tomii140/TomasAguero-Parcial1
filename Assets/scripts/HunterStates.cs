using UnityEngine;

// ------------------- ESTADO PATROL -------------------
public class HunterPatrolState : IState
{
    private HunterAI _hunter;
    private int _idxWp = 0;
    private bool _reversa = false;

    public HunterPatrolState(HunterAI hunter) => _hunter = hunter;

    public void Enter() => _hunter.SetColorFeedback(Color.green);

    public void Update()
    {
        // 1. Transición Prioritaria: Recolección de Boids muertos en visión
        Boid muerto = _hunter.BuscarBoidEntorno(true);
        if (muerto && Vector2.Distance(_hunter.transform.position, muerto.transform.position) < _hunter.visionRadius)
        {
            _hunter.SetVictimaTarget(muerto);
            _hunter.FSM.ChangeState(new HunterGatherState(_hunter));
            return;
        }

        // 2. Transición a Ataque: TBA finalizado + Boid vivo en rango de percepción
        if (_hunter.TimerTBA <= 0)
        {
            Boid vivo = _hunter.BuscarBoidEntorno(false);
            if (vivo && Vector2.Distance(_hunter.transform.position, vivo.transform.position) < _hunter.visionRadius)
            {
                _hunter.SetVictimaTarget(vivo);
                _hunter.FSM.ChangeState(new HunterAttackState(_hunter));
                return;
            }
        }

        // 3. Recorrido de Waypoints (Ping-Pong / Inverso)
        if (_hunter.waypoints == null || _hunter.waypoints.Length == 0) return;

        Vector2 dest = _hunter.waypoints[_idxWp].position;
        _hunter.MeterFuerza(_hunter.Seek(dest));

        if (Vector2.Distance(_hunter.transform.position, dest) < 1.2f)
        {
            if (!_reversa)
            {
                if (_idxWp + 1 < _hunter.waypoints.Length) _idxWp++;
                else { _reversa = true; _idxWp--; }
            }
            else
            {
                if (_idxWp - 1 >= 0) _idxWp--;
                else { _reversa = false; _idxWp++; }
            }
        }
    }

    public void Exit() { }
}

// ------------------- ESTADO ATTACK -------------------
public class HunterAttackState : IState
{
    private HunterAI _hunter;

    public HunterAttackState(HunterAI hunter) => _hunter = hunter;

    public void Enter() => _hunter.SetColorFeedback(Color.red);

    public void Update()
    {
        Boid target = _hunter.GetVictimaTarget();

        if (target == null || target.isDead)
        {
            _hunter.FSM.ChangeState(new HunterPatrolState(_hunter));
            return;
        }

        float d = Vector2.Distance(_hunter.transform.position, target.transform.position);

        // Abandona rango de visión: Vuelve a Patrol SIN reiniciar TBA (Consigna)
        if (d > _hunter.visionRadius)
        {
            _hunter.SetVictimaTarget(null);
            _hunter.FSM.ChangeState(new HunterPatrolState(_hunter));
            return;
        }

        // Evaluación de rangos según la consigna:
        if (d <= _hunter.MeleeAttackRadius)
        {
            // Ataque Melee Exitoso
            target.Morir();
            _hunter.ResetTBA();
            _hunter.SetVictimaTarget(null);
            _hunter.FSM.ChangeState(new HunterPatrolState(_hunter));
        }
        else if (d <= _hunter.RangeAttackRadius)
        {
            // Ataque a Distancia Exitoso
            target.Morir();
            _hunter.ResetTBA();
            _hunter.SetVictimaTarget(null);
            _hunter.FSM.ChangeState(new HunterPatrolState(_hunter));
        }
        else
        {
            // Fuera de rango de ataque: Perseguir para acortar distancia
            _hunter.MeterFuerza(_hunter.Seek(target.transform.position));
        }
    }

    public void Exit() { }
}

// ------------------- ESTADO GATHER -------------------
public class HunterGatherState : IState
{
    private HunterAI _hunter;
    private bool _corrutinaIniciada = false;

    public HunterGatherState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.blue);
        _corrutinaIniciada = false;
    }

    public void Update()
    {
        Boid target = _hunter.GetVictimaTarget();
        if (target == null)
        {
            _hunter.FSM.ChangeState(new HunterPatrolState(_hunter));
            return;
        }

        _hunter.MeterFuerza(_hunter.Arrive(target.transform.position, 1.5f));

        if (!_corrutinaIniciada && Vector2.Distance(_hunter.transform.position, target.transform.position) < 0.7f)
        {
            _corrutinaIniciada = true;
            _hunter.IniciarCorrutinaRecoleccion();
        }
    }

    public void Exit() { }
}