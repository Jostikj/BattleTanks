using Steamworks;
using UnityEngine;

public class SteamInitializer : MonoBehaviour
{
    public static bool SteamInitialized { get; private set; }
    void Start()
    {
        if (SteamAPI.Init())
        {
            SteamLobbyData.Instance.InitializeMySteamData(new PlayerSteamData(SteamUser.GetSteamID()));

            Debug.Log("Steam успешно инициализирован!");
            Debug.Log($"»грок залогинен в Steam как: {SteamLobbyData.Instance.MySteamData.Name}");
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