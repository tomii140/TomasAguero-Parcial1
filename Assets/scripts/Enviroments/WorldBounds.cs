using UnityEngine;

public class WorldBounds : MonoBehaviour
{
    public static WorldBounds Instance { get; private set; }

    [Header("Map Boundaries XZ Plane")]
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 20f;
    [SerializeField] private float minZ = -20f;
    [SerializeField] private float maxZ = 20f;

    public float MinX => minX;
    public float MaxX => maxX;
    public float MinZ => minZ;
    public float MaxZ => maxZ;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Vector3 ClampPosition(ref Vector3 position, ref Vector3 velocity)
    {
        if (position.x < minX) { position.x = minX; velocity.x = -velocity.x; }
        else if (position.x > maxX) { position.x = maxX; velocity.x = -velocity.x; }

        if (position.z < minZ) { position.z = minZ; velocity.z = -velocity.z; }
        else if (position.z > maxZ) { position.z = maxZ; velocity.z = -velocity.z; }

        return position;
    }

    public Vector3 WrapPosition(Vector3 position)
    {
        float offset = 0.5f;
        bool wrapped = false;
        Vector3 oldPos = position;

        if (position.x < minX)
        {
            position.x = maxX - offset;
            wrapped = true;
        }
        else if (position.x > maxX)
        {
            position.x = minX + offset;
            wrapped = true;
        }

        if (position.z < minZ)
        {
            position.z = maxZ - offset;
            wrapped = true;
        }
        else if (position.z > maxZ)
        {
            position.z = minZ + offset;
            wrapped = true;
        }

        if (wrapped)
        {
            Debug.Log($"<color=green>[WRAP EXITOSO]</color> De {oldPos} a nueva pos: {position}");
        }

        return position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
        Vector3 size = new Vector3(maxX - minX, 1f, maxZ - minX);
        Gizmos.DrawWireCube(center, size);
    }
}