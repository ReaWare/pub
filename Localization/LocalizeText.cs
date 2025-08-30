using UnityEngine;
using TMPro;

namespace ReaGame.Localization
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeText : MonoBehaviour
    {
        public string key;
        public bool useFormat;
        public string[] args; // per {0},{1}...

        TextMeshProUGUI _txt;

        void Awake()
        {
            _txt = GetComponent<TextMeshProUGUI>();
            Apply();
            if (LocalizationManager.I != null)
                LocalizationManager.I.OnLanguageChanged += Apply;
        }

        void OnDestroy()
        {
            if (LocalizationManager.I != null)
                LocalizationManager.I.OnLanguageChanged -= Apply;
        }

        void Apply()
        {
            if (LocalizationManager.I == null) return;
            _txt.text = useFormat
                ? LocalizationManager.I.Format(key, args)
                : LocalizationManager.I.Get(key);
        }
    }
}
