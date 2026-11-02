using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class HunterFleeState : IState
{
    private readonly HunterAI _hunter;
    private List<Node> _fleePath;
    private Node _targetSafeNode;
    private bool _isInSafeZone = false;

    public HunterFleeState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.magenta);
        _isInSafeZone = false;
        _targetSafeNode = null;
        _fleePath = null;

        // 1. Encontramos el nodo de zona segura más cercano
        _targetSafeNode = _hunter.GetNearestSafeZoneNode();
        if (_targetSafeNode != null && GraphManager.Instance != null)
        {
            // 2. Calculamos la ruta con A*
            _fleePath = GraphManager.Instance.FindPath(_hunter.transform.position, _targetSafeNode.transform.position);
            if (_fleePath != null && _fleePath.Count > 0)
            {
                _hunter.FollowPath(_fleePath, _hunter.WaypointThreshold);
            }
        }
    }

    public void Update()
    {
        if (_isInSafeZone || _targetSafeNode == null) return;

        // Distancia física directa al nodo de la zona segura objetivo
        float distToSafeNode = Vector3.Distance(_hunter.transform.position, _targetSafeNode.transform.position);

        // Si el cazador llegó físicamente a la zona segura (menos de 1.5 metros del nodo destino)
        if (distToSafeNode <= 1.5f)
        {
            _isInSafeZone = true;
            _hunter.StartCoroutine(SafeZoneWaitRoutine());
            return;
        }

        // Si aún hay ruta de A* por recorrer, avanzamos por los nodos
        if (_fleePath != null && _fleePath.Count > 0)
        {
            _hunter.ExecutePathMovement();
        }

        // Aplicamos una fuerza constante de Arrive directa al nodo seguro para asegurar que 
        // entre por completo y no se quede trabado a mitad de camino ni "obsesionado" en el borde.
        Vector3 fleeForce = _hunter.Arrive(_targetSafeNode.transform.position, 3f);
        _hunter.AddForce(fleeForce);
    }

    private IEnumerator SafeZoneWaitRoutine()
    {
        _hunter.ResetVelocity();
        _hunter.FollowPath(null, 0f); // Limpiamos la ruta para que no arrastre referencias

        yield return new WaitForSeconds(_hunter.SafeZoneWaitTime);

        // Al finalizar el tiempo de espera, evalúa correctamente su destino según las recolecciones
        if (_hunter.CurrentGathers >= _hunter.RequiredGathers)
        {
            _hunter.FSM.ChangeState(_hunter.ExitState);
        }
        else
        {
            _hunter.FSM.ChangeState(_hunter.SearchState);
        }
    }

    public void Exit()
    {
        _fleePath = null;
        _targetSafeNode = null;
        _isInSafeZone = false;
        _hunter.FollowPath(null, 0f);
    }
}