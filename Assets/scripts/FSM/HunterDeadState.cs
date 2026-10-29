using UnityEngine;

public class HunterDeadState : IState
{
    private readonly HunterAI _hunter;
    private float _timer = 0f;

    public HunterDeadState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.black);
        _hunter.gameObject.SetActive(false);
        _timer = 0f;
    }

    public void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _hunter.RespawnTime)
        {
            _hunter.ResetGathers();
            _hunter.transform.position = _hunter.InitialSpawnPoint.position;
            _hunter.gameObject.SetActive(true);
            _hunter.FSM.ChangeState(_hunter.SearchState);
        }
    }

    public void Exit() { }
}