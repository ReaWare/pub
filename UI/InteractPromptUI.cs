using UnityEngine;
using TMPro; // usa un TextMeshProUGUI

public class InteractPromptUI : MonoBehaviour
{
    [SerializeField] Interactor interactor;
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] CanvasGroup group;
    [SerializeField] float fadeSpeed = 12f;

    float targetAlpha;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();
        if (interactor) interactor.OnPromptChanged += SetText;
        SetText("");
    }

    void OnDestroy()
    {
        if (interactor) interactor.OnPromptChanged -= SetText;
    }

    void SetText(string txt)
    {
        if (label) label.text = txt;
        targetAlpha = string.IsNullOrEmpty(txt) ? 0f : 1f;
    }

    void Update()
    {
        if (!group) return;
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        group.blocksRaycasts = group.interactable = group.alpha > 0.99f;
    }
}
