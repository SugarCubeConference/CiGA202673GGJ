using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        Debug.Log("Goal reached. Hook win flow here.", this);
    }
}
