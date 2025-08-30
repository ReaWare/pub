using System.Collections;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DayCycle day;
    [SerializeField] private GameObject endOfDayOverlay; // root fullscreen
    [SerializeField] private GameObject summaryPanel;     // figlio overlay
    [SerializeField] private GameObject buttonsPanel;     // figlio overlay

    [Header("Options")]
    [SerializeField] private bool startFirstDayOnStart = true;
    [SerializeField] private float overlayHideDelaySeconds = 0.20f;

    public int CurrentDay { get; private set; } = 1;

    void Awake()
    {
        if (!day) day = FindObjectOfType<DayCycle>(true);
    }

    void OnEnable()
    {
        if (day) day.OnDayEnded += OnDayEnded;
    }
    void OnDisable()
    {
        if (day) day.OnDayEnded -= OnDayEnded;
    }

    void Start()
    {
        HideAll();
        Time.timeScale = 1f; // assicurati di partire sbloccata
        if (startFirstDayOnStart) StartCurrentDay();
    }

    void OnDayEnded(bool _)
    {
        // Pausa e mostra solo il riepilogo
        ShowSummaryAndPause();
    }

    void StartCurrentDay()
    {
        // Chiudi overlay, RIATTIVA il tempo, e fai partire il Day
        HideAll();
        Time.timeScale = 1f;
        day.StartDay();
        Debug.Log($"[GameFlow] Day {CurrentDay} started (timeScale={Time.timeScale})");
    }

    // ---------- Overlay helpers ----------
    void HideAll()
    {
        if (summaryPanel) summaryPanel.SetActive(false);
        if (buttonsPanel) buttonsPanel.SetActive(false);
        if (endOfDayOverlay) endOfDayOverlay.SetActive(false);
    }

    void ShowSummaryAndPause()
    {
        if (endOfDayOverlay) endOfDayOverlay.SetActive(true);
        if (summaryPanel) summaryPanel.SetActive(true);
        if (buttonsPanel) buttonsPanel.SetActive(false);
        Time.timeScale = 0f; // ferma TUTTO sotto
    }

    void ShowButtonsAndPause()
    {
        if (endOfDayOverlay) endOfDayOverlay.SetActive(true);
        if (summaryPanel) summaryPanel.SetActive(false);
        if (buttonsPanel) buttonsPanel.SetActive(true);
        Time.timeScale = 0f; // resta fermo
    }

    // ---------- Bottoni ----------
    public void GoToButtons() { ShowButtonsAndPause(); }
    public void ContinueToNextDay() { StartCoroutine(Co_Continue(nextDay: true)); }
    public void RetryDay() { StartCoroutine(Co_Continue(nextDay: false)); }

    IEnumerator Co_Continue(bool nextDay)
    {
        // chiudi overlay; attendi il fade in tempo REALE; poi riparti
        HideAll();
        if (overlayHideDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(overlayHideDelaySeconds);

        if (nextDay) CurrentDay++;
        StartCurrentDay();
    }
}
