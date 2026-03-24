using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] Slider hpBar;
    [SerializeField] Text levelText;
    [SerializeField] Text goldText;
    [SerializeField] Image goldIcon;

    HealthComponent playerHP;

    void Start()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        playerHP = player.GetComponent<HealthComponent>();
        if (playerHP != null)
            playerHP.OnHealthChanged += UpdateHealthBar;

        UpdateGold(0);
    }

    void Update()
    {
        if (GameManager.Instance != null && levelText != null)
            levelText.text = "Уровень " + GameManager.Instance.GetCurrentLevel();
    }

    public void UpdateHealthBar(float cur, float max)
    {
        if (hpBar == null) return;
        hpBar.maxValue = max;
        hpBar.value = cur;
    }

    public void UpdateGold(int amount)
    {
        if (goldText != null) goldText.text = amount.ToString();
    }

    void OnDestroy()
    {
        if (playerHP != null)
            playerHP.OnHealthChanged -= UpdateHealthBar;
    }
}
