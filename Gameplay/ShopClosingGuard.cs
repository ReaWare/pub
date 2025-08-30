using System.Collections.Generic;
using UnityEngine;

public class ShopClosingGuard : MonoBehaviour
{
    [SerializeField] private DayCycle day;
    [SerializeField] private bool autoScanOnStart = true;
    [SerializeField] private bool autoScanOnDayStart = true;

    HashSet<CustomerController> _alive = new HashSet<CustomerController>();
    public int Count => _alive.Count;
    public bool IsEmpty => _alive.Count == 0;

    void Awake()
    {
        if (!day) day = FindObjectOfType<DayCycle>(true);
    }

    void OnEnable()
    {
        if (day) day.OnDayStarted += OnDayStarted;
    }
    void OnDisable()
    {
        if (day) day.OnDayStarted -= OnDayStarted;
    }

    void Start()
    {
        if (autoScanOnStart) Rescan();
    }

    void OnDayStarted()
    {
        _alive.Clear();      // riparti pulita
        if (autoScanOnDayStart) Rescan(); // aggancia eventuali clienti già presenti
    }

    /// <summary>Registra tutti i CustomerController già presenti in scena.</summary>
    public void Rescan()
    {
        foreach (var c in FindObjectsOfType<CustomerController>())
            RegisterNew(c);
        Debug.Log($"[Guard] Rescan: tracking {_alive.Count} customers.");
    }

    public void RegisterNew(CustomerController c)
    {
        if (!c || _alive.Contains(c)) return;
        _alive.Add(c);

        var tracker = c.gameObject.GetComponent<StoreOccupantTracker>();
        if (!tracker) tracker = c.gameObject.AddComponent<StoreOccupantTracker>();
        tracker.Init(this, c);
    }

    public void Unregister(CustomerController c)
    {
        if (c && _alive.Remove(c))
        {
            if (_alive.Count == 0 && day != null)
                day.TryEndIfPossible();
        }
    }
}

public class StoreOccupantTracker : MonoBehaviour
{
    ShopClosingGuard _guard;
    CustomerController _ctrl;

    public void Init(ShopClosingGuard guard, CustomerController ctrl)
    {
        _guard = guard;
        _ctrl = ctrl;
    }

    void OnDestroy()
    {
        if (_guard && _ctrl) _guard.Unregister(_ctrl);
    }
}
