using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SlotIndicator : MonoBehaviour
{
    public SpriteRenderer sr;
    public Color emptyColor = new Color(1f, 1f, 1f, 0.2f);
    public Color occupiedColor = new Color(1f, 1f, 1f, 0.9f);
    public bool pulseWhenOccupied = true;
    public float pulseSpeed = 6f;

    bool occupied;

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        Apply();
    }

    void Update()
    {
        if (!sr) return;
        if (occupied && pulseWhenOccupied)
        {
            float a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
            var c = occupiedColor; c.a = a; sr.color = c;
        }
    }

    public void SetOccupied(bool v)
    {
        occupied = v;
        Apply();
    }

    void Apply()
    {
        if (!sr) return;
        sr.color = occupied ? occupiedColor : emptyColor;
    }
}
