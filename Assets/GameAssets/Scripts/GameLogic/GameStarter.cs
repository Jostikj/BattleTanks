using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStarter : MonoBehaviour
{
    [SerializeField] private GameObject GameManager;

    private void Start()
    {
        OnEnable();
    }

    private void OnEnable()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnGameStartedHost += StartGameHost;
            LobbyManager.Instance.OnGameStartedClient += StartGameClient;
        }
    }

    private void OnDisable()
    {
        LobbyManager.Instance.OnGameStartedHost -= StartGameHost;
        LobbyManager.Instance.OnGameStartedClient -= StartGameClient;
    }

    public void StartGameHost(string mapName)
    {
        GlobalStartGame(mapName);
        NetworkManager.singleton.StartHost();
    }

    public void StartGameClient(string mapName)
    {
        GlobalStartGame(mapName);
        NetworkManager.singleton.StartClient();
    }

    private void GlobalStartGame(string mapName)
    {
        SceneManager.LoadScene(mapName);
        UIManager.Instance.OpenGameUI();
        GameManager.SetActive(true);
    }
}
