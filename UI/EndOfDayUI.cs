using TMPro;
using UnityEngine;

public class EndOfDayUI : MonoBehaviour
{
    public DayCycle day;
    public GameObject panel;
    public TMP_Text title;
    public TMP_Text body;

    void Awake() { if (panel != null) panel.SetActive(false); }
    void OnEnable() { if (day != null) day.OnDayEnded += HandleEnd; }
    void OnDisable() { if (day != null) day.OnDayEnded -= HandleEnd; }

    void HandleEnd(bool success)
    {
        if (panel == null) return;
        panel.SetActive(true);
        if (title) title.text = success ? "Giorno superato" : "Giorno fallito";
        if (body) body.text = $"Incassato oggi: € {Wallet.I.DailyTotal} / Target: € {Wallet.I.DailyTarget}";
        Time.timeScale = 0f; // pausa soft
        Debug.Log($"[EndOfDayUI] EndDay success={success}, panel={(panel ? panel.name : "NULL")}");

    }

    public void Button_NextDay()
    {
        CleanupCustomers();
        CloseAndRestart();
    }

    public void Button_RetryDay()
    {
        Wallet.I.ResetDay(); // non azzera il Balance totale, solo il totale di oggi
        CleanupCustomers();
        CloseAndRestart();
    }

    void CloseAndRestart()
    {
        if (panel) panel.SetActive(false);
        Time.timeScale = 1f;
        day.StartDay();
    }

    void CleanupCustomers()
    {
        foreach (var c in FindObjectsOfType<CustomerController>())
            Destroy(c.gameObject);
    }




}
