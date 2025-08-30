// SummaryUI.cs — usa DayCycle + Wallet (niente ShopTimer / GameManager)
using TMPro;
using UnityEngine;

public class SummaryUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameObject panel;              // pannello riassunto
    [SerializeField] TMP_Text dayText;
    [SerializeField] TMP_Text incomeText;
    [SerializeField] TMP_Text costsText;
    [SerializeField] TMP_Text netText;
    [SerializeField] TMP_Text balanceText;

    [Header("Sistemi")]
    [SerializeField] DayCycle day;                  // assegna lo stesso di EndOfDay
    [SerializeField] Wallet wallet;                 // il tuo Wallet

    [Header("Costi fissi")]
    [SerializeField] int dailyRent = 30;

    [Header("Progressione semplice")]
    [SerializeField] int dayNumber = 1;             // contatore giorno locale

    void Awake()
    {
        if (!day) day = FindObjectOfType<DayCycle>(true);
        if (!wallet) wallet = FindObjectOfType<Wallet>(true);
        Hide();
    }

    void OnEnable()
    {
        if (day != null) day.OnDayEnded += OnDayEnded;
    }

    void OnDisable()
    {
        if (day != null) day.OnDayEnded -= OnDayEnded;
    }

    void OnDayEnded(bool success)
    {
        Time.timeScale = 0f;

        int income = wallet ? wallet.DailyTotal : 0;
        int costs = dailyRent;
        int net = income - costs;
        int newBal = wallet ? wallet.Balance - costs : 0; // (non ancora applicato)

        if (dayText) dayText.text = $"Day {dayNumber} — Summary";
        if (incomeText) incomeText.text = $"Income:  € {income}";
        if (costsText) costsText.text = $"Costs:   € {costs}";
        if (netText) netText.text = $"Net:     € {net}";
        if (balanceText) balanceText.text = $"Balance after: € {newBal}";

        if (panel) panel.SetActive(true);
    }

    // bottone "Continue"
    public void ContinueNextDay()
    {
        if (wallet) wallet.Add(-dailyRent); // applica i costi
        dayNumber++;

        if (panel) panel.SetActive(false);
        Time.timeScale = 1f;

        if (day != null) day.StartDay();   // resetta DailyTotal e riparte
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
    }
}
