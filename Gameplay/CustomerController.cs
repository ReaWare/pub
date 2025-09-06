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
    private int _stolenCount = 0;
    private bool _warnedNoProvider = false;
    private Coroutine moveRoutine;
    private bool isExiting = false;
    public bool IsExiting => isExiting; // usato dalla cassa per filtrare



    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true; // contatti anche da kinematic
        rb.freezeRotation = true;           // niente rotazioni
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
        int totalStands = (stands != null) ? stands.Length : 0;
        int n = 0;
        if (totalStands > 0)
        {
            n = Mathf.Clamp(
                Random.Range(archetype.standsToVisit.x, archetype.standsToVisit.y + 1),
                0, Mathf.Max(1, totalStands)
            );
        }

        List<Transform> pool = new List<Transform>(stands ?? System.Array.Empty<Transform>());
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

            // muoviti verso lo stand con un po' di jitter
            Vector3 spot = target.position + (Vector3)(Random.insideUnitCircle * standJitter);
            yield return MoveTo(spot);
            yield return new WaitForSeconds(0.4f);

            // chance di prendere un item dal punto visitato (con cleanliness + stato scaffale)
            var item = target.GetComponentInParent<Item>() ?? target.GetComponentInChildren<Item>();
            bool buy = DecidePurchase(archetype.buyChance, target);

            if (buy && item)
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
                if (!it) continue;
                loss += it.price;
                picked++;
            }

            if (loss > 0)
            {
                Wallet.I.Add(-loss);
                PopupFloatingText.I?.ShowLoss(loss, transform.position);
            }

            yield return Exit();
            yield break;
        }

        // --- PAGA O ESCI ---
        if (Cart.Count > 0)
        {
            WantsToPay = true;
            
            var reg = cashierPoint ? cashierPoint.GetComponentInParent<CashRegister>() : null;
            if (reg != null)
            {
                myRegister = reg;
                reg.JoinQueue(this); 
                Debug.Log($"[Customer] {name} wants to pay. Cart={Cart.Count} -> join {reg?.name}");
                yield break; // ora la coda gestisce i movimenti
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
    }

    public void ClearCartAndExit()
    {
        Debug.Log($"[Customer] ClearCartAndExit START -> {name}");

        if (myRegister != null) myRegister.LeaveQueue(this);

        WantsToPay = false;
        Cart.Clear();

        isExiting = true;                        // (se hai aggiunto il flag come ti avevo proposto)
        if (moveRoutine != null) { StopCoroutine(moveRoutine); moveRoutine = null; }

        SetGhostMode(true);

        StartCoroutine(Exit());
    }



    IEnumerator Exit()
    {
        Debug.Log($"[Customer] Exit START -> {name}");

        const float TIMEOUT = 6f;
        if (exitPoint)
            yield return MoveToWithTimeout(exitPoint.position, TIMEOUT);

        Debug.Log($"[Customer] Exit DONE -> {name}");
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
            if (to.sqrMagnitude <= stopSqr) break;

            Vector2 dir = to.normalized;
            float step = Mathf.Abs(archetype.walkSpeed * speedMul) * Time.fixedDeltaTime;

            if (rb) rb.MovePosition(cur + dir * step);
            else transform.position = cur + dir * step;

            yield return wait;
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
            TheftLedger.Add(value);
            WantsToPay = false;
            speedMul = Mathf.Max(1f, archetype.thiefRunMultiplier);
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
            if (Time.time >= deadline) break;

            Vector2 dir = to.normalized;
            float step = Mathf.Abs(archetype.walkSpeed * speedMul) * Time.fixedDeltaTime;

            if (rb) rb.MovePosition(cur + dir * step);
            else transform.position = cur + dir * step;

            yield return wait;
        }
    }

    public void Queue_MoveTo(Vector3 pos)
    {
        if (isExiting) { Debug.Log($"[Customer] IGNORE Queue_MoveTo (exiting) -> {name}"); return; }
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveTo(pos));
    }



    void OnDestroy()
    {
        if (CustomerLimiter.I != null)
            CustomerLimiter.I.NotifyDestroyed(this);
    }

    void Log(string m) => Debug.Log($"[Customer:{name}] {m}");

    // --- Decisione d'acquisto (usa StoreAmbience + ShelfState) ---
    bool DecidePurchase(float archetypeBuyChance, Transform stand)
    {
        // trova lo ShelfState sullo stand (o parent/child)
        ShelfState shelf = null;
        if (stand)
        {
            shelf = stand.GetComponent<ShelfState>()
                 ?? stand.GetComponentInParent<ShelfState>()
                 ?? stand.GetComponentInChildren<ShelfState>();
        }

        // moltiplicatori: globale (pulizia) + locale (ordine/stock)
        float globalMult = StoreAmbience.I ? StoreAmbience.I.BuyMult : 1f;

        float localMult = 1f;
        if (shelf)
        {
            localMult *= Mathf.Lerp(0.85f, 1.15f, shelf.order);
            if (shelf.stock == 0) localMult *= 0.6f;
        }

        bool buy = Random.value < (archetypeBuyChance * globalMult * localMult);

        // feedback sullo scaffale
        if (shelf)
        {
            if (buy) shelf.TakeOne();
            else shelf.BrowseDamage(0.25f);
            shelf.Refresh();
        }

        return buy;
    }
}
