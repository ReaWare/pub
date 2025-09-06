using UnityEngine;
using UnityEngine.Tilemaps;

/// Movimento a griglia 4-direzioni, una cella per input.
[RequireComponent(typeof(Transform))]
public class PlayerGridMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Grid grid;
    [SerializeField] Tilemap walkableTilemap;   // es. Floor
    [SerializeField] Tilemap blockedTilemap;    // es. Walls/Obstacles

    [Header("Movement")]
    [SerializeField, Min(0.5f)] float moveSpeed = 6f; // velocità verso il centro cella
    [SerializeField] bool requireWalkableTile = true; // muovi solo dove c’è il tile di Floor

    Rigidbody2D rb;
    bool isMoving;
    Vector3 targetWorld;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!grid) grid = GetComponentInParent<Grid>();
    }

    void Start()
    {
        // Snap iniziale al centro cella
        var cell = grid.WorldToCell(transform.position);
        targetWorld = grid.GetCellCenterWorld(cell);
        if (rb) rb.position = targetWorld;
        else transform.position = targetWorld;
    }

    void Update()
    {
        if (isMoving) return;

        Vector2Int dir = ReadDiscreteInput();
        if (dir == Vector2Int.zero) return;

        var current = grid.WorldToCell(transform.position);
        var dest = current + new Vector3Int(dir.x, dir.y, 0);

        if (!IsCellWalkable(dest)) return;

        targetWorld = grid.GetCellCenterWorld(dest);
        isMoving = true;
    }

    void FixedUpdate()
    {
        if (!isMoving) return;

        var pos = (Vector3)(rb ? (Vector2)rb.position : transform.position);
        var next = Vector3.MoveTowards(pos, targetWorld, moveSpeed * Time.fixedDeltaTime);

        if (rb) rb.MovePosition(next);
        else transform.position = next;

        if ((next - targetWorld).sqrMagnitude <= 0.0001f)
            isMoving = false;
    }

    Vector2Int ReadDiscreteInput()
    {
        // Priorità orizzontale per evitare diagonali
        int x = (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) ? 1
              : (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) ? -1 : 0;

        if (x != 0) return new Vector2Int(x, 0);

        int y = (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) ? 1
              : (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) ? -1 : 0;

        return new Vector2Int(0, y);
    }

    bool IsCellWalkable(Vector3Int cell)
    {
        // Bloccato se esiste un tile su blocked
        if (blockedTilemap && blockedTilemap.HasTile(cell)) return false;

        // Se richiedi camminabilità “esplicita”, deve esserci un tile su walkable
        if (requireWalkableTile && walkableTilemap)
            return walkableTilemap.HasTile(cell);

        // Altrimenti libero
        return true;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!grid) return;
        Gizmos.matrix = grid.transform.localToWorldMatrix;
        var cell = grid.WorldToCell(transform.position);
        var center = grid.GetCellCenterWorld(cell);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(grid.cellSize.x, grid.cellSize.y, 0.01f));
    }
#endif
}
