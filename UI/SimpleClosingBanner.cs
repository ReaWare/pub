using UnityEngine;

public class SimpleClosingBanner : MonoBehaviour
{
    [SerializeField] private DayCycle day;
    [Range(0f, 1f)][SerializeField] private float showAt = 0.85f;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!day) return;
        bool shouldShow = day.NormalizedTime >= showAt;
        if (gameObject.activeSelf != shouldShow)
            gameObject.SetActive(shouldShow);
    }
}
