using UnityEngine;

public class HunterPatrolState : IState
{
    private HunterAI _hunter;
    private int _idxWp= 0;
    private bool _reverse= false;

    public HunterPatrolState(HunterAI hunter) => _hunter= hunter;

    public void Enter() => _hunter.SetColorFeedback(Color.green);

    public void Update()
    {
        Boid dead= _hunter.FindBoidInVision(true);
        if (dead && Vector2.Distance(_hunter.transform.position, dead.transform.position)< _hunter.visionRadius)
        {
            _hunter.SetTargetBoid(dead);
            _hunter.FSM.ChangeState(new HunterGatherState(_hunter));
            return;
        }

        if (_hunter.TimerTBA<= 0)
        {
            Boid alive= _hunter.FindBoidInVision(false);
            if (alive && Vector2.Distance(_hunter.transform.position, alive.transform.position)< _hunter.visionRadius)
            {
                _hunter.SetTargetBoid(alive);
                _hunter.FSM.ChangeState(new HunterAttackState(_hunter));
                return;
            }
        }

        if (_hunter.waypoints== null || _hunter.waypoints.Length== 0) return;

        Vector2 dest= _hunter.waypoints[_idxWp].position;
        _hunter.AddForce(_hunter.Seek(dest));

        if (Vector2.Distance(_hunter.transform.position, dest)< 1.2f)
        {
            if (!_reverse)
            {
                if (_idxWp+ 1 < _hunter.waypoints.Length) _idxWp++;
                else { _reverse= true; _idxWp--; }
            }
            else
            {
                if (_idxWp - 1 >= 0) _idxWp--;
                else { _reverse= false; _idxWp++; }
            }
        }
    }

    public void Exit() { }
}

public class HunterAttackState : IState
{
    private HunterAI _hunter;

    public HunterAttackState(HunterAI hunter) => _hunter= hunter;

    public void Enter() => _hunter.SetColorFeedback(Color.red);

    public void Update()
    {
        Boid target= _hunter.GetTargetBoid();

        if (target== null || target.isDead)
        {
            _hunter.FSM.ChangeState(new HunterPatrolState(_hunter));
            return;
        }

        float d= Vector2.Distance(_hunter.transform.position, target.transform.position);

        if (d> _hunter.visionRadius)
        {
            _hunter.SetTargetBoid(null);
            _hunter.FSM.ChangeState(new HunterPatrolState(_hunter));
            return;
        }

        if (d<= _hunter.MeleeAttackRadius)
        {
            target.Die();
            _hunter.ResetTBA();
            _hunter.SetTargetBoid(null);
            _hunter.FSM.ChangeState(new HunterPatrolState(_hunter));
        }
        else if (d<= _hunter.RangeAttackRadius)
        {
            target.Die();
            _hunter.ResetTBA();
            _hunter.SetTargetBoid(null);
            _hunter.FSM.ChangeState(new HunterPatrolState(_hunter));
        }
        else
        {
            _hunter.AddForce(_hunter.Seek(target.transform.position));
        }
    }

    public void Exit() { }
}

public class HunterGatherState : IState
{
    private HunterAI _hunter;
    private bool _routineStarted= false;

    public HunterGatherState(HunterAI hunter) => _hunter= hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.blue);
        _routineStarted= false;
    }

    public void Update()
    {
        Boid target= _hunter.GetTargetBoid();
        if (target== null)
        {
            _hunter.FSM.ChangeState(new HunterPatrolState(_hunter));
            return;
        }

        _hunter.AddForce(_hunter.Arrive(target.transform.position, 1.5f));

        if (!_routineStarted && Vector2.Distance(_hunter.transform.position, target.transform.position)< 0.7f)
        {
            _routineStarted= true;
            _hunter.StartGatheringRoutine();
        }
    }

    public void Exit() { }
}