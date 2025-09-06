using UnityEngine;

public class StoreAmbience : MonoBehaviour
{
    public static StoreAmbience I { get; private set; }
    [Range(0.6f, 1.4f)] public float cleanliness = 1f;   // 1.0 = base; >1 = meglio

    [SerializeField] float decayPerSecond = 0.02f;        // opzionale
    void Awake() { if (I && I != this) { Destroy(gameObject); return; } I = this; }
    void Update() { cleanliness = Mathf.Clamp(cleanliness - decayPerSecond * Time.deltaTime, 0.6f, 1.4f); }
    public float BuyMult => cleanliness;
    public void Clean(float v) => cleanliness = Mathf.Clamp(cleanliness + v, 0.6f, 1.4f);
}
