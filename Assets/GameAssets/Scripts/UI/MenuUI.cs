using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    public GameObject PlayMenu;
    public GameObject SteamPlayMenu;
    public GameObject SteamLobbyCreator;
    public GameObject SteamLobby;
    public GameObject SteamJoinLobby;

    private void Awake()
    {
        if (PlayMenu != null)
            PlayMenu.SetActive(false);

        if (SteamPlayMenu != null)
            SteamPlayMenu.SetActive(false);

        if (SteamLobbyCreator != null)
            SteamLobbyCreator.SetActive(false);

        if (SteamLobby != null)
            SteamLobby.SetActive(false);

        if (SteamJoinLobby != null)
            SteamJoinLobby.SetActive(false);

        SteamLobbyManager.Instance.onLobbyEntered += OnEnteredLobby;
    }

    public void LocalPlayButton()
    {
        SceneManager.LoadScene("gameScene (local)");
    }

    public void ExitButton()
    {
        Application.Quit();
    }

    public void OnEnteredLobby()
    {
        SteamLobby.SetActive(true);
        SteamLobbyCreator.SetActive(false);
        SteamJoinLobby.SetActive(false);
    }

    public void OnExitedLobby()
    {
        SteamLobby.SetActive(false);
    }
}