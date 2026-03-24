using UnityEngine;

public enum GameState { Playing, Paused, LevelComplete, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameState State { get; private set; }

    [SerializeField] WeaponData startingWeapon;

    PlayerController player;
    HealthComponent playerHP;
    int currentLevel = 1;
    int maxLevel = 3;

    int sessionGold = 0;
    int weaponDamage = 2;
    int maxHealth = 10;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SaveManager.DeleteSave();
        currentLevel = 1;
        sessionGold = 0;
        weaponDamage = 2;
        maxHealth = 10;
        StartLevel(currentLevel);
    }

    public void StartLevel(int level)
    {
        currentLevel = level;
        State = GameState.Playing;
        Time.timeScale = 1;

        // врубаем боевую музыку
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTrack("fight");
            AudioManager.Instance.RestoreVolume();
        }

        player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        playerHP = player.GetComponent<HealthComponent>();
        playerHP.Initialize(maxHealth);
        playerHP.invincible = false; 
        playerHP.OnDeath += OnPlayerDied;
        playerHP.OnHit += OnPlayerHit;

        if (startingWeapon != null)
            player.EquipWeapon(startingWeapon);

        // hud обновить
        var hud = FindObjectOfType<HUDController>();
        if (hud != null)
        {
            hud.UpdateGold(sessionGold);
            hud.UpdateHealthBar(maxHealth, maxHealth);
        }

        var wm = WaveManager.Instance;
        if (wm != null)
        {
            wm.Init(player.transform);
            wm.StartLevel(currentLevel);
        }
    }

    public void PauseGame(bool pause)
    {
        if (pause) { State = GameState.Paused; Time.timeScale = 0; }
        else { State = GameState.Playing; Time.timeScale = 1; }
    }

    public void AddGold(int amount)
    {
        sessionGold += amount;
        var hud = FindObjectOfType<HUDController>();
        if (hud != null) hud.UpdateGold(sessionGold);
    }

    public int GetGold() => sessionGold;
    public int GetWeaponDamage() => weaponDamage;
    public int GetMaxHealth() => maxHealth;
    public float GetCurrentHP() => playerHP != null ? playerHP.GetHP() : 0;

    public void OnLevelComplete()
    {
        State = GameState.LevelComplete;

        // тише музыку пока прокачиваемся
        if (AudioManager.Instance != null)
            AudioManager.Instance.DimMusic();

        // подобрать все монеты
        var coins = FindObjectsOfType<GoldCoin>();
        foreach (var coin in coins)
        {
            coin.CollectSilently();
            Destroy(coin.gameObject);
        }
        // убрать лечилки
        foreach (var heal in FindObjectsOfType<HealPickup>())
            Destroy(heal.gameObject);

        var hud = FindObjectOfType<HUDController>();
        if (hud != null) hud.UpdateGold(sessionGold);

        Time.timeScale = 0;

        if (currentLevel >= maxLevel)
        {
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Epilogue");
        }
        else
        {
            var lcUI = FindObjectOfType<LevelCompleteUI>();
            if (lcUI != null) lcUI.Show(currentLevel);
        }
    }

    // --- апгрейды ---
    public bool TryUpgradeDamage()
    {
        if (sessionGold < 2) return false;
        sessionGold -= 2;
        weaponDamage++;
        var hud = FindObjectOfType<HUDController>();
        if (hud != null) hud.UpdateGold(sessionGold);
        return true;
    }

    public bool TryUpgradeHealth()
    {
        if (sessionGold < 2) return false;
        sessionGold -= 2;
        maxHealth += 3;
        // хилим до максимума
        if (playerHP != null)
            playerHP.Initialize(maxHealth);
        var hud = FindObjectOfType<HUDController>();
        if (hud != null)
        {
            hud.UpdateGold(sessionGold);
            hud.UpdateHealthBar(maxHealth, maxHealth);
        }
        return true;
    }

    public void GoToNextLevel()
    {
        currentLevel++;
        Time.timeScale = 1;

        // музыку назад на норм
        if (AudioManager.Instance != null)
            AudioManager.Instance.RestoreVolume();

        foreach (var e in FindObjectsOfType<EnemyAI>())
            Destroy(e.gameObject);
        foreach (var s in FindObjectsOfType<EnemySword>())
            Destroy(s.gameObject);
        foreach (var f in FindObjectsOfType<Fireball>())
            Destroy(f.gameObject);
        foreach (var c in FindObjectsOfType<GoldCoin>())
            Destroy(c.gameObject);
        foreach (var h in FindObjectsOfType<HealPickup>())
            Destroy(h.gameObject);

        // игрок в центр
        if (player != null)
        {
            player.transform.position = new Vector3(0, -10f, 0);
            player.rb.velocity = Vector2.zero;
            player.rb.angularVelocity = 0f;
        }

        if (playerHP != null)
        {
            playerHP.OnDeath -= OnPlayerDied;
            playerHP.OnHit -= OnPlayerHit;
            playerHP.Initialize(maxHealth);
            playerHP.OnDeath += OnPlayerDied;
            playerHP.OnHit += OnPlayerHit;
        }

        State = GameState.Playing;
        WaveManager.Instance.StartLevel(currentLevel);
    }

    public void EndLevel(bool isVictory)
    {
        State = GameState.GameOver;
        Time.timeScale = 0;
        var goUI = FindObjectOfType<GameOverUI>();
        if (goUI != null) goUI.Show(currentLevel, 0);
    }

    void OnPlayerDied()
    {
        playerHP.OnDeath -= OnPlayerDied;
        playerHP.OnHit -= OnPlayerHit;
        EndLevel(false);
    }

    void OnPlayerHit()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound("player_hit");
    }

    public int GetCurrentLevel() => currentLevel;
}
