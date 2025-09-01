using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CustomerController : MonoBehaviour
{
    [Header("Setup")]
    public CustomerArchetype archetype;
    public Transform entryPoint;
    public Transform exitPoint;
    public Transform[] stands;
    public Transform cashierPoint; // punto davanti alla cassa (sul trigger)

    [Header("Movimento")]
    public float stopDistance = 0.05f;
    public float standJitter = 0.12f;                 // caos davanti agli stand
    public Vector2 cashierJitter = new Vector2(0.12f, 0.06f); // coda in cassa

    [Header("Runtime")]
    public List<Item> Cart = new List<Item>();
    public bool WantsToPay { get; private set; }
    public bool IsThief { get; private set; }

    private CashRegister myRegister;
    private Rigidbody2D rb;
    private float speedMul = 1f;
    private int _stolenCount = 0;          // persiste tra uno stand e l’altro
    private bool _warnedNoProvider = false;
    private Coroutine moveRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    IEnumerator Start()
    {
        // Limite globale: se oltre quota/concorrenza, autodistruggiti
        if (CustomerLimiter.I != null && !CustomerLimiter.I.TryAdmit(this))
        {
            Destroy(gameObject);
            yield break;
        }

        // entra
        if (entryPoint) transform.position = entryPoint.position;

        // visita N stand
        int n = Mathf.Clamp(
            Random.Range(archetype.standsToVisit.x, archetype.standsToVisit.y + 1),
            0, Mathf.Max(1, stands.Length)
        );

        List<Transform> pool = new List<Transform>(stands);
        for (int i = 0; i < n; i++)
        {
            Transform target;
            if (archetype.allowRevisit)
            {
                target = stands[Random.Range(0, stands.Length)];
            }
            else
            {
                if (pool.Count == 0) break;
                int idx = Random.Range(0, pool.Count);
                target = pool[idx];
                pool.RemoveAt(idx);
            }

            Vector3 spot = target.position + (Vector3)(Random.insideUnitCircle * standJitter);
            yield return MoveTo(spot);
            yield return new WaitForSeconds(0.4f);

            // chance di prendere un item dal punto visitato
            var item = target.GetComponentInParent<Item>() ?? target.GetComponentInChildren<Item>();
            if (item && Random.value <= archetype.buyChance)
                Cart.Add(item);
        }

        // --- KLEPTO: decide di rubare? ---
        if (Cart.Count > 0 && archetype != null && Random.value < archetype.stealChance)
        {
            IsThief = true;
            speedMul = Mathf.Max(1f, archetype.thiefRunMultiplier);

            int maxSteal = Mathf.Clamp(archetype.stealItems.y, 0, Cart.Count);
            int minSteal = Mathf.Clamp(archetype.stealItems.x, 0, maxSteal);
            int toSteal = Mathf.Clamp(Random.Range(minSteal, maxSteal + 1), 0, Cart.Count);

            int loss = 0, picked = 0;
            for (int i = 0; i < Cart.Count && picked < toSteal; i++)
            {
                var it = Cart[i];
                if (!it) continue; // evita NRE se l'Item è stato distrutto
                loss += it.price;
                picked++;
            }

            if (loss > 0)
            {
                Wallet.I.Add(-loss);
                PopupFloatingText.I?.ShowLoss(loss, transform.position);
            }

            yield return Exit();
            yield break; // chiudi la coroutine qui
        }

        // --- PAGA O ESCI ---
        if (Cart.Count > 0)
        {
            WantsToPay = true;
            var reg = cashierPoint ? cashierPoint.GetComponentInParent<CashRegister>() : null;
            if (reg != null)
            {
                myRegister = reg;          // salva la cassa
                reg.JoinQueue(this);
                yield break;               // attendi la coda; fine coroutine
            }
            else if (cashierPoint != null)
            {
                // fallback: avvicinati alla zona cassa
                Vector3 paySpot = cashierPoint.position + new Vector3(
                    Random.Range(-cashierJitter.x, cashierJitter.x),
                    Random.Range(-cashierJitter.y, cashierJitter.y), 0f);
                yield return MoveTo(paySpot);
                yield break;
            }
        }

        // nessun carrello o niente cassa → esci
        yield return Exit();
        yield break;
    }

    public void ClearCartAndExit()
    {
        // esci dalla coda se agganciato
        if (myRegister != null) myRegister.LeaveQueue(this);

        WantsToPay = false;
        Cart.Clear();

        // diventa "fantasma" per non incastrarsi con colliders
        SetGhostMode(true);

        StartCoroutine(Exit());
    }

    IEnumerator Exit()
    {
        const float TIMEOUT = 6f;
        if (exitPoint)
            yield return MoveToWithTimeout(exitPoint.position, TIMEOUT);

        Destroy(gameObject);
    }

    IEnumerator MoveTo(Vector3 target)
    {
        var wait = new WaitForFixedUpdate();
        float stopSqr = stopDistance * stopDistance;

        while (true)
        {
            Vector2 cur = rb ? rb.position : (Vector2)transform.position;
            Vector2 to = (Vector2)target - cur;
            float d2 = to.sqrMagnitude;
            if (d2 <= stopSqr) break;

            Vector2 dir = to.normalized;
            float step = Mathf.Abs(archetype.walkSpeed * speedMul) * Time.fixedDeltaTime;

            if (rb) rb.MovePosition(cur + dir * step);
            else transform.position = cur + dir * step;

            yield return wait; // muoviti a cadenza fisica
        }
    }

    void MaybeStealAtStand(Transform standPoint)
    {
        if (!standPoint) return;
        if (_stolenCount >= archetype.stealItems.y) return;

        float security = SecurityManager.I?.GetSecurityFactor(transform.position) ?? 0f;
        float effectiveChance = Mathf.Clamp01(archetype.stealChance * (1f - security));
        if (Random.value >= effectiveChance) return;

        var provider = standPoint.GetComponentInParent<StandPriceProvider>();
        if (!provider)
        {
            if (!_warnedNoProvider)
            {
                Debug.LogWarning($"[Customer] Nessun StandPriceProvider su/attorno a {standPoint.name}");
                _warnedNoProvider = true;
            }
            return;
        }

        int toSteal = Random.Range(archetype.stealItems.x, archetype.stealItems.y + 1);
        float value = 0f;

        for (int i = 0; i < toSteal; i++)
        {
            value += provider.PickRandomPrice();
            _stolenCount++;
            if (_stolenCount >= archetype.stealItems.y) break;
        }

        if (value > 0f)
        {
            TheftLedger.Add(value);                                  // registra perdita
            WantsToPay = false;                                      // se ruba, non paga
            speedMul = Mathf.Max(1f, archetype.thiefRunMultiplier);  // scappa più veloce
        }
    }

    void SetGhostMode(bool on)
    {
        var cols = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
            cols[i].isTrigger = on;
    }

    IEnumerator MoveToWithTimeout(Vector3 target, float timeoutSeconds)
    {
        var wait = new WaitForFixedUpdate();
        float stopSqr = stopDistance * stopDistance;
        float deadline = Time.time + Mathf.Max(0.1f, timeoutSeconds);

        while (true)
        {
            Vector2 cur = rb ? rb.position : (Vector2)transform.position;
            Vector2 to = (Vector2)target - cur;
            if (to.sqrMagnitude <= stopSqr) break;
            if (Time.time >= deadline) break;  // failsafe

            Vector2 dir = to.normalized;
            float step = Mathf.Abs(archetype.walkSpeed * speedMul) * Time.fixedDeltaTime;

            if (rb) rb.MovePosition(cur + dir * step);
            else transform.position = cur + dir * step;

            yield return wait;
        }
    }

    public void Queue_MoveTo(Vector3 pos)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveTo(pos));
    }

    void OnDestroy()
    {
        if (CustomerLimiter.I != null)
            CustomerLimiter.I.NotifyDestroyed(this);
    }

    void Log(string m) => Debug.Log($"[Customer:{name}] {m}");
}
