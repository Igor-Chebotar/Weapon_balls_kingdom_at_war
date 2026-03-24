using UnityEngine;

public class ArenaBackground : MonoBehaviour
{
    [SerializeField] Sprite streetFloor;
    [SerializeField] Sprite castleFloor;
    [SerializeField] Sprite throneFloor;

    SpriteRenderer sr;

    [SerializeField] float arenaSize = 28f;

    // цвета фона за ареной для каждого уровня
    // уровень 1 — тёплый коричневый (улица, булыжник)
    // уровень 2 — холодный серый (каменный замок)
    // уровень 3 — тёмно-бордовый (тронный зал, красные ковры)
    static readonly Color[] bgColors = {
        new Color(0.28f, 0.22f, 0.15f), // ур.1
        new Color(0.18f, 0.18f, 0.22f), // ур.2
        new Color(0.25f, 0.1f, 0.1f),   // ур.3
    };

    SpriteRenderer[] borders;
    Camera mainCam;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;

        // находим бордеры по имени
        var found = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var go in FindObjectsOfType<SpriteRenderer>())
        {
            if (go.gameObject.name.StartsWith("Border_"))
                found.Add(go);
        }
        borders = found.ToArray();

        UpdateFloor(1);
    }

    void Update()
    {
        if (GameManager.Instance != null)
            UpdateFloor(GameManager.Instance.GetCurrentLevel());
    }

    int lastLevel = -1;
    void UpdateFloor(int level)
    {
        if (level == lastLevel) return;
        lastLevel = level;
        if (sr == null) return;

        switch (level)
        {
            case 1: if (streetFloor != null) sr.sprite = streetFloor; break;
            case 2: if (castleFloor != null) sr.sprite = castleFloor; break;
            case 3: if (throneFloor != null) sr.sprite = throneFloor; break;
        }

        // подогнать масштаб под арену
        if (sr.sprite != null)
        {
            float w = sr.sprite.bounds.size.x;
            float h = sr.sprite.bounds.size.y;
            transform.localScale = new Vector3(arenaSize / w, arenaSize / h, 1f);
        }

        // меняем цвет фона
        int idx = Mathf.Clamp(level - 1, 0, bgColors.Length - 1);
        Color bg = bgColors[idx];

        // бордеры
        if (borders != null)
            foreach (var b in borders)
                if (b != null) b.color = bg;

        // камера
        if (mainCam != null)
            mainCam.backgroundColor = bg;
    }
}
