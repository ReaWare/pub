using UnityEngine;

[DefaultExecutionOrder(200)]
public class EndOfDayBookkeeping : MonoBehaviour
{
    [SerializeField] private DayCycle day;

    void Awake()
    {
        if (!day) day = FindObjectOfType<DayCycle>(true);
    }

    void OnEnable()
    {
        if (!day) day = FindObjectOfType<DayCycle>(true);
        if (day != null) day.OnDayEnded += OnEnded;
        else Debug.LogWarning("[EndOfDayBookkeeping] DayCycle non trovato");
    }

    void OnDisable()
    {
        if (day != null) day.OnDayEnded -= OnEnded;
    }

    void OnEnded(bool success)
    {
        if (Wallet.I) Wallet.I.AddLoss(TheftLedger.StolenValue);
        TheftLedger.Reset();
    }
}

