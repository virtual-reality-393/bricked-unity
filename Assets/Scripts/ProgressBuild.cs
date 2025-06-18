using UnityEngine;

public class ProgressBuild : MonoBehaviour
{
    public int minProgressValue = 3;
    public int maxProgressValue = 5;

    int progress = 0; // Progress value, can be set in the Inspector or modified at runtime

    bool isBuildComplete = false; // Flag to check if the build is complete

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
        Vector3 position = brick.transform.position; // Get the position of the current child object
        GameManager.Instance.StartCoroutine(GameUtils.BuildBrickEffect(brick, position + Vector3.up * 0.2f, position, 0.8f)); // Start the falling effect coroutine
        progress++;
    }

    public void IncrementRandom()
    {
        int randomProgress = Random.Range(minProgressValue, maxProgressValue + 1); // Generate a random progress value between v1 and v2
        for (int i = 0; i < randomProgress; i++)
        {
            if (progress >= transform.childCount) // Check if progress exceeds the number of child objects
            {
                Debug.LogWarning("Progress exceeds the number of child objects. Resetting progress.");
                //ResetProgress(); // Reset progress if it exceeds the number of children
                return;
            }
            GameObject brick = transform.GetChild(progress).gameObject;
            Vector3 position = brick.transform.position; // Get the position of the current child object
            GameManager.Instance.StartCoroutine(GameUtils.BuildBrickEffect(brick, position + Vector3.up * 0.2f, position, 0.8f, -0.25f * i, () =>
            {
                if (progress >= transform.childCount-1) // Check if progress exceeds the number of child objects
                {
                    isBuildComplete = true; // Set build complete flag to true
                    Debug.Log("Build complete!");
                }
            })); // Start the falling effect coroutine
            progress++;
        }
    }

    public void ComplteBuild()
    {
        progress = transform.childCount; // Reset progress
        foreach (Transform t in transform)
        {
            t.gameObject.SetActive(true); // Deactivate all child objects
        }
        transform.GetChild(progress-4).gameObject.SetActive(false); // Deactivate the last 4 child objects
        transform.GetChild(progress-1).gameObject.SetActive(false); // Deactivate the last 4 child objects
        transform.GetChild(progress-2).gameObject.SetActive(false); // Deactivate the last 4 child objects
        transform.GetChild(progress-3).gameObject.SetActive(false); // Deactivate the last 4 child objects

        progress -= 4;
    }

    public bool IsBuildComplete()
    {
        return isBuildComplete; // Check if progress has reached the total number of child objects
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
