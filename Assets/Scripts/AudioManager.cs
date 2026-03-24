using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource musicSource;

    // музыкальные треки
    AudioClip menuClip;
    AudioClip fightClip;
    AudioClip finalClip;

    // sfx всякие
    AudioClip sfx_fireball;
    AudioClip sfx_playerHit;
    AudioClip sfx_kingHit;
    AudioClip sfx_heal;
    AudioClip sfx_coinPickup;
    AudioClip sfx_buttonClick;
    AudioClip sfx_mob;
    AudioClip sfx_metallMob;

    float normalVolume = 1f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // грузим треки
        menuClip = Resources.Load<AudioClip>("Music/music_menu");
        fightClip = Resources.Load<AudioClip>("Music/music_fight");
        finalClip = Resources.Load<AudioClip>("Music/music_final");

        // грузим sfx
        sfx_fireball = Resources.Load<AudioClip>("SFX/fireball_shot");
        sfx_playerHit = Resources.Load<AudioClip>("SFX/player_hit");
        sfx_kingHit = Resources.Load<AudioClip>("SFX/king_hit");
        sfx_heal = Resources.Load<AudioClip>("SFX/heal");
        sfx_coinPickup = Resources.Load<AudioClip>("SFX/coin_pickup");
        sfx_buttonClick = Resources.Load<AudioClip>("SFX/button_click");
        sfx_mob = Resources.Load<AudioClip>("SFX/knight_hit");
        sfx_metallMob = Resources.Load<AudioClip>("SFX/heavy_knight_hit");
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.PlayOneShot(clip, volume);
    }

    // быстрый вызов звука по имени
    public void PlaySound(string name, float vol = 1f)
    {
        switch (name)
        {
            case "fireball": PlaySFX(sfx_fireball, vol); break;
            case "player_hit": PlaySFX(sfx_playerHit, vol); break;
            case "king_hit": PlaySFX(sfx_kingHit, vol); break;
            case "heal": PlaySFX(sfx_heal, vol); break;
            case "coin": PlaySFX(sfx_coinPickup, vol); break;
            case "button": PlaySFX(sfx_buttonClick, vol); break;
            case "knight_hit": PlaySFX(sfx_mob, vol); break;
            case "heavy_knight_hit": PlaySFX(sfx_metallMob, vol); break;
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = normalVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayTrack(string trackName)
    {
        switch (trackName)
        {
            case "menu": PlayMusic(menuClip); break;
            case "fight": PlayMusic(fightClip); break;
            case "final": PlayMusic(finalClip); break;
        }
    }

    public void SetMusicVolume(float vol)
    {
        if (musicSource != null)
            musicSource.volume = vol;
    }

    public void DimMusic()
    {
        SetMusicVolume(0.3f);
    }

    public void RestoreVolume()
    {
        SetMusicVolume(normalVolume);
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }
}
