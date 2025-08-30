// ShopStateController.cs  —  VIEW/ADAPTER dello stato negozio
using System;
using UnityEngine;

public enum ShopState { Open, Closing, Closed }

public class ShopStateController : MonoBehaviour
{
    [Header("Fonte verità")]
    public DayCycle day;                         // assegna l'unico DayCycle in scena

    [Header("Quando passa a 'Closing' (frazione del giorno)")]
    [Range(0f, 1f)] public float closingAt = 0.8f; // es. 80% del tempo → Closing

    public ShopState State { get; private set; } = ShopState.Closed;

    // Seconds rimanenti (lettura da DayCycle)
    public float TimeRemaining =>
        (day && day.dayLengthSeconds > 0)
            ? Mathf.Max(0f, day.dayLengthSeconds * (1f - day.NormalizedTime))
            : 0f;

    public event Action<ShopState> OnStateChanged;

    void OnEnable()
    {
        if (!day) day = FindObjectOfType<DayCycle>(true);
        if (day)
        {
            day.OnDayStarted += HandleDayStarted;
            day.OnDayEnded += HandleDayEnded;
        }
    }

    void OnDisable()
    {
        if (day)
        {
            day.OnDayStarted -= HandleDayStarted;
            day.OnDayEnded -= HandleDayEnded;
        }
    }

    void Update()
    {
        if (!day || !day.IsRunning) return;

        // Mappa il tempo del giorno allo stato Open/Closing
        var next = (day.NormalizedTime < closingAt) ? ShopState.Open : ShopState.Closing;
        if (next != State) SetState(next);
    }

    void HandleDayStarted() { SetState(ShopState.Open); }
    void HandleDayEnded(bool success) { SetState(ShopState.Closed); }

    // Comandi manuali (rimpiazzano O/C/X di debug)
    public void ForceOpen() { if (day != null) day.StartDay(); }
    public void ForceClose() { if (day != null) day.EndDay(); }

    void SetState(ShopState s)
    {
        if (State == s) return;
        State = s;
        OnStateChanged?.Invoke(State);
        Debug.Log($"[ShopState] -> {State}");
    }
}
