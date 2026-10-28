using System.Collections.Generic;
using UnityEngine;

public class GraphManager : MonoBehaviour
{
    public static GraphManager Instance { get; private set; }

    [Header("Lista de Nodos del Mapa")]
    [SerializeField] private List<Node> allNodes = new List<Node>();

    private int currentSearchID = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos, bool excludeSafeZones = false)
    {
        Node startNode = GetNearestNode(startPos);
        Node targetNode = GetNearestNode(targetPos);

        if (startNode == null || targetNode == null) return null;
        if (excludeSafeZones && targetNode.Zone == ZoneType.SafeZone) return null;

        currentSearchID++; // Incrementamos el ID para evitar recorrer toda la lista de nodos

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.GCost = 0;
        startNode.HCost = Vector3.Distance(startNode.transform.position, targetNode.transform.position);
        startNode.Parent = null;
        startNode.LastSearchID = currentSearchID;

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            int currentIndex = 0;

            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost ||
                   (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                    currentIndex = i;
                }
            }

            openSet.RemoveAt(currentIndex);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (var neighbor in currentNode.Neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor)) continue;

                // Si es un lobo buscando y el vecino es Zona Segura, lo salta
                if (excludeSafeZones && neighbor.Zone == ZoneType.SafeZone) continue;

                float tentativeGCost = currentNode.GCost + Vector3.Distance(currentNode.transform.position, neighbor.transform.position);

                // Inicializar nodo si es la primera vez que se visita en esta búsqueda
                if (neighbor.LastSearchID != currentSearchID)
                {
                    neighbor.GCost = float.MaxValue;
                    neighbor.LastSearchID = currentSearchID;
                }

                if (tentativeGCost < neighbor.GCost)
                {
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = Vector3.Distance(neighbor.transform.position, targetNode.transform.position);
                    neighbor.Parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode && currentNode != null)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }
        path.Reverse();
        return path;
    }

    public Node GetNearestNode(Vector3 position)
    {
        Node nearest = null;
        float minDistance = float.MaxValue;

        foreach (var node in allNodes)
        {
            float dist = Vector3.Distance(position, node.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = node;
            }
        }
        return nearest;
    }
}