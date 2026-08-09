using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance;

    public GameMode GameMode { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else Destroy(gameObject);
    }

    public void SetGameMode(GameMode gameMode)
    {
        GameMode = gameMode;
        Debug.Log("Режим игры сменён на  " + gameMode);
    }
}