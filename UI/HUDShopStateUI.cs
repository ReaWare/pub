// HUDShopStateUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDShopStateUI : MonoBehaviour
{
    public ShopStateController shop;
    public TextMeshProUGUI label;
    public Image banner;            // facoltativo (sfondo)
    public Color openColor = new Color(0.2f, 1f, 0.2f, 0.95f);
    public Color closingColor = new Color(1f, 0.8f, 0.2f, 0.95f);
    public Color closedColor = new Color(1f, 0.2f, 0.2f, 0.95f);

    void OnEnable() { if (shop) shop.OnStateChanged += Refresh; }
    void OnDisable() { if (shop) shop.OnStateChanged -= Refresh; }
    void Start() { if (shop) Refresh(shop.State); }

    void Refresh(ShopState s)
    {
        switch (s)
        {
            case ShopState.Open:
                SetUI("APERTO", openColor, false);
                break;
            case ShopState.Closing:
                SetUI("CHIUSURA", closingColor, true); // lampeggia
                break;
            case ShopState.Closed:
                SetUI("CHIUSO", closedColor, false);
                break;
        }
    }

    void SetUI(string text, Color c, bool blink)
    {
        if (label) label.text = text;
        if (banner) banner.color = c;
        StopAllCoroutines();
        if (blink) StartCoroutine(Blink());
        else if (banner) banner.canvasRenderer.SetAlpha(1f);
    }

    System.Collections.IEnumerator Blink()
    {
        while (true)
        {
            if (banner) banner.canvasRenderer.SetAlpha(1f);
            yield return new WaitForSeconds(0.4f);
            if (banner) banner.canvasRenderer.SetAlpha(0.4f);
            yield return new WaitForSeconds(0.4f);
        }
    }
}
