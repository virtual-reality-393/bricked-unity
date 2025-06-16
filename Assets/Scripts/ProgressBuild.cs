using UnityEngine;

public class ProgressBuild : MonoBehaviour
{

    int progress = 0; // Progress value, can be set in the Inspector or modified at runtime

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (Transform t in transform)
        {
            t.gameObject.SetActive(false); // Deactivate all child objects initially
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // Method to increment progress by one
    public void IncrementProgress()
    {
        if (progress >= transform.childCount) // Check if progress exceeds the number of child objects
        {
            Debug.LogWarning("Progress exceeds the number of child objects. Resetting progress.");
            //ResetProgress(); // Reset progress if it exceeds the number of children
            return;
        }
        GameObject brick = transform.GetChild(progress).gameObject; 
        brick.SetActive(true); // Activate the current child object
        Vector3 position = brick.transform.position; // Get the position of the current child object
        GameManager.Instance.StartCoroutine(GameUtils.FallingBrickEffect(brick, position + Vector3.up * 0.12f, position, 0.8f)); // Start the falling effect coroutine
        progress++;
    }

    // Method to reset progress to zero
    public void ResetProgress()
    {
        progress = 0; // Reset progress
        foreach (Transform t in transform)
        {
            t.gameObject.SetActive(false); // Deactivate all child objects
        }
    }
}
