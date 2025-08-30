using System.Collections;
using TMPro;
using UnityEngine;

public class PopupFloatingText : MonoBehaviour
{
    public static PopupFloatingText I;
    public TMP_Text prefab;        // prefab TMP (UI)
    public RectTransform container; // parent nel Canvas (usa il Canvas stesso)
    Camera cam;

    void Awake() { I = this; cam = Camera.main; if (!container) container = GetComponent<RectTransform>(); }

    public void ShowLoss(int amount, Vector3 worldPos)
    {
        if (!prefab || !container) return;
        var t = Instantiate(prefab, container);
        t.text = $"-€{amount}";
        t.color = new Color(1f, 0.3f, 0.3f, 1f);
        StartCoroutine(Animate(t, worldPos + new Vector3(0, 0.6f, 0)));
    }

    IEnumerator Animate(TMP_Text t, Vector3 worldPos)
    {
        var rt = t.rectTransform;
        Vector3 screen = (cam ? cam : Camera.main).WorldToScreenPoint(worldPos);
        Vector3 start = screen, end = screen + new Vector3(0, 60, 0);

        float dur = 0.8f, e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float k = e / dur;
            rt.position = Vector3.Lerp(start, end, k);
            t.alpha = 1f - k;
            yield return null;
        }
        Destroy(t.gameObject);
    }
}
