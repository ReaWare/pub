using UnityEngine;

public class CustomerImpatience : MonoBehaviour
{
    [Header("Parametri")]
    [SerializeField] float patienceSeconds = 25f;
    [SerializeField, Range(0f, 1f)] float warnAt = 0.5f; // 50%
    [SerializeField, Range(0f, 1f)] float rageAt = 0.95f; // 95%

    float timer; bool inQueue, isFront, warned;
    public void SetInQueue(bool v) { inQueue = v; if (!v) { timer = 0; warned = false; } }
    public void SetIsFront(bool v) { isFront = v; }

    void FixedUpdate()
    {
        if (!inQueue || isFront) return;
        timer += Time.fixedDeltaTime;

        float p = Mathf.Clamp01(timer / patienceSeconds);
        if (!warned && p >= warnAt) { warned = true; /* TODO: bark UI o stress++ */ }
        if (p >= rageAt) RageQuit();
    }

    void RageQuit()
    {
        // TODO: integra con il tuo CustomerController (stato "LeaveStore").
        Destroy(gameObject); // placeholder: despawn
    }
}
