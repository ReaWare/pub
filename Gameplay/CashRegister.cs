using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CashRegister : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Tasto per incassare (letto dal Player, non qui)")]
    public KeyCode interactKey = KeyCode.E; // lasciato per reference, l'input è centralizzato nel Player
    int _lastCheckoutFrame = -1;
    float _lastSfxTime = -1f;

    [Header("Prossimità")]
    [Tooltip("Incassa solo se il Player è dentro il trigger della cassa")]
    public bool requireProximity = true;
    [Tooltip("Tag del Player che deve entrare nel trigger")]
    public string playerTag = "Player";
    bool _playerInRange;

    [Header("Coda")]
    [Tooltip("Slot 0 = davanti alla cassa (dentro il trigger)")]
    public Transform[] queueSlots;     // Slot0, Slot1, Slot2...
    [Tooltip("Se vuoto usa queueSlots[0] (opzionale)")]
    public Transform cashierPoint;

    [Header("Audio")]
    [SerializeField] AudioSource audioSrc;
    [SerializeField] AudioClip sfxDing;    // ka-ching
    [SerializeField] AudioClip sfxError;   // blip se premi E ma nessuno è pronto
    [SerializeField] float sfxMinInterval = 0.12f; // antispam suoni

    readonly List<CustomerController> queue = new List<CustomerController>();

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

    // ⛔️ NIENTE Update: l'input (E) lo gestisce il Player e chiama TryCheckout()

    // --- Trigger 2D per prossimità Player ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!requireProximity) return;
        if (other.CompareTag(playerTag))
            _playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!requireProximity) return;
        if (other.CompareTag(playerTag))
            _playerInRange = false;
    }

    // --- API chiamate dai clienti ---
    public void JoinQueue(CustomerController c)
    {
        if (!c || queue.Contains(c)) return;
        queue.Add(c);
        RepositionQueue();
    }

    public void LeaveQueue(CustomerController c)
    {
        if (!c) return;
        if (queue.Remove(c))
            RepositionQueue();
    }

    void RepositionQueue()
    {
        if (queueSlots == null || queueSlots.Length == 0) return;

        for (int i = 0; i < queue.Count; i++)
        {
            var c = queue[i];
            if (!c) continue;

            var slot = i < queueSlots.Length ? queueSlots[i] : queueSlots[queueSlots.Length - 1];
            if (!slot) continue;

            Vector2 j = c.cashierJitter;
            Vector3 pos = slot.position + new Vector3(
                Random.Range(-j.x, j.x),
                Random.Range(-j.y, j.y),
                0f
            );

            c.Queue_MoveTo(pos);
        }
    }

    // --- Logica incasso centralizzata ---
    public bool TryCheckout()
    {
        if (requireProximity && !_playerInRange) return false;


        // evita doppia esecuzione nello stesso frame
        if (_lastCheckoutFrame == Time.frameCount) return false;
        _lastCheckoutFrame = Time.frameCount;

        // Nessuno pronto → beep singolo e basta
        if (!HasReadyCustomer())
        {
            BeepError();
            return false;
        }

        var c = queue[0];
        int total = GetCartTotal(c);
        if (total <= 0) { BeepError(); return false; }

        // 💰 incasso
        Wallet.I.Add(total);

        // 🔔 feedback (ding singolo, con antispam)
        PlayDing();

        // Uscita cliente (gestisce la coda)
        c.ClearCartAndExit();
        return true;
    }

    public bool HasReadyCustomer()
    {
        if (queue.Count == 0) return false;
        var c = queue[0];
        if (c == null) return false;
        if (!c.WantsToPay) return false;
        return GetCartTotal(c) > 0;
    }

    int GetCartTotal(CustomerController c)
    {
        if (c == null || c.Cart == null) return 0;
        int sum = 0;
        for (int i = 0; i < c.Cart.Count; i++)
        {
            var item = c.Cart[i];
            if (item != null) sum += item.price;   // coerente con CustomerController (usa int price)
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
        if (Time.time - _lastSfxTime < sfxMinInterval) return; // antispam
        _lastSfxTime = Time.time;
        audioSrc.PlayOneShot(sfxError);
    }
}
