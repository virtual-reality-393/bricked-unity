using System;
using UnityEngine;


[RequireComponent(typeof(PlaneSpawner))]
public abstract class BaseGame : MonoBehaviour
{
    private PlaneSpawner _spawner;

    protected GameState GameState { get; set; }
    protected GameObject TablePlane { get; private set; }

    [SerializeField] protected ObjectDetector objectDetector;

    private void Awake()
    {
        _spawner = GetComponent<PlaneSpawner>();
        
        _spawner.OnPlaneSpawned += OnPlaneSpawned;
        GameState = GameState.Awake;
        
        
        objectDetector.OnObjectsDetected += OnBricksDetected;
    }

    protected virtual void OnPlaneSpawned(object sender, PlaneSpawnedEventArgs e)
    {
        TablePlane = e.Plane;
    }

    protected abstract void OnBricksDetected(object sender, ObjectDetectedEventArgs e);

    private void Update()
    {
        switch (GameState)
        {
            case GameState.Setup:
                Setup();
                break;
            case GameState.Game:
                Loop();
                break;
            default:
                break;
        }
    }

    protected virtual void Setup()
    {
        GameState = GameState.Setup;
    }
    protected abstract void Loop();
}


public enum GameState
{
    Awake,
    Setup,
    Game
}
