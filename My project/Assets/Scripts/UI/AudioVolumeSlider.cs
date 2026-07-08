using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class AudioVolumeSlider : MonoBehaviour
{
    public enum Channel
    {
        Music,
        Sfx
    }

    private Slider slider;
    private Channel channel;
    private bool isBound;

    public void Setup(Channel targetChannel)
    {
        channel = targetChannel;
        slider = GetComponent<Slider>();

        if (!isBound)
        {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
            isBound = true;
        }

        RefreshFromAudioManager();
    }

    private void OnEnable()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        RefreshFromAudioManager();
    }

    private void OnDestroy()
    {
        if (slider != null && isBound)
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float sliderValue)
    {
        if (AudioManager.Instance == null)
            return;

        if (channel == Channel.Music)
            AudioManager.Instance.SetMusicVolumeFromSlider(sliderValue);
        else
            AudioManager.Instance.SetSfxVolumeFromSlider(sliderValue);
    }

    public void RefreshFromAudioManager()
    {
        if (slider == null || AudioManager.Instance == null)
            return;

        float normalizedVolume = channel == Channel.Music
            ? AudioManager.Instance.MusicVolume
            : AudioManager.Instance.SfxVolume;

        slider.SetValueWithoutNotify(normalizedVolume * 100f);
    }
}
