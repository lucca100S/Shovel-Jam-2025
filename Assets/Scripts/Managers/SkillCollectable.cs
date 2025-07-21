using UnityEngine;

public class SkillCollectable : MonoBehaviour
{
    public Skills unlockableSkill;
    public string popUpText;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.UnlockSkill(unlockableSkill);
            GameManager.Instance.EnqueueMessage(popUpText);
            Destroy(gameObject);
        }
    }
}
