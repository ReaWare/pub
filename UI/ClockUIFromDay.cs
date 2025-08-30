using TMPro;
using UnityEngine;

public class ClockUIFromDay : MonoBehaviour
{
    public DayCycle day;
    public TMP_Text text;

    void Update()
    {
        if (day == null || text == null) return;
        float t = day.NormalizedTime;                  // 0..1 del giorno
        int secondsLeft = Mathf.CeilToInt((1f - t) * day.dayLengthSeconds);
        text.text = $"Time: {secondsLeft}s";
    }
}
