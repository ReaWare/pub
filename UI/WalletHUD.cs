using TMPro;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TMP_Text))]
public class WalletHUD : MonoBehaviour
{
    public bool showTarget = true;
    public bool useDailyTotal = true; // TRUE = mostra incasso del giorno

    TMP_Text _txt;
    bool _subscribed;

    void Awake() { _txt = GetComponent<TMP_Text>(); }

    void OnEnable() { StartCoroutine(SubscribeWhenReady()); }
    void OnDisable()
    {
        if (_subscribed && Wallet.I != null) Wallet.I.OnMoneyChanged -= Refresh;
        _subscribed = false;
    }

    IEnumerator SubscribeWhenReady()
    {
        while (Wallet.I == null) yield return null; // aspetta 1+ frame se serve
        Wallet.I.OnMoneyChanged += Refresh;
        _subscribed = true;
        Refresh();
    }

    void Refresh()
    {
        if (Wallet.I == null) { _txt.text = "€ --"; return; }
        int value = useDailyTotal ? Wallet.I.DailyTotal : Wallet.I.Balance;
        _txt.text = showTarget
            ? $"€ {value} / {Wallet.I.DailyTarget}"
            : $"€ {value}";
    }
}
