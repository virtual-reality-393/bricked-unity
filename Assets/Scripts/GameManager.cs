using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public GameObject GetBrick(string color)
    {
        switch (color)
        {
            case "red":
                return redBrickPrefab;
            case "blue":
                return blueBrickPrefab;
            case "green":
                return greenBrickPrefab;
            case "yellow":
                return yellowBrickPrefab;
            default:
                throw new ArgumentException("Invalid color brick");
        }
    }

    public GameObject cubePrefab; 
    public GameObject stackPrefab;
    public GameObject cylinderPrefab;

    [SerializeField]
    private GameObject blueBrickPrefab;
    
    [SerializeField]
    private GameObject redBrickPrefab;
    
    [SerializeField]
    private GameObject greenBrickPrefab;
    
    [SerializeField]
    private GameObject yellowBrickPrefab;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

}
