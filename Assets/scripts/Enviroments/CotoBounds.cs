using UnityEngine;

public class CotoBounds : MonoBehaviour
{
    public static CotoBounds Instance { get; private set; }

    [Header("Configuración del Recinto")]
    [SerializeField] private Transform centerNode; // El transform del nodo del medio
    [SerializeField] private float radiusX = 4f;   // Distancia/radio en el eje X
    [SerializeField] private float radiusZ = 4f;   // Distancia/radio en el eje Z
    [SerializeField] private float boundaryForce = 30f;

    [Header("Debug Visual")]
    [SerializeField] private bool showGizmos = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public float MinX => centerNode != null ? centerNode.position.x - radiusX : -10f;
    public float MaxX => centerNode != null ? centerNode.position.x + radiusX : 10f;
    public float MinZ => centerNode != null ? centerNode.position.z - radiusZ : -10f;
    public float MaxZ => centerNode != null ? centerNode.position.z + radiusZ : 10f;
    public float Force => boundaryForce;

    public Vector3 GetBoundaryCorrection(Vector3 currentPosition)
    {
        Vector3 correction = Vector3.zero;

        if (currentPosition.x < MinX) correction += Vector3.right * boundaryForce * (MinX - currentPosition.x);
        if (currentPosition.x > MaxX) correction += Vector3.left * boundaryForce * (currentPosition.x - MaxX);
        if (currentPosition.z < MinZ) correction += Vector3.forward * boundaryForce * (MinZ - currentPosition.z);
        if (currentPosition.z > MaxZ) correction += Vector3.back * boundaryForce * (currentPosition.z - MaxZ);

        return correction;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || centerNode == null) return;

        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3(centerNode.position.x, centerNode.position.y, centerNode.position.z);
        Vector3 size = new Vector3(radiusX * 2f, 1f, radiusZ * 2f);
        Gizmos.DrawWireCube(center, size);
    }
}