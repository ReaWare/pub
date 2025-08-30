// Assets/_Project/Scripts/Gameplay/StandPriceProvider.cs
using UnityEngine;

public class StandPriceProvider : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string name;
        public float price = 10f;
        [Range(0f, 1f)] public float weight = 1f;
    }

    [Header("Catalogo (opzionale)")]
    public Entry[] catalog;

    [Header("Se catalogo vuoto → fallback range")]
    public Vector2 priceRange = new Vector2(5f, 30f);

    float _weightSum;

    void OnValidate()
    {
        if (priceRange.x > priceRange.y) (priceRange.x, priceRange.y) = (priceRange.y, priceRange.x);
        RecalcWeight();
    }

    void Awake() { RecalcWeight(); }

    void RecalcWeight()
    {
        _weightSum = 0f;
        if (catalog == null) return;
        foreach (var e in catalog) _weightSum += Mathf.Max(0f, e.weight);
    }

    public float PickRandomPrice()
    {
        if (catalog != null && catalog.Length > 0 && _weightSum > 0f)
        {
            float r = Random.value * _weightSum;
            foreach (var e in catalog)
            {
                r -= Mathf.Max(0f, e.weight);
                if (r <= 0f) return Mathf.Max(0f, e.price);
            }
        }
        return Random.Range(priceRange.x, priceRange.y);
    }
}
