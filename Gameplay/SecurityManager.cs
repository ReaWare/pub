using UnityEngine;

public class SecurityManager : MonoBehaviour
{
    public static SecurityManager I;
    [SerializeField] Transform player;
    [SerializeField] float playerDeterrenceRadius = 3f;

    void Awake() => I = this;

    public float GetSecurityFactor(Vector3 pos)
    {
        float d = Vector3.Distance(pos, player.position);
        return Mathf.Clamp01(1f - (d / playerDeterrenceRadius)); // vicino al player = 1, lontano = 0
    }
}
