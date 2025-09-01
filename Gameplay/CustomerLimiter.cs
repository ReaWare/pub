using System.Collections.Generic;
using UnityEngine;

public class CustomerLimiter : MonoBehaviour
{
    public static CustomerLimiter I;

    [Header("Limiti globali")]
    public int dailyQuota = 3;
    public int maxConcurrent = 3;
    public bool enforce = true;

    int _spawnedToday = 0;
    readonly HashSet<CustomerController> _alive = new HashSet<CustomerController>();

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetDay()
    {
        _spawnedToday = 0;
        _alive.Clear();
    }

    public bool CanAdmitAnother()
    {
        if (!enforce) return true;
        return _spawnedToday < dailyQuota && _alive.Count < maxConcurrent;
    }

    // Chiamato in CustomerController.Start()
    public bool TryAdmit(CustomerController cc)
    {
        if (!enforce)
        {
            _alive.Add(cc);
            return true;
        }

        if (_spawnedToday >= dailyQuota || _alive.Count >= maxConcurrent)
            return false;

        _alive.Add(cc);
        _spawnedToday++;
        return true;
    }

    public void NotifyDestroyed(CustomerController cc)
    {
        _alive.Remove(cc);
    }
}
