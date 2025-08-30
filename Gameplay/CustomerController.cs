using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CustomerController : MonoBehaviour
{
    public CustomerArchetype archetype;
    public Transform entryPoint;
    public Transform exitPoint;
    public Transform[] stands;
    public Transform cashierPoint; // punto davanti alla cassa (sul trigger)
    public float stopDistance = 0.05f;
    public float standJitter = 0.12f;   // caos davanti agli stand
    public Vector2 cashierJitter = new Vector2(0.12f, 0.06f); // coda in cassa
    private CashRegister myRegister;
    float speedMul = 1f;
    public bool IsThief { get; private set; }
    private int _stolenCount = 0;          // persiste tra uno stand e l’altro
    private bool _warnedNoProvider = false;





    public List<Item> Cart = new List<Item>();
    public bool WantsToPay { get; private set; }

    Rigidbody2D rb;

    void Awake() { rb = GetComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic; }

    IEnumerator Start()
    {
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
        if (Cart.Count > 0 && Random.value < archetype.stealChance)
        {
            IsThief = true;
            speedMul = Mathf.Max(1f, archetype.thiefRunMultiplier);

            int toSteal = Mathf.Clamp(Random.Range(archetype.stealItems.x, archetype.stealItems.y + 1), 1, Cart.Count);
            int loss = 0;
            for (int i = 0; i < toSteal; i++) loss += Cart[i].price;

            Wallet.I.Add(-loss);
            PopupFloatingText.I?.ShowLoss(loss, transform.position);

            // scappa subito
            yield return Exit();
            yield break;
        }


        if (Cart.Count > 0 && Random.value <= 0.9f)
        {
            WantsToPay = true;
            var reg = cashierPoint ? cashierPoint.GetComponentInParent<CashRegister>() : null;
            if (reg != null)
            {
                myRegister = reg;          // <— salva la cassa
                reg.JoinQueue(this);
            }
            else
            {
                // fallback...
                Vector3 paySpot = cashierPoint.position + new Vector3(
                    Random.Range(-cashierJitter.x, cashierJitter.x),
                    Random.Range(-cashierJitter.y, cashierJitter.y), 0f);
                yield return MoveTo(paySpot);
            }
        }
        else { yield return Exit(); }

    }

    public void ClearCartAndExit()
    {
        // se ero in coda, esci dalla coda
        if (myRegister != null) myRegister.LeaveQueue(this);
        Cart.Clear();
        StartCoroutine(Exit());
    }

    IEnumerator Exit()
    {
        WantsToPay = false;
        if (exitPoint) yield return MoveTo(exitPoint.position);
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






    Coroutine moveRoutine;
    public void Queue_MoveTo(Vector3 pos)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveTo(pos));
    }


    void Log(string m) => Debug.Log($"[Customer:{name}] {m}");
}
