namespace DynaBomberClient.Communication.ClientMsg
{
    public enum ClientMessageTypes
    {
        StatusUpdate = 0,
        PositionUpdate = 1,
        BombSet = 2,
        // Lobby messages
        GetGameList = 3,
        JoinGame = 4
    }
}