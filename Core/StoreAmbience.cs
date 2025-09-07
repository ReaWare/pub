using UnityEngine;

public class StoreAmbience : MonoBehaviour
{
    public static StoreAmbience I { get; private set; }
    [Range(0.6f, 1.4f)] public float cleanliness = 1f;
    [SerializeField] float decayPerSecond = 0.02f;

    void Awake() { if (I && I != this) { Destroy(gameObject); return; } I = this; }
    void Update() { cleanliness = Mathf.Clamp(cleanliness - decayPerSecond * Time.deltaTime, 0.6f, 1.4f); }

    public float BuyMult => cleanliness;               // 0.6..1.4
    public void Clean(float v) => cleanliness = Mathf.Clamp(cleanliness + Mathf.Abs(v), 0.6f, 1.4f);
    public void Dirty(float v) => cleanliness = Mathf.Clamp(cleanliness - Mathf.Abs(v), 0.6f, 1.4f);
}
