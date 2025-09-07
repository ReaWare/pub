using UnityEngine;

[CreateAssetMenu(menuName = "Customers/Archetype")]
public class CustomerArchetype : ScriptableObject
{
    [Header("Shopping")]
    [Range(0f, 1f)] public float buyChance = 0.6f;
    public Vector2Int standsToVisit = new Vector2Int(1, 3); // min..max inclusivo
    public bool allowRevisit = false;                       // se true può ripassare sugli stessi
    public float walkSpeed = 2f;

    [Header("Theft")]
    [Range(0f, 1f)] public float stealChance = 0.15f;      // chance di rubare se ha preso roba
    public Vector2Int stealItems = new Vector2Int(1, 2);   // quante cose ruba (min..max)
    public float thiefRunMultiplier = 1.3f;                 // corre un po’ di più quando scappa


    [Header("Interact con stand")]
    public Vector2Int takePerStand = new Vector2Int(1, 2); // prenderà 1..N per stand
    [Range(0f, 1f)] public float neatness = 0.5f;           // 1=molto ordinato, 0=disordinato


    [Header("Tempo davanti allo stand")]
    public Vector2 lingerAtStand = new Vector2(0.5f, 2.0f); // min..max secondi

}
