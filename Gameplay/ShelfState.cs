using UnityEngine;

public class ShelfState : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite fullSprite;
    public Sprite messySprite;
    public Sprite emptySprite;

    [Header("Target (lascia vuoto: usa SpriteRenderer locale)")]
    public SpriteRenderer target;

    [Header("State")]
    [Range(0, 1)] public float order = 1f;   // 1 ordinato, 0 disastro
    public int stock = 4;
    public int lowStockThreshold = 1;       // sotto/uguale → mostra MESSY

    void Awake()
    {
        if (!target) target = GetComponent<SpriteRenderer>();
        Refresh();
    }

    void OnValidate()
    {
        if (!target) target = GetComponent<SpriteRenderer>();
        Refresh();
    }

    public void Tidy(float amount = 1f) { order = Mathf.Clamp01(order + amount); Refresh(); }
    public void BrowseDamage(float amount = .25f) { order = Mathf.Clamp01(order - amount); Refresh(); }
    public void TakeOne() { stock = Mathf.Max(0, stock - 1); Refresh(); }
    public void Restock(int amount = 2) { stock += amount; order = 1f; Refresh(); }

    public void Refresh()
    {
        if (!target) return;

        if (stock <= 0)
        {
            target.sprite = emptySprite;
            return;
        }

        // diventa "messy" se poco stock O se disordinato
        bool lowStock = stock <= lowStockThreshold;
        bool disordered = order < 0.4f;

        target.sprite = (lowStock || disordered) ? messySprite : fullSprite;
    }
}
