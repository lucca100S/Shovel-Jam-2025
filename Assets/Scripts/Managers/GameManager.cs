using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    GameObject player;
    public Vector3 respawnPosition;

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
    }

    public void RespawnPlayer()
    {
        player.transform.position = respawnPosition;
        player.GetComponent<PlayerController>().rb.linearVelocity = Vector3.zero;
    }
}
