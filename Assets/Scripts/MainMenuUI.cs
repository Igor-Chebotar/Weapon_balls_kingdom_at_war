using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Button playBtn;
    [SerializeField] Button quitBtn;

    void Start()
    {
        // запускаем музыку для главного меню!
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayTrack("menu");

        if (playBtn != null)
            playBtn.onClick.AddListener(() => {
                PlayBtnSound();
                SceneManager.LoadScene("Story");
            });

        if (quitBtn != null)
            quitBtn.onClick.AddListener(() => {
                PlayBtnSound();
                Application.Quit();
            });
    }

    void PlayBtnSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound("button");
    }
}
