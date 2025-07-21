using UnityEngine;

public class TransitionScript : MonoBehaviour
{
   void EndOfAnimation()
    {
        GameManager.Instance.RestartGame();
    }
}
