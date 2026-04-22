using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages shared background music and sound effects for the entire game.
/// </summary>
public class GameAudioManager : Singleton<GameAudioManager>
{
    private const string BgmSourceObjectName = "BgmAudioSource";
    private const string SfxSourceObjectName = "SfxAudioSource";

    private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private bool _isInitialized;

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        EnsureAudioSources();
        EnsureInitialized();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Ensures shared audio sources and configured clips are ready for playback.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        EnsureAudioSources();
        CacheClip(GameConstants.AUDIO_BGM_MAIN);
        CacheClip(GameConstants.AUDIO_SFX_BUTTON);
        CacheClip(GameConstants.AUDIO_SFX_QUIZ_CORRECT);
        CacheClip(GameConstants.AUDIO_SFX_QUIZ_WRONG);
        _isInitialized = true;
    }

    /// <summary>
    /// Starts or resumes the shared looping background music.
    /// </summary>
    public void PlaySharedBgm()
    {
        EnsureInitialized();

        AudioClip clip = GetClip(GameConstants.AUDIO_BGM_MAIN);
        if (clip == null || _bgmSource == null)
        {
            return;
        }

        if (_bgmSource.isPlaying && _bgmSource.clip == clip)
        {
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.volume = GameConstants.AUDIO_BGM_VOLUME;
        _bgmSource.Play();
    }

    /// <summary>
    /// Plays the shared button click sound effect once.
    /// </summary>
    public void PlayButtonClick()
    {
        PlayOneShot(GameConstants.AUDIO_SFX_BUTTON);
    }

    /// <summary>
    /// Plays the quiz answer feedback sound effect once.
    /// </summary>
    /// <param name="isCorrect">Whether the answer is correct.</param>
    public void PlayQuizAnswerResult(bool isCorrect)
    {
        PlayOneShot(isCorrect ? GameConstants.AUDIO_SFX_QUIZ_CORRECT : GameConstants.AUDIO_SFX_QUIZ_WRONG);
    }

    #endregion

    #region Internal Helpers

    private void EnsureAudioSources()
    {
        _bgmSource = EnsureAudioSource(BgmSourceObjectName);
        if (_bgmSource != null)
        {
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.volume = GameConstants.AUDIO_BGM_VOLUME;
        }

        _sfxSource = EnsureAudioSource(SfxSourceObjectName);
        if (_sfxSource != null)
        {
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.volume = GameConstants.AUDIO_SFX_VOLUME;
        }
    }

    private AudioSource EnsureAudioSource(string childName)
    {
        Transform existingChild = transform.Find(childName);
        GameObject sourceObject = existingChild != null ? existingChild.gameObject : new GameObject(childName);
        if (existingChild == null)
        {
            sourceObject.transform.SetParent(transform, false);
        }

        AudioSource source = sourceObject.GetComponent<AudioSource>();
        if (source == null)
        {
            source = sourceObject.AddComponent<AudioSource>();
        }

        return source;
    }

    private void PlayOneShot(string resourcePath)
    {
        EnsureInitialized();

        AudioClip clip = GetClip(resourcePath);
        if (clip == null || _sfxSource == null)
        {
            return;
        }

        _sfxSource.PlayOneShot(clip, GameConstants.AUDIO_SFX_VOLUME);
    }

    private AudioClip GetClip(string resourcePath)
    {
        return CacheClip(resourcePath);
    }

    private AudioClip CacheClip(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (_clipCache.TryGetValue(resourcePath, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            Debug.LogError($"[GameAudioManager] : Missing audio clip resource: {resourcePath}");
        }

        _clipCache[resourcePath] = clip;
        return clip;
    }

    #endregion
}
