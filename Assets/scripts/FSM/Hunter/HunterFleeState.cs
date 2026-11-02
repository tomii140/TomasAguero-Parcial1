using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class HunterFleeState : IState
{
    private readonly HunterAI _hunter;
    private List<Node> _fleePath;
    private int _currentIndex = 0;
    private bool _isInSafeZone = false;

    public HunterFleeState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.magenta);
        _isInSafeZone = false;
        _fleePath = null;
        _currentIndex = 0;

        Node nearestSafe = _hunter.GetNearestSafeZoneNode();
        if (nearestSafe != null && GraphManager.Instance != null)
        {
            // Calculamos la ruta con A* desde la posición actual hasta la zona segura
            _fleePath = GraphManager.Instance.FindPath(_hunter.transform.position, nearestSafe.transform.position);
        }
    }

    public void Update()
    {
        if (_isInSafeZone) return;

        // 1. Verificación de llegada física a la Zona Segura utilizando el nodo final del path si existe, o el más cercano
        Node targetNode = (_fleePath != null && _fleePath.Count > 0) ? _fleePath[_fleePath.Count - 1] : _hunter.GetNearestSafeZoneNode();

        if (targetNode != null)
        {
            float distToTarget = Vector3.Distance(_hunter.transform.position, targetNode.transform.position);
            Node currentNearest = GraphManager.Instance.GetNearestNode(_hunter.transform.position);

            if ((currentNearest != null && currentNearest.Zone == ZoneType.SafeZone) || distToTarget <= 2.0f)
            {
                _isInSafeZone = true;
                _hunter.StartCoroutine(SafeZoneWaitRoutine());
                return;
            }
        }

        // 2. Navegación estricta nodo por nodo mediante control local de índice (_currentIndex)
        if (_fleePath != null && _fleePath.Count > 0)
        {
            if (_currentIndex < _fleePath.Count)
            {
                Vector3 targetNodePos = _fleePath[_currentIndex].transform.position;

                // Aplicar Arrive hacia el nodo actual del path (respetando pasillos y paredes)
                Vector3 force = _hunter.Arrive(targetNodePos, 2.0f);
                _hunter.AddForce(force);

                // Control de distancia horizontal XZ para avanzar al siguiente nodo de forma precisa
                Vector3 hunterXZ = new Vector3(_hunter.transform.position.x, 0f, _hunter.transform.position.z);
                Vector3 nodeXZ = new Vector3(targetNodePos.x, 0f, targetNodePos.z);

                if (Vector3.Distance(hunterXZ, nodeXZ) < 2.0f)
                {
                    _currentIndex++;
                }
                return;
            }
        }

        // 3. Respaldo por si el path viene vacío: Arrive directo al nodo seguro más cercano
        Node safeNode = _hunter.GetNearestSafeZoneNode();
        if (safeNode != null)
        {
            Vector3 fallbackForce = _hunter.Arrive(safeNode.transform.position, 3.0f);
            _hunter.AddForce(fallbackForce);
        }
    }

    private IEnumerator SafeZoneWaitRoutine()
    {
        _hunter.ResetVelocity();
        _fleePath = null;
        _currentIndex = 0;

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
        _currentIndex = 0;
        _isInSafeZone = false;
        if (_hunter != null)
        {
            _hunter.FollowPath(null, 0f);
        }
    }
}