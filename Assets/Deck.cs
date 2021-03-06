﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/*
 * 
 * This class is a container of collection of cards.
 * Each deck should be assigned a unique ID so that GameMaster can access them.
 * 
 * 
 * 
 * */
public class Deck : MonoBehaviour {

	public int reserveDeckID;			
	protected List<Card> cards;				// collection of gameObjects references (cards). List should be quite similar to Vector
	protected int deckID;					// unique ID assigned to each deck.
	private bool initializeFlag = true;		// Once ID is set, it cannot be changed again. I added variable just for the sake of protection, but I might be complicating things.
	private Transform referenceTransform;	// this class includes reference position, scale and rotation of entire deck. Individual cards will be positioned based on this reference transform
	private int currentLayoutType;			//current layout type

	//===================================== Getter and Setter functions as well as initialization =====================================
	public int DeckID{	//c# simplified getter and setter technique. deckList[i].DeckID is enough! getDeckID() does not work!
		get{return deckID;}
		set{if (initializeFlag){deckID = value; initializeFlag=false;}else{Debug.LogError ("Deck ID assignment denied");}}
	}
	public List<Card> CARDS {
			get{ return cards;}
	}
	~Deck() {
		destroyAll ();
	}
	
	public Card peekTopCard()
	{
		return cards [cards.Count - 1];
	}


	public void new_card(int rank, int suit)	// create a new card object into the scene, and adds its reference to the cardlist.
	{
		GameObject newCard = (GameObject)Instantiate (Resources.Load ("prefab/card"), referenceTransform.localPosition, Quaternion.identity);
		// instantiate() creates an object into the scene. Then, it returns Object class.
		newCard.GetComponent<Card>().initializeCard (rank,suit);	//set rank and suit
		newCard.GetComponent <Transform>().parent = (Transform)this.GetComponent <Transform>();// let all new cards become a child of this deck.
		newCard.GetComponent<Card>().showFace ();
		cards.Add (newCard.GetComponent<Card>());	// add newCard to list of cards.
		//Debug.Log (cards.Count + " cards count ");
		setupLayout(currentLayoutType);
	}

	public void addExistingCard(Card card)	//add existing card. It should be distinguished from new_card
	{
		cards.Add (card);
		card.GetComponent <Transform>().parent = (Transform)this.GetComponent <Transform>();
		setupLayout (currentLayoutType);
		//Debug.Log ("control transfer successful");
	}


	public virtual void destroyAll()
	{
		while (cards.Count>0) {
			if (cards[0]!=null)
			{
				Destroy (cards [0].gameObject);
				cards.Remove (cards [0]);
			}
			else
				break;
		}
		cards = new List<Card> ();
		//Debug.Log ("After destroying everything, "+cards.Count + " cards Left ");
	}

	//===================================== Public container functions =====================================
	public void transferCardTo(Deck another, bool cardOpen, int rank=0, int suit=0)
	{
		int debugSourceCountBefore = cards.Count;
		int debugDestCountBefore = another.cards.Count;
		Debug.Log ("cards count of source, from = "+cards.Count);
		Debug.Log ("cards count of destination, from = "+another.cards.Count);
		if (cards.Count>0)
		{
			// transfer top card
			if (cards.Find (x => x.Rank==rank && x.Suit==suit)==null)
			{
				if (cardOpen)
					cards[cards.Count-1].GetComponent<Card>().showFace ();
				cards[cards.Count-1].GetComponent<Card>().stopBlinkAnim ();
				//Debug.Log ("Card count = " + list.Count);
				//Debug.Log ("card to be transferred : Rank = " + list[list.Count-1].GetComponent <Card>().Rank);
				another.addExistingCard (cards[cards.Count-1]);
				cards.Remove (cards [cards.Count - 1]);
			}
			// transfer specific card
			else
			{
				if (cardOpen)
					cards.Find (x => x.Rank==rank && x.Suit==suit).GetComponent<Card>().showFace ();
				cards.Find (x => x.Rank==rank && x.Suit==suit).GetComponent<Card>().stopBlinkAnim();
				//Debug.Log ("Card count = " + list.Count);
				//Debug.Log ("card to be transferred : Rank = " + list[list.Count-1].GetComponent <Card>().Rank);
				another.addExistingCard (cards.Find (x => x.Rank==rank && x.Suit==suit));
				cards.Remove (cards.Find (x => x.Rank==rank && x.Suit==suit));

			}
			setupLayout(currentLayoutType);
			// sound effect
			audio.clip = Resources.Load <AudioClip>("audio/shuffling-cards");
			audio.Play ();
			StartCoroutine (audioStopAfter (0.3f));
		}
		Debug.Log ("cards count of source, to = "+cards.Count);
		Debug.Log ("cards count of destination, to = "+another.cards.Count);
		if (debugSourceCountBefore - cards.Count != another.cards.Count - debugDestCountBefore) {
			Debug.LogWarning ("Warning!!! Transfer card function malfunctioning");
		}
	}

	private IEnumerator audioStopAfter(float time)
	{
		yield return new WaitForSeconds(time);
		audio.Stop ();
	}

	public void generateFullCardDeck()
	{
		for (int i=1; i<=4; ++i) {
			for (int j=1; j<=13; ++j) {
				new_card (j, i);
			}
		}
	}
	
	public void shuffle()
	{
		for (int i=0; i<cards.Count; i++)
		{

			int swap_target = Random.Range (0,cards.Count);
			// if connected to network, request for a common random seed
			if (!(!Network.isClient && (!Network.isServer || Network.connections.Length < 1))) {
				swap_target = networkManager.networkObject.getCommonRandomValue(i,cards.Count);
			}
			Card temp = cards[i];
			cards[i] = cards[swap_target];
			cards[swap_target] = temp;
		}
		setScaleAndOrder ();
		setupLayout (currentLayoutType);

	}
	
	public void sort()
	{
		cards.Sort ((x, y) => x.Rank);
		setScaleAndOrder ();
		setupLayout (currentLayoutType);
	}


	//=================== control Position, movement and appearance of each cards in patterened way ===================
	public void setupLayout(int type)	// sets position of every and each cards inside the deck.
	{
		currentLayoutType = type;
		setScaleAndOrder ();
		for (int i=0; i<cards.Count; i++)
			cards[i].setTargetPos(computeIndividualCardTargetPos (type, i));
	}
	public void setLayoutType(int type)	// sets position of every and each cards inside the deck.
	{
		currentLayoutType = type;
	}
	public void openDeck()	// show face of all cards in deck
	{
		for (int i=0; i<cards.Count; i++)
			cards[i].GetComponent<Card>().showFace ();
	}

	public void closeDeck()	// show background for all cards in deck
	{
		for (int i=0; i<cards.Count; i++)
			cards[i].GetComponent<Card>().showBackground ();
	}
	private void setScaleAndOrder()		// Sets every card's scale corresponding to its parent's transform, and sets sorting order as well.
	{
		for (int i=0; i<cards.Count; i++)
		{
			cards [i].setSortingOrder (i+(deckID%100)*100);
			cards[i].GetComponent<Transform>().localScale = referenceTransform.localScale;
		}
	}

	/*
	 * Default case: collapse (Main deck)
	 * Case 1: spread by overlapping with each other. To the right.
	 * Case 2: same, spreads from middle. But almost collapsing. (used for shuffling animation!)
	 * Case 3: spreads out without overlap. To right.
	 * Case 4: spreads out without overlap. Spreads from middle.
	 * Case 5: rotates in a z axis.
	 * Case 6: Spreads in an arc shape. yPos is proportional to 1-cos(xPos)
	 * Case 7: Similar.
	 * 
	 * */
	//returns two 3D vectors (position, rotation in eulerian form) for each cards, relative to its index in the list.
	//this is desired 3D target vector for individual cards inside each deck.
	private Vector3[] computeIndividualCardTargetPos(int orientationType, int indexReference)
	{
		// 0th index represents position.
		// 1th index represents Euler rotation.
		Vector3[] pos = new Vector3[2];
		pos[0] = new Vector3(0,0,0);
		pos [1] = new Vector3 (0,0,0);
		switch (orientationType)
		{
		case 1:
			pos[0] = new Vector3 (indexReference*0.3f,0,0.001f);
			break;
		case 2:
			pos[0] = new Vector3 (indexReference*0.03f - (0.03f*(cards.Count-1))*0.5f,0,0.001f);
			break;
		case 3:
			pos[0] = new Vector3 (indexReference*1.2f,0,0.001f);
			break;
		case 4:
			pos[0] = new Vector3 (indexReference*1.2f - (1.2f*(cards.Count-1))*0.5f,0,0.001f);
			break;
		case 5:
			pos[0] = new Vector3 (indexReference*0.3f,0,0);
			pos[1] = new Vector3 (indexReference*(-120f)/(cards.Count),0,0.001f);
			break;
		case 6:			// You will see (1-cos) wave drawn on Y-axis
			float maximumTilt = 30f;
			maximumTilt *= Mathf.Sqrt (cards.Count)/7;		//maximum angle normalization
			float middleCardIndex = cards.Count/2f-0.5f;		//reference point for yDist=0
			float yDist = Mathf.Abs(indexReference-middleCardIndex);	//Y localPos, linear shape
			yDist = (1-Mathf.Cos(yDist/(30f)))*3f;		// y localPos transformed from linear to curved shape.
			pos[0] = new Vector3 (indexReference*0.3f - (0.3f*(cards.Count-1))*0.5f,-yDist,0.001f);
			pos[1] = new Vector3 (0,0,((indexReference+0.5f)*(-maximumTilt))/(cards.Count) + (maximumTilt/2));
			break;
		case 7:			// Non-normalized version of case 6
			float maximumTilt1 = 85f;
			float middleCardIndex1 = cards.Count/2f-0.5f;		//reference point for yDist=0
			float yDist1 = Mathf.Abs(indexReference-middleCardIndex1);	//Y distance from middle
			yDist1 = (1-Mathf.Cos(yDist1/(10f)))*5f;		// y distance transformed from linear to curve
			pos[0] = new Vector3 (indexReference*0.2f - (0.2f*(cards.Count-1))*0.5f,-yDist1,0.001f);
			pos[1] = new Vector3 (0,0,((indexReference+0.5f)*(-maximumTilt1))/(cards.Count) + (maximumTilt1/2));
			break;
		default:
			pos[0] = new Vector3 (indexReference*0.0015f,indexReference*0.0015f,0);
			break;
		}
		return pos;
	}


	//===================================== Overriding Monobehavior functions =====================================
	public void Awake()
	{
		if (reserveDeckID>200)
			deckID = 0;
		else if (reserveDeckID >= 100)
			deckID = reserveDeckID;
		cards = new List<Card>();
		referenceTransform = GetComponent<Transform> ();
		gameObject.AddComponent ("AudioSource");
	}
	// Use this for initialization
	public void Start () {
		GameMaster.reportDeckToGameMaster (this);
	}
	
	// Update is called once per frame
	void Update () {
	}


}
