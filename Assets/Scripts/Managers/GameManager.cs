using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    GameObject player;
    InputActionAsset inputAction;
    public Vector3 respawnPosition;

    public GameObject menuPanel;
    public Animator uiAnimator;

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

        player.GetComponent<PlayerController>().inputActions.FindActionMap("Player").Disable();
        player.GetComponent<PlayerController>().inputActions.FindActionMap("UI").Enable();
    }

    //private void Update()
    //{
    //    Debug.Log(player.GetComponent<PlayerController>().inputActions.FindActionMap("Player").enabled);
    //}

    public void RespawnPlayer()
    {
        player.transform.position = respawnPosition;
        player.GetComponent<PlayerController>().rb.linearVelocity = Vector3.zero;
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

    #endregion
}
