using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;
    [SerializeField] Button resumeBtn;
    [SerializeField] Button menuBtn;

    bool paused = false;

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (resumeBtn != null)
            resumeBtn.onClick.AddListener(Resume);

        if (menuBtn != null)
            menuBtn.onClick.AddListener(GoToMenu);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (paused)
                Resume();
            else
                Pause();
        }
    }

    void Pause()
    {
        paused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame(true);
    }

    void Resume()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound("button");
        paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame(false);
    }

    void GoToMenu()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound("button");
        Time.timeScale = 1; // не забыть
        SceneManager.LoadScene("MainMenu");
    }
}
