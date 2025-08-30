using UnityEngine;

public class DayCycle : MonoBehaviour
{
    [Header("Durata")]
    public float dayLengthSeconds = 180f;

    [Header("Chiusura")]
    [Range(0.5f, 0.99f)] public float closingThresholdNormalized = 0.85f; // quando smetti di far entrare gente
    [Tooltip("Se vero, il giorno finisce solo quando il negozio è vuoto.")]
    public bool waitForStoreEmpty = true;
    [SerializeField] private ShopClosingGuard closingGuard;

    public System.Action OnDayStarted;
    public event System.Action OnClosingStarted;
    public System.Action<bool> OnDayEnded; // true = success

    float t;
    bool _closingFired = false;
    bool _endRequested = false;

    public bool IsRunning { get; private set; }
    public float NormalizedTime => Mathf.Clamp01(dayLengthSeconds <= 0 ? 0f : t / dayLengthSeconds);

    [SerializeField] private bool startOnAwake = false; // lascia OFF, parte il GameFlow

    void Start()
    {
        if (startOnAwake) StartDay();
        if (!closingGuard) closingGuard = FindObjectOfType<ShopClosingGuard>(true);
    }

    public void StartDay()
    {
        t = 0f;
        IsRunning = true;
        _closingFired = false;
        _endRequested = false;
        if (Wallet.I) Wallet.I.ResetDay();
        Debug.Log("[DayCycle] StartDay");
        OnDayStarted?.Invoke();
    }

    void Update()
    {
        if (!IsRunning) return;

        t += Time.deltaTime;

        // Entriamo in "Closing" (smetti di spawnare nuovi clienti)
        if (!_closingFired && NormalizedTime >= closingThresholdNormalized)
        {
            _closingFired = true;
            OnClosingStarted?.Invoke();
        }

        // Tempo scaduto → richiedi chiusura, ma non chiudere finché c'è gente
        if (!_endRequested && t >= dayLengthSeconds)
        {
            _endRequested = true;
            Debug.Log("[DayCycle] Time over → closing pending (wait store empty if needed)");

            // ✦ QUI il pezzo che chiedevi: prima di provare a chiudere, ricontrolla chi è in negozio
            if (closingGuard) closingGuard.Rescan();

            TryEndIfPossible();
        }
    }

    public void TryEndIfPossible()
    {
        if (!IsRunning) return;
        if (!_endRequested) return; // non abbiamo ancora raggiunto la fine tempo

        if (!waitForStoreEmpty)
        {
            EndDay();
            return;
        }

        // Se non ho una guardia, chiudo subito; altrimenti chiudo solo se vuoto
        if (!closingGuard || closingGuard.IsEmpty)
        {
            EndDay();
        }
        else
        {
            Debug.Log($"[DayCycle] Waiting customers to leave... count={closingGuard.Count}");
        }
    }

    public void EndDay()
    {
        if (!IsRunning) return;
        IsRunning = false;
        bool success = Wallet.I && Wallet.I.DailyTotal >= Wallet.I.DailyTarget;
        Debug.Log($"[DayCycle] EndDay success={success}");
        OnDayEnded?.Invoke(success);
    }
}
