using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class jamScript : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
	public KMBombModule module;

    public KMSelectable toggleButton;
    public TextMesh[] screenText;

    private KeyCode[] typableKeys =
    {
        KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Backspace
    };
    private bool focused;
    private int typedDigits;

    private int[] numbers = new int[8];
    private int[] topFunctions = new int[4];
    private int[] bottomFunctions = new int[4];
    private int sharedFunction;
    private int[] inputNumbers = new int[4];
    private int[] outputNumbers = new int[4];
    private float holdingTime = 0f;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool isAnimating, inputMode, firstTimeInput = true, buttonHeld, buttonisPressed, moduleSolved; 
    void Awake()
    {
    	moduleId = moduleIdCounter++;
        toggleButton.OnInteract += () => { toggleHolder(); return false; };
        toggleButton.OnInteractEnded += () => { toggleHandler(); };
        GetComponent<KMSelectable>().OnFocus += delegate () { focused = true; };
        GetComponent<KMSelectable>().OnDefocus += delegate () { focused = false; };
        if (Application.isEditor)
            focused = true;
    }

    void Start()
    {
        for (int i = 0; i < screenText.Length; i++)
        {
            numbers[i] = UnityEngine.Random.Range(1, 1000);
            screenText[i].text = numbers[i].ToString("000");
        }
        int[] dupSelectFunc = new int[15] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        sharedFunction = UnityEngine.Random.Range(0, dupSelectFunc.Length);
        dupSelectFunc = dupSelectFunc.Where(val => val != sharedFunction).ToArray();
        Debug.LogFormat("[Dual Sequences #{0}] The shared function is Function {1}.", moduleId, sharedFunction + 1);
        for (int i = 0; i < 3; i++)
        {
            topFunctions[i] = dupSelectFunc[UnityEngine.Random.Range(0, dupSelectFunc.Length)];
            dupSelectFunc = dupSelectFunc.Where(val => val != topFunctions[i]).ToArray();
            Debug.LogFormat("[Dual Sequences #{0}] One of the other functions on the top screen is Function {1}.", moduleId, topFunctions[i] + 1);
        }
        for (int i = 0; i < 3; i++)
        {
            bottomFunctions[i] = dupSelectFunc[UnityEngine.Random.Range(0, dupSelectFunc.Length)];
            dupSelectFunc = dupSelectFunc.Where(val => val != bottomFunctions[i]).ToArray();
            Debug.LogFormat("[Dual Sequences #{0}] One of the other functions on the bottom screen is Function {1}.", moduleId, bottomFunctions[i] + 1);
        }
        topFunctions[3] = sharedFunction;
        topFunctions.Shuffle();
        bottomFunctions[3] = sharedFunction;
        bottomFunctions.Shuffle();
        StartCoroutine("numberCycler");
    }

    void toggleHolder()
    {
        if (moduleSolved) { return; }
        buttonisPressed = true;
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        toggleButton.AddInteractionPunch(0.4f);
        buttonHeld = true;
        StartCoroutine(buttonHolding());
    }

    IEnumerator buttonHolding()
    {
        while (buttonHeld)
        {
            yield return null;
            if (isAnimating)
            {
                buttonisPressed = false;
            }
            else if (buttonisPressed || !inputMode)
            {
                holdingTime += Time.deltaTime;
                if (holdingTime > 1f)
                {
                    audio.PlaySoundAtTransform("Cue", transform);
                    break;
                }
            }
        }
    }

    void toggleHandler()
    {
        if (moduleSolved || isAnimating || !buttonisPressed)
        {
            holdingTime = 0f;
            return; 
        }
        StopCoroutine("numberCycler");
        StopCoroutine("Reset");
        buttonHeld = false;
        if (!inputMode && holdingTime > 1f)
        {
            StartCoroutine("Reset");
        }
        else if (!inputMode)
        {
            inputMode = true;
            StartCoroutine("toInput");
        }
        else if (inputMode && typedDigits == 12)
        {
            bool toSolve = true;
            inputMode = false;
            var sb = new StringBuilder();
            Debug.LogFormat("[Dual Sequences #{0}] Submission time! Let's see...", moduleId);
            for (int i = 0; i < 4; i++)
            {
                if (outputNumbers[i] != Convert.ToInt32(screenText[i + 4].text))
                {
                    Debug.LogFormat("[Dual Sequences #{0}] Input {1} for value {2} does not match the answer {3}...", moduleId, screenText[i + 4].text, inputNumbers[i], outputNumbers[i]);
                    toSolve = false;
                    for (int j = 0; j < 3; j++)
                    {
                        if(screenText[i+4].text[j] == outputNumbers[i].ToString("000")[j])
                        {
                            sb.Append("<color=lime>" + screenText[i + 4].text[j] + "</color>");
                        }
                        else
                        {
                            sb.Append("<color=red>" + screenText[i + 4].text[j] + "</color>");
                        }
                    }
                    screenText[i + 4].text = sb.ToString();
                }
                else
                {
                    Debug.LogFormat("[Dual Sequences #{0}] Input {1} for value {2} does match the answer {3}...", moduleId, screenText[i + 4].text, inputNumbers[i], outputNumbers[i]);
                    screenText[i + 4].color = new Color(0f, 1f, 0f, 1f);
                }
                sb.Remove(0, sb.Length);
            }
            if (toSolve)
            {
                module.HandlePass();
                moduleSolved = true;
                Debug.LogFormat("[Dual Sequences #{0}] All inputs correct, module solved!", moduleId);
                StartCoroutine(SolveAnim());
            }
            else
            {
                module.HandleStrike();
                Debug.LogFormat("[Dual Sequences #{0}] Some values are wrong, strike!", moduleId);
                typedDigits = 0;
                StartCoroutine(StrikeAnimTop());
                StartCoroutine(StrikeAnimBottom());
            }
        }
        holdingTime = 0f;
    }

    IEnumerator numberCycler()
    {
        string[] placeholderText = new string[8];
        int[] placeholderFunctions = new int[4];
        while (!moduleSolved)
        {
            for (int i = 0; i < screenText.Length; i++)
            {
                screenText[i].text = "";
                screenText[i].color = new Color(1f, 1f, 1f, 1f);
                if (i < 4) { numbers[i] = functions.function(numbers[i], topFunctions[i]); }
                else { numbers[i] = functions.function(numbers[i], bottomFunctions[i - 4]); }
                placeholderText[i] = numbers[i].ToString("000");
            }
            Array.Copy(topFunctions, placeholderFunctions, topFunctions.Length);
            for (int i = 0; i < topFunctions.Length; i++)
            {
                if (i == 0) { topFunctions[i] = placeholderFunctions[placeholderFunctions.Length - 1]; }
                else { topFunctions[i] = placeholderFunctions[i - 1]; }
            }
            Array.Copy(bottomFunctions, placeholderFunctions, bottomFunctions.Length);
            for (int i = 0; i < bottomFunctions.Length; i++)
            {
                if (i == 0) { bottomFunctions[i] = placeholderFunctions[placeholderFunctions.Length - 1]; }
                else { bottomFunctions[i] = placeholderFunctions[i - 1]; }
            }
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < screenText.Length; i++)
                {
                    screenText[i].text += placeholderText[i][j];
                }
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(2.7f);
        }
    }
    
    IEnumerator Reset()
    {
        isAnimating = true;
        if (screenText[0].text.Length != 0)
        {
            int temp = screenText[0].text.Length;
            for (int j = 0; j < temp; j++)
            {
                for (int i = 0; i < screenText.Length; i++)
                {
                    screenText[i].text = screenText[i].text.Remove(screenText[i].text.Length - 1);
                    audio.PlaySoundAtTransform("Tick", transform);
                }
                yield return new WaitForSeconds(0.3f);
            }
        }
        yield return new WaitForSeconds(1f);
        string[] placeholderText = new string[8];
        for (int i = 0; i < screenText.Length; i++)
        {
            numbers[i] = UnityEngine.Random.Range(1, 1000);
            placeholderText[i] = numbers[i].ToString("000");
        }
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < screenText.Length; i++)
            {
                screenText[i].text += placeholderText[i][j];
            }
            yield return new WaitForSeconds(0.1f);
        }
        isAnimating = false;
        yield return new WaitForSeconds(2.7f);
        StartCoroutine("numberCycler");
    }

    IEnumerator toInput()
    {
        isAnimating = true;
        StringBuilder sb = new StringBuilder();
        if (screenText[0].text.Length != 0)
        {
            int temp = screenText[0].text.Length;
            for (int j = 0; j < temp; j++)
            {
                for (int i = 0; i < screenText.Length; i++)
                {
                    screenText[i].text = screenText[i].text.Remove(screenText[i].text.Length - 1);
                    audio.PlaySoundAtTransform("Tick", transform);
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
        yield return new WaitForSeconds(1f);
        string placeholderText = "";
        for (int i = 0; i < inputNumbers.Length; i++)
        {
            if (firstTimeInput)
            {
                inputNumbers[i] = UnityEngine.Random.Range(1, 1000);
            }
            outputNumbers[i] = functions.function(inputNumbers[i], sharedFunction);
            placeholderText = inputNumbers[i].ToString("000");
            sb.Append(placeholderText + ", ");
            for (int j = 0; j < placeholderText.Length; j++)
            {
                screenText[i].text += placeholderText[j];
                audio.PlaySoundAtTransform("Tick", transform);
                yield return new WaitForSeconds(0.1f);
            }
        }
        firstTimeInput = false;
        sb.Remove(sb.Length - 2, 2);
        Debug.LogFormat("[Dual Sequences #{0}] Input mode initiated! The numbers given are {1}.", moduleId, sb.ToString());
        isAnimating = false;
    }

    IEnumerator StrikeAnimTop()
    {
        isAnimating = true;
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                screenText[i].text = screenText[i].text.Remove(screenText[i].text.Length - 1);
                audio.PlaySoundAtTransform("Tick", transform);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator StrikeAnimBottom()
    {
        isAnimating = true;
        string[] placeholderText = new string[4];
        for (int i = 0; i < 4; i++)
        {
            placeholderText[i] = screenText[i + 4].text;
        }
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                screenText[j + 4].text = placeholderText[j];
                audio.PlaySoundAtTransform("Wrong", transform);
            }
            yield return new WaitForSeconds(0.4f);
            for (int j = 0; j < 4; j++)
            {
                screenText[j + 4].text = "";
            }
            yield return new WaitForSeconds(0.4f);
        }
        isAnimating = false;
        StartCoroutine("numberCycler");
    }

    IEnumerator SolveAnim()
    {
        isAnimating = true;
        audio.PlaySoundAtTransform("Solv", transform);
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < screenText.Length; i++)
            {
                screenText[i].text = screenText[i].text.Remove(screenText[i].text.Length - 1);
                audio.PlaySoundAtTransform("Tick", transform);
            }
            yield return new WaitForSeconds(0.5f);
        }
        isAnimating = false;
    }

    void Update() //Runs every frame.
    {
        for (int i = 0; i < typableKeys.Count(); i++)
        {
            if (Input.GetKeyDown(typableKeys[i]) && focused)
            {
                if (i < 20) { handleKey(i); }
                else if (i < 22 && inputMode) { StartCoroutine(handleEnter()); }
                else { handleBack(); }
            }
        }
    }

    void handleKey(int k)
    {
        if (!inputMode || moduleSolved || isAnimating || typedDigits >= 12) { return; }
        audio.PlaySoundAtTransform("Tick", transform);
        screenText[4 + typedDigits / 3].text += (k % 10).ToString();
        typedDigits++;
    }

    void handleBack()
    {
        if (!inputMode || moduleSolved || isAnimating || typedDigits == 0) { return; }
        audio.PlaySoundAtTransform("Tick", transform);
        screenText[4 + (typedDigits - 1) / 3].text = screenText[4 + (typedDigits - 1) / 3].text.Substring(0, screenText[4 + (typedDigits - 1) / 3].text.Length - 1);
        typedDigits--;
    }

    IEnumerator handleEnter()
    {
        toggleButton.OnInteract();
        yield return null;
        toggleButton.OnInteractEnded();
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"<!{0} toggle> to press the toggle button, <!{0} reset> to hold and release the toggle button, <!{0} set 123 345 567 789> to type in 123, 345, 567, 789 into the display";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*toggle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            toggleButton.OnInteract();
            yield return null;
            toggleButton.OnInteractEnded();
        }
        else if (Regex.IsMatch(parameters[0], @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            toggleButton.OnInteract();
            while (holdingTime <= 1f)
            {
                yield return null;
            }
            toggleButton.OnInteractEnded();
        }
        else if (Regex.IsMatch(parameters[0], @"^\s*set\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var presses = new List<int>();
            if (!inputMode) { yield return "sendtochaterror The module's not in submission mode yet. Command ignored."; yield break; }
            if (parameters.Length > 5)
            {
                yield return "sendtochaterror You can only set up to 4 values per command.";
                yield break;
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror There is no value to set.";
                yield break;
            }
            for (int i = 1; i < parameters.Length; i++)
            {
                for (int j = 0; j < parameters[i].Length; j++)
                {
                    if (parameters[i].Length != 3) { yield return "sendtochaterror You can only set three-digit values at a time."; yield break; }
                    else if (parameters[i][j] - '0' < 0 || parameters[i][j] - '0' > 9)
                    {
                        yield return "sendtochaterror One of the values is invalid."; 
                        yield break;
                    }
                    else
                    {
                        presses.Add(parameters[i][j] - '0');
                    }
                }
            }
            for (int i = 0; i < typedDigits; i++)
            {
                handleBack();
                yield return null;
            }
            foreach (int k in presses)
            {
                handleKey(k);
                yield return new WaitForSeconds(0.1f);
            }
        }

    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            if (isAnimating)
            {
                yield return null;
            }
            else if (!inputMode)
            {
                toggleButton.OnInteract();
                yield return null;
                toggleButton.OnInteractEnded();
            }
            else 
            {
                while (typedDigits != 0)
                {
                    handleBack();
                    yield return null;
                }
                for (int i = 0; i < outputNumbers.Length; i++)
                {
                    for (int j = 0; j < outputNumbers[i].ToString("000").Length; j++)
                    {
                        handleKey(outputNumbers[i].ToString("000")[j] - '0');
                        yield return null;
                    }
                }
                toggleButton.OnInteract();
                yield return null;
                toggleButton.OnInteractEnded();
            }
            yield return null;
        }
    }

}
