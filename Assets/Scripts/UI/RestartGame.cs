using UnityEngine;

public class RestartGame : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.StartTransition();
        }
    }
}
