using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CashRegister : MonoBehaviour
{
    [Header("Prossimità (Player → opzionale)")]
    public bool requireProximity = true;
    public string playerTag = "Player";
    bool _playerInRange;

    [Header("Coda")]
    public Transform[] queueSlots;           // 0 = davanti
    public Transform cashierPoint;         // se null usa slot 0
    public float serveDistance = 0.10f;

    [Header("Audio")]
    [SerializeField] AudioSource audioSrc;
    [SerializeField] AudioClip sfxDing;
    [SerializeField] AudioClip sfxError;
    [SerializeField] float sfxMinInterval = 0.12f;

    [Header("Anti-spam")]
    [SerializeField] float interactCooldownSeconds = 0.15f;
    [SerializeField] bool autoKickInvalidHead = true;
    [SerializeField] float sweepEvery = 0.5f;
    float _nextSweep;


    readonly List<CustomerController> queue = new();
    bool isBusy = false;
    float interactCooldown = 0f;
    float _lastSfxTime = -1f;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Awake()
    {
        if (!audioSrc) audioSrc = GetComponent<AudioSource>();
        if (!cashierPoint && queueSlots != null && queueSlots.Length > 0)
            cashierPoint = queueSlots[0];
    }

    void Update()
    {
        if (interactCooldown > 0f) interactCooldown -= Time.deltaTime;
        if (autoKickInvalidHead && Time.time >= _nextSweep)
        {
            _nextSweep = Time.time + sweepEvery;
            SweepQueueHead(); // caccia chi sta in testa ma non pagherà mai
        }
    }

    // --- Prossimità Player (se attiva) ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (requireProximity && other.CompareTag(playerTag)) _playerInRange = true;
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (requireProximity && other.CompareTag(playerTag)) _playerInRange = false;
    }

    // ================= API clienti =================
    public void JoinQueue(CustomerController c)
    {
        if (!c) return;
        Debug.Log($"[Register] JoinQueue REQ from {c.name} (wants={c.WantsToPay}, cart={GetCartTotal(c)})");
        if (queue.Contains(c)) return;

        // (se vuoi far entrare tutti, anche cart=0, non mettere guardie qui)
        queue.Add(c);
        RepositionQueue();
        Debug.Log($"[Register] JoinQueue OK. count={queue.Count}");
    }

    // ===============================================

    // ================ Input (Player) ================
    public bool TryCheckout()
    {
        if (requireProximity && !_playerInRange) return false;
        if (interactCooldown > 0f || isBusy) return false;

        interactCooldown = interactCooldownSeconds;

        if (!HasServeableCustomer())
        {
            BeepError();
            return false;
        }

        DoCheckout();
        return true;
    }
    // ===============================================

    // Cliente servibile adesso?
    public bool HasServeableCustomer()
    {
        SweepQueueHead();                 // tieni pulita la testa fila
        if (queue.Count == 0) return false;

        // prendi la testa della fila
        var c = queue[0];
        if (!c) { queue.RemoveAt(0); return false; }
        if (!c.WantsToPay) return false;
        if (GetCartTotal(c) <= 0) return false;

        // punto del cliente: usa i PIEDI se c'è un Collider2D, altrimenti il transform
        Vector2 pCustomer = (Vector2)c.transform.position;
        var col = c.GetComponent<Collider2D>();
        if (col != null)
        {
            var b = col.bounds;                          // world bounds
            pCustomer = new Vector2(b.center.x, b.min.y); // X al centro, Y ai piedi
        }

        // confronto con il cashierPoint (o slot0) in 2D
        Vector2 pService = cashierPoint ? (Vector2)cashierPoint.position
                                        : (queueSlots != null && queueSlots.Length > 0 ? (Vector2)queueSlots[0].position
                                                                                       : pCustomer);

        float d2 = (pCustomer - pService).sqrMagnitude;
        return d2 <= serveDistance * serveDistance;
    }

    // CashRegister.cs
    void SweepQueueHead()
    {
        bool changed = false;

        // ripulisci solo la testa finché è invalida
        while (queue.Count > 0)
        {
            var c = queue[0];
            if (!c)
            {
                // già distrutto altrove
                queue.RemoveAt(0);
                changed = true;
                continue;
            }

            int tot = GetCartTotal(c);

            // non pagherà mai: mandalo via e togli DOVUNQUE in modo idempotente
            if (!c.WantsToPay || tot <= 0)
            {
                c.ClearCartAndExit();        // può chiamare LeaveQueue() → Remove(c)
                queue.Remove(c);             // se è ancora in testa, rimuovi; se è già stato tolto: no-op
                changed = true;
                continue;
            }

            // primo valido → stop
            break;
        }

        if (changed) RepositionQueue();
    }



    // alias legacy per codice vecchio
    public bool HasReadyCustomer() => HasServeableCustomer();


    public event System.Action OnCheckoutSuccess;

    // Incasso
    void DoCheckout()
    {
        isBusy = true;

        var c = queue[0];
        int total = GetCartTotal(c);
        if (total <= 0)
        {
            BeepError();
            isBusy = false;
            return;
        }

        // 💰 incasso
        Wallet.I.Add(total);
        PlayDing();

        // rimuovi dalla coda e fai uscire
        queue.RemoveAt(0);
        if (c) c.ClearCartAndExit();

        // avanza gli altri
        RepositionQueue();

        isBusy = false;
        OnCheckoutSuccess?.Invoke();
    }

    void RepositionQueue()
    {
        if (queueSlots == null || queueSlots.Length == 0) return;

        for (int i = 0; i < queue.Count; i++)
        {
            var c = queue[i];
            if (!c) continue;

            var slot = i < queueSlots.Length ? queueSlots[i] : queueSlots[^1];
            if (!slot) continue;

            // niente jitter sul primo: deve stare proprio sul punto di servizio
            Vector3 pos = slot.position;
            if (i > 0)
            {
                Vector2 j = c.cashierJitter;
                pos += new Vector3(Random.Range(-j.x, j.x), 0f, 0f); // Y=0
            }

            c.Queue_MoveTo(pos);
        }
    }

    // Può parlare con il primo? (nessun pagamento da fare ma è in raggio)
    public bool HasTalkableHead()
    {
        if (queue.Count == 0) return false;
        var c = queue[0];
        if (!c) return false;

        // se è servibile (vuole pagare e ha carrello > 0), NON è "parlante"
        if (c.WantsToPay && GetCartTotal(c) > 0) return false;

        return HeadInRange(c);
    }

    // Azione primaria: prima prova a incassare, sennò parla
    public void InteractPrimary()
    {
        if (requireProximity && !_playerInRange) { BeepError(); return; }
        if (interactCooldown > 0f || isBusy) return;
        interactCooldown = interactCooldownSeconds;

        if (HasServeableCustomer()) { DoCheckout(); return; }
        if (HasTalkableHead()) { BeginTalk(); return; }

        BeepError();
    }

    // Placeholder per i dialoghi (per ora solo un log)
    public void BeginTalk()
    {
        var c = (queue.Count > 0) ? queue[0] : null;
        if (!c) return;

        // Qui in futuro: DialogueSystem.StartConversation(c)
        Debug.Log($"[Register] TALK with {c.name} (placeholder)");
    }

    // --- helper distanza “a livello piedi” ---
    bool HeadInRange(CustomerController c)
    {
        Vector2 pCustomer = (Vector2)c.transform.position;
        var col = c.GetComponent<Collider2D>();
        if (col) pCustomer = new Vector2(col.bounds.center.x, col.bounds.min.y);

        Vector2 pService = cashierPoint
            ? (Vector2)cashierPoint.position
            : (queueSlots != null && queueSlots.Length > 0 ? (Vector2)queueSlots[0].position : pCustomer);

        return (pCustomer - pService).sqrMagnitude <= serveDistance * serveDistance;
    }






    public void LeaveQueue(CustomerController c)
    {
        if (!c) return;
        // idempotente: anche se non è in lista non succede nulla
        queue.Remove(c);
        RepositionQueue();
    }


    int GetCartTotal(CustomerController c)
    {
        if (c == null || c.Cart == null) return 0;
        int sum = 0;
        for (int i = 0; i < c.Cart.Count; i++)
        {
            var item = c.Cart[i];
            if (item != null) sum += item.price;
        }
        return sum;
    }

    void PlayDing()
    {
        if (!audioSrc || !sfxDing) return;
        if (Time.time - _lastSfxTime < sfxMinInterval) return;
        _lastSfxTime = Time.time;
        audioSrc.PlayOneShot(sfxDing);
    }

    void BeepError()
    {
        if (!audioSrc || !sfxError) return;
        if (Time.time - _lastSfxTime < sfxMinInterval) return;
        _lastSfxTime = Time.time;
        audioSrc.PlayOneShot(sfxError);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (cashierPoint)
        {
            Gizmos.color = new Color(0, 1, 0, 0.35f);
            Gizmos.DrawWireSphere(cashierPoint.position, serveDistance);
        }
    }
#endif
}
