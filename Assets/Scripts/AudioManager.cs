using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource sfxSource;     // For SFX
    public AudioSource musicSource;   // For background music

    [Header("Player SFX")]
    public AudioClip playerShoot;
    public AudioClip playerDie;
    public AudioClip playerHit;
    //public AudioClip playerWin;

    [Header("Enemy SFX")]
    public AudioClip enemyShoot;
    public AudioClip enemyDie;
    public AudioClip enemyHit;

    [Header("Animal SFX")]
    public AudioClip animalLockedOn;
    public AudioClip animalLunge;
    public AudioClip animalHitPlayer;

    [Header("UI / Meta SFX")]
    public AudioClip upgradeSelectedSfx;

    [Header("Music")]
    public AudioClip backgroundMusic;

    private void Awake()
    {
        // Simple singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        

        if (sfxSource == null)
        {
            Debug.LogWarning("AudioManager: sfxSource is not assigned.");
        }

        if (musicSource == null)
        {
            Debug.LogWarning("AudioManager: musicSource is not assigned.");
        }
    }

    // Generic SFX helper
    void PlaySFXClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // Player
    public void PlayPlayerShoot()   { PlaySFXClip(playerShoot); }
    public void PlayPlayerDie()     { PlaySFXClip(playerDie); }
    public void PlayPlayerHit()     { PlaySFXClip(playerHit); }
    //public void PlayPlayerWin()     { PlaySFXClip(playerWin); }

    // Enemy
    public void PlayEnemyShoot()    { PlaySFXClip(enemyShoot); }
    public void PlayEnemyDie()      { PlaySFXClip(enemyDie); }
    public void PlayEnemyHit()      { PlaySFXClip(enemyHit); }

    // Animal
    public void PlayAnimalLockedOn(){ PlaySFXClip(animalLockedOn); }
    public void PlayAnimalLunge()  { PlaySFXClip(animalLunge); }
    public void PlayAnimalHitPlayer(){ PlaySFXClip(animalHitPlayer); }

    // UI / Meta
    public void PlayUpgradeSelected()
    {
        PlaySFXClip(upgradeSelectedSfx);
    }

    // Music
    public void PlayBackgroundMusic(bool loop = true)
    {
        if (musicSource == null || backgroundMusic == null) return;

        musicSource.clip = backgroundMusic;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopBackgroundMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }
}
