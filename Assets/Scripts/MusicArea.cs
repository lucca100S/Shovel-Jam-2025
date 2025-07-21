using UnityEngine;

public class MusicArea : MonoBehaviour
{
    [SerializeField] private int index;
    int oldIndex = -1;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            oldIndex = MusicManager.CurrentIndex;
            MusicManager.Play(index);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MusicManager.Play(oldIndex);
            oldIndex = -1;
        }
    }
}
