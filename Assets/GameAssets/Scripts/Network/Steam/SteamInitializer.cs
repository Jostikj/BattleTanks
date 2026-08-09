using Steamworks;
using UnityEngine;

public class SteamInitializer : MonoBehaviour
{
    public static bool SteamInitialized { get; private set; }
    void Start()
    {
        if (SteamAPI.Init())
        {
            LobbyData.Instance.InitializeMySteamData(new PlayerData(SteamUser.GetSteamID()));

            Debug.Log("Steam успешно инициализирован!");
            Debug.Log($"»грок залогинен в Steam как: {LobbyData.Instance.MyData.Name}");
            SteamInitialized = true;
        }
        else
        {
            SteamInitialized = false;
            Debug.LogError("Ќе удалось инициализировать Steam! ”бедитесь, что Steam запущен.");
        }
    }

    void Update()
    {
        if (SteamAPI.IsSteamRunning())
        {
            SteamAPI.RunCallbacks();
        }
    }

    void OnApplicationQuit()
    {
        if (SteamAPI.IsSteamRunning())
        {
            SteamAPI.Shutdown();
            Debug.Log("Steamworks завершил работу.");
        }
    }
}