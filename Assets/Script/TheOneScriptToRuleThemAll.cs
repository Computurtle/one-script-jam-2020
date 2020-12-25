using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TheOneScriptToRuleThemAll : MonoBehaviour
{
    public static TheOneScriptToRuleThemAll Instance; // variable that holds the instance for the singleton setup

    [Header("Score (Supporters)")]
    public Image debateBarImage;
    public Image roundTimerImage;
    public int currentScore;
    public int targetScore = 1000;
    public TextMeshProUGUI currentScoreText;
    public TextMeshProUGUI targetScoreText;
    public TextMeshProUGUI scoreAddText;

    [Header("Menu Items")]
    public GameObject howToScreen;
    public GameObject mainMenuButtons;

    [Header("Health")]
    public float currentHealth = 50;
    public float maxHealth = 50;
    public float healthDrain;
    public Animator[] endGameAnims;

    [Header("Difficulty")]
    public bool isHardDifficulty;
    public float difficultyMultiplier = 1f;
    public float difficultyIncrease = 0.3f;
    public float maxDifficulty;


    [Header("Round Delay")]
    public Vector2 roundDelay;
    public float roundDelayTimer;

    [Header("Minigames")]
    public MultipleChoice multiChoiceGame;
    public Hitman hitmanGame;
    public BottleThrow bottleThrow;
    public MathProblem mathProblem;
    public List<Minigame> minigames = new List<Minigame>();
    public int currentMinigame;

    [Header("Crowd")]
    public CrowdStatus crowdStatus = CrowdStatus.waiting;
    public GameObject[] crowd;
    public enum CrowdStatus { answer, question, waiting} //answer. waiting for player to make choice, quesiton one of their hands up. waiting for person to put hand up

    [Header("SFX")]
    public AudioSource crowdClap;
    public AudioSource bottleBreak;


    void Awake()
    {
        // Run singleton check/setup
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Add minigame classes to games
        minigames.Add(multiChoiceGame);
        minigames.Add(hitmanGame);
        minigames.Add(bottleThrow);
        minigames.Add(mathProblem);
        UpdateScoreUI();
        // Initialise each minigame (assign game master)
        foreach(Minigame minigame in minigames)
        {
            minigame.Initialise(this);
        }

        // Setup difficultyMultiplier value based on difficulty
        if (isHardDifficulty) {
            difficultyMultiplier = 2f;
            roundDelay = new Vector2(roundDelay.x / difficultyMultiplier, roundDelay.y / difficultyMultiplier);
        }

        // Setup crowd members question listener
        crowd = GameObject.FindGameObjectsWithTag("Crowd");
        foreach (GameObject crowdMember in crowd)
        {
            crowdMember.transform.Find("Canvas").Find("QuestionPrompt").gameObject.SetActive(false);
            crowdMember.transform.Find("Canvas").GetComponent<Canvas>().worldCamera = Camera.main;
            crowdMember.GetComponent<Animator>().Play("CrowdIdle", -1, Random.Range(0f, 1f));
            crowdMember.transform.Find("Canvas").Find("QuestionPrompt").GetComponent<Button>().onClick.AddListener(CrowdClicked);
        }
        UpdateScoreUI();
    }

    public void UpdateScoreUI()
    {

        currentScoreText.SetText(currentScore.ToString() + " Supporters");
        int val = -(currentScore - supportersTarget);
        if (currentScore >= targetScore)
        {
            EndGame(0);
        }
        if (val < 0)
        {
            scoreAddText.color = Color.red;
            scoreAddText.enabled = true;
        }
        else if (val > 0)
        {

            scoreAddText.color = Color.green;
            scoreAddText.enabled = true;
        }
        else
        {
            scoreAddText.enabled = false;
        }
        scoreAddText.SetText((val).ToString());
    }

    int newMinigame = -1;

    public void StartNewMinigame()
    {
        float totalMinigameChance = 0;
        foreach(Minigame minigame in minigames)
        {
            totalMinigameChance += minigame.chanceOfHappening;
        }
        float newMinigameRandom = Random.Range(0f, totalMinigameChance);
        totalMinigameChance = 0;
        for (int i = 0; i < minigames.Count; i++)
        {
            if (newMinigameRandom > totalMinigameChance)
                newMinigame = i;
            totalMinigameChance += minigames[i].chanceOfHappening;
        }
        currentMinigame = newMinigame;
        minigames[currentMinigame].GenerateGame();
        crowdStatus = TheOneScriptToRuleThemAll.CrowdStatus.question;
        roundDelayTimer = Random.Range(roundDelay.x, roundDelay.y) / difficultyMultiplier;
        difficultyMultiplier += difficultyIncrease;
        if (maxDifficulty <= difficultyMultiplier)
            difficultyMultiplier = maxDifficulty;
    }

    public void CrowdClicked()
    {

        minigames[currentMinigame].GenerateNewQuestion();
        crowdStatus = CrowdStatus.answer;
    }

    private void Update()
    {
        if (crowdStatus == CrowdStatus.waiting)
        {
            roundDelayTimer -= Time.deltaTime;
            if (roundDelayTimer <= 0)
                StartNewMinigame();
        }
        else
        {
            minigames[currentMinigame].Update();
        }


        TakeHealth((healthDrain * difficultyMultiplier) * Time.deltaTime);
        debateBarImage.fillAmount = currentHealth / maxHealth;
        if (supporterTimer <= 1)
        {
            supporterTimer += Time.deltaTime;
            currentScore = Mathf.RoundToInt(Mathf.Lerp(startingSupporters, supportersTarget, supporterTimer));
            UpdateScoreUI();
        }
        else
        {
            currentScore = supportersTarget;
            UpdateScoreUI();
        }

    }

    public void TakeHealth(float amount)
    {
        currentHealth -= amount;
        if(amount > 1)
            Debug.Log("Take health: " + amount);
        if (currentHealth <= 0)
        {
            EndGame(1);
        }
    }

    public void AddHealth(float amount)
    {
        currentHealth += amount;
        Debug.Log("Add health: " + amount);
        if (currentHealth >= 100)
        {
            currentHealth = 100;
        }
    }

    float supporterTimer;
    int startingSupporters;
    int supportersTarget;
    public void ChangeSupporters(int amount)
    {
        supporterTimer = 0;
        
            
        int tempScore = currentScore;
        startingSupporters = currentScore;
        if (amount > 0)
        {
            amount = Mathf.Abs(amount);
            tempScore += Mathf.RoundToInt(amount + (amount * (currentHealth / maxHealth)));
        }
        else
        {
            amount = Mathf.Abs(amount);
            tempScore -= Mathf.RoundToInt((amount + (amount * (-(currentHealth / maxHealth) +0.5f))));
        }
        Debug.Log("suppporters: " + amount + "\t" + "new value: " + (tempScore - currentScore).ToString());
        if (tempScore <= 0)
            tempScore = 0;
        supportersTarget = tempScore;
        

       
        UpdateScoreUI();
    }
    public void EndCurrentGame()
    {
        crowdStatus = CrowdStatus.waiting;
    }

    public void PlayCrowdAnimation(string animation, float amount)
    {
        crowdClap.Play();
        foreach (GameObject crowdMember in crowd)
        {

            if (Random.Range(0, 1f) <= amount)
            {
                crowdMember.GetComponent<Animator>().Play("CrowdCheer", -1, Random.Range(0f, 0.6f));
            }

        }
    }

    [ContextMenu("Lose")]
    public void Lose()
    {
        endGameAnims[1].SetBool("showScreen", true);
    }
    [ContextMenu("Win")]
    public void Win()
    {
        endGameAnims[0].SetBool("showScreen", true);
    }

    public void EndGame(int endGameScreen)
    {
        endGameAnims[endGameScreen].SetBool("showScreen", true);
    }


    // Multichoice - Function to be called when player selects an option
    public void SubmitData(int data)
    {
        minigames[currentMinigame].EnterData(data);
    }
    #region Menu Functions


    public void Replay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    // Play button that will lead to character/difficulty select
    public void Play() {
        SceneManager.LoadSceneAsync("TestScene");
    }
    // Player selects white president, difficulty is set to easy (default values)
    public void EasyDifficulty() {
        //TODO: Have a character select screen after hitting play, if white president is chosen, run this to start the game normally.
    }

    // Player selects black president, difficulty is set to hard (multiplier set in inspector)
    public void HardDifficulty() {
        //TODO: Have a character select screen after hitting play, if black president is chosen, run this to start the game with the difficultyMultiplier modified.
    }

    // Options button that leads to the options menu
    public void Options() {
        //TODO: Options aren't extremely necessary for this game, can maybe add to later.
    }

    // HowToPlay button that leads to the how to play informational screen
    public void HowToPlay() {
        mainMenuButtons.SetActive(false);
        howToScreen.SetActive(true);
    }

    // Back button that leads back to the main menu
    public void Back() {
        mainMenuButtons.SetActive(true);
        howToScreen.SetActive(false);
    }

    // Exit button that will exit the game (shut down process)
    public void Exit() {
        Application.Quit();
    }

    public void BottleSmash()
    {
        bottleBreak.Play();
    }
    public AudioSource sniperShot;
    public void SniperShot()
    {
        sniperShot.Play();
    }
    #endregion
}

[System.Serializable]
public class Minigame
{
    //Base class for all minigames
   protected TheOneScriptToRuleThemAll gameManager;
    protected float currentTime;
    public float allowedTime;
    float setAllowedTime;
    public bool started;
    public GameObject[] objectsUsed; //The ui objects to enable/disable on question start complete
    public float failDamage;
    public float chanceOfHappening = 1;
    public int failSupporterAmount = 20, passSupporterAmount = 20;


    public virtual void Initialise(TheOneScriptToRuleThemAll newManager)
    {
        setAllowedTime = allowedTime;
        currentTime = allowedTime;
        gameManager = newManager;
    }

    //Called once per frame
    public virtual void Update()
    {
        
        if (currentTime <= 0)
            FailGame();

        currentTime -= Time.deltaTime;
        gameManager.roundTimerImage.fillAmount = currentTime / allowedTime;
    }

    //Called when the game should be created and displayed
    public virtual  void GenerateGame()
    {
        currentTime = setAllowedTime / gameManager.difficultyMultiplier;
        allowedTime = setAllowedTime/ gameManager.difficultyMultiplier;
    }
    //Removes any leftover gameobjects on the screen
    public virtual void RemoveGame()
    {
        foreach (GameObject obj in objectsUsed)
        {
            obj.SetActive(false);
        }
    }

    public virtual void GenerateNewQuestion()
    {

    }

    public virtual void AudioCue()
    {

    }

    //Different functions for different inputs for when the game is completed.
    public virtual void CompleteGame()
    {
        gameManager.roundTimerImage.fillAmount = 0f;
    }
    public virtual void CompleteGame(int num)
    {

    }
    //Called usually when the timer is run out.
    public virtual void FailGame()
    {
        gameManager.roundTimerImage.fillAmount = 0f;
        gameManager.TakeHealth(failDamage);
        gameManager.EndCurrentGame();
        RemoveGame();
        gameManager.ChangeSupporters(-failSupporterAmount);
    }
    public virtual void EnterData(int data)
    {

    }
}

[System.Serializable]
public class MultipleChoice : Minigame
{
    public float baseHealthIncrease = 10; //Health increase
    public float unanswerDamage = 10; //Amount of health lost when a question isnt answered
    public AnimationCurve healthReturnCurve;
    public float incorrectDamageMultiplier = 10;
    public Text questionText;
    public MultiChoice[] multiChoiceQuestions;
    int previousQuestion = -1;
    int correctButton;
    public int[] buttonOrder = new int[4];
    public Button[] multiChoiceButtons;
    public int activeCrowdMember;

    public override void GenerateGame()
    {
        base.GenerateGame();
        activeCrowdMember = 0;
        activeCrowdMember = Random.Range(0, gameManager.crowd.Length);
        gameManager.crowd[activeCrowdMember].transform.Find("Canvas").Find("QuestionPrompt").gameObject.SetActive(true);
    }

    // Multichoice - Function to be called when the player clicks a QuestionPrompt from the crowd
    public override void GenerateNewQuestion()
    {
        foreach (GameObject obj in objectsUsed)
        {
            obj.SetActive(true);
        }
        gameManager.crowd[activeCrowdMember].transform.Find("Canvas").Find("QuestionPrompt").gameObject.SetActive(false);

        //Make sure it isnt the same quesiton as before
        int randomMutliQuestion = previousQuestion;
        randomMutliQuestion = Random.Range(0, multiChoiceQuestions.Length);
        while (randomMutliQuestion == previousQuestion)
        {
            randomMutliQuestion = Random.Range(0, multiChoiceQuestions.Length);
        }
        //Set question text
        previousQuestion = randomMutliQuestion;
        questionText.text = multiChoiceQuestions[previousQuestion].question;
        int randomAnswerButton = Random.Range(0, 4);
        List<int> falseQuestionsLeft = new List<int> { 1, 2, 3 };
        //Needs to be fixed up to just use the value method and not 1 correct answer
        for (int i = 0; i < multiChoiceButtons.Length; i++)
        {
            if (i == randomAnswerButton)
            {
                multiChoiceButtons[i].GetComponentInChildren<Text>().text = multiChoiceQuestions[previousQuestion].answers[0];
                correctButton = i;
                buttonOrder[i] = 0;
            }
            else
            {
                //Randomly order answers
                int randomIndex = falseQuestionsLeft[Random.Range(0, falseQuestionsLeft.Count)];

                multiChoiceButtons[i].GetComponentInChildren<Text>().text = multiChoiceQuestions[previousQuestion].answers[randomIndex];
                falseQuestionsLeft.Remove(randomIndex);
                buttonOrder[i] = randomIndex;

            }

        }
    }

    public override void EnterData(int data)
    {

        float answerValue = multiChoiceQuestions[previousQuestion].value[buttonOrder[data]];
        Debug.Log(answerValue);
        if (answerValue < 0)
            gameManager.TakeHealth(-answerValue * incorrectDamageMultiplier);
        else
            gameManager.AddHealth(answerValue * baseHealthIncrease * healthReturnCurve.Evaluate(-(currentTime / allowedTime - 1)));

        if (answerValue > 0)
        {
            gameManager.PlayCrowdAnimation("CrowdCheer", answerValue);
        }
        else
        {
            //gameManager.PlayCrowdAnimation("CrowdBoo", answerValue);
        }
        gameManager.ChangeSupporters(Mathf.RoundToInt((answerValue >= 0) ? (passSupporterAmount * answerValue) : (failSupporterAmount * answerValue)));
        CompleteGame();
        gameManager.EndCurrentGame();
        RemoveGame();

    }
    public override void FailGame()
    {
        base.FailGame();
        gameManager.crowd[activeCrowdMember].transform.Find("Canvas").Find("QuestionPrompt").gameObject.SetActive(false);
    }
}
// Multichoice class for holding question and answers data
[System.Serializable]
public class MultiChoice
{
    public string question;
    [Tooltip("First answer is correct. Randomly ordered")]
    public string[] answers =  new string[4];
    [Tooltip("Multiplier of score of choosing the question")]
    [Range(-1f, 1f)]
    public float[] value = new float[4];
}

[System.Serializable]
public class Hitman : Minigame
{
    public GameObject reticle;
    public GameObject president;
    public GameObject DuckImage;
    public AnimationClip sniperAnim;
    public float healthReturnMult;

    public override void GenerateGame()
    {
        base.GenerateGame();
        Debug.Log("hitman Game");
        reticle.GetComponent<Animator>().Play("Base Layer.SniperSway");

        reticle.GetComponent<Animator>().speed = sniperAnim.length / allowedTime;
        gameManager.Invoke("SniperShot", allowedTime);
        foreach (GameObject obj in objectsUsed)
        {
            obj.SetActive(true);
        }
        DuckImage.SetActive(true);
    }

    public override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(KeyCode.S))
        {
            DuckImage.SetActive(false);
            president.GetComponent<Animator>().Play("Base Layer.Duck", -1);
            gameManager.ChangeSupporters(passSupporterAmount);
            base.CompleteGame();
            gameManager.EndCurrentGame();
            gameManager.AddHealth(healthReturnMult);
            RemoveGame();
        }
        if (currentTime < 0.25f)
        {
            DuckImage.SetActive(false);
        }
    }
}

[System.Serializable]
public class BottleThrow : Minigame
{
    public GameObject bottle;
    public GameObject president;
    public GameObject DuckImage;
    public AnimationClip throwAnim;
    public float healthReturnMult;

    public override void GenerateGame()
    {
        base.GenerateGame();
        Debug.Log("bottle Game");
        bottle.GetComponent<Animator>().Play("Base Layer.BottleThrow", -1);
        bottle.GetComponent<Animator>().speed = throwAnim.length / allowedTime;
        gameManager.Invoke("BottleSmash", allowedTime);
        DuckImage.SetActive(true);
    }

    public override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(KeyCode.S))
        {
            DuckImage.SetActive(false);
            president.GetComponent<Animator>().Play("Base Layer.Duck", -1);
            base.CompleteGame();
            gameManager.ChangeSupporters(passSupporterAmount);
            gameManager.AddHealth(healthReturnMult);
            gameManager.EndCurrentGame();
            RemoveGame();
        }
        if (currentTime < 0.25f)
        {
            DuckImage.SetActive(false);
        }
    }

}

[System.Serializable]
public class MathProblem : Minigame
{
    public Text displayNumberText;
    public Text questionText;
    [Tooltip("Use # to insert problem into")]
    public string questionString = "Use # to insert problem into";
    public int maxQuestionSize = 30;
    public AnimationCurve healthReturn;
    public float healthReturnMultiplayer = 10;
    int answer;
    string inputCode;
    int activeCrowdMember = 0;
    public override void GenerateGame()
    {
        base.GenerateGame();
        activeCrowdMember = 0;
        activeCrowdMember = Random.Range(0, gameManager.crowd.Length);
        gameManager.crowd[activeCrowdMember].transform.Find("Canvas").Find("QuestionPrompt").gameObject.SetActive(true);
    }

    public override void GenerateNewQuestion()
    {
        gameManager.crowd[activeCrowdMember].transform.Find("Canvas").Find("QuestionPrompt").gameObject.SetActive(false);
        foreach (GameObject obj in objectsUsed)
        {
            obj.SetActive(true);
        }
        int int1 = Random.Range(1, 30);
        int int2 = Random.Range(1, 30);
        string question = questionString;
        int index = question.IndexOf('#');
        question = question.Remove(index, 1);
        if (int1 > int2)
        {
            question = question.Insert(index, int1.ToString() + " + " + int2.ToString());
            answer = int1 + int2;
        }
        else
        {
            question = question.Insert(index, int2.ToString() + " - " + int1.ToString());
            answer = int2 - int1;
        }
        inputCode = "";
        questionText.text = question;
        UpdateAnswer();
    }

    public override void Update()
    {
        base.Update();
        #region Inputs
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            inputCode += "0";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            inputCode += "1";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            inputCode += "2";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            inputCode += "3";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            inputCode += "4";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            inputCode += "5";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            inputCode += "6";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            inputCode += "7";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            inputCode += "8";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            inputCode += "9";
        }
        else if(Input.GetKeyDown(KeyCode.Backspace))
        {
            if (inputCode.Length != 0)
                inputCode = inputCode.Remove(inputCode.Length - 1);
        }
        else if(Input.GetKeyDown(KeyCode.Return))
        {
            CheckCode();
        }
        #endregion
        UpdateAnswer();
    }
    public override void EnterData(int data)
    {
        if(data <= 9)
        {
            inputCode += data.ToString();
        }
        else if(data == 11)
        {
            inputCode= "";
            //Clear
        }
        else if(data == 12)
        {
            CheckCode();
            //Submit
        }
        UpdateAnswer();
    }
    public override void CompleteGame()
    {
        gameManager.crowd[activeCrowdMember].transform.Find("Canvas").Find("QuestionPrompt").gameObject.SetActive(false);
        base.CompleteGame();
        gameManager.EndCurrentGame();
        RemoveGame();
        gameManager.AddHealth(healthReturnMultiplayer * healthReturn.Evaluate((currentTime/allowedTime)));
        gameManager.PlayCrowdAnimation("CrowdCheer", 0.7f);
    }

    public override void FailGame()
    {
        gameManager.crowd[activeCrowdMember].transform.Find("Canvas").Find("QuestionPrompt").gameObject.SetActive(false);
        base.FailGame();
        gameManager.EndCurrentGame();
        RemoveGame();
        //gameManager.PlayCrowdAnimation("CrowdBoo", 0.7f);
    }

    void CheckCode()
    {
        if (inputCode == answer.ToString())
        {
            CompleteGame();
            gameManager.ChangeSupporters(passSupporterAmount);
        }
        else
        {
            FailGame();
        }
    }

    void UpdateAnswer()
    {
        displayNumberText.text = inputCode;
    }
}