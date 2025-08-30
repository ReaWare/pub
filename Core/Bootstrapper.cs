using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] string firstSceneName = "Game";

    void Awake()
    {
        if (FindObjectOfType<RunData>(true) == null)
        {
            var go = new GameObject("RunData");
            go.AddComponent<RunData>();
            DontDestroyOnLoad(go);
        }

        var active = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(firstSceneName) && active != firstSceneName)
            SceneManager.LoadScene(firstSceneName);
    }
}
