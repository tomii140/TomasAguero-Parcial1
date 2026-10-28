using System.Collections.Generic;
using UnityEngine;

public enum ZoneType
{
    Standard,
    InitialZone,
    HuntingZone,
    SafeZone,
    ExitZone
}

public class Node : MonoBehaviour
{
    [Header("Configuración de Zona")]
    [SerializeField] private ZoneType zoneType = ZoneType.Standard;
    public ZoneType Zone => zoneType;

    [Header("Conexiones del Grafo")]
    [SerializeField] private List<Node> neighbors = new List<Node>();
    public List<Node> Neighbors => neighbors;

    // Variables para A*
    public float GCost { get; set; }
    public float HCost { get; set; }
    public float FCost => GCost + HCost;
    public Node Parent { get; set; }
    public int LastSearchID { get; set; } = -1; // Clave para evitar recorrer allNodes en cada frame de búsqueda

    private void OnDrawGizmos()
    {
        // Color según el tipo de zona para feedback visual
        switch (zoneType)
        {
            case ZoneType.InitialZone: Gizmos.color = Color.green; break;
            case ZoneType.HuntingZone: Gizmos.color = Color.yellow; break;
            case ZoneType.SafeZone: Gizmos.color = Color.magenta; break;
            case ZoneType.ExitZone: Gizmos.color = Color.blue; break;
            default: Gizmos.color = Color.cyan; break;
        }

        Gizmos.DrawSphere(transform.position, 0.3f);

        Gizmos.color = Color.gray;
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null)
            {
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
        }
    }
}