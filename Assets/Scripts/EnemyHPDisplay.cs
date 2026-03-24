using UnityEngine;

public class EnemyHPDisplay : MonoBehaviour
{
    TextMesh txt;
    HealthComponent hp;

    void Start()
    {
        hp = GetComponent<HealthComponent>();

        // создаём текст прямо на враге
        var txtObj = new GameObject("HPText");
        txtObj.transform.SetParent(transform, false);
        txtObj.transform.localPosition = Vector3.zero;

        txt = txtObj.AddComponent<TextMesh>();
        txt.alignment = TextAlignment.Center;
        txt.anchor = TextAnchor.MiddleCenter;
        txt.characterSize = 0.12f;
        txt.fontSize = 40;
        txt.color = Color.white;

        // сверху
        var mr = txtObj.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sortingOrder = 10;

        if (hp != null)
        {
            hp.OnHealthChanged += OnHPChanged;
            UpdateText();
        }
    }

    void OnHPChanged(float cur, float max)
    {
        UpdateText();
    }

    void UpdateText()
    {
        if (txt == null || hp == null) return;
        int hpInt = Mathf.CeilToInt(hp.GetHP());
        txt.text = hpInt.ToString();
    }

    void OnDestroy()
    {
        if (hp != null)
            hp.OnHealthChanged -= OnHPChanged;
    }
}
