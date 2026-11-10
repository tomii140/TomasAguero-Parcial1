using UnityEngine;

public class WorldBounds : MonoBehaviour
{
    public static WorldBounds Instance { get; private set; }

    [Header("Map Limits (XZ Plane)")]
    [SerializeField] private float minX = -25f;
    [SerializeField] private float maxX = 25f;
    [SerializeField] private float minZ = -25f;
    [SerializeField] private float maxZ = 25f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Método para restringir la posición de cualquier agente dentro del mapa
    public Vector3 ClampPosition(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.z = Mathf.Clamp(position.z, minZ, maxZ);
        return position;
    }

    public bool IsOutOfBounds(Vector3 position)
    {
        return position.x < minX || position.x > maxX || position.z < minZ || position.z > maxZ;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
        Vector3 size = new Vector3(maxX - minX, 0.1f, maxZ - minZ);
        Gizmos.DrawWireCube(center, size);
    }
}