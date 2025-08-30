using TMPro;
using UnityEngine;

public class DayLabelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private GameFlowController flow;

    void Reset()
    {
        dayText = GetComponent<TMP_Text>();
        flow = FindObjectOfType<GameFlowController>(true);
    }

    void Awake()
    {
        if (!dayText) dayText = GetComponent<TMP_Text>();
        if (!flow) flow = FindObjectOfType<GameFlowController>(true);
    }

    void Update()
    {
        if (!dayText || !flow) return;
        dayText.text = $"Giorno {flow.CurrentDay}";
    }
}
