using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioSettings : MonoBehaviour
{
    [Header("Audio Setup")]
    [SerializeField] AudioMixer mainMixer;
    [SerializeField] Slider mainSlider;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    // คีย์สำหรับ Save ใน PlayerPrefs
    private const string Main_KEY = "Main_Volume";
    private const string BGM_KEY = "BGM_Volume";
    private const string SFX_KEY = "SFX_Volume";

    void Start()
    {
        mainSlider.value = PlayerPrefs.GetFloat(Main_KEY, 0.75f);
        bgmSlider.value = PlayerPrefs.GetFloat(BGM_KEY, 0.75f);
        sfxSlider.value = PlayerPrefs.GetFloat(SFX_KEY, 0.75f);

        SetMainVolume(mainSlider.value);
        SetBGMVolume(bgmSlider.value);
        SetSFXVolume(sfxSlider.value);

        mainSlider.onValueChanged.AddListener(SetMainVolume);
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMainVolume(float value)
    {
        float db = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20;
        mainMixer.SetFloat("Master", db);
        
        PlayerPrefs.SetFloat(Main_KEY, value);
    }

    public void SetBGMVolume(float value)
    {
        float db = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20;
        mainMixer.SetFloat("BGM", db);
        
        PlayerPrefs.SetFloat(BGM_KEY, value);
    }

    public void SetSFXVolume(float value)
    {
        float db = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20;
        mainMixer.SetFloat("SFX", db);
        
        PlayerPrefs.SetFloat(SFX_KEY, value);
    }

    void OnDisable()
    {
        PlayerPrefs.Save();
    }
}