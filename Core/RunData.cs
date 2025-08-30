// RunData.cs — persistenza semplice di giorno e soldi
using UnityEngine;

public class RunData : MonoBehaviour
{
    public static RunData I;
    public int Day { get; private set; } = 1;
    public int Money { get; private set; } = 0;

    const string KEY_DAY = "run_day";
    const string KEY_MONEY = "run_money";

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);

        Day = PlayerPrefs.GetInt(KEY_DAY, 1);
        Money = PlayerPrefs.GetInt(KEY_MONEY, 0);
    }

    void Start()
    {
        // allinea il Wallet all'ultimo saldo salvato
        if (Wallet.I) Wallet.I.Balance = Money;
    }

    void OnEnable()
    {
        if (Wallet.I) Wallet.I.OnMoneyChanged += SyncMoney;
        var day = FindObjectOfType<DayCycle>(true);
        if (day) day.OnDayEnded += OnDayEnded;
    }

    void OnDisable()
    {
        if (Wallet.I) Wallet.I.OnMoneyChanged -= SyncMoney;
        var day = FindObjectOfType<DayCycle>(true);
        if (day) day.OnDayEnded -= OnDayEnded;
    }

    void SyncMoney()
    {
        if (!Wallet.I) return;
        Money = Wallet.I.Balance;
        PlayerPrefs.SetInt(KEY_MONEY, Money);
    }

    void OnDayEnded(bool _)
    {
        Day++;
        PlayerPrefs.SetInt(KEY_DAY, Day);
    }
}
