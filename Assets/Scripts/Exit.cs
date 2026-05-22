using UnityEngine;
using UnityEngine.AI;

public class Exit : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Something entered Exit");
        if (other.CompareTag("Agent")) // Check if the collider belongs to an agent
        {
            Debug.Log("Agent entered Exit");
            //    Destroy(other.gameObject); // Destroy the agent game object
            other.gameObject.SetActive(false); // Deactivate the agent game object instead of destroying it
        }
    }

}
