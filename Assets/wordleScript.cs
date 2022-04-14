using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class wordleScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable ModuleSelectable;
    public TextMesh[] Letters;
    public GameObject[] Tiles;
    public Material[] TileColors; //Dark, Dark Filled, Dark BG, Light, Light Filled, Light BG, Gray, Yellow, Green, Blue, Orange
    public GameObject ButtonObj;
    public TextMesh ButtonText;
    public GameObject Background;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    bool lightMode = false;
    bool highContrastMode = false;
    bool hardMode = false;

    bool focused = false;
    private KeyCode[] Keys =
	{
        KeyCode.Backspace, KeyCode.Return,
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M
    };
    string letters = "QWERTYUIOPASDFGHJKLZXCVBNM";
    string word;
    int stage = 0;
    string[] guesses = {"", "", "", "", "", ""};
    char[] wordButBroken = {'_', '_', '_', '_', '_'};
    string[] guessColors = {"", "", "", "", "", ""};
    int[] wrongValues = {0, 0, 0, 1, 1, 0, -4, 6, -4, -3, 0, 6, -6, -1, 5, 1, -2};
    string[] congratsMessages = { "Genius", "Magnificent", "Impressive", "Splendid", "Great", "Phew" };
    int[][] correctValues = {
        new int[32] { 0, 1, 8, 9, 5, 2, 1, -3, -19, -7,   1, 11,  4,   0, -5, -4,  -3, -3,  0,   1,  1,  0,  0,  0,  0,  0,  0,  0,  0, 0, 0, 0 },
        new int[32] { 0, 0, 0, 1, 8, 9, 5,  2,   1, -3, -19, -7,  1,  11,  3,  1,  -5, -4, -3,  -2, -1,  0,  0,  1,  1,  0,  0,  0,  0, 0, 0, 0 },
        new int[32] { 0, 0, 0, 0, 0, 0, 1,  8,   9,  5,   2,  1, -3, -19, -7,  1,  11,  3,  1,  -5, -4, -3, -1, -1,  0,  0,  0,  1,  0, 0, 0, 0 },
        new int[32] { 0, 0, 0, 0, 0, 0, 0,  0,   0,  1,   8,  9,  5,   2,  1, -3, -19, -7,  1,  11,  4,  0, -2, -7, -3, -2,  0,  0,  0, 0, 1, 0 },
        new int[32] { 0, 0, 0, 0, 0, 0, 0,  0,   0,  0,   0,  0,  1,   8,  9,  5,   2,  1, -3, -20, -6,  1,  6,  9,  0, -5, -4, -3, -2, 0, 0, 1 }
    };
    bool flipping = false;
    char[] hardYellows = {'_', '_', '_', '_', '_'};
    char[] hardGreens = {'_', '_', '_', '_', '_'};
    string[] ordinals = {"1st", "2nd", "3rd", "4th", "5th"};

    void Awake () {
        moduleId = moduleIdCounter++;
        if (Application.isEditor)
        {
            focused = true;
        }
        ModuleSelectable.OnFocus += delegate () { focused = true; };
        ModuleSelectable.OnDefocus += delegate () { focused = false; };
    }

    // Use this for initialization
    void Start () {
        if (lightMode) {
            Background.GetComponent<Renderer>().material = TileColors[5];
            ButtonObj.GetComponent<Renderer>().material = TileColors[2];
            ButtonText.color = Color.white;
            for (int q = 0; q < 30; q++) {
                Tiles[q].GetComponent<Renderer>().material = TileColors[3];
                Letters[q].color = Color.black;
            }
        }
        ButtonObj.SetActive(false);
        word = wordList.validAnswers.PickRandom().ToUpper();
        Debug.LogFormat("[Wordle #{0}] Word is {1}", moduleId, word);
    }

    void Update () {
        for (int k = 0; k < Keys.Count(); k++) {
            if (Input.GetKeyDown(Keys[k])) {
                HandleKey(k);
            }
        }
    }

    void HandleKey (int n) {
        if (flipping || !focused) {
            return;
        }
        if (n == 0) {
            if (guesses[stage] != "") {
                guesses[stage] = guesses[stage].Substring(0, guesses[stage].Length - 1);
                RemoveLetter(guesses[stage].Length);
            }
        } else if (n == 1) {
            if (guesses[stage].Length == 5) {
                if (wordList.validGuesses.Contains(guesses[stage].ToLower())) {
                    if (hardMode) {
                        for (int y = 0; y < 5; y++) {
                            if (hardYellows[y] != '_' && !guesses[stage].Contains(hardYellows[y])) {
                                StartCoroutine(Message(String.Format("Guess must contain {0}", hardYellows[y].ToString().ToUpper()), 0.02f));
                                StartCoroutine(WrongAnimation(stage));
                                return;
                            }
                        }
                        for (int g = 0; g < 5; g++) {
                            if (hardGreens[g] != '_' && hardGreens[g] != guesses[stage][g]) {
                                StartCoroutine(Message(String.Format("{0} letter must be {1}", ordinals[g], hardGreens[g].ToString().ToUpper()), 0.02f));
                                StartCoroutine(WrongAnimation(stage));
                                return;
                            }
                        }
                    }
                    CalculateColors(stage);
                    flipping = true;
                    StartCoroutine(FlipAnimation(stage));
                } else {
                    StartCoroutine(Message("Not in word list", 0.02f));
                    StartCoroutine(WrongAnimation(stage));
                }
            }
        } else {
            if (guesses[stage].Length != 5) {
                guesses[stage] = guesses[stage] + letters[n-2];
                AddLetter(guesses[stage].Length - 1, letters[n-2].ToString());
            }
        }
    }

    void AddLetter(int p, string l) {
        Letters[stage*5 + p].text = l;
        StartCoroutine(AnimateTile(stage*5 + p));
    }

    void RemoveLetter(int p) {
        Letters[stage*5 + p].text = "";
        Tiles[stage*5 + p].GetComponent<Renderer>().material = TileColors[(lightMode ? 3 : 0)];
    }

    void CalculateColors(int s) {
        for (int t = 0; t < 5; t++) {
            wordButBroken[t] = word[t];
        }
        int[] currentColors = { 0, 0, 0, 0, 0 };
        char[] lets = {'a', 'y', 'g'}; 
        char[] hcLets = {'a', 'b', 'o'}; 

        for (int l = 0; l < 5; l++) {
            if (guesses[s][l] == word[l]) {
                currentColors[l] = 2;
                wordButBroken[l] = '_';
                if (hardMode) {
                    hardGreens[l] = word[l];
                }
            }
        }

        for (int o = 0; o < 5; o++) {
            if (currentColors[o] == 2) {
                continue;
            } else {
                if (wordButBroken.Contains(guesses[s][o])) {
                    currentColors[o] = 1;
                    Adjust(guesses[s][o]);
                    if (hardMode) {
                        hardYellows[o] = guesses[s][o];
                    }
                }
            }
        }

        guessColors[s] = "" + currentColors[0] + currentColors[1] + currentColors[2] + currentColors[3] + currentColors[4];
        if (highContrastMode) {
            Debug.LogFormat("[Wordle #{0}] {1}{2}{3}{4}{5}", moduleId, hcLets[currentColors[0]], hcLets[currentColors[1]], hcLets[currentColors[2]], hcLets[currentColors[3]], hcLets[currentColors[4]] );
        } else {
            Debug.LogFormat("[Wordle #{0}] {1}{2}{3}{4}{5}", moduleId, lets[currentColors[0]], lets[currentColors[1]], lets[currentColors[2]], lets[currentColors[3]], lets[currentColors[4]] );
        }
    }

    void Adjust (char c) {
        for (int l = 0; l < 5; l++) {
            if (wordButBroken[l] == c) {
                wordButBroken[l] = '_';
                break;
            }
        }
    }

    IEnumerator AnimateTile(int t) {
        Tiles[t].GetComponent<Renderer>().material = TileColors[(lightMode ? 5 : 2)];
        Tiles[t].transform.localScale = new Vector3(0.016161f, 0.0025f, 0.016161f);
        yield return new WaitForSeconds(0.066666f);
        Tiles[t].GetComponent<Renderer>().material = TileColors[(lightMode ? 4 : 1)];
        Tiles[t].transform.localScale = new Vector3(0.021818f, 0.0025f, 0.021818f);
        yield return new WaitForSeconds(0.033333f);
        Tiles[t].transform.localScale = new Vector3(0.02f, 0.0025f, 0.02f);
    }

    IEnumerator FlipAnimation(int s) {
        StartCoroutine(FlipOne(s*5));
        yield return new WaitForSeconds(0.333333f);
        StartCoroutine(FlipOne(s*5+1));
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(FlipOne(s*5+2));
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(FlipOne(s*5+3));
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(FlipOne(s*5+4));
    }

    IEnumerator FlipOne(int t) {
        int n = 6;
        for (int f = 0; f < 6; f++) {
            Tiles[t].transform.Rotate(15f, 0f, 0f, Space.Self);
            yield return new WaitForSeconds(0.033333f);
        }
        switch (guessColors[stage][t%5]) {
            case '1': n = (highContrastMode ? 9 : 7); break;
            case '2': n = (highContrastMode ? 10 : 8); break;
            default: break;
        }
        Tiles[t].GetComponent<Renderer>().material = TileColors[n];
        Tiles[t].transform.Rotate(180f, 0f, 0f, Space.Self);
        if (lightMode) { 
            Letters[t].color = Color.white; 
        }
        for (int b = 0; b < 8; b++) {
            Tiles[t].transform.Rotate(11.25f, 0f, 0f, Space.Self);
            yield return new WaitForSeconds(0.033333f);
        }
        if (t%5 == 4) {
            if (guessColors[stage] == "22222") {
                StartCoroutine(SolveAnimation(stage));
            } else {
                stage++;
                if (stage == 6) {
                    StartCoroutine(Reset());
                } else {
                    flipping = false;
                }
            }
        }
    }

    IEnumerator WrongAnimation(int s) {
        for (int x = 0; x < wrongValues.Length; x++) {
            for (int t = 0; t < 5; t++) {
                Tiles[s*5+t].transform.localPosition += new Vector3(0.0003636f*wrongValues[x], 0f, 0f);
            }
            yield return new WaitForSeconds(0.033333f);
        }
    }

    IEnumerator SolveAnimation(int s) {
        ButtonObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        ButtonObj.SetActive(true);
        ButtonText.text = congratsMessages[s];
        for (int b = 0; b < 5; b++) {
            Tiles[s*5+b].transform.localPosition += new Vector3(0f, 0.0001f, 0f);
        }
        for (int f = 0; f < 32; f++) {
            for (int t = 0; t < 5; t++) {
                Tiles[s*5+t].transform.localPosition += new Vector3(0f, 0f, 0.0003636f*correctValues[t][f]);
            }
            yield return new WaitForSeconds(0.033333f);
        }
        for (int a = 0; a < 5; a++) {
            Tiles[s*5+a].transform.localPosition -= new Vector3(0f, 0.0001f, 0f);
        }
        GetComponent<KMBombModule>().HandlePass();
        yield return new WaitForSeconds(0.3f);
        ButtonObj.SetActive(false);
        ButtonText.text = "";
    }

    IEnumerator Reset() {
        StartCoroutine(Message(word, 0.01f));
        for (int r = 5; r > -1; r--) {
            for (int l = 4; l > -1; l--) {
                Letters[r*5 + l].text = ""; 
                guesses[r] = "";
                guessColors[r] = "";
                StartCoroutine(AnimateTile(r*5 + l));
                yield return new WaitForSeconds(0.033333f);
            }
        }
        word = wordList.validAnswers.PickRandom().ToUpper();
        Debug.LogFormat("[Wordle #{0}] Word is {1}", moduleId, word);
        stage = 0;
        flipping = false;
    }

    IEnumerator Message (string s, float f) {
        ButtonObj.transform.localScale = new Vector3(f, 0.01f, 0.01f);
        ButtonObj.SetActive(true);
        ButtonText.text = s;
        yield return new WaitForSeconds(1f);
        ButtonObj.SetActive(false);
        ButtonText.text = "";
    }
}
