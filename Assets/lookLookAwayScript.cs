using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;
using Rnd = UnityEngine.Random;

public class lookLookAwayScript : MonoBehaviour {

	public KMAudio Audio;
	public KMBombModule Module;
	public KMBombInfo Info;
	public TextMesh ScreenArrow;
	public KMSelectable ModulePlate;
	public GameObject VisibilityCube;

	public Color[] ArrowColour;

	//log moment
	static int _moduleIdCounter = 1;
	int _moduleId;
	private bool moduleSolved;

	private int[,] arrowField = new int[7, 7] {
		{-11, 7, 6, 3, 2, 6, -11},
		{1, 2, 0, 4, 5, 0, 4},
		{7, 4, 5, 1, 7, 2, 3},
		{0, 3, 6, -11, 6, 5, 1},
		{7, 1, 5, 0, 4, 3, 7},
		{4, 2, 6, 3, 7, 2, 0},
		{-11, 4, 7, 1, 6, 5, -11}
	};
		
	private int[] arrowFieldIndexRow = {1,4,2,4,2,1,5,5};
	private int[] arrowFieldIndexCol = {2,1,5,5,1,4,2,4};
	private int[] arrowFieldRowOffset = {-1,0,1,0,-1,1,1,-1};
	private int[] arrowFieldColOffset = {0,1,0,-1,1,1,-1,-1};

	private string[] directionLetters = {"A","E","B","F","C","G","D","H"};
	private string[] directionName = {"Up","Up-Right","Right","Down-Right","Down","Down-Left","Left","Up-Left"};
	private int[] _directionShuffleArr = { 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3, 4, 5, 6, 7};

	private int submissionAmount;
	private int _timerLastDigit;
	private int _currentDirection;
	private int _currentDirectionRead;
	private int _PreviousDirection;
	private bool striking;
	private bool visible;

	List<int> _SubmissionSequence = new List<int>();
	List<int> _givenDirection = new List<int>();
	List<int> _correctDirection = new List<int>();

	// Use this for initialization
	void Start ()
	{
		_moduleId = _moduleIdCounter++;

		//select given direction sequence
		_directionShuffleArr.Shuffle();
		for (int i = 0; i < 8; i++)
			_givenDirection.Add(_directionShuffleArr[i]);
		Debug.LogFormat("[Look, Look Away #{0}] Given direction sequence: {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}.", _moduleId,
			directionName[_givenDirection[0]],
			directionName[_givenDirection[1]],
			directionName[_givenDirection[2]],
			directionName[_givenDirection[3]],
			directionName[_givenDirection[4]],
			directionName[_givenDirection[5]],
			directionName[_givenDirection[6]],
			directionName[_givenDirection[7]]
		);

		//calculate correct sequence

		int indexRow, indexCol, currentCalc, currentNext;
		for (int i = 0; i < 7; i++) {
			currentCalc = _givenDirection [i];
			currentNext = _givenDirection [i + 1];
			indexRow = arrowFieldIndexRow [currentCalc] + arrowFieldRowOffset [currentNext];
			indexCol = arrowFieldIndexCol [currentCalc] + arrowFieldColOffset [currentNext];
			_correctDirection.Add(arrowField[indexRow, indexCol]);
			Debug.LogFormat ("[Look, Look Away #{0}] Moving Direction {1} ({2}) from Arrow {3} ({4}), landed on {5}. ", _moduleId,
				i+2,
				directionName [currentNext],
				i+1,
				directionName [currentCalc],
				directionName [_correctDirection [i]]
			);
		}
		indexRow = arrowFieldIndexRow [_givenDirection [7]] + arrowFieldRowOffset [_givenDirection [0]];
		indexCol = arrowFieldIndexCol [_givenDirection [7]] + arrowFieldColOffset [_givenDirection [0]];
		_correctDirection.Add(arrowField[indexRow, indexCol]);

		Debug.LogFormat ("[Look, Look Away #{0}] Moving Direction 1 ({1}) from Arrow 8 ({2}), landed on {3}. ", _moduleId,
			directionName [_givenDirection [7]],
			directionName [_givenDirection [0]],
			directionName [_correctDirection [7]]
		);
			
		Debug.LogFormat("[Look, Look Away #{0}] Correct Sequence: {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}.", _moduleId,
			directionName[_correctDirection[0]],
			directionName[_correctDirection[1]],
			directionName[_correctDirection[2]],
			directionName[_correctDirection[3]],
			directionName[_correctDirection[4]],
			directionName[_correctDirection[5]],
			directionName[_correctDirection[6]],
			directionName[_correctDirection[7]]
		);

		ScreenArrow.color = ArrowColour [0];

		ModulePlate.OnHighlightEnded += delegate () {
			ProcessSubmission ();
			return;
		};
			
		StartCoroutine(ArrowCycle());
	}

	private IEnumerator ArrowCycle()
	{
		_currentDirectionRead = 0;
		while (!moduleSolved)
		{
			ScreenArrow.text = directionLetters[_currentDirection];
			yield return new WaitUntil(() => _PreviousDirection != _currentDirection);
			_PreviousDirection = _currentDirection;
			if (!visible) {
				if (_currentDirection == _givenDirection [_currentDirectionRead]) {
					Audio.PlaySoundAtTransform ("blip" + (_currentDirectionRead + 1), Module.transform);
					//Debug.LogFormat("Played blip{0} while arrow pointing {1}",_currentDirectionRead + 1,directionName[_currentDirectionRead]);
					_currentDirectionRead = (_currentDirectionRead + 1) % 8;
				}
			} 
			else 
			{
				_currentDirectionRead = 0;
			}
			striking = false;

		}
		ScreenArrow.text = ":)";
	}

	private void ProcessSubmission()
	{
		GetComponent<KMAudio>().PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.SelectionTick, ModulePlate.transform);
		if (moduleSolved)
			return;

		if (submissionAmount < 7) {
			_SubmissionSequence.Add (_currentDirection);
			submissionAmount++;
			Debug.LogFormat("[Look, Look Away #{0}] Submitted {1}, {2} out of 8 directions submitted.", _moduleId,
				directionName[_currentDirection],
				submissionAmount
			);
			Audio.PlaySoundAtTransform ("blip" + (submissionAmount + 1), Module.transform);
			ScreenArrow.color = ArrowColour [2];
			return;
		}
		else
		{
			_SubmissionSequence.Add (_currentDirection);
			Debug.LogFormat("[Look, Look Away #{0}] Submitted {1}, 8 out of 8 directions submitted.", _moduleId,
				directionName[_currentDirection]
			);
			Debug.LogFormat("[Look, Look Away #{0}] All 8 directions submitted, checking answer...", _moduleId);
			for (int i = 0; i < 8; i++)
			{
				if (_SubmissionSequence [i] != _correctDirection [i]) {
					Debug.LogFormat ("[Look, Look Away #{0}] Expected {1}, Submitted {2}, Incorrect!", _moduleId,
						directionName [_correctDirection [i]],
						directionName [_SubmissionSequence [i]]
					);
					Module.HandleStrike ();
					submissionAmount = 0;
					_SubmissionSequence.Clear();
					Audio.PlaySoundAtTransform ("strike", Module.transform);
					striking = true;
					return;
				}
				else
				{
					Debug.LogFormat ("[Look, Look Away #{0}] Expected {1}, Submitted {2}, {3} out of 8 directions correct.", _moduleId,
						directionName [_correctDirection [i]],
						directionName [_SubmissionSequence [i]],
						i+1
					);
				}
			}
			moduleSolved = true;
			submissionAmount = 0;
			Debug.LogFormat("[Look, Look Away #{0}] All 8 directions correct, Module Disarmed!", _moduleId);
			ScreenArrow.color = ArrowColour [0];
			Audio.PlaySoundAtTransform ("solve", Module.transform);
			StartCoroutine(SolveAnimation());

			return;
		}
	}

	private static Rnd random = new Rnd();

	private IEnumerator SolveAnimation()
	{
		for (int i = 0; i < 26; i++)
		{
			string[] letterRand = {"A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z","a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z"};
			ScreenArrow.text =  letterRand[Rnd.Range(0,51)];
			yield return new WaitForSeconds (0.03f);
		}
		ScreenArrow.text = "<>";
		Module.HandlePass();
	}

	void Awake () {

	}

	void Activate()
	{
	}

	RaycastHit hit;
	Ray ray;
		
	// Update is called once per frame
	void Update () {
		//Debug.Log(Vector3.Distance(VisibilityCube.transform.position, Camera.main.transform.position));
		if(Vector3.Distance(VisibilityCube.transform.position, Camera.main.transform.position) < 1f)
		{
			//Debug.Log("Module is visible");
			visible = true;
			if (!striking) {
				if (submissionAmount > 0) {
					ScreenArrow.color = ArrowColour [2];
				} else {
					ScreenArrow.color = ArrowColour [0];
				}
			} else {
				ScreenArrow.color = ArrowColour [1];
			}
		}
		else
		{
			//Debug.Log("Module is not visible");
			visible = false;
			ScreenArrow.color = ArrowColour [3];
			if (submissionAmount > 0)
			{
				submissionAmount = 0;
				_SubmissionSequence.Clear ();
				Debug.LogFormat ("[Look, Look Away #{0}] Module is no longer in view, inputs have been reset.", _moduleId);
				Audio.PlaySoundAtTransform ("reset", Module.transform);
			}
		}

		_currentDirection = 7 - (int)Info.GetTime() % 8;
		
	}


}
