using UnityEngine;

public class ShelfState : MonoBehaviour
{
    [Header("Rendering (per conteggio)")]
    [Tooltip("Sprite FULL per index = numero capi visibili (0..capacity)")]
    public Sprite[] fullByCount;      // es: [0=emptyFull, 1, 2, 3, 4]
    [Tooltip("Sprite MESSY per index = numero capi visibili (0..capacity)")]
    public Sprite[] messyByCount;     // es: [0=emptyMessy, 1, 2, 3, 4]
    [Tooltip("Se vuoto, usa lo SpriteRenderer sullo stesso GO")]
    public SpriteRenderer target;

    [Header("Stato")]
    [Min(0)] public int capacity = 4;      // quanti posti sullo stand
    [Min(0)] public int stock = 4;         // quanti capi esposti (0..capacity)
    [Range(0f, 1f)] public float order = 1; // 1 = lindo, 0 = disastro
    [Range(0f, 1f)] public float messyThreshold = 0.6f; // sotto → mostra messy

    void Awake() { if (!target) target = GetComponent<SpriteRenderer>(); Clamp(); Refresh(); }
    void OnValidate() { if (!target) target = GetComponent<SpriteRenderer>(); Clamp(); Refresh(); }

    void Clamp()
    {
        capacity = Mathf.Max(0, capacity);
        stock = Mathf.Clamp(stock, 0, capacity);
        order = Mathf.Clamp01(order);
    }

    public void Refresh()
    {
        if (!target) return;

        int idx = Mathf.Clamp(stock, 0, capacity);
        bool messy = (order < messyThreshold);

        var arr = messy ? messyByCount : fullByCount;
        if (arr != null && idx >= 0 && idx < arr.Length && arr[idx] != null)
            target.sprite = arr[idx];
    }

    // ---- API usate da player/clienti ----
    public bool TakeOne() { if (stock <= 0) return false; stock--; Refresh(); return true; }
    public void PutBack(bool tidy) { stock = Mathf.Min(capacity, stock + 1); order = Mathf.Clamp01(order + (tidy ? +0.25f : -0.20f)); Refresh(); }
    public void BrowseDamage(float a = 0.20f) { order = Mathf.Clamp01(order - Mathf.Abs(a)); Refresh(); }
    public void Tidy(float a = 0.50f) { order = Mathf.Clamp01(order + Mathf.Abs(a)); Refresh(); }
    public void Restock(int amount) { stock = Mathf.Clamp(stock + Mathf.Max(0, amount), 0, capacity); Refresh(); }






#if UNITY_EDITOR
[ContextMenu("Debug/Force Messy")]
void ForceMessy() { order = 0f; Refresh(); }

[ContextMenu("Debug/Force Full")]
void ForceFull() { order = 1f; Refresh(); }

[ContextMenu("Debug/Log State")]
void LogState() {
    Debug.Log($"[ShelfState] cap={capacity} stock={stock} order={order:0.00} thr={messyThreshold:0.00} bank={(order<messyThreshold?"MESSY":"FULL")} idx={Mathf.Clamp(stock,0,capacity)} sprite={(target?target.sprite?.name:"<none>")}");
}
#endif

}





