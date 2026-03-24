using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Text resultText;
    [SerializeField] Text goldEarnedText;
    [SerializeField] Button retryBtn;
    [SerializeField] Button menuBtn;

    void Start()
    {
        if (panel != null) panel.SetActive(false);

        if (retryBtn != null)
            retryBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySound("button");
                Time.timeScale = 1;
                SceneManager.LoadScene("Game");
            });

        if (menuBtn != null)
            menuBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySound("button");
                Time.timeScale = 1;
                SceneManager.LoadScene("MainMenu");
            });
    }

    public void Show(int level, int goldEarned)
    {
        if (panel != null) panel.SetActive(true);

        if (resultText != null)
            resultText.text = "Поражение на уровне " + level;

        if (goldEarnedText != null)
            goldEarnedText.text = "";
    }
}
