using Mirror;
using Steamworks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamLobbyManager : MonoBehaviour
{
    #region Переписал
    [SerializeField] private string gameSceneName = "Map1";

    [Header("Singleton")]
    public static SteamLobbyManager Instance;

    [Header("Components")]
    [SerializeField] private SteamLobbyUI SteamLobbyUI;

    [Header("Actions")]
    public Action OnGameStart;
    public Action onLobbyEntered;

    [Header("Flags")]
    private bool isLobbyCreating = false;
    private bool isGameStarting = false;

    [Header("Callbacks")]
    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<GameLobbyJoinRequested_t> joinRequestCallback;
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<LobbyDataUpdate_t> lobbyDataUpdateCallback;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallnack;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
        Debug.LogWarning("test");

        lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        joinRequestCallback = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyDataUpdateCallback = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
        lobbyChatUpdateCallnack = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    }

    public void CreateGameLobby()
    {
        if (isLobbyCreating) return;

        SteamMatchmaking.CreateLobby(SteamLobbyData.Instance.LobbyType, SteamLobbyData.Instance.MaxPlayersCount);
        isLobbyCreating = true;
        Debug.Log("Запрос на создание лобби отправлен в Steam...");
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        isLobbyCreating = false;

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"Не удалось создать лобби. Код ошибки: {callback.m_eResult}");
            return;
        }

        SteamLobbyData.Instance.SetLobbyID(new CSteamID(callback.m_ulSteamIDLobby));
        Debug.Log($"Лобби успешно создано! ID: {SteamLobbyData.Instance.LobbyID}");

        SteamMatchmaking.SetLobbyData(SteamLobbyData.Instance.LobbyID, "lobbyName", SteamLobbyData.Instance.LobbyName);
        SteamMatchmaking.SetLobbyData(SteamLobbyData.Instance.LobbyID, "lobbyMaxPlayersCount", SteamLobbyData.Instance.MaxPlayersCount.ToString());
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"Получен запрос на вступление в лобби {callback.m_steamIDLobby}");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        SteamLobbyData.Instance.SetPlayersCount(SteamMatchmaking.GetNumLobbyMembers(SteamLobbyData.Instance.LobbyID));
        SteamLobbyData.Instance.SetLobbyID(new CSteamID(callback.m_ulSteamIDLobby));
        SteamLobbyData.Instance.SetHostID(new CSteamID((ulong)SteamMatchmaking.GetLobbyOwner(SteamLobbyData.Instance.LobbyID)));
        SteamLobbyData.Instance.SetMaxPlayersCount(Convert.ToInt32(SteamMatchmaking.GetLobbyData(SteamLobbyData.Instance.LobbyID, "lobbyMaxPlayersCount")));
        SteamLobbyData.Instance.SetLobbyName(SteamMatchmaking.GetLobbyData(SteamLobbyData.Instance.LobbyID, "lobbyName"));

        for (int i = 0; i < SteamLobbyData.Instance.PlayersCount; i++)
        {
            SteamLobbyData.Instance.AddPlayer(SteamMatchmaking.GetLobbyMemberByIndex(SteamLobbyData.Instance.LobbyID, i));
        }

        NetworkManager.singleton.networkAddress = SteamLobbyData.Instance.HostID.ToString();

        onLobbyEntered?.Invoke();
    }

    public void InviteFriends()
    {
        if (!SteamLobbyData.Instance.LobbyID.IsValid())
        {
            Debug.LogWarning("Нет активного лобби для приглашения");
            return;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(SteamLobbyData.Instance.LobbyID);
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby != SteamLobbyData.Instance.LobbyID.m_SteamID) return;
        if (!NetworkServer.active && !isGameStarting)
        {
            ClientGameStartedCheack();
        }
    }

    private void ClientGameStartedCheack()
    {
        if (SteamMatchmaking.GetLobbyData(SteamLobbyData.Instance.LobbyID, "game_started") == "1")
        {
            isGameStarting = true;
            SceneManager.LoadScene(gameSceneName);
            NetworkManager.singleton.StartClient();
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        ConnectedAndDisconnectedPlayer(callback);
    }

    private void ConnectedAndDisconnectedPlayer(LobbyChatUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby != (ulong)SteamLobbyData.Instance.LobbyID) return;

        var state = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;
        CSteamID steamID = new CSteamID(callback.m_ulSteamIDUserChanged);

        if ((state & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
        {
            SteamLobbyData.Instance.AddPlayer(steamID);
            Debug.Log($"Игрок {steamID} присоединился!");
        }

        if ((state & EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0)
        {
            SteamLobbyData.Instance.RemovePlayer(steamID);
            Debug.Log($"Игрок {steamID} покинул лобби.");
        }

        if ((state & EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0)
        {
            SteamLobbyData.Instance.RemovePlayer(steamID);
            Debug.Log($"Игрок {steamID} был отключен.");
        }
    }

    public void StartGame()
    {
        if (SteamLobbyData.Instance.MySteamData.SteamID != SteamLobbyData.Instance.HostID) return;
        if (isGameStarting) return;
        isGameStarting = true;

        SteamMatchmaking.SetLobbyData(SteamLobbyData.Instance.LobbyID, "game_started", "1");

        SceneManager.LoadScene(gameSceneName);
        NetworkManager.singleton.StartHost();
        OnGameStart?.Invoke();
    }

    public void OnExitLobby()
    {
        SteamLobbyData.Instance.RemoveAllPlayers();
        SteamMatchmaking.LeaveLobby(SteamLobbyData.Instance.LobbyID);
        Debug.Log("Вы вышли из лобби...");
    }

    public bool JoinLobbyToID(string steamID)
    {
        if (ulong.TryParse(steamID, out ulong lobbyIDNumber) && lobbyIDNumber != 0)
        {
            CSteamID lobbyID = new CSteamID(lobbyIDNumber);
            SteamMatchmaking.JoinLobby(lobbyID);
            Debug.Log($"Пытаемся присоединиться к лобби с ID: {lobbyID}");
            return true;
        }
        else
        {
            Debug.LogError("Неверный формат ID лобби! ID должен состоять только из цифр.");
            return false;
        }
    }
    #endregion
}