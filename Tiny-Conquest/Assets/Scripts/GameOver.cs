using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class GameOver : MonoBehaviour
{
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private GameObject gameOverPanel;

    private void Update()
    {
        CheckGameOver();
    }

    public void CheckGameOver()
    {
        Platform[] platforms = FindObjectsByType<Platform>(FindObjectsSortMode.None);

        bool playerOwnsAll = false;
        bool aiOwnsAll = false;

        foreach (Platform platform in platforms)
        {
            if (platform.Owner == PlatformOwner.Player)
            {
                playerOwnsAll = true;
            }
            else if (platform.Owner == PlatformOwner.AI)
            {
                aiOwnsAll = true;
            }
            else if (platform.Owner == PlatformOwner.Neutral)
            {
                playerOwnsAll = false;
                aiOwnsAll = false;
                break;
            }
        }

        if (playerOwnsAll && !aiOwnsAll)
        {
            Debug.Log("Player Wins!");
            gameOverPanel.SetActive(true);
            gameOverText.text = "You Win!";
            
        }
        else if (!playerOwnsAll && aiOwnsAll)
        {
            Debug.Log("AI Wins!");
            gameOverPanel.SetActive(true);
            gameOverText.text = "You Lose!";
        }
    }

    public void MainMenuButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScreen");
    }
}
