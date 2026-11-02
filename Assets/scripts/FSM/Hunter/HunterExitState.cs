using UnityEngine;
using System.Collections.Generic;

public class HunterExitState : IState
{
    private readonly HunterAI _hunter;
    private List<Node> _exitPath;
    private int _currentIndex = 0;
    private bool _hasReachedExit = false;

    public HunterExitState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(new Color(1f, 0.5f, 0f)); // Naranja para salida
        _hunter.ResetVelocity();
        _hasReachedExit = false;
        _exitPath = null;
        _currentIndex = 0;

        if (_hunter.ExitPoint != null && GraphManager.Instance != null)
        {
            Node startNode = GraphManager.Instance.GetNearestNode(_hunter.transform.position);
            Node targetNode = GraphManager.Instance.GetNearestNode(_hunter.ExitPoint.position);

            if (startNode != null && targetNode != null)
            {
                // Calculamos la ruta mediante A* de la misma forma que en Search
                _exitPath = GraphManager.Instance.FindPath(startNode.transform.position, targetNode.transform.position);
            }
        }
    }

    public void Update()
    {
        if (_hasReachedExit || _hunter.ExitPoint == null) return;

        // 1. Validar distancia física al punto final de salida
        float distToExit = Vector3.Distance(_hunter.transform.position, _hunter.ExitPoint.position);
        if (distToExit <= 2.0f)
        {
            _hasReachedExit = true;
            CompleteExitRoutine();
            return;
        }

        // 2. Si tenemos ruta A* activa
        if (_exitPath != null && _exitPath.Count > 0)
        {
            // Si aún nos quedan nodos por recorrer en el path
            if (_currentIndex < _exitPath.Count)
            {
                Vector3 targetNodePos = _exitPath[_currentIndex].transform.position;

                // Aplicar Arrive hacia el nodo actual (idéntico al funcionamiento robusto de Search)
                Vector3 force = _hunter.Arrive(targetNodePos, 2.0f);
                _hunter.AddForce(force);

                // Control de distancia horizontal XZ para avanzar de nodo sin trabarse
                Vector3 hunterXZ = new Vector3(_hunter.transform.position.x, 0f, _hunter.transform.position.z);
                Vector3 nodeXZ = new Vector3(targetNodePos.x, 0f, targetNodePos.z);

                if (Vector3.Distance(hunterXZ, nodeXZ) < 2.0f)
                {
                    _currentIndex++;
                }
                return;
            }
        }

        // 3. Tramo final: Si se terminaron los nodos, vamos directo a la puerta con Arrive
        Vector3 finalForce = _hunter.Arrive(_hunter.ExitPoint.position, 3.0f);
        _hunter.AddForce(finalForce);
    }

    private void CompleteExitRoutine()
    {
        _hunter.ResetGathers();
        _hunter.ResetVelocity();

        // Limpiamos la ruta para que no queden referencias visuales de gizmos
        _exitPath = null;
        _currentIndex = 0;

        if (_hunter.InitialSpawnPoint != null)
        {
            _hunter.transform.position = _hunter.InitialSpawnPoint.position;
        }

        _hunter.FSM.ChangeState(_hunter.SearchState);
    }

    public void Exit()
    {
        _exitPath = null;
        _currentIndex = 0;
        _hasReachedExit = false;
    }
}