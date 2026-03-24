using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StoryUI : MonoBehaviour
{
    [SerializeField] Text storyText;
    [SerializeField] Button startBtn;

    void Start()
    {
        // музон из меню пусть играет дальше
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayTrack("menu");

        if (storyText != null)
        {
            storyText.text = "Королевство гибнет под властью кровавого короля, чья жестокость погрузила земли в страх и хаос.\n\nРыцарь отправляется в поход, чтобы положить конец его тирании. Но сначала ему предстоит пройти через верных подданных короля и прорваться к самому трону.\n\nЛишь смерть короля сможет вернуть людям надежду.";
            // подгружаем свой шрифт
            var font = Resources.Load<Font>("Lora");
            if (font != null) storyText.font = font;
        }

        if (startBtn != null)
            startBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySound("button");
                SceneManager.LoadScene("Game");
            });
    }
}
