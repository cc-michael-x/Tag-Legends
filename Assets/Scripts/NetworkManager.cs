﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.IO;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager instance;
    readonly TypedLobby typedLobby = new TypedLobby("SqlTypedLobby", LobbyType.SqlLobby);
    public const string ELO_PROP_KEY = "C0";
    public const int MaxPlayersDefault = 5;
    string[] roomPropertiesLobby = { ELO_PROP_KEY };
    string matchmakingSqlQuery;
    public bool rankedGame = false;

    private void Awake()
    {
        // If an instance already exists and it's not this one - destroy to avoid duplicate NetworkManager object
        if (instance != null && instance != this)
            gameObject.SetActive(false);
        else
        {
            // Set the instance
            instance = this;
            // Don't destroy NetworkManager game object when switching scenes
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        matchmakingSqlQuery = "C0 BETWEEN -100 + " + CloudManager.instance.GetRank().ToString() + " AND 100 + " + CloudManager.instance.GetRank().ToString();
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        // increase the progress bar of the loading screen
        MenuLoading.instance.PhotonConnectionDone();
    }

    // get list of rooms based on string query
    public void GetListOfRooms()
    {
        PhotonNetwork.GetCustomRoomList(typedLobby, matchmakingSqlQuery);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        if (roomList.Count > 0)
            PopulateGrid.instance.PopulateRoomList(roomList);
    }

    public Room CurrentRoom()
    {
        return PhotonNetwork.CurrentRoom;
    }

    public void CreateRoom(string roomName, int numberOfPlayers)
    {
        int rank;
        int.TryParse(CloudManager.instance.GetRank().ToString(), out rank);

        RoomOptions roomOptions = new RoomOptions();
        
        roomOptions.IsOpen = true;

        // number of players is specified when creating a custom game
        if (numberOfPlayers <= 1)
        {
            // if it's a ranked game, add the elo to the room
            roomOptions.IsVisible = true;
            roomOptions.MaxPlayers = MaxPlayersDefault;
        }
        else
        {
            roomOptions.IsVisible = false;
            roomOptions.MaxPlayers = (byte) numberOfPlayers;
        }
        
        if (rankedGame)
        {
            roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { ELO_PROP_KEY, rank } };
            roomOptions.CustomRoomPropertiesForLobby = roomPropertiesLobby;
            PhotonNetwork.CreateRoom(roomName, roomOptions, typedLobby);
        } 
        else
        {
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    // join specific unranked room
    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    // join random unranked room
    public void JoinRandomRoomUnranked()
    {
        rankedGame = false;
        PhotonNetwork.JoinRandomRoom();
    }

    // join random ranked room
    public void JoinRandomRoomRanked()
    {
        // set ranked game to true
        rankedGame = true;

        // set custom room properties - elo
        ExitGames.Client.Photon.Hashtable customRoomProperties = new ExitGames.Client.Photon.Hashtable { { ELO_PROP_KEY, CloudManager.instance.GetRank().ToString() } };

        // join random room
        PhotonNetwork.JoinRandomRoom(customRoomProperties, MaxPlayersDefault, MatchmakingMode.FillRoom, typedLobby, matchmakingSqlQuery);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // create a room if unable to join one
        CreateRoom("", 0);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        CreateRoom(Menu.instance.CustomGameName.text, Menu.instance.GetMaxNumberOfPlayersFromDropdown());
    }

    public override void OnJoinedRoom()
    {
        Menu.instance.UpdateCustomGamePlayersDenominator(PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public override void OnCreatedRoom()
    {
        Menu.instance.UpdateCustomGamePlayersDenominator(PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient &&
            PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            // send an rpc call to all players in the room to load the "Game" scene
            photonView.RPC("ChangeScene", RpcTarget.All, "Game");
        }
    }

    [PunRPC]
    public void ChangeScene(string sceneName)
    {
        // when a game has started - make the room impossible to join
        PhotonNetwork.CurrentRoom.IsOpen = false;
        // load game scene
        PhotonNetwork.LoadLevel(sceneName);
    }
}