#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;

public class ProjectAutoSetup
{
    [MenuItem("WeaponBalls/Setup Project")]
    public static void SetupProject()
    {
        if (!EditorUtility.DisplayDialog("Weapon Balls Setup",
            "Это создаст папки, префабы, SO, сцены и настроит проект.\nПродолжить?", "Да", "Отмена"))
            return;

        CreateFolders();
        CopyAudioToResources();
        SetupLayers();
        SetupCollisionMatrix();
        // top-down: убрать гравитацию
        Physics2D.gravity = Vector2.zero;
        var mats = CreateMaterials();
        var prefabs = CreatePrefabs(mats);
        var soData = CreateScriptableObjects(prefabs);
        CopyEnemyDataToResources();
        CreateGameScene(prefabs, soData, mats);
        CreateMainMenuScene();
        CreateStoryScene();
        CreateEpilogueScene();
        SetupBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // открыть MainMenu чтоб Play запускал с неё
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

        Debug.Log("=== Weapon Balls: проект собран! ===");
    }

    // ======== ПАПКИ ========

    static void CreateFolders()
    {
        string[] folders = {
            "Assets/Scripts",
            "Assets/Prefabs",
            "Assets/ScriptableObjects",
            "Assets/Scenes",
            "Assets/Materials",
            "Assets/Editor",
            "Assets/Textures",
            "Assets/Audio",
            "Assets/Fonts",
            "Assets/Resources",
            "Assets/Resources/Music"
        };

        foreach (var f in folders)
        {
            if (!AssetDatabase.IsValidFolder(f))
            {
                string parent = Path.GetDirectoryName(f).Replace("\\", "/");
                string name = Path.GetFileName(f);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }

    // ======== АУДИО В RESOURCES ========

    static void CopyEnemyDataToResources()
    {
        // копируем EnemyData SO в Resources
        string[] names = { "EnemyData_Knight", "EnemyData_HeavyKnight", "EnemyData_King" };
        foreach (var n in names)
        {
            string src = "Assets/ScriptableObjects/" + n + ".asset";
            string dst = "Assets/Resources/" + n + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(dst) != null)
                AssetDatabase.DeleteAsset(dst);
            if (System.IO.File.Exists(src))
                AssetDatabase.CopyAsset(src, dst);
        }

        // копируем шрифт в Resources
        string fontSrc = "Assets/Fonts/Lora.ttf";
        string fontDst = "Assets/Resources/Lora.ttf";
        if (AssetDatabase.LoadAssetAtPath<Object>(fontDst) != null)
            AssetDatabase.DeleteAsset(fontDst);
        if (System.IO.File.Exists(fontSrc))
            AssetDatabase.CopyAsset(fontSrc, fontDst);

        // копируем спрайты мечей в Resources
        string[] swordFiles = { "sword_player", "sword_heavy" };
        foreach (var sw in swordFiles)
        {
            string swSrc = "Assets/Textures/" + sw + ".png";
            string swDst = "Assets/Resources/" + sw + ".png";
            if (AssetDatabase.LoadAssetAtPath<Object>(swDst) != null)
                AssetDatabase.DeleteAsset(swDst);
            if (System.IO.File.Exists(swSrc))
            {
                AssetDatabase.CopyAsset(swSrc, swDst);
                var imp = AssetImporter.GetAtPath(swDst) as TextureImporter;
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spritePixelsPerUnit = 32f;
                    imp.alphaIsTransparency = true;
                    imp.filterMode = FilterMode.Point;
                    imp.maxTextureSize = 512;
                    // pivot у рукояти — лезвие торчит наружу
                    imp.spritePivot = new Vector2(0.5f, 0.12f);
                    var settings = new TextureImporterSettings();
                    imp.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    imp.SetTextureSettings(settings);
                    imp.SaveAndReimport();
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void CopyAudioToResources()
    {
        // копируем музыку
        string[] tracks = { "music_menu", "music_fight", "music_final" };
        foreach (var t in tracks)
        {
            string src = "Assets/Audio/" + t + ".mp3";
            string dst = "Assets/Resources/Music/" + t + ".mp3";
            if (System.IO.File.Exists(src) && !System.IO.File.Exists(dst))
                AssetDatabase.CopyAsset(src, dst);
        }

        // копируем SFX
        if (!AssetDatabase.IsValidFolder("Assets/Resources/SFX"))
            AssetDatabase.CreateFolder("Assets/Resources", "SFX");

        string[] sfxFiles = {
            "fireball_shot.mp3", "player_hit.mp3", "king_hit.mp3",
            "heal.mp3", "coin_pickup.mp3", "button_click.wav",
            "knight_hit.mp3", "heavy_knight_hit.mp3"
        };
        foreach (var f in sfxFiles)
        {
            string src = "Assets/Audio/SFX/" + f;
            string dst = "Assets/Resources/SFX/" + f;
            if (System.IO.File.Exists(src) && !System.IO.File.Exists(dst))
                AssetDatabase.CopyAsset(src, dst);
        }

        AssetDatabase.Refresh();
    }

    // ======== СЛОИ ========

    static void SetupLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        SetLayer(layers, 8, "Player");
        SetLayer(layers, 9, "Enemy");
        SetLayer(layers, 10, "Weapon");
        SetLayer(layers, 11, "Projectile");

        tagManager.ApplyModifiedProperties();
    }

    static void SetLayer(SerializedProperty layers, int idx, string name)
    {
        SerializedProperty slot = layers.GetArrayElementAtIndex(idx);
        if (string.IsNullOrEmpty(slot.stringValue))
            slot.stringValue = name;
    }

    static int LayerIdx(string name)
    {
        // хардкод потому что мы только что создали
        switch (name)
        {
            case "Player": return 8;
            case "Enemy": return 9;
            case "Weapon": return 10;
            case "Projectile": return 11;
            default: return 0;
        }
    }

    // ======== COLLISION MATRIX ========

    static void SetupCollisionMatrix()
    {
        Physics2D.IgnoreLayerCollision(LayerIdx("Player"), LayerIdx("Weapon"), true);
        Physics2D.IgnoreLayerCollision(LayerIdx("Weapon"), LayerIdx("Weapon"), true);
        Physics2D.IgnoreLayerCollision(LayerIdx("Player"), LayerIdx("Projectile"), true);
        Physics2D.IgnoreLayerCollision(LayerIdx("Weapon"), LayerIdx("Projectile"), true);
        // монеты (IgnoreRaycast=2) не мешают никому кроме игрока
        Physics2D.IgnoreLayerCollision(2, LayerIdx("Enemy"), true);
        Physics2D.IgnoreLayerCollision(2, LayerIdx("Weapon"), true);
        Physics2D.IgnoreLayerCollision(2, LayerIdx("Projectile"), true);
        Physics2D.IgnoreLayerCollision(2, 2, true); // монеты друг с другом
    }

    // ======== МАТЕРИАЛЫ ========

    struct MatSet
    {
        public Material player, rusher, tank, weapon, projectile, wall;
    }

    static MatSet CreateMaterials()
    {
        MatSet m;
        m.player = MakeMat("Mat_Player", new Color(0.55f, 0.55f, 0.6f));
        m.rusher = MakeMat("Mat_Enemy_Rusher", new Color(0.9f, 0.2f, 0.2f));
        m.tank = MakeMat("Mat_Enemy_Tank", new Color(0.5f, 0.1f, 0.1f));
        m.weapon = MakeMat("Mat_Weapon", new Color(0.6f, 0.6f, 0.6f));
        m.projectile = MakeMat("Mat_Projectile", new Color(1f, 0.9f, 0.2f));
        m.wall = MakeMat("Mat_Wall", new Color(0.3f, 0.3f, 0.3f));
        return m;
    }

    static Material MakeMat(string name, Color col)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = col;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ======== ПРЕФАБЫ ========

    struct PrefabSet
    {
        public GameObject player, meleeWeapon, rangedWeapon, projectile, enemy, damagePopup;
    }

    static PrefabSet CreatePrefabs(MatSet mats)
    {
        PrefabSet p;
        p.player = CreatePlayerPrefab(mats.player);
        p.meleeWeapon = CreateMeleeWeaponPrefab(mats.weapon);
        p.rangedWeapon = CreateRangedWeaponPrefab(mats.weapon);
        p.projectile = CreateProjectilePrefab(mats.projectile);
        p.enemy = CreateEnemyPrefab(mats.weapon); // нейтральный серый, цвет задаётся из SO
        p.damagePopup = CreateDamagePopupPrefab();
        return p;
    }

    static GameObject CreatePlayerPrefab(Material mat)
    {
        string path = "Assets/Prefabs/Player.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject obj = new GameObject("Player");
        obj.layer = LayerIdx("Player");
        obj.transform.localScale = new Vector3(2.1f, 2.1f, 1f);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite();
        sr.material = mat;
        sr.color = mat.color;

        var rb = obj.AddComponent<Rigidbody2D>();
        rb.mass = 2f;
        rb.angularDrag = 3f;
        rb.gravityScale = 0;
        rb.drag = 0f;

        var pCol = obj.AddComponent<CircleCollider2D>();
        var pBounceMat = new PhysicsMaterial2D("PlayerBounce");
        pBounceMat.bounciness = 1f;
        pBounceMat.friction = 0f;
        string pMatPath = "Assets/Materials/PlayerBounce.physicsMaterial2D";
        if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(pMatPath) == null)
            AssetDatabase.CreateAsset(pBounceMat, pMatPath);
        else
            pBounceMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(pMatPath);
        pCol.sharedMaterial = pBounceMat;
        obj.AddComponent<HealthComponent>();
        obj.AddComponent<PlayerController>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    static GameObject CreateMeleeWeaponPrefab(Material mat)
    {
        string path = "Assets/Prefabs/MeleeWeapon.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject obj = new GameObject("MeleeWeapon");
        obj.layer = LayerIdx("Weapon");

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.material = mat;
        sr.color = mat.color;
        obj.transform.localScale = new Vector3(0.4f, 1.5f, 1f);

        var rb = obj.AddComponent<Rigidbody2D>();
        rb.mass = 2f;
        rb.gravityScale = 0;

        var col = obj.AddComponent<BoxCollider2D>();

        obj.AddComponent<MeleeWeapon>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    static GameObject CreateRangedWeaponPrefab(Material mat)
    {
        string path = "Assets/Prefabs/RangedWeapon.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject obj = new GameObject("RangedWeapon");
        obj.layer = LayerIdx("Weapon");

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.material = mat;
        sr.color = new Color(0.5f, 0.4f, 0.2f); // коричневатый для лука
        obj.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        var rb = obj.AddComponent<Rigidbody2D>();
        rb.mass = 1f;
        rb.gravityScale = 0;

        obj.AddComponent<BoxCollider2D>();
        obj.AddComponent<RangedWeapon>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    static GameObject CreateProjectilePrefab(Material mat)
    {
        string path = "Assets/Prefabs/Arrow.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject obj = new GameObject("Arrow");
        obj.layer = LayerIdx("Projectile");

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite();
        sr.material = mat;
        sr.color = mat.color;
        obj.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

        var rb = obj.AddComponent<Rigidbody2D>();
        rb.mass = 0.1f;
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        obj.AddComponent<CircleCollider2D>();
        obj.AddComponent<Projectile>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    static GameObject CreateEnemyPrefab(Material mat)
    {
        string path = "Assets/Prefabs/Enemy.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject obj = new GameObject("Enemy");
        obj.layer = LayerIdx("Enemy");
        obj.transform.localScale = new Vector3(2.1f, 2.1f, 1f);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite();
        sr.material = mat;
        sr.color = mat.color;

        var rb = obj.AddComponent<Rigidbody2D>();
        rb.mass = 1.5f;
        rb.gravityScale = 0;
        rb.drag = 0f;
        rb.angularDrag = 0f;

        var col = obj.AddComponent<CircleCollider2D>();

        // идеальный отскок
        var bounceMat = new PhysicsMaterial2D("EnemyBounce");
        bounceMat.bounciness = 1f;
        bounceMat.friction = 0f;
        string matPath = "Assets/Materials/EnemyBounce.physicsMaterial2D";
        if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath) == null)
            AssetDatabase.CreateAsset(bounceMat, matPath);
        else
            bounceMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
        col.sharedMaterial = bounceMat;
        obj.AddComponent<HealthComponent>();
        obj.AddComponent<EnemyAI>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    static GameObject CreateDamagePopupPrefab()
    {
        string path = "Assets/Prefabs/DamagePopup.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject obj = new GameObject("DamagePopup");

        var canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        var rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2f, 1f);

        // текст
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        var text = txtObj.AddComponent<Text>();
        text.text = "0";
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = GetCustomFont();

        var txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.sizeDelta = new Vector2(200, 100);
        txtRt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        obj.AddComponent<DamagePopup>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    // ======== SCRIPTABLE OBJECTS ========

    struct SOData
    {
        public WeaponData sword, hammer, bow;
        public EnemyData knight, heavyKnight, king;
    }

    static SOData CreateScriptableObjects(PrefabSet prefabs)
    {
        SOData d;

        // оружие
        d.sword = CreateOrLoadSO<WeaponData>("Assets/ScriptableObjects/WeaponData_Sword.asset");
        d.sword.Name = "Sword";
        d.sword.Type = WeaponType.Melee;
        d.sword.Mass = 2f;
        d.sword.BaseDamage = 1.5f;
        d.sword.AttackRate = 0;
        d.sword.ProjectilePrefab = null;
        EditorUtility.SetDirty(d.sword);

        d.hammer = CreateOrLoadSO<WeaponData>("Assets/ScriptableObjects/WeaponData_Hammer.asset");
        d.hammer.Name = "Hammer";
        d.hammer.Type = WeaponType.Melee;
        d.hammer.Mass = 5f;
        d.hammer.BaseDamage = 3f;
        d.hammer.AttackRate = 0;
        d.hammer.ProjectilePrefab = null;
        EditorUtility.SetDirty(d.hammer);

        d.bow = CreateOrLoadSO<WeaponData>("Assets/ScriptableObjects/WeaponData_Bow.asset");
        d.bow.Name = "Bow";
        d.bow.Type = WeaponType.Ranged;
        d.bow.Mass = 1f;
        d.bow.BaseDamage = 2f;
        d.bow.AttackRate = 0.5f;
        d.bow.ProjectilePrefab = prefabs.projectile;
        EditorUtility.SetDirty(d.bow);

        // враги
        d.knight = CreateOrLoadSO<EnemyData>("Assets/ScriptableObjects/EnemyData_Knight.asset");
        d.knight.enemyName = "Knight";
        d.knight.type = EnemyType.Knight;
        d.knight.maxHealth = 6;
        d.knight.moveSpeed = 7.5f;
        d.knight.attackDamage = 1;
        d.knight.mass = 3;
        d.knight.color = new Color(0.87f, 0.72f, 0.53f);
        d.knight.stunResistance = 8f;
        EditorUtility.SetDirty(d.knight);

        d.heavyKnight = CreateOrLoadSO<EnemyData>("Assets/ScriptableObjects/EnemyData_HeavyKnight.asset");
        d.heavyKnight.enemyName = "HeavyKnight";
        d.heavyKnight.type = EnemyType.HeavyKnight;
        d.heavyKnight.maxHealth = 20;
        d.heavyKnight.moveSpeed = 3.5f;
        d.heavyKnight.attackDamage = 2;
        d.heavyKnight.mass = 5;
        d.heavyKnight.color = new Color(0.3f, 0.3f, 0.3f);
        d.heavyKnight.stunResistance = 12f;
        EditorUtility.SetDirty(d.heavyKnight);

        d.king = CreateOrLoadSO<EnemyData>("Assets/ScriptableObjects/EnemyData_King.asset");
        d.king.enemyName = "King";
        d.king.type = EnemyType.King;
        d.king.maxHealth = 12;
        d.king.moveSpeed = 2.5f;
        d.king.attackDamage = 3;
        d.king.mass = 5;
        d.king.color = new Color(0.9f, 0.15f, 0.15f);
        d.king.stunResistance = 15f;
        EditorUtility.SetDirty(d.king);

        // принудительно сохранить SO на диск до копирования в Resources
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return d;
    }

    static T CreateOrLoadSO<T>(string path) where T : ScriptableObject
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        T so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    // ======== СЦЕНА GAME ========

    static void CreateGameScene(PrefabSet prefabs, SOData soData, MatSet mats)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Арена (стены) ---
        float arenaSize = 28f;
        float wallThick = 1f;

        // --- Фон арены (текстура пола) ---
        var floorObj = new GameObject("ArenaFloor");
        floorObj.transform.position = Vector3.zero;
        var floorSR = floorObj.AddComponent<SpriteRenderer>();
        floorSR.sortingOrder = -10;
        // загружаем текстуры
        var streetSprite = LoadTextureAsSprite("Assets/Textures/floor_street.png", 300f);
        var castleSprite = LoadTextureAsSprite("Assets/Textures/floor_castle.png", 200f);
        var throneSprite = LoadTextureAsSprite("Assets/Textures/floor_throne.png", 200f);
        if (streetSprite != null) floorSR.sprite = streetSprite;
        // масштаб чтобы покрыть арену
        floorObj.transform.localScale = new Vector3(arenaSize / floorSR.bounds.size.x * floorObj.transform.localScale.x,
            arenaSize / floorSR.bounds.size.y * floorObj.transform.localScale.y, 1f);

        var arenaBg = floorObj.AddComponent<ArenaBackground>();
        SetSerializedField(arenaBg, "streetFloor", streetSprite);
        SetSerializedField(arenaBg, "castleFloor", castleSprite);
        SetSerializedField(arenaBg, "throneFloor", throneSprite);

        CreateWall("Wall_Top", new Vector3(0, arenaSize / 2 + wallThick / 2, 0), new Vector3(arenaSize + wallThick * 2, wallThick, 1), mats.wall);
        CreateWall("Wall_Bottom", new Vector3(0, -arenaSize / 2 - wallThick / 2, 0), new Vector3(arenaSize + wallThick * 2, wallThick, 1), mats.wall);
        CreateWall("Wall_Left", new Vector3(-arenaSize / 2 - wallThick / 2, 0, 0), new Vector3(wallThick, arenaSize, 1), mats.wall);
        CreateWall("Wall_Right", new Vector3(arenaSize / 2 + wallThick / 2, 0, 0), new Vector3(wallThick, arenaSize, 1), mats.wall);

        // --- Фон за ареной: однотонные бордеры ---
        float borderSize = 50f;
        float boffset = arenaSize / 2 + wallThick + borderSize / 2;
        // цвет бордеров по умолчанию (уровень 1), ArenaBackground будет менять
        Color borderColor = new Color(0.28f, 0.22f, 0.15f); // тёплый коричневый
        CreateBorderOverlay("Border_Top", new Vector3(0, boffset, 0), new Vector3(arenaSize + borderSize * 2 + 20, borderSize, 1), borderColor);
        CreateBorderOverlay("Border_Bottom", new Vector3(0, -boffset, 0), new Vector3(arenaSize + borderSize * 2 + 20, borderSize, 1), borderColor);
        CreateBorderOverlay("Border_Left", new Vector3(-boffset, 0, 0), new Vector3(borderSize, arenaSize + borderSize * 2 + 20, 1), borderColor);
        CreateBorderOverlay("Border_Right", new Vector3(boffset, 0, 0), new Vector3(borderSize, arenaSize + borderSize * 2 + 20, 1), borderColor);

        // --- Игрок ---
        var playerObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.player);
        playerObj.transform.position = new Vector3(0, -10f, 0);
        playerObj.name = "Player";

        // --- Камера ---
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 12;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.28f, 0.22f, 0.15f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.gameObject.AddComponent<CameraFollow>();
        }

        // --- Менеджеры ---
        // GameManager
        var gmObj = new GameObject("GameManager");
        var gm = gmObj.AddComponent<GameManager>();
        SetSerializedField(gm, "startingWeapon", soData.sword);

        // WaveManager
        var wmObj = new GameObject("WaveManager");
        var wm = wmObj.AddComponent<WaveManager>();
        SetSerializedField(wm, "enemyPrefab", prefabs.enemy);
        // принудительно сохраняем SO перед назначением
        AssetDatabase.SaveAssets();
        SetSerializedArray(wm, "enemyTypes", new Object[] { soData.knight, soData.heavyKnight, soData.king });
        // дублируем через рефлексию на случай если Serialized не подхватил
        var etField = typeof(WaveManager).GetField("enemyTypes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (etField != null)
            etField.SetValue(wm, new EnemyData[] { soData.knight, soData.heavyKnight, soData.king });
        SetSerializedField(wm, "spawnRadius", 12f);
        SetSerializedField(wm, "baseEnemyCount", 3);
        SetSerializedField(wm, "wavePause", 3f);

        // PoolManager
        var pmObj = new GameObject("PoolManager");
        var pm = pmObj.AddComponent<PoolManager>();
        // пулы настроим через serialized
        SetPoolManagerEntries(pm, prefabs);

        // AudioManager
        var amObj = new GameObject("AudioManager");
        var am = amObj.AddComponent<AudioManager>();
        // аудио-источники
        var sfxSrc = amObj.AddComponent<AudioSource>();
        var musicSrc = amObj.AddComponent<AudioSource>();
        musicSrc.loop = true;
        SetSerializedField(am, "sfxSource", sfxSrc);
        SetSerializedField(am, "musicSource", musicSrc);

        // --- HUD Canvas ---
        CreateHUDCanvas(prefabs);

        // EventSystem
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
    }

    static void CreateWall(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;
        obj.transform.localScale = scale;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.material = mat;
        sr.color = mat.color;

        var col = obj.AddComponent<BoxCollider2D>();

        // статический rigidbody чтоб враги не пролетали
        var rb = obj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    static void CreateBorderOverlay(string name, Vector3 pos, Vector3 scale, Color color, Sprite texSprite = null)
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 50; // поверх всего

        if (texSprite != null)
        {
            sr.sprite = texSprite;
            sr.color = Color.white;
            // масштабируем чтобы покрыть нужную площадь
            float needW = scale.x;
            float needH = scale.y;
            float sprW = texSprite.bounds.size.x;
            float sprH = texSprite.bounds.size.y;
            obj.transform.localScale = new Vector3(needW / sprW, needH / sprH, 1f);
        }
        else
        {
            sr.sprite = MakeSquareSprite();
            sr.color = color;
            obj.transform.localScale = scale;
        }
    }

    static void CreateHUDCanvas(PrefabSet prefabs)
    {
        // --- Основной Canvas ---
        var canvasObj = new GameObject("HUDCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        var hud = canvasObj.AddComponent<HUDController>();

        // --- HP Bar (левый верхний угол) ---
        var hpBarObj = CreateUIElement("HPBar", canvasObj.transform, new Vector2(0, 0), new Vector2(350, 35), new Vector2(0, 1));
        var hpRt = hpBarObj.GetComponent<RectTransform>();
        hpRt.anchorMin = new Vector2(0, 1);
        hpRt.anchorMax = new Vector2(0, 1);
        hpRt.pivot = new Vector2(0, 1);
        hpRt.anchoredPosition = new Vector2(20, -20);
        var slider = hpBarObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 100;

        // фон слайдера
        var bg = CreateUIElement("Background", hpBarObj.transform, Vector2.zero, new Vector2(350, 35), new Vector2(0.5f, 0.5f));
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);

        // заливка
        var fillArea = CreateUIElement("Fill Area", hpBarObj.transform, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = Vector2.zero;
        fillAreaRt.offsetMax = Vector2.zero;

        var fill = CreateUIElement("Fill", fillArea.transform, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 0.2f);

        slider.fillRect = fillRt;

        SetSerializedField(hud, "hpBar", slider);

        // --- Gold display (под HP баром) ---
        // жёлтый кружок (используем круглый спрайт)
        var goldIconObj = new GameObject("GoldIcon");
        goldIconObj.transform.SetParent(canvasObj.transform, false);
        var giRt = goldIconObj.AddComponent<RectTransform>();
        giRt.anchorMin = new Vector2(0, 1);
        giRt.anchorMax = new Vector2(0, 1);
        giRt.pivot = new Vector2(0, 1);
        giRt.anchoredPosition = new Vector2(20, -65);
        giRt.sizeDelta = new Vector2(28, 28);
        var giImg = goldIconObj.AddComponent<Image>();
        giImg.sprite = MakeCircleSprite();
        giImg.color = new Color(1f, 0.85f, 0.1f);

        // текст рядом с иконкой
        var goldTxtObj = CreateUIText("GoldText", canvasObj.transform, new Vector3(0, 0, 0), "0", 20);
        var goldTxtRt = goldTxtObj.GetComponent<RectTransform>();
        goldTxtRt.anchorMin = new Vector2(0, 1);
        goldTxtRt.anchorMax = new Vector2(0, 1);
        goldTxtRt.pivot = new Vector2(0, 1);
        goldTxtRt.anchoredPosition = new Vector2(55, -63);
        goldTxtRt.sizeDelta = new Vector2(100, 30);
        goldTxtObj.alignment = TextAnchor.MiddleLeft;

        SetSerializedField(hud, "goldText", goldTxtObj);
        SetSerializedField(hud, "goldIcon", giImg);

        // --- Level Text (снизу по центру) ---
        var levelTxt = CreateUIText("LevelText", canvasObj.transform, new Vector3(0, 0, 0), "Уровень 1", 22);
        var levelRt = levelTxt.GetComponent<RectTransform>();
        levelRt.anchorMin = new Vector2(0.5f, 0);
        levelRt.anchorMax = new Vector2(0.5f, 0);
        levelRt.pivot = new Vector2(0.5f, 0);
        levelRt.anchoredPosition = new Vector2(0, 20);
        SetSerializedField(hud, "levelText", levelTxt);

        // --- DamagePopup ref ---
        SetSerializedField(hud, "damagePopupPrefab", prefabs.damagePopup);

        // --- Pause Panel ---
        var pausePanel = CreateUIPanel("PausePanel", canvasObj.transform, new Color(0, 0, 0, 0.7f));
        pausePanel.SetActive(false);

        CreateUIText("PauseTitle", pausePanel.transform, new Vector3(0, 80, 0), "ПАУЗА", 36);
        var resumeBtn = CreateUIButton("ResumeBtn", pausePanel.transform, new Vector3(0, 0, 0), "Продолжить");
        var menuBtnP = CreateUIButton("MenuBtn", pausePanel.transform, new Vector3(0, -60, 0), "В меню");

        var pauseUI = canvasObj.AddComponent<PauseMenuUI>();
        SetSerializedField(pauseUI, "pausePanel", pausePanel);
        SetSerializedField(pauseUI, "resumeBtn", resumeBtn.GetComponent<Button>());
        SetSerializedField(pauseUI, "menuBtn", menuBtnP.GetComponent<Button>());

        // --- Level Complete Panel ---
        var lcPanel = CreateUIPanel("LevelCompletePanel", canvasObj.transform, new Color(0, 0, 0, 0.7f));
        lcPanel.SetActive(false);

        var lcText = CreateUIText("LevelCompleteText", lcPanel.transform, new Vector3(0, 100, 0), "Уровень пройден!", 32);

        // кнопка далее
        var nextBtn = CreateUIButton("NextLevelBtn", lcPanel.transform, new Vector3(0, 50, 0), "Начать уровень");
        nextBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 50);

        // прокачка урона
        var dmgVal = CreateUIText("DmgValue", lcPanel.transform, new Vector3(-40, -50, 0), "Урон от меча: 1", 18);
        dmgVal.alignment = TextAnchor.MiddleLeft;
        dmgVal.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 30);
        var dmgBtn = CreateUIButton("DmgUpgradeBtn", lcPanel.transform, new Vector3(140, -50, 0), "2 зм");
        dmgBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 35);

        // прокачка здоровья
        var hpVal = CreateUIText("HpValue", lcPanel.transform, new Vector3(-40, -105, 0), "Здоровье: 5/5", 18);
        hpVal.alignment = TextAnchor.MiddleLeft;
        hpVal.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 30);
        var hpBtn = CreateUIButton("HpUpgradeBtn", lcPanel.transform, new Vector3(140, -105, 0), "2 зм");
        hpBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 35);

        var lcUI = canvasObj.AddComponent<LevelCompleteUI>();
        SetSerializedField(lcUI, "panel", lcPanel);
        SetSerializedField(lcUI, "levelText", lcText);
        SetSerializedField(lcUI, "nextLevelBtn", nextBtn.GetComponent<Button>());
        SetSerializedField(lcUI, "dmgValueText", dmgVal);
        SetSerializedField(lcUI, "dmgUpgradeBtn", dmgBtn.GetComponent<Button>());
        SetSerializedField(lcUI, "hpValueText", hpVal);
        SetSerializedField(lcUI, "hpUpgradeBtn", hpBtn.GetComponent<Button>());

        // --- GameOver Panel ---
        var goPanel = CreateUIPanel("GameOverPanel", canvasObj.transform, new Color(0, 0, 0, 0.8f));
        goPanel.SetActive(false);

        var resultTxt = CreateUIText("ResultText", goPanel.transform, new Vector3(0, 80, 0), "Вы дошли до волны X", 28);
        var goldTxt = CreateUIText("GoldText", goPanel.transform, new Vector3(0, 30, 0), "Заработано: 0 золота", 20);
        var retryBtn = CreateUIButton("RetryBtn", goPanel.transform, new Vector3(-100, -50, 0), "Заново");
        var menuBtnGO = CreateUIButton("MenuBtnGO", goPanel.transform, new Vector3(100, -50, 0), "В меню");

        var goUI = canvasObj.AddComponent<GameOverUI>();
        SetSerializedField(goUI, "panel", goPanel);
        SetSerializedField(goUI, "resultText", resultTxt);
        SetSerializedField(goUI, "goldEarnedText", goldTxt);
        SetSerializedField(goUI, "retryBtn", retryBtn.GetComponent<Button>());
        SetSerializedField(goUI, "menuBtn", menuBtnGO.GetComponent<Button>());
    }

    // ======== СЦЕНА MAIN MENU ========

    static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // тёмный фон
        var cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = new Color(0.15f, 0.2f, 0.3f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        var canvasObj = new GameObject("MenuCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        // замок — на весь экран, покачивание вправо-влево
        var castleSprite = LoadTextureAsSprite("Assets/Textures/castle_menu.png", 100f);
        if (castleSprite != null)
        {
            var castleObj = new GameObject("CastleImage");
            castleObj.transform.SetParent(canvasObj.transform, false);
            var crt = castleObj.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(-50, -50);
            crt.offsetMax = new Vector2(50, 50);
            var cimg = castleObj.AddComponent<Image>();
            cimg.sprite = castleSprite;
            cimg.preserveAspect = false;
            cimg.raycastTarget = false;
            castleObj.AddComponent<MenuCastleFloat>();
        }

        // кнопки поверх замка, без названия
        var playBtn = CreateUIButton("PlayBtn", canvasObj.transform, new Vector3(0, -60, 0), "Играть");
        playBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 55);
        var quitBtn = CreateUIButton("QuitBtn", canvasObj.transform, new Vector3(0, -130, 0), "Выход");
        quitBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 55);

        var menuUI = canvasObj.AddComponent<MainMenuUI>();
        SetSerializedField(menuUI, "playBtn", playBtn.GetComponent<Button>());
        SetSerializedField(menuUI, "quitBtn", quitBtn.GetComponent<Button>());

        // EventSystem
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // AudioManager — живёт через DontDestroyOnLoad, создаём тут чтоб музыка была с самого начала
        var amObj = new GameObject("AudioManager");
        var am = amObj.AddComponent<AudioManager>();
        var sfxSrc = amObj.AddComponent<AudioSource>();
        var musicSrc = amObj.AddComponent<AudioSource>();
        musicSrc.loop = true;
        SetSerializedField(am, "sfxSource", sfxSrc);
        SetSerializedField(am, "musicSource", musicSrc);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    // ======== СЦЕНА STORY ========

    static void CreateStoryScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // чёрный фон через камеру
        var cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);

        var canvasObj = new GameObject("StoryCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        // фон — горящая деревня (размытая)
        var storyBgSprite = LoadTextureAsSprite("Assets/Textures/story_bg.png", 100f);
        if (storyBgSprite != null)
        {
            var bgObj = new GameObject("StoryBgImage");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(-50, -50);
            bgRt.offsetMax = new Vector2(50, 50);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = storyBgSprite;
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;
            // чуть затемнить чтобы текст читался
            bgImg.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            bgObj.transform.SetAsFirstSibling();
        }

        var storyTxt = CreateUIText("StoryText", canvasObj.transform, new Vector3(0, 80, 0),
            "Королевство гибнет под властью кровавого короля, чья жестокость погрузила земли в страх и хаос.\n\nРыцарь отправляется в поход, чтобы положить конец его тирании. Но сначала ему предстоит пройти через верных подданных короля и прорваться к самому трону.\n\nЛишь смерть короля сможет вернуть людям надежду.", 22);
        // растянуть текстовое поле чтобы влез весь текст
        storyTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 350);
        storyTxt.alignment = TextAnchor.MiddleCenter;
        storyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        storyTxt.verticalOverflow = VerticalWrapMode.Overflow;
        var startBtn = CreateUIButton("StartBtn", canvasObj.transform, new Vector3(0, -160, 0), "Начать уровень 1");
        startBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 50);

        var storyUI = canvasObj.AddComponent<StoryUI>();
        SetSerializedField(storyUI, "storyText", storyTxt);
        SetSerializedField(storyUI, "startBtn", startBtn.GetComponent<Button>());

        // EventSystem
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Story.unity");
    }

    // ======== СЦЕНА EPILOGUE ========

    static void CreateEpilogueScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);

        var canvasObj = new GameObject("EpilogueCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        // фон — тронный зал (размытый)
        var epiBgSprite = LoadTextureAsSprite("Assets/Textures/epilogue_bg.png", 100f);
        if (epiBgSprite != null)
        {
            var bgObj = new GameObject("EpilogueBgImage");
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(-50, -50);
            bgRt.offsetMax = new Vector2(50, 50);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = epiBgSprite;
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;
            bgImg.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            bgObj.transform.SetAsFirstSibling();
        }

        var storyTxt = CreateUIText("EpilogueText", canvasObj.transform, new Vector3(0, 80, 0),
            "Кровавый король мёртв, и вместе с ним пала его власть.\n\nСтрах больше не сковывает королевство, а тьма начинает отступать. Впереди ещё долгий путь к восстановлению, но мир наконец вернулся на эти земли.\n\nКоролевство снова сможет расцвести.", 22);
        // растянуть текстовое поле
        storyTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 350);
        storyTxt.alignment = TextAnchor.MiddleCenter;
        storyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        storyTxt.verticalOverflow = VerticalWrapMode.Overflow;
        var menuBtn = CreateUIButton("MenuBtn", canvasObj.transform, new Vector3(0, -160, 0), "В главное меню");
        menuBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 50);

        var epiUI = canvasObj.AddComponent<EpilogueUI>();
        SetSerializedField(epiUI, "storyText", storyTxt);
        SetSerializedField(epiUI, "menuBtn", menuBtn.GetComponent<Button>());

        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Epilogue.unity");
    }

    // ======== BUILD SETTINGS ========

    static void SetupBuildSettings()
    {
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Story.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Epilogue.unity", true)
        };
    }

    // ======== ТЕКСТУРЫ ========

    static Sprite LoadTextureAsSprite(string path, float pixelsPerUnit)
    {
        // настроить импорт как спрайт
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("Не удалось загрузить спрайт: " + path);
        return sprite;
    }

    // ======== УТИЛИТЫ (UI) ========

    static GameObject CreateUIElement(string name, Transform parent, Vector2 anchoredPos, Vector2 size, Vector2 pivot)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        rt.pivot = pivot;
        return obj;
    }

    static Font _customFont;
    static Font GetCustomFont()
    {
        if (_customFont != null) return _customFont;
        _customFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Lora.ttf");
        if (_customFont == null)
            _customFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return _customFont;
    }

    static Text CreateUIText(string name, Transform parent, Vector3 localPos, string content, int fontSize)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 50);
        rt.localPosition = localPos;

        var txt = obj.AddComponent<Text>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.font = GetCustomFont();

        // обводка для читабельности
        var outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        return txt;
    }

    static GameObject CreateUIButton(string name, Transform parent, Vector3 localPos, string label)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 45);
        rt.localPosition = localPos;

        // фон кнопки — Image белый, цвет через ColorBlock
        var img = obj.AddComponent<Image>();
        img.color = Color.white;

        // золотая обводка
        var outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0.75f, 0.6f, 0.25f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        var btn = obj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.12f, 0.06f, 0.9f);
        colors.highlightedColor = new Color(0.45f, 0.2f, 0.1f, 1f);
        colors.pressedColor = new Color(0.12f, 0.07f, 0.03f, 1f);
        colors.selectedColor = new Color(0.25f, 0.15f, 0.08f, 0.9f);
        colors.colorMultiplier = 1f;
        btn.colors = colors;

        // текст внутри кнопки
        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(obj.transform, false);
        var txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var txt = txtObj.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.95f, 0.85f, 0.55f); // золотистый текст
        txt.font = GetCustomFont();

        // тень на тексте кнопки
        var txtShadow = txtObj.AddComponent<Shadow>();
        txtShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        txtShadow.effectDistance = new Vector2(1f, -1f);

        return obj;
    }

    static GameObject CreateUIPanel(string name, Transform parent, Color bgColor)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.color = bgColor;

        return obj;
    }

    // ======== УТИЛИТЫ (Serialized поля) ========

    static void SetSerializedField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }

    static void SetSerializedField(Object target, string fieldName, float value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.floatValue = value;
            so.ApplyModifiedProperties();
        }
    }

    static void SetSerializedField(Object target, string fieldName, int value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.intValue = value;
            so.ApplyModifiedProperties();
        }
    }

    static void SetSerializedArray(Object target, string fieldName, Object[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null && prop.isArray)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedProperties();
        }
    }

    static void SetSerializedButtonArray(Object target, string fieldName, Button[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null && prop.isArray)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedProperties();
        }
    }

    static void SetSerializedTextArray(Object target, string fieldName, Text[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null && prop.isArray)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedProperties();
        }
    }

    static void SetPoolManagerEntries(PoolManager pm, PrefabSet prefabs)
    {
        var so = new SerializedObject(pm);
        var poolsProp = so.FindProperty("pools");
        if (poolsProp == null) return;

        poolsProp.arraySize = 2;

        // Arrow pool
        var entry0 = poolsProp.GetArrayElementAtIndex(0);
        entry0.FindPropertyRelative("tag").stringValue = "Arrow";
        entry0.FindPropertyRelative("prefab").objectReferenceValue = prefabs.projectile;
        entry0.FindPropertyRelative("startSize").intValue = 10;

        // DamagePopup pool
        var entry1 = poolsProp.GetArrayElementAtIndex(1);
        entry1.FindPropertyRelative("tag").stringValue = "DamagePopup";
        entry1.FindPropertyRelative("prefab").objectReferenceValue = prefabs.damagePopup;
        entry1.FindPropertyRelative("startSize").intValue = 10;

        so.ApplyModifiedProperties();
    }

    // ======== СПРАЙТЫ-ЗАГЛУШКИ ========

    static Sprite _circleSprite;
    static Sprite _squareSprite;

    static Sprite MakeCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;

        // ищем встроенный или создаём
        string path = "Assets/Materials/Circle.png";
        if (!File.Exists(path))
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = dist <= radius ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            AssetDatabase.Refresh();

            // настроить как спрайт
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.SaveAndReimport();
            }
        }

        _circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return _circleSprite;
    }

    static Sprite MakeSquareSprite()
    {
        if (_squareSprite != null) return _squareSprite;

        string path = "Assets/Materials/Square.png";
        if (!File.Exists(path))
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.SaveAndReimport();
            }
        }

        _squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return _squareSprite;
    }
}
#endif
