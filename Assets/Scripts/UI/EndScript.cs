using UnityEngine;

public class EndScript : MonoBehaviour
{
   public void EndOfGame()
   {
        GameManager.Instance.EndGame();
   }
}
