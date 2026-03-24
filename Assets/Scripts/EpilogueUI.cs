using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EpilogueUI : MonoBehaviour
{
    [SerializeField] Text storyText;
    [SerializeField] Button menuBtn;

    void Start()
    {
        // финальная тема, эпик!
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayTrack("final");

        if (storyText != null)
        {
            storyText.text = "Кровавый король мёртв, и вместе с ним пала его власть.\n\nСтрах больше не сковывает королевство, а тьма начинает отступать. Впереди ещё долгий путь к восстановлению, но мир наконец вернулся на эти земли.\n\nКоролевство снова сможет расцвести.";
            var font = Resources.Load<Font>("Lora");
            if (font != null) storyText.font = font;
        }

        if (menuBtn != null)
            menuBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySound("button");
                SceneManager.LoadScene("MainMenu");
            });
    }
}
