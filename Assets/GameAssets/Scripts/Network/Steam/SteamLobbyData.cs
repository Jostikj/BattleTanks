using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SteamLobbyData : MonoBehaviour
{
    private Dictionary<CSteamID, PlayerSteamData> _players = new Dictionary<CSteamID, PlayerSteamData>();

    public static SteamLobbyData Instance { get; private set; }
    public int PlayersCount { get; private set; }
    public int MaxPlayersCount { get; private set; }
    public string LobbyName { get; private set; }
    public CSteamID LobbyID { get; private set; }
    public CSteamID HostID { get; private set; }
    public PlayerSteamData MySteamData { get; private set; }
    public ELobbyType LobbyType { get; private set; }

    [Header("Actions")]
    public Action<PlayerSteamData> OnPlayerConnected;
    public Action<PlayerSteamData> OnPlayerDisconnected;

    public Action OnPlayersCountChanged;
    public Action OnHostIDChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;
    }

    public void AddPlayer(CSteamID steamID)
    {
        _players.Add(steamID, new PlayerSteamData(steamID));
        PlayersCount++;
        OnPlayerConnected?.Invoke(GetPlayer(steamID));
    }

    public void RemovePlayer(CSteamID steamID)
    {
        _players.Remove(steamID);
        PlayersCount--;
        OnPlayerDisconnected?.Invoke(GetPlayer(steamID));
    }

    public void RemoveAllPlayers()
    {
        _players.Clear();
        PlayersCount = 0;
    }

    public PlayerSteamData GetPlayer(CSteamID steamID)
    {
        return _players[steamID];
    }

    public void SetHostID(CSteamID hostID)
    {
        HostID = hostID;
        OnHostIDChanged?.Invoke();
    }

    public void SetMaxPlayersCount(int maxPlayersCount)
    {
        MaxPlayersCount = maxPlayersCount;
    }

    public void SetLobbyName(string lobbyName)
    {
        LobbyName = lobbyName;
    }

    public void SetLobbyID(CSteamID steamID)
    {
        LobbyID = steamID;
    }

    public void SetMySteamData(PlayerSteamData mySteamData)
    {
        MySteamData = mySteamData;
    }

    public void SetPlayersCount(int count)
    {
        PlayersCount = count;
        OnPlayersCountChanged?.Invoke();
    }

    public void SetLobbyType(ELobbyType lobbyType)
    {
        LobbyType = lobbyType;
    }

    public void InitializeMySteamData(PlayerSteamData player)
    {
        MySteamData = player;
    }
}