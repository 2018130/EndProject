using System;
using UnityEngine;
using UnityEngine.Audio;


[Serializable]
public struct AudioData
{
    public string key;
    public AudioClip clip;
}

public class AudioManager : SingletonBehaviour<AudioManager>
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM")]
    [SerializeField] private AudioData[] bgmList;

    [Header("SFX")]
    [SerializeField] private AudioData[] sfxList;

    // Mixer 파라미터 이름 (Mixer에서 Expose한 이름과 동일하게)
    private const string BGM_PARAM = "BGMVolume";
    private const string SFX_PARAM = "SFXVolume";
    private const string MASTER_PARAM = "MasterVolume";


    // ─── BGM ───────────────────────────────────────

    public void PlayBGM(string key, bool loop = true)
    {
        foreach (var data in bgmList)
        {
            if (data.key != key) continue;

            if (bgmSource.clip == data.clip) return;
            bgmSource.clip = data.clip;
            bgmSource.loop = loop;
            bgmSource.Play();
            return;
        }
        Debug.LogWarning($"BGM 키 없음: {key}");
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // ─── SFX ───────────────────────────────────────

    public void PlaySFX(string key)
    {
        foreach (var data in sfxList)
        {
            if (data.key != key) continue;

            sfxSource.PlayOneShot(data.clip);
            return;
        }

        Debug.LogWarning($"SFX 키 없음: {key}");
    }

    // ─── Volume (0f ~ 1f 로 받아서 dB 변환) ────────

    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat(MASTER_PARAM, ToDb(value));
    }

    public void SetBGMVolume(float value)
    {
        audioMixer.SetFloat(BGM_PARAM, ToDb(value));
    }

    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat(SFX_PARAM, ToDb(value));
    }

    // 0~1 → dB 변환 (0은 -80dB로 무음 처리)
    private float ToDb(float value)
    {
        return value <= 0f ? -80f : Mathf.Log10(value) * 20f;
    }
}