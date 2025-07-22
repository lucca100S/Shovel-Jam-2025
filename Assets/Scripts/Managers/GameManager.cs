using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    GameObject player;
    public InputActionAsset inputActions;
    InputAction pauseAction;
    public Vector3 respawnPosition;

    public GameObject menuPanel;
    public GameObject finalPanel;
    public GameObject pausePanel;
    public Animator uiAnimator;

    public GameObject startButton;
    public GameObject continueButton;

    public TMP_Text messageText;
    public float timeBetweenLetters = 0.5f;
    public float timeBetweenSentences = 1;
    Queue<string> messageQueue = new Queue<string>();
    bool typing = false;

    public bool gamePaused = true;
    float letterTimer = 0;
    float sentenceTimer = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        player = GameObject.Find("Player");

        pauseAction = inputActions.FindAction("Pause");

        pauseAction.performed += PauseAction;
    }

    private void OnDisable()
    {
        pauseAction.performed -= PauseAction;
    }

    private void Update()
    {
        WriteMessage();
    }

    public void RespawnPlayer()
    {
        player.transform.position = respawnPosition;
        player.GetComponent<PlayerController>().rb.linearVelocity = Vector3.zero;
        player.GetComponent<PlayerController>().onGround = true;
    }

    public void UnlockSkill(Skills skill)
    {
        switch (skill)
        {
            case Skills.Hook:
                player.GetComponent<GrapplingHook>().grapplingHookEnabled = true;
                player.GetComponent<GrapplingHook>().gunTipMesh.SetActive(true);
                break;
            case Skills.WallJump:
                player.GetComponent<PlayerController>().unlockedWallJump = true;
                break;
        }
    }

    #region UI Management
    public void DisableMenuPanel()
    {
        gamePaused = false;
        menuPanel.gameObject.SetActive(false);
    }

    public void EnableMenuPanel()
    {
        gamePaused = true;
        messageText.SetText("");
        messageQueue.Clear();
        StopAllCoroutines();
        typing = false;
        menuPanel.gameObject.SetActive(true);

    }

    public void EnableFinalPanel()
    {
        gamePaused = true;
        messageText.SetText("");
        messageQueue.Clear();
        StopAllCoroutines();
        typing = false;
        finalPanel.gameObject.SetActive(true);
    }

    public void DisabelFinalPanel()
    {
        gamePaused = false;
        finalPanel.gameObject.SetActive(false);
    }

    public void PauseAction(InputAction.CallbackContext context)
    {
        ManagePause();
    }

    public void ManagePause()
    {
        if (!menuPanel.activeSelf)
        {
            if (pausePanel.activeSelf)
            {
                gamePaused = false;
                pausePanel.SetActive(false);
            }
            else
            {
                gamePaused = true;
                pausePanel.SetActive(true);
            }
        }
    }

    public void StartTransition()
    {
        uiAnimator.SetTrigger("transition");
    }

    public void RestartGame()
    {
        EnableMenuPanel();
        startButton.GetComponent<Animator>().SetBool("glitch", true);
        respawnPosition = new Vector3(0, 0, 0);
        RespawnPlayer();
    }

    public void StartEndGame()
    {
        uiAnimator.SetTrigger("endgame");
    }

    public void EndGame()
    {
        EnableFinalPanel();
        continueButton.GetComponent<Animator>().SetBool("glitch", true);
        respawnPosition = new Vector3(0, 0, 0);
        RespawnPlayer();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Quit in Unity Editor
#endif

        Application.Quit(); // Quit in build
    }
    #endregion

    #region Messages
    public void EnqueueMessage(string message)
    {
        messageQueue.Enqueue(message);
    }

    void WriteMessage()
    {
        if (messageQueue.Count != 0 && !typing)
        {
            string message = messageQueue.Dequeue();
            StartCoroutine(TypeMessage(message));
        }
    }

    IEnumerator TypeMessage(string message)
    {
        typing = true;
        messageText.SetText("");

        foreach (char letter in message)
        {
            letterTimer = 0;
            string currentText = messageText.text;
            messageText.SetText(currentText + letter);

            yield return new WaitUntil(CanDisplayNextLetter);
        }

        yield return new WaitUntil(() =>
        {
            if (!gamePaused)
                sentenceTimer += Time.deltaTime;
            return !gamePaused && sentenceTimer > timeBetweenSentences;
        });
        sentenceTimer = 0;
        messageText.SetText("");
        typing = false;
    }

    private bool CanDisplayNextLetter()
    {
        letterTimer += Time.deltaTime;
        return !gamePaused && letterTimer > timeBetweenLetters;
    }
    #endregion
}
