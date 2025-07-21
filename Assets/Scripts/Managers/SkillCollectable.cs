using UnityEngine;

public class SkillCollectable : MonoBehaviour
{
    public Skills unlockableSkill;
    string popUpText;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.UnlockSkill(unlockableSkill);
            Destroy(gameObject);
        }
    }
}
