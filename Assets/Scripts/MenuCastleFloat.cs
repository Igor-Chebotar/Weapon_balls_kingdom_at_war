using UnityEngine;
using UnityEngine.UI;

public class MenuCastleFloat : MonoBehaviour
{
    float baseX;
    float amplitude = 15f;
    float speed = 0.4f;

    RectTransform rt;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        if (rt != null)
            baseX = rt.anchoredPosition.x;
    }

    void Update()
    {
        if (rt == null) return;
        float x = baseX + Mathf.Sin(Time.time * speed) * amplitude;
        rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
    }
}
