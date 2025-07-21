using UnityEngine;

public class TransitionScript : MonoBehaviour
{

    void EndOfAnimation()
    {
        GameManager.Instance.RestartGame();
    }

    void EndOfGame()
    {
        GameManager.Instance.EndGame();
    }
}
