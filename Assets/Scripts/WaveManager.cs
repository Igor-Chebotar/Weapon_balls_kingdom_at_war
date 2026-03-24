using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [SerializeField] EnemyData[] enemyTypes;
    [SerializeField] GameObject enemyPrefab;

    int currentLevel = 0;
    int aliveCount = 0;
    Transform playerTransform;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // гарантируем что данные врагов корректные
        EnsureEnemyData();
    }

    // создаём данные врагов прямо в рантайме если что-то не так
    void EnsureEnemyData()
    {
        bool needRebuild = enemyTypes == null || enemyTypes.Length < 3;
        if (!needRebuild)
        {
            // проверяем что типы правильные
            for (int i = 0; i < 3; i++)
            {
                if (enemyTypes[i] == null) { needRebuild = true; break; }
            }
        }
        // доп. проверка — если первый враг имеет дефолтный HP (5), значит SO не обновился
        if (!needRebuild && enemyTypes[0] != null && enemyTypes[0].maxHealth == 5f)
            needRebuild = true;

        if (!needRebuild) return;

        
        enemyTypes = new EnemyData[3];

        // Knight
        var knight = ScriptableObject.CreateInstance<EnemyData>();
        knight.enemyName = "Knight";
        knight.type = EnemyType.Knight;
        knight.maxHealth = 6;
        knight.moveSpeed = 7.5f;
        knight.attackDamage = 1;
        knight.mass = 3;
        knight.color = new Color(0.87f, 0.72f, 0.53f);
        knight.stunResistance = 8f;
        enemyTypes[0] = knight;

        // HeavyKnight
        var heavy = ScriptableObject.CreateInstance<EnemyData>();
        heavy.enemyName = "HeavyKnight";
        heavy.type = EnemyType.HeavyKnight;
        heavy.maxHealth = 20;
        heavy.moveSpeed = 3.5f;
        heavy.attackDamage = 2;
        heavy.mass = 5;
        heavy.color = new Color(0.3f, 0.3f, 0.3f);
        heavy.stunResistance = 12f;
        enemyTypes[1] = heavy;

        // King
        var king = ScriptableObject.CreateInstance<EnemyData>();
        king.enemyName = "King";
        king.type = EnemyType.King;
        king.maxHealth = 12;
        king.moveSpeed = 2.5f;
        king.attackDamage = 3;
        king.mass = 5;
        king.color = new Color(0.9f, 0.15f, 0.15f);
        king.stunResistance = 15f;
        enemyTypes[2] = king;
    }

    public void Init(Transform player)
    {
        playerTransform = player;
    }

    public void StartLevel(int level)
    {
        currentLevel = level;
        aliveCount = 0;

        switch (level)
        {
            case 1: SpawnLevel1(); break;
            case 2: SpawnLevel2(); break;
            case 3: SpawnLevel3(); break;
            default: SpawnLevel1(); break;
        }
    }

    // позиция игрока при старте уровня
    static readonly Vector2 playerStart = new Vector2(0, -10f);

    void SpawnLevel1()
    {
        SpawnEnemy(0, new Vector2(-5.6f, 5.6f));
        SpawnEnemy(0, new Vector2(5.6f, 5.6f));
        SpawnEnemy(0, new Vector2(0, 8.4f));
        SpawnEnemy(0, new Vector2(-8.4f, 2.8f));
        SpawnEnemy(0, new Vector2(8.4f, 2.8f));
    }

    void SpawnLevel2()
    {
        SpawnEnemy(0, new Vector2(-7f, 4.2f));
        SpawnEnemy(0, new Vector2(7f, 4.2f));
        SpawnEnemy(0, new Vector2(0, 7f));
        // тяжёлый рыцарь летит на игрока
        Vector2 heavyPos = new Vector2(0, 2.8f);
        SpawnEnemy(1, heavyPos, (playerStart - heavyPos).normalized);
    }

    void SpawnLevel3()
    {
        // король летит вбок, не на игрока
        SpawnEnemy(2, new Vector2(0, 10f), new Vector2(1f, 0f));
        // тяжёлые летят на игрока
        Vector2 h1 = new Vector2(-7f, 4.2f);
        Vector2 h2 = new Vector2(7f, 4.2f);
        SpawnEnemy(1, h1, (playerStart - h1).normalized);
        SpawnEnemy(1, h2, (playerStart - h2).normalized);
    }

    void SpawnEnemy(int typeIdx, Vector2 pos, Vector2 initDir = default)
    {
        if (enemyTypes == null || typeIdx >= enemyTypes.Length || enemyTypes[typeIdx] == null)
        {
            
            return;
        }

        var obj = Instantiate(enemyPrefab, pos, Quaternion.identity);
        var ai = obj.GetComponent<EnemyAI>();
        if (ai == null) return;

        ai.ConfigureFromData(enemyTypes[typeIdx]);

        // если задано направление — переопределяем начальный импульс
        if (initDir != default)
            ai.SetInitialDirection(initDir);

        aliveCount++;
    }

    public void RegisterEnemyDeath()
    {
        aliveCount--;
        if (aliveCount <= 0)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnLevelComplete();
        }
    }

    public int GetCurrentWave() => currentLevel;
}
