using UnityEngine;

public class LimiterDayHook : MonoBehaviour
{
    DayCycle _day;

    void OnEnable()
    {
        _day = FindObjectOfType<DayCycle>();
        if (_day != null)
        {
            _day.OnDayStarted += OnDayStarted;
            _day.OnDayEnded += OnDayEnded;
        }
    }

    void OnDisable()
    {
        if (_day != null)
        {
            _day.OnDayStarted -= OnDayStarted;
            _day.OnDayEnded -= OnDayEnded;
        }
    }

    void OnDayStarted()
    {
        if (CustomerLimiter.I != null)
            CustomerLimiter.I.ResetDay();
    }

    void OnDayEnded(bool _reachedTarget)
    {
        // opzionale
    }
}
