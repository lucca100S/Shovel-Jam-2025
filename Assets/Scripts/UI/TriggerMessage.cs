using UnityEngine;

public class TriggerMessage : MonoBehaviour
{
    public string text;
    public int nbOfTriggers = 3;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && nbOfTriggers > 0)
        {
            GameManager.Instance.EnqueueMessage(text);
            nbOfTriggers--;
        }
    }
}
