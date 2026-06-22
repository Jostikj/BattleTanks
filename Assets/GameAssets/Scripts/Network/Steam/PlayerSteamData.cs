using Steamworks;
using UnityEngine;

public class PlayerSteamData
{
    private readonly CSteamID _steamID;
    private readonly Texture2D _avatar;
    private readonly string _name;

    public PlayerSteamData(CSteamID steamID)
    {
        _steamID = steamID;
        _name = SteamFriends.GetFriendPersonaName(steamID);
        _avatar = GetSteamAvatar(steamID);
    }

    public static Texture2D GetSteamAvatar(CSteamID steamID)
    {
        int handle = SteamFriends.GetLargeFriendAvatar(steamID);
        if (handle == 0) return null;
        if (handle == -1)
        {
            SteamFriends.RequestUserInformation(steamID, false);
            return null;
        }

        SteamUtils.GetImageSize(handle, out uint width, out uint height);
        if (width == 0 || height == 0) return null;

        int byteCount = (int)(width * height * 4);
        byte[] imageData = new byte[byteCount];
        if (!SteamUtils.GetImageRGBA(handle, imageData, byteCount))
            return null;

        int rowSize = (int)width * 4;
        byte[] tempRow = new byte[rowSize];
        for (int y = 0; y < height / 2; y++)
        {
            int topRow = y * rowSize;
            int bottomRow = (int)((height - 1 - y) * rowSize);
            System.Buffer.BlockCopy(imageData, topRow, tempRow, 0, rowSize);
            System.Buffer.BlockCopy(imageData, bottomRow, imageData, topRow, rowSize);
            System.Buffer.BlockCopy(tempRow, 0, imageData, bottomRow, rowSize);
        }

        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(imageData);
        texture.Apply(false);
        return texture;
    }

    public CSteamID SteamID => _steamID;
    public Texture2D Avatar => _avatar;
    public string Name => _name;
}