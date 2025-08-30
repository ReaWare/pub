using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    private const string Key = "masterVolume";

    private void OnEnable()
    {
        float v = PlayerPrefs.GetFloat(Key, 1f);
        if (masterVolumeSlider)
        {
            masterVolumeSlider.value = v;
            masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        AudioListener.volume = v;
    }

    private void OnDisable()
    {
        if (masterVolumeSlider)
            masterVolumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(Key, v);
    }
}
