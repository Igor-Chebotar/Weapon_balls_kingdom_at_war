using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelCompleteUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Text levelText;
    [SerializeField] Button nextLevelBtn;

    // прокачка
    [SerializeField] Text dmgValueText;
    [SerializeField] Button dmgUpgradeBtn;
    [SerializeField] Text hpValueText;
    [SerializeField] Button hpUpgradeBtn;

    bool isVictory = false;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (nextLevelBtn != null) nextLevelBtn.onClick.AddListener(OnButtonClick);
        if (dmgUpgradeBtn != null) dmgUpgradeBtn.onClick.AddListener(OnUpgradeDamage);
        if (hpUpgradeBtn != null) hpUpgradeBtn.onClick.AddListener(OnUpgradeHealth);
    }

    public void Show(int completedLevel)
    {
        isVictory = false;
        if (panel != null) panel.SetActive(true);
        if (levelText != null) levelText.text = "Уровень " + completedLevel + " пройден!";

        var btnText = nextLevelBtn.GetComponentInChildren<Text>();
        if (btnText != null) btnText.text = "Начать уровень " + (completedLevel + 1);

        RefreshUpgrades();
    }

    public void ShowVictory()
    {
        isVictory = true;
        if (panel != null) panel.SetActive(true);
        if (levelText != null) levelText.text = "Победа!";

        var btnText = nextLevelBtn.GetComponentInChildren<Text>();
        if (btnText != null) btnText.text = "В главное меню";

        RefreshUpgrades();
    }

    void RefreshUpgrades()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (dmgValueText != null)
            dmgValueText.text = "Урон: " + gm.GetWeaponDamage();

        if (hpValueText != null)
            hpValueText.text = "HP: " + (int)gm.GetCurrentHP() + "/" + gm.GetMaxHealth();
    }

    void OnUpgradeDamage()
    {
        if (GameManager.Instance != null && GameManager.Instance.TryUpgradeDamage())
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySound("button");
            RefreshUpgrades();
        }
    }

    void OnUpgradeHealth()
    {
        if (GameManager.Instance != null && GameManager.Instance.TryUpgradeHealth())
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySound("button");
            RefreshUpgrades();
        }
    }

    void OnButtonClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound("button");

        if (isVictory)
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            if (panel != null) panel.SetActive(false);
            GameManager.Instance.GoToNextLevel();
        }
    }
}
