using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    GameObject player;
    InputActionAsset inputAction;
    public Vector3 respawnPosition;

    public GameObject menuPanel;
    public GameObject finalPanel;
    public Animator uiAnimator;

    public TMP_Text messageText;
    public float timeBetweenLetters = 0.5f;
    public float timeBetweenSentences = 1;
    Queue<string> messageQueue = new Queue<string>();
    bool typing = false;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.Find("Player");

        //player.GetComponent<PlayerController>().inputActions.FindActionMap("Player").Disable();
        //player.GetComponent<PlayerController>().inputActions.FindActionMap("UI").Enable();
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
        menuPanel.gameObject.SetActive(false);
        player.GetComponent<PlayerController>().inputActions.FindActionMap("Player").Enable();
        player.GetComponent<PlayerController>().inputActions.FindActionMap("UI").Enable();
    }

    public void EnableMenuPanel()
    {
        menuPanel.gameObject.SetActive(true);
        player.GetComponent<PlayerController>().inputActions.FindActionMap("Player").Disable();
        player.GetComponent<PlayerController>().inputActions.FindActionMap("UI").Enable();
    }

    public void EnableFinalPanel()
    {
        finalPanel.gameObject.SetActive(true);
        player.GetComponent<PlayerController>().inputActions.FindActionMap("Player").Disable();
        player.GetComponent<PlayerController>().inputActions.FindActionMap("UI").Enable();
    }

    public void DisabelFinalPanel()
    {
        finalPanel.gameObject.SetActive(false);
        player.GetComponent<PlayerController>().inputActions.FindActionMap("Player").Enable();
        player.GetComponent<PlayerController>().inputActions.FindActionMap("UI").Enable();
    }

    public void StartTransition()
    {
        uiAnimator.SetTrigger("transition");
    }

    public void RestartGame()
    {
        EnableMenuPanel();
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
        respawnPosition = new Vector3(0, 0, 0);
        RespawnPlayer();
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

        int i = 0;
        foreach (char letter in message)
        {
            string currentText = messageText.text;
            messageText.SetText(currentText + letter);

            yield return new WaitForSecondsRealtime(timeBetweenLetters);
        }

        yield return new WaitForSecondsRealtime(timeBetweenSentences);
        messageText.SetText("");
        typing = false;
    }
    #endregion
}
