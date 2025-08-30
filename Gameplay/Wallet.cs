using System;
using UnityEngine;

public class Wallet : MonoBehaviour
{
    public static Wallet I;
    public int Balance;
    [Header("Obiettivo del giorno")]
    public int DailyTarget = 80;
    [NonSerialized] public int DailyTotal;

    public event Action OnMoneyChanged;

    void Awake() { I = this; }

    public void Add(int amount)
    {
        Balance += amount;
        DailyTotal += amount;
        OnMoneyChanged?.Invoke();
    }


    // --- Perdite giornaliere (furti, resi, ecc.) ---
    public float LossesToday { get; private set; }

    public void AddLoss(float amount)
    {
        LossesToday += Mathf.Max(0f, amount);
    }

    public void ResetDay() { 
        DailyTotal = 0;
        LossesToday = 0f;
    }
}
