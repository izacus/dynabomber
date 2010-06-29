﻿namespace DynaBomber_Server.Interop.ServerMsg
{
    public enum ServerMessageTypes
    {
        Map = 1,
        Player = 2,
        StatusUpdate = 3,
        BombExplosion = 4,
        PlayerDeath = 5,
        GameOver = 6,
        // Lobby messages
        GameList = 7,
        SimpleResponse = 8
    }
}