using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[Serializable] public class LocItem { public string key; public string value; }
[Serializable] public class LocTable { public LocItem[] items; }

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager I { get; private set; }

    [Header("Config")]
    [Tooltip("Lingua di fallback se il file selezionato manca")]
    public string defaultLanguage = "it";

    [Tooltip("Cartella dentro Resources che contiene i file lingua (es. Resources/Localization/it.json)")]
    public string folderPath = "Localization"; // quindi: Resources/Localization/it.json

    // cache runtime
    Dictionary<string, string> _table = new Dictionary<string, string>(1024);
    public string CurrentLanguage { get; private set; }

    public event Action OnLanguageChanged;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        var saved = PlayerPrefs.GetString("lang", "");
        SetLanguage(string.IsNullOrEmpty(saved) ? defaultLanguage : saved);
    }

    public void SetLanguage(string lang)
    {
        CurrentLanguage = lang;
        LoadTable(lang);
        PlayerPrefs.SetString("lang", lang);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    void LoadTable(string lang)
    {
        _table.Clear();

        // Cerca Resources/Localization/<lang>.json
        TextAsset ta = Resources.Load<TextAsset>($"{folderPath}/{lang}");
        if (ta != null)
        {
            var tbl = JsonUtility.FromJson<LocTable>(ta.text);
            if (tbl?.items != null)
            {
                foreach (var kv in tbl.items)
                {
                    if (kv == null || string.IsNullOrEmpty(kv.key)) continue;
                    _table[kv.key] = kv.value ?? "";
                }
            }
        }
        else
        {
            Debug.LogWarning($"[Loc] Missing lang file: {lang}, loading default {defaultLanguage}");
            if (lang != defaultLanguage) LoadTable(defaultLanguage);
        }
    }

    public string Get(string key)
    {
        if (_table.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v)) return v;
        // Fallback utile per debug: vedi subito la chiave mancante a schermo
        return $"#{key}#";
    }

    public string Format(string key, params object[] args)
        => string.Format(Get(key), args);
}

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
        _txt.text = useFormat ? LocalizationManager.I.Format(key, args)
                              : LocalizationManager.I.Get(key);
    }
}
