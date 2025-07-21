using UnityEngine;

public class UpdateSpawnpoint : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.respawnPosition = transform.position;
        }
    }
}
