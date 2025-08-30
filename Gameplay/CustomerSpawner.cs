using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] private DayCycle day;
    [SerializeField] private bool autoFindDay = true;
    [SerializeField] private ShopClosingGuard closingGuard;

    [Header("Stop (Closing)")]
    [Range(0f, 1f)][SerializeField] private float stopAtNormalized = 0.85f;

    [Header("Prefabs & Scene points")]
    public CustomerController customerPrefab;
    public Transform entryPoint, exitPoint, cashierPoint;
    public Transform[] stands;
    public CustomerArchetype[] archetypes;

    [Header("Curva clienti/minuto (x = 0..1 del giorno)")]
    public AnimationCurve customersPerMinute =
        new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.5f, 1.2f), new Keyframe(1f, 0.4f));

    [Header("Limiti")]
    public int maxAlive = 6;

    // stato interno
    private bool canSpawn = false;
    private float accumulator = 0f;

    void Awake()
    {
        if (!day && autoFindDay) day = FindObjectOfType<DayCycle>(true);
        if (!closingGuard) closingGuard = FindObjectOfType<ShopClosingGuard>(true);
    }

    void OnEnable()
    {
        if (day)
        {
            day.OnDayStarted += HandleDayStarted;
            day.OnClosingStarted += HandleClosingStarted;
            day.OnDayEnded += HandleDayEnded;
        }
        accumulator = 0f;
        canSpawn = day && day.IsRunning && day.NormalizedTime < stopAtNormalized;
    }

    void OnDisable()
    {
        if (day)
        {
            day.OnDayStarted -= HandleDayStarted;
            day.OnClosingStarted -= HandleClosingStarted;
            day.OnDayEnded -= HandleDayEnded;
        }
    }

    void HandleDayStarted() { accumulator = 0f; canSpawn = true; }
    void HandleClosingStarted() { canSpawn = false; accumulator = 0f; Debug.Log("[Spawner] Closing: stop spawn"); }
    void HandleDayEnded(bool _) { canSpawn = false; accumulator = 0f; }

    void Update()
    {
        if (!Application.isPlaying || !day) return;

        if (!day.IsRunning) return; // niente se il giorno è fermo
        if (day.NormalizedTime >= stopAtNormalized) return; // hard fence in closing
        if (!canSpawn) return;

        float ratePerMinute = Mathf.Max(0f, customersPerMinute.Evaluate(day.NormalizedTime));
        float ratePerSec = ratePerMinute / 60f;
        accumulator += ratePerSec * Time.deltaTime;

        while (accumulator >= 1f && CountAlive() < maxAlive)
        {
            SpawnOne();
            accumulator -= 1f;
        }
    }

    int CountAlive() => FindObjectsOfType<CustomerController>().Length;

    void SpawnOne()
    {
        if (!customerPrefab) return;

        var c = Instantiate(customerPrefab);
        c.entryPoint = entryPoint;
        c.exitPoint = exitPoint;
        c.cashierPoint = cashierPoint;
        c.stands = stands;

        if (archetypes != null && archetypes.Length > 0)
            c.archetype = archetypes[Random.Range(0, archetypes.Length)];

        if (closingGuard) closingGuard.RegisterNew(c);
    }
}
