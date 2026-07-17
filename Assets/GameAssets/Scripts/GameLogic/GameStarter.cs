using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStarter : MonoBehaviour
{
    public static GameStarter Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void StartGameHost(string mapName)
    {
        NetworkManager.singleton.StartHost();
        GlobalStartGame(mapName);
    }

    public void StartGameClient(string mapName)
    {
        NetworkManager.singleton.StartClient();
        GlobalStartGame(mapName);
    }

    private void GlobalStartGame(string mapName)
    {
        SceneManager.LoadScene(mapName);
        UIManager.Instance.OpenGameUI();
    }
}
