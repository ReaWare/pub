using System.Collections.Generic;
using UnityEngine;

public class CashRegister : MonoBehaviour
{
    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Queue")]
    [Tooltip("Slot 0 = davanti alla cassa (dentro il trigger)")]
    public Transform[] queueSlots;     // Slot0, Slot1, Slot2...
    public Transform cashierPoint;     // opzionale; se assente uso queueSlots[0]

    [Header("Audio")]
    [SerializeField] AudioSource audioSrc;
    [SerializeField] AudioClip sfxDing;    // ka-ching
    [SerializeField] AudioClip sfxError;   // blip se premi E ma nessuno è pronto

    readonly List<CustomerController> queue = new List<CustomerController>();

    void Awake()
    {
        if (!audioSrc) audioSrc = GetComponent<AudioSource>();
    }

    // --- API per i clienti ---
    public void JoinQueue(CustomerController c)
    {
        if (c == null || queue.Contains(c)) return;
        queue.Add(c);
        UpdateQueueTargets();
    }

    public void LeaveQueue(CustomerController c)
    {
        int i = queue.IndexOf(c);
        if (i >= 0)
        {
            queue.RemoveAt(i);
            UpdateQueueTargets();
        }
    }

    void Update()
    {
        if (queue.Count == 0) return;

        // pulizia
        queue.RemoveAll(c => c == null);

        // candidato: il primo valido (WantsToPay + carrello)
        CustomerController candidate = null;
        foreach (var c in queue)
        {
            if (c != null && c.WantsToPay && c.Cart != null && c.Cart.Count > 0)
            { candidate = c; break; }
        }

        if (Input.GetKeyDown(interactKey))
        {
            if (candidate != null)
            {
                int total = 0;
                foreach (var it in candidate.Cart) total += it.price;

                Wallet.I.Add(total);
                if (audioSrc && sfxDing) audioSrc.PlayOneShot(sfxDing);

                candidate.ClearCartAndExit();
                queue.Remove(candidate);
                UpdateQueueTargets();
                Debug.Log($"[Register] €{total} incassati | Daily={Wallet.I.DailyTotal} | Balance={Wallet.I.Balance}");
            }
            else
            {
                if (audioSrc && sfxError) audioSrc.PlayOneShot(sfxError);
            }
        }
    }

    void UpdateQueueTargets()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            var c = queue[i];
            if (c == null) continue;
            c.Queue_MoveTo(GetSlotPosition(i));
        }
        UpdateIndicators();
    }

    Vector3 GetSlotPosition(int index)
    {
        if (queueSlots != null && queueSlots.Length > 0)
        {
            int clamped = Mathf.Clamp(index, 0, queueSlots.Length - 1);
            var tr = queueSlots[clamped];
            if (tr) return tr.position;
        }
        Vector3 basePos = cashierPoint ? cashierPoint.position : transform.position;
        return basePos + new Vector3(0f, -0.35f * index, 0f); // fallback a scaletta
    }

    void UpdateIndicators()
    {
        if (queueSlots == null) return;
        for (int i = 0; i < queueSlots.Length; i++)
        {
            var slot = queueSlots[i];
            if (!slot) continue;
            var ind = slot.GetComponentInChildren<SlotIndicator>();
            if (ind) ind.SetOccupied(i < queue.Count);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (queueSlots == null) return;
        for (int i = 0; i < queueSlots.Length; i++)
        {
            if (!queueSlots[i]) continue;
            Gizmos.color = (i == 0) ? Color.green : new Color(1f, 0.9f, 0.1f, 1f);
            Gizmos.DrawWireSphere(queueSlots[i].position, 0.12f);
        }
    }
#endif
}
