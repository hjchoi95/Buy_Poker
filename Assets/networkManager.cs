﻿using UnityEngine;
using System.Collections;



/**
 * Class networkManager.
 * 
 * Mainly uses Network interface nicely provided by unity.
 * Also uses unity masterServer that can be accessed for free.
 * 
 * back-end is hidden very well behind the actual layer we are using.
 * search-automation is done, only at a front-end side.
 * 
 * 
 * Every player can, with the help of master server,
 * - either register as a host.
 * - or join an existing host.
 * 
 * 
 * multiple player connection seems hard to implement. (at least it should take a lot of time and have to change a lot of existing code)
 * Right now, networkManager work specifically for 2 players.
 * the process of registering as host, or joining host is automated.
 * If empty room is found, join.
 * If not, (all room is full, or there is no room), create new room.
 * 
 * After connection,
 * both players create a network object, which can communicate with each each other by calling functions on the other side.
 * 
 * 
 * After all, unity made it so easy for us, and it works just like a magic!!
 * 
 * */
public class networkManager : MonoBehaviour {

	public string GAMENAME="chat";	// GAMENAME changes to "duel" in a multiplayer game, so that chat room and game room don't collide
	public static float REFRESH_HOST_TIMEOUT = 3f;
	public static float SEARCH_TIMEOUT=30f;		// automatically cancels search after SEARCH_TIMEOUT
	float search_wait_time;	// how many seconds did you wait to search?
	float refresh_start_time;	// starttime for searching for host.
	float search_start_time;	// starttime for searching.

	string statusMsg;

	public Transform samplePref;	// samplePref is a preFab that is loaded on a unity scene file. (consists of networkObject script and networkView)

	float btnX;
	float btnY;
	float btnW;
	float btnH;

	bool searching;		// currently searching (search button pressed)
	bool refreshing;	// currently looking up for existing host.
	HostData[] hostData;

	public static networkBidObject networkObject;
	private Transform netObj;

	// Use this for initialization
	void Start () {
		GameMaster.networkRequired=true;
		GameMaster.multiplayerMode = true;
		search_wait_time = 5f;
		btnX=Screen.width*0.05f;
		btnY=Screen.height*0.05f;
		btnW=Screen.width*0.15f;
		btnH=Screen.height*0.2f;
		refreshing = false;
		refresh_start_time = 0;
		hostData = new HostData[0];
		searching = false;
		search_start_time = 0;
		statusMsg = "";
	}
	
	// Update is called once per frame
	void Update () {
		// refresh for existing host, and retrieves host data.
		if (refreshing && (MasterServer.PollHostList ().Length > 0 || refresh_start_time+REFRESH_HOST_TIMEOUT<Time.time )) {
			refreshing=false;
			Debug.Log ("Host count = " + MasterServer.PollHostList ().Length);
			hostData = MasterServer.PollHostList ();
		}
		// If no connection after SEARCH_TIMEOUT, stops searching.
		if (searching && search_start_time + SEARCH_TIMEOUT < Time.time/* && Network.connections.Length<1*/) {
			Debug.Log ("Search failed!!");
			statusMsg="search failed!";
			searching =false;
			MasterServer.UnregisterHost();
			Network.Disconnect ();
		}
		// if you get to keep waiting, just print dot dot dot to keep you from being impatient
		if (searching && search_start_time + search_wait_time < Time.time && Network.connections.Length<1)
		{
			if (((int)search_wait_time)%4==0)
				statusMsg="waiting for clients";
			else
				statusMsg+=".";
			search_wait_time+=1f;
		}
	}

	public IEnumerator startNetworkingSearch()
	{
		searching = true;
		search_start_time = Time.time;
		bool hostFound = false;
		search_wait_time = 7f;

		// first step : refresh the host list.
		refreshHostList ();
		statusMsg="searching for host..";
		while (refreshing) {
			yield return new WaitForSeconds(0.1f);
			if (!searching)
				break;
		}

		// second step : for all list of hosts, search for empty room and connect.
		if (searching)
		{
			for (int i=0; i<hostData.Length; i++) {
				if (hostData[i].connectedPlayers<2 && searching)
				{
					statusMsg="host ID "+i+" found!";
					Network.Connect(hostData[i]);
					hostFound=true;
					searching = false;
					break;
				}
			}
		}
		// third step : create and register new host, and wait for new players coming in.
		if (!hostFound && searching)
		{
			statusMsg="("+hostData.Length+") registering as host";
			Debug.Log ("Host not found! Creating a new host...");
			Network.InitializeServer (2,25001,!Network.HavePublicAddress ());
			MasterServer.RegisterHost (GAMENAME,"host ID "+hostData.Length);
		}
	}


	void StartServer()
	{

	}

	void refreshHostList()
	{
		MasterServer.RequestHostList (GAMENAME);
		refreshing = true;
		refresh_start_time = Time.time;
	}

	void OnServerInitialized()
	{
		Debug.Log ("Server initialized!");
	}
	
	void OnConnectedToServer()
	{
		Debug.Log ("connected to host!");
		statusMsg="connected to host!";
		searching = false;
		netObj = (Transform)Instantiate (samplePref, new Vector3(8,3,0), Quaternion.identity);
		networkObject=netObj.GetComponent<networkBidObject>();
		networkObject.debug ();
		GameMaster.UserID = 1;
	}
	
	void OnPlayerConnected()
	{
		Debug.Log ("connected to player!");
		statusMsg="connected to player!";
		searching = false;
		netObj = (Transform)Instantiate (samplePref, new Vector3(8,3,0), Quaternion.identity);
		networkObject=netObj.GetComponent<networkBidObject>();
		networkObject.debug ();
		GameMaster.UserID = 2;
		networkObject.broadcastRandomValues();

	}

	void OnDisconnectedFromServer()
	{
		Debug.Log ("disconnected from host!");
		// destroy the network object on disconnect
		Destroy (netObj.gameObject);
		Destroy (this);
	}

	void OnPlayerDisconnected()
	{
		Debug.Log ("player disconnected!");
		// destroy the network object on disconnect
		Destroy (netObj.gameObject);
		Destroy (this);
	}

	void OnMasterServerEvent(MasterServerEvent mse){
		if (mse == MasterServerEvent.RegistrationSucceeded)
		{
			Debug.Log ("Registered Server");
			//statusMsg="waiting for clients";
		}

	}

	public void forceDisconnect()
	{
		MasterServer.UnregisterHost();
		Network.Disconnect ();
		if (networkObject)
			Destroy (networkObject);

	}

	void OnGUI()
	{
		GUIStyle boxStyle = new GUIStyle (GUI.skin.button);
		boxStyle.normal.textColor = Color.green;
		boxStyle.hover.textColor = Color.cyan;
		boxStyle.active.textColor = Color.cyan;
		boxStyle.fontSize = Utils.adjustUISize (16,true);

		GameMaster.multiplayerMode = true;

		// this if statement is used over, over and over. Check if you are connected or not. should be a better way.
		// !!!! I realized This whole statement can be changed by one line, but I am quite lazy to do that right now
		if (!Network.isClient && (!Network.isServer || Network.connections.Length<1)) {
			GameMaster.networkRequired=true;
			if (!searching)
			{
				if (GUI.Button (new Rect (btnX, btnY, btnW, btnH), "Start Search",boxStyle)) {
					Debug.Log ("Start searching for opponent");
					statusMsg="accessing server..";
					StartCoroutine(startNetworkingSearch ());
				}
			}
			else if (GUI.Button (new Rect (btnX, btnY, btnW, btnH), "Cancel Search"+"\n\n"+statusMsg,boxStyle))
			{
				Debug.Log ("Search cancel");
				searching=false;
				MasterServer.UnregisterHost();
				Network.Disconnect ();
			}
		}
		else if (GUI.Button (new Rect (btnX, btnY, btnW, btnH/2), "Connected!!\n\n"+statusMsg,boxStyle))
		{
			Debug.Log ("Disconnected");
			forceDisconnect ();
		}
		else
		{
			//GUI.Box (new Rect (btnX, btnY, btnW, btnH), "Connected!",boxStyle);
			GameMaster.networkRequired=false;
			if (GameMaster.networkWaitingForPlayers) {
				statusMsg="waitingForPlayers...";
				if (networkObject.isPlayersReady ())
				{
					statusMsg="connection ok";
					GameMaster.networkWaitingForPlayers=false;
				}
			}
		}


		/*
		for (int i=0; i<hostData.Length; i++) {
			GUI.Box (new Rect (btnX+Screen.height*0.4f, btnY*1.2f+btnH*i, btnW*3f, btnH), hostData[i].gameName);
			
		}*/

	}
}
