using System.Collections;
using UnityEngine;

public class WaveMusicManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource waveMusicSource;
    public AudioSource breakMusicSource;
    public AudioSource gameOverMusicSource;

    [Header("Settings")]
    public float crossfadeDuration = 2.0f;
    public float maxVolume = 0.5f;

    private Coroutine activeCrossfade;
    private AudioSource currentSource;

    public void StartWaveMusic()
    {
        if (activeCrossfade != null) StopCoroutine(activeCrossfade);
        
        // If nothing is playing yet, default to fading out the break music
        AudioSource fadeOut = currentSource != null ? currentSource : breakMusicSource;
        
        activeCrossfade = StartCoroutine(Crossfade(fadeOut, waveMusicSource));
        currentSource = waveMusicSource;
    }

    public void StartBreakMusic()
    {
        if (activeCrossfade != null) StopCoroutine(activeCrossfade);
        
        AudioSource fadeOut = currentSource != null ? currentSource : waveMusicSource;
        
        activeCrossfade = StartCoroutine(Crossfade(fadeOut, breakMusicSource));
        currentSource = breakMusicSource;
    }

    public void StartGameOverMusic()
    {
        if (activeCrossfade != null) StopCoroutine(activeCrossfade);
        
        // Fade out whatever is currently playing (Wave or Break) and fade in Game Over
        activeCrossfade = StartCoroutine(Crossfade(currentSource, gameOverMusicSource));
        currentSource = gameOverMusicSource;
    }

    private IEnumerator Crossfade(AudioSource fadeOutSource, AudioSource fadeInSource)
    {
        float timeElapsed = 0f;
        float startingFadeOutVolume = fadeOutSource != null ? fadeOutSource.volume : 0f;

        // Start the new track if it isn't already playing
        if (fadeInSource != null && !fadeInSource.isPlaying)
        {
            fadeInSource.Play();
        }

        while (timeElapsed < crossfadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / crossfadeDuration;

            // Turn one down, turn the other up
            if (fadeOutSource != null) fadeOutSource.volume = Mathf.Lerp(startingFadeOutVolume, 0f, t);
            if (fadeInSource != null) fadeInSource.volume = Mathf.Lerp(0f, maxVolume, t);

            yield return null;
        }

        // Ensure exact final volumes
        if (fadeOutSource != null)
        {
            fadeOutSource.volume = 0f;
            fadeOutSource.Pause(); 
        }
        
        if (fadeInSource != null)
        {
            fadeInSource.volume = maxVolume;
        }
    }
}