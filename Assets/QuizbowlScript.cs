using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class QuizbowlScript : MonoBehaviour
{

    static int moduleIdCounter = 1;
    int moduleId;
    int currentClue = 0;
    int ans = 0;
    int toss = 0;
    string selectedTossup = "";
    string answer = "";
    bool _isSolved = false;

    string yourAnswer = "";
    private string currentClueDisplay;

    public KMBombModule bombModule;

    string[] clues;
    List<string> acceptableAnswers = new List<string>();
    string[] answers;

    bool showNext;
    bool nextTossup = false;
    bool showBuzz;
    bool activated;
    bool connecting = false;
    bool focused = false;
    bool Submit = false;
    bool autosolving = false;

    string TheLetters = "<eQWERTYUIOPASDFGHJKLZXCVBNM1234567890-'. ";

    private KeyCode[] TheKeys =
    {
        KeyCode.Backspace, KeyCode.Return,
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M,
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        KeyCode.Minus, KeyCode.Quote, KeyCode.Period, KeyCode.Space
    };

    public TextMesh Hint;
    public TextMesh Answering;
    public Renderer BuzzerLights;
    public Material[] colors;
    public KMAudio Audio;
    public AudioClip[] Buzzing;
    public KMSelectable Next;
    public KMSelectable Buzz;
    public KMSelectable Activate;
    public KMSelectable ModuleSelectable;

    // Use this for initialization
    void Awake()
    {
        moduleId = moduleIdCounter++;
        BuzzerLights.material = colors[0];

        Next.OnInteract += delegate () { buttonPress(); return false; };
        Buzz.OnInteract += delegate () { Buzzer(); return false; };
        Activate.OnInteract += delegate () { EnterMode(); return false; };

        if (Application.isEditor)
            focused = true;
        ModuleSelectable.OnFocus += delegate () { focused = true; };
        ModuleSelectable.OnDefocus += delegate () { focused = false; };
    }

    void Start()
    {
        Submit = false;

        StartCoroutine(Pinger());
        Hint.text = "Connecting...";
    }

    void EnterMode()
    {
        activated = false;

        clues = selectedTossup.Split('.');

        for (int i = 0; i < clues.Length; i++)
        {
            clues[i] += @".";
            if (i < clues.Length - 1 && clues[i + 1].StartsWith("\""))
            {
                clues[i] += "\"";
                clues[i + 1] = clues[i + 1].TrimStart('\"', ' ');
            }
        }

        currentClueDisplay = clues[0];
        StartCoroutine(TextingClue());
    }

    // Update is called once per frame
    void Update()
    {
        if (showNext == false)
        {
            Next.gameObject.SetActive(false);
        }
        else
        {
            Next.gameObject.SetActive(true);
        }
        if (showBuzz == false)
        {
            Buzz.gameObject.SetActive(false);
        }
        else
        {
            Buzz.gameObject.SetActive(true);
        }
        if (activated == false)
        {
            Activate.gameObject.SetActive(false);
        }
        else
        {
            Activate.gameObject.SetActive(true);
        }

        if (Submit == true)
        {
            Answering.text = yourAnswer.Replace("$", yourAnswer);
            for (int i = 0; i < TheKeys.Count(); i++)
            {
                if (Input.GetKeyDown(TheKeys[i]))
                {
                    if (TheLetters[i].ToString() == "<".ToString())
                    {
                        if (_isSolved == false)
                        {
                            handleBack();
                        }
                    }
                    else if (TheLetters[i].ToString() == "e".ToString())
                    {
                        if (_isSolved == false)
                        {
                            handleEnter();
                        }
                    }
                    else
                    {
                        if (_isSolved == false)
                        {
                            handleKey(TheLetters[i]);
                        }
                    }
                }
            }
        }
    }

    void handleBack()
    {
        if (focused || autosolving)
        {
            if (yourAnswer.Length != 0)
            {
                yourAnswer = yourAnswer.Substring(0, yourAnswer.Length - 1);
            }
        }
    }

    void handleEnter()
    {
        if (focused || autosolving)
        {
            bool right = false;
            Debug.LogFormat("[Quizbowl #{0}] Submitted: {1}", moduleId, yourAnswer);

            for (int i = 0; i < acceptableAnswers.Count; i++)
            {
                if (!right)
                {
                    if (yourAnswer.ToUpper() == acceptableAnswers[i].ToUpper())
                    {
                        right = true;
                    }
                    else
                    {
                        right = false;
                    }
                }
            }

            if (right == true)
            {
                BuzzerLights.material = colors[1];
                Hint.text = "";
                Answering.text = "";
                _isSolved = true;
                bombModule.HandlePass();
            }
            else
            {
                bombModule.HandleStrike();
                BuzzerLights.material = colors[2];
                yourAnswer = "";
                Answering.text = "";
                Submit = false;
                StartCoroutine(TextingClue());
                StartCoroutine(TextingAnswer());
                if (currentClue >= clues.Length - 2)
                {
                    clues[0] = " ANSWERS:\n";
                    for (int i = 0; i < acceptableAnswers.Count; i++)
                    {
                        clues[0] += acceptableAnswers[i] + "\n";
                    }
                    clues[0] += "Press Next to get Next Tossup.";
                    currentClueDisplay = clues[0];
                    currentClue = 0;
                    acceptableAnswers.Clear();
                    nextTossup = true;
                    showBuzz = false;
                }
            }
        }
    }

    void handleKey(char c)
    {
        if (focused || autosolving)
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TypewriterKey, transform);
            if (yourAnswer.Length != 100)
            {
                yourAnswer = yourAnswer + c;
            }
        }
    }

    void buttonPress()
    {
        if (nextTossup)
        {
            Submit = false;
            nextTossup = false;
            showNext = false;
            StartCoroutine(Pinger());
            Hint.text = "Connecting...";
        }
        else
        {
            currentClue++;
            currentClueDisplay = clues[currentClue];
            StartCoroutine(TextingClue());
        }
    }

    void Buzzer()
    {
        int bu = Rnd.Range(0, 2);
        switch (bu)
        {
            case 0:
                Audio.PlaySoundAtTransform("buzz1", transform);
                break;
            case 1:
                Audio.PlaySoundAtTransform("buzz2", transform);
                break;
        }
        yourAnswer = "";
        Submit = true;
        showNext = false;
        showBuzz = false;
        BuzzerLights.material = colors[1];
    }

    IEnumerator TextingClue()
    {
        showNext = false;
        showBuzz = true;
        bool lettered = false;
        Hint.text = "";
        int spaceCount = 0;
        int letterCount = 0;
        for (int b = 0; b < currentClueDisplay.Length; b++)
        {
            if (Submit == true)
            {
                break;
            }
            Hint.text += currentClueDisplay[b].ToString();
            if (currentClueDisplay[b].ToString() == " ")
            {
                spaceCount += 1;
                lettered = false;
                letterCount = 0;
            }

            if (currentClueDisplay[b].ToString() == "-")
            {
                spaceCount += 1;
                lettered = false;
                letterCount = 0;
            }
            if (currentClueDisplay[b].ToString() != " " && currentClueDisplay[b].ToString() != "-")
            {
                letterCount += 1;
            }
            if (letterCount >= 10 && lettered == false)
            {
                spaceCount += 1;
                lettered = true;
            }
            if (currentClueDisplay[b].ToString() == "\n")
            {
                spaceCount = 0;
            }
            if (spaceCount >= 6 && lettered == false)
            {
                Hint.text = (Hint.text + "\n " + "");
                spaceCount = 0;
            }

            yield return new WaitForSecondsRealtime(0.03f);
        }
        if ((Hint.text.Length >= currentClueDisplay.Length && currentClue < clues.Length - 2) || nextTossup)
        {
            showNext = true;
        }
    }

    IEnumerator TextingAnswer()
    {
        yield return new WaitForSecondsRealtime(1.0f);
        BuzzerLights.material = colors[0];
    }

    IEnumerator Pinger()
    {
        connecting = true;
        toss = Rnd.Range(0, 200) * 2;
        ans = toss + 1;
        selectedTossup = TossupList.phrases[toss];
        answer = TossupList.phrases[ans];
        yield return new WaitForSecondsRealtime(1.0f);
        if (answer != null && selectedTossup != "null")
        {

            if (answer.Contains(" or ".ToLower()))
            {
                answers = Regex.Split(answer, " or ");
                for (int i = 0; i < answers.Length; i++)
                {
                    acceptableAnswers.Add(answers[i]);
                }
            }
            else
            {
                acceptableAnswers.Add(answer);
            }

        }

        if (selectedTossup.Contains("?"))
        {
            selectedTossup = selectedTossup.Replace("?", ".");
        }
        if (selectedTossup.Contains("”"))
        {
            selectedTossup = selectedTossup.Replace("”", "\"");
        }
        if (selectedTossup.Contains("“"))
        {
            selectedTossup = selectedTossup.Replace("“", "\"");
        }
        
        string chari1 = "âáàäāçéèêḥíīïñóöșúū";
        string chari2 = "aaaaaceeehiiinoosuu";
        string[] chari3 = { "Ã³", "Ã­", "Ã¤", "Ã¶" };
        string[] chari4 = { "ó", "í", "ä", "ö" };
        for (int j = 0; j < acceptableAnswers.Count(); j++)
        {
            for (int i = 0; i < chari1.Length; i++)
            {
                if (acceptableAnswers[j].Contains(chari1[i]))
                {
                    acceptableAnswers[j] = acceptableAnswers[j].Replace(chari1[i], chari2[i]);
                }
            }
            for (int i = 0; i < chari3.Length; i++)
            {
                if (acceptableAnswers[j].Contains(chari3[i]))
                {
                    acceptableAnswers[j] = acceptableAnswers[j].Replace(chari3[i], chari4[i]);
                }
            }

        }
        for (int i = 0; i < chari3.Length; i++)
        {
            if (selectedTossup.Contains(chari3[i]))
            {
                selectedTossup = selectedTossup.Replace(chari3[i], chari4[i]);
            }
            if (answer.Contains(chari3[i]))
            {
                answer = answer.Replace(chari3[i], chari4[i]);
            }
        }

        yield return new WaitForSecondsRealtime(1.0f);

        Debug.LogFormat("[Quizbowl #{0}] Tossup: {1}", moduleId, selectedTossup);
        Debug.LogFormat("[Quizbowl #{0}] Acceptable Answers: {1}, Answers are as follows:", moduleId, acceptableAnswers.Count);
        foreach (string obj in acceptableAnswers)
            Debug.LogFormat("[Quizbowl #{0}] {1}", moduleId, obj);
        connecting = false;
        activated = true;
        Hint.text = "Connected";
    }

    //twitch plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} activate [Presses the activate button] | !{0} next [Presses the next button] | !{0} submit <ans> [Submits the specified answer 'ans']";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*activate\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (!activated)
            {
                yield return "sendtochaterror This button cannot be pressed right now!";
                yield break;
            }
            Activate.OnInteract();
        }
        if (Regex.IsMatch(command, @"^\s*next\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (!showNext)
            {
                yield return "sendtochaterror This button cannot be pressed right now!";
                yield break;
            }
            Next.OnInteract();
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify an answer to submit!";
            else
            {
                for (int i = 7; i < command.Length; i++)
                {
                    if (!"QWERTYUIOPASDFGHJKLZXCVBNM1234567890-'. ".Contains(command.ToUpper()[i]))
                    {
                        yield return "sendtochaterror!f The specified character '" + command[i] + "' is invalid!";
                        yield break;
                    }
                }
                if (!showBuzz)
                {
                    yield return "sendtochaterror You cannot submit an answer right now!";
                    yield break;
                }
                Buzz.OnInteract();
                yield return new WaitForSeconds(.1f);
                for (int i = 7; i < command.Length; i++)
                {
                    handleKey(command.ToUpper()[i]);
                    yield return new WaitForSeconds(.1f);
                }
                bool correct = false;
                foreach (string obj in acceptableAnswers)
                {
                    if (command.Substring(7).ToLower() == obj.ToLower())
                    {
                        correct = true;
                        break;
                    }
                }
                if (correct)
                    yield return "solve";
                else
                    yield return "strike";
                handleEnter();
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        autosolving = true;
        while (connecting) yield return true;
        if (activated)
        {
            Activate.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        if (showBuzz)
        {
            Buzz.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        while (Answering.text != "")
        {
            handleBack();
            yield return new WaitForSeconds(.1f);
        }
        int choice = Rnd.Range(0, acceptableAnswers.Count);
        for (int i = 0; i < acceptableAnswers[choice].Length; i++)
        {
            handleKey(acceptableAnswers[choice].ToUpper()[i]);
            yield return new WaitForSeconds(.1f);
        }
        handleEnter();
        autosolving = false;
    }

}
