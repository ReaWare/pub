using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Kickstart")]
    [Tooltip("Riempi fino a maxConcurrent appena inizia la giornata.")]
    public bool topUpOnDayStart = true;

    [Tooltip("Logga il motivo quando non spawna (diagnostica).")]
    public bool logWhyNotSpawning = true;

    [Header("Bootstrap")]
    [Tooltip("Se ON, ad inizio giornata 'aggancia' anche i Customer già in scena.")]
    public bool bootstrapExistingAtStart = false;

    [Header("Prefab & Riferimenti")]
    public GameObject[] customerPrefabs;      // opzionale: più varianti
    public GameObject customerPrefab;         // usato se customerPrefabs è vuoto
    public Transform entryPoint;
    public Transform exitPoint;
    public Transform[] stands;
    public Transform cashierPoint;

    [Header("Archetipi")]
    public CustomerArchetype defaultArchetype;
    public CustomerArchetype[] randomArchetypes;
    public bool useRandomArchetype = false;

    [Header("Quantità")]
    [Tooltip("Quanti clienti al massimo in TUTTA la giornata (spawnati da questo spawner)")]
    public int dailyQuota = 3;

    [Tooltip("Quanti clienti consentiti contemporaneamente")]
    public int maxConcurrent = 3;

    [Tooltip("Stoppa proprio il componente quando raggiunge la quota")]
    public bool stopComponentAtQuota = true;

    [Header("Concorrenza globale")]
    [Tooltip("Se ON, limita i clienti contando TUTTI i CustomerController presenti in scena (non solo quelli spawnati da questo componente).")]
    public bool countAllCustomersInScene = true;

    [Tooltip("Ogni quanto aggiornare il conteggio globale (sec)")]
    public float sceneCountRefresh = 0.25f;

    [Header("Timing")]
    public bool useSpawnCurve = true;
    public AnimationCurve spawnRateOverDay = AnimationCurve.Linear(0, 0.5f, 1, 0.5f);
    [Range(0f, 1f)] public float stopSpawningAfter = 0.90f;
    public float minSpawnInterval = 0.75f;

    [Header("Chiusura")]
    [Tooltip("Ferma gli spawn appena entri nella finestra di Closing (prima della fine ufficiale).")]
    public bool autoStopOnClosing = true;
    [Range(0f, 1f)] public float closingThresholdNormalized = 0.85f;

    [Header("Debug")]
    public bool logSpawns = false;
    public string debugId = "Spawner";

    // --- stato ---
    int _spawnedToday = 0;
    float _lastSpawnTime = -999f;
    readonly List<CustomerController> _alive = new List<CustomerController>();
    bool _canSpawn = false;

    DayCycle _day;
    float _nextSceneCountRefresh = 0f;
    int _cachedSceneCount = 0;



    // in cima alla classe
    void HandleClosing()
    {
        if (!autoStopOnClosing) return;
        _canSpawn = false;
        if (stopComponentAtQuota) enabled = false;
        if (logSpawns) Debug.Log($"[{debugId}] Closing → spawner OFF");
    }



    void OnEnable()
    {
        _day = FindObjectOfType<DayCycle>();
        if (_day != null)
        {
            _day.OnDayStarted += HandleDayStarted;
            _day.OnDayEnded += HandleDayEnded;
            _day.OnClosingStarted += HandleClosing;   // 👈 aggancio
        }
        if (_day == null) HandleDayStarted(); // test senza DayCycle
    }

    void OnDisable()
    {
        if (_day != null)
        {
            _day.OnDayStarted -= HandleDayStarted;
            _day.OnDayEnded -= HandleDayEnded;
            _day.OnClosingStarted -= HandleClosing;   // 👈 sgancio
        }
    }


    void HandleDayStarted()
    {

        _alive.Clear();
        if (bootstrapExistingAtStart)
            BootstrapExistingCustomers();

        if (topUpOnDayStart) StartCoroutine(TopUpNextFrame());

    }

    System.Collections.IEnumerator TopUpNextFrame()
    {
        // aspetta 1 frame per permettere a DayCycle/Limiter/Guard di allinearsi
        yield return null;
        TopUp();
    }

    void TopUp()
    {
        // prova a spawnare finché non raggiungi maxConcurrent o la quota.
        int safety = 10; // evita loop infinito in caso di setup strano
        while (safety-- > 0)
        {
            int active = GetActiveCustomersCount(true);
            if (active >= maxConcurrent) break;
            if (_spawnedToday >= dailyQuota) break;
            if (CustomerLimiter.I != null && !CustomerLimiter.I.CanAdmitAnother()) break;

            SpawnOne();
        }
    }


    void HandleDayEnded(bool reachedDailyTarget)
    {
        _canSpawn = false;
        if (logSpawns) Debug.Log($"[{debugId}] DayEnded");


    }

    void Update()
    {
        if (!_canSpawn) return;

        // 🧱 STOP forte: raggiunta la quota? basta.
        if (_spawnedToday >= dailyQuota)
        {
            if (stopComponentAtQuota) enabled = false; // spegne il componente (niente più tentativi)
            return;
        }

        float nt = _day ? _day.NormalizedTime : 0f;

        // ⛔ Fermati nella finestra di Closing
        if (autoStopOnClosing && nt >= closingThresholdNormalized)
        {
            if (stopComponentAtQuota) enabled = false;
            return;
        }

        if (nt >= stopSpawningAfter) return;

        PurgeDead();

        // Concorrenza: conta globale o locale
        int active = GetActiveCustomersCount(true);  // <-- true
        if (active >= maxConcurrent) return;


        if (Time.time - _lastSpawnTime < minSpawnInterval) return;

        bool shouldSpawn = false;
        if (useSpawnCurve)
        {
            float rate = Mathf.Max(0f, spawnRateOverDay.Evaluate(nt)); // clienti/sec
            float p = rate * Time.deltaTime;
            if (Random.value < p) shouldSpawn = true;
        }
        else
        {
            shouldSpawn = true; // appena c’è posto
        }

        if (shouldSpawn) SpawnOne();
    }

    void SpawnOne()
    {
        // Evita instanziazione se il limite globale dice di no
        if (CustomerLimiter.I != null && !CustomerLimiter.I.CanAdmitAnother())
            return;

        var prefab = PickPrefab();
        if (!prefab || entryPoint == null)
        {
            Debug.LogWarning($"[CustomerSpawner:{name}] Prefab o EntryPoint mancante.", this);
            if (stopComponentAtQuota) enabled = false;   // evita spam e futuri tentativi
            return;
        }

        var go = Instantiate(prefab, entryPoint.position, Quaternion.identity);
        var cc = go.GetComponent<CustomerController>();
        if (!cc)
        {
            Debug.LogError("[CustomerSpawner] Il prefab non ha CustomerController.");
            Destroy(go);
            return;
        }

        // wiring coerente con CustomerController
        cc.entryPoint = entryPoint;
        cc.exitPoint = exitPoint;
        cc.stands = stands;
        cc.cashierPoint = cashierPoint;
        cc.archetype = PickArchetype();

        _alive.Add(cc);
        _spawnedToday++;
        _lastSpawnTime = Time.time;

        AttachNotifier(cc);

        if (logSpawns)
            Debug.Log($"[{debugId}] Spawn #{_spawnedToday}/{dailyQuota} | alive(local): {_alive.Count} | active(scene): {GetActiveCustomersCount(true)}");
    }

    // --- Helpers ---
    void BootstrapExistingCustomers()
    {
        var existing = FindObjectsOfType<CustomerController>(includeInactive: false);
        foreach (var cc in existing)
        {
            if (!_alive.Contains(cc))
            {
                _alive.Add(cc);
                AttachNotifier(cc);
            }
        }
        // Nota: non aumentiamo _spawnedToday; la quota conta solo quelli generati dallo spawner
        _cachedSceneCount = existing.Length;
        _nextSceneCountRefresh = Time.time + sceneCountRefresh;
    }

    void AttachNotifier(CustomerController cc)
    {
        var notifier = cc.GetComponent<DespawnNotifier>();
        if (!notifier) notifier = cc.gameObject.AddComponent<DespawnNotifier>();
        notifier.onDespawn = OnCustomerDespawned;
        notifier.controller = cc;
    }

    GameObject PickPrefab()
    {
        if (customerPrefabs != null && customerPrefabs.Length > 0)
        {
            var pool = new List<GameObject>();
            foreach (var p in customerPrefabs)
                if (p != null) pool.Add(p);

            if (pool.Count > 0)
                return pool[Random.Range(0, pool.Count)];
        }
        return customerPrefab;
    }

    CustomerArchetype PickArchetype()
    {
        if (useRandomArchetype && randomArchetypes != null && randomArchetypes.Length > 0)
            return randomArchetypes[Random.Range(0, randomArchetypes.Length)];
        return defaultArchetype;
    }

    void OnCustomerDespawned(CustomerController cc)
    {
        _alive.Remove(cc);
    }

    void PurgeDead()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
            if (_alive[i] == null) _alive.RemoveAt(i);
    }

    int GetActiveCustomersCount(bool forceRefresh = false)
    {
        if (!countAllCustomersInScene) return _alive.Count;

        if (forceRefresh || Time.time >= _nextSceneCountRefresh)
        {
            _cachedSceneCount = FindObjectsOfType<CustomerController>(includeInactive: false).Length;
            _nextSceneCountRefresh = Time.time + Mathf.Max(0.05f, sceneCountRefresh);
        }
        return _cachedSceneCount;
    }
}

// Helper per notificare quando un cliente viene distrutto
public class DespawnNotifier : MonoBehaviour
{
    public System.Action<CustomerController> onDespawn;
    public CustomerController controller;

    void OnDestroy()
    {
        if (onDespawn != null) onDespawn(controller);
    }
}
