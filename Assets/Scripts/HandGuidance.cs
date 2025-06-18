using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class HandGuidance : MonoBehaviour
{
    public GameObject handPrefab;
    public GameObject targetHand;
    public DetectedObjectManager detectedObjectManager;
    public PlaceStackGame game;

    private bool _animated;
    public float movementSpeed;

    private GameObject _guidingHand;
    private Vector3 redBrickPosition;
    private Vector3 finalGoal;
    private int id;
    private bool finished;
    private GuidanceState _state;
    void Start()
    {
        _guidingHand = Instantiate(handPrefab,targetHand.transform.position,targetHand.transform.rotation);
        _guidingHand.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (finished) return;
        Debug.LogWarning(_state);
        if (detectedObjectManager.LifeTimeObjects["red"].Count > 0)
        {
            redBrickPosition = detectedObjectManager.LifeTimeObjects["red"][0].obj.transform.position;
        }

        if (_state != GuidanceState.Setup && game.complted[id])
        {
            finished = true;
        }

        switch (_state)
        {
            case GuidanceState.Setup:
                if (detectedObjectManager.LifeTimeObjects["red"].Count > 0)
                {
                    
                    for (var i = 0; i < game.stacksToBuild.Count; i++)
                    {
                        var stack = game.stacksToBuild[i];
                        if (stack.Contains("red"))
                        {
                            finalGoal = game.spawnPoints[i].position;
                            _state = GuidanceState.MoveToBrick;
                            id = i;
                            break;
                        }
                    }
                }
                
                break;
            
            case GuidanceState.MoveToBrick:
                StartCoroutine(MoveHand(targetHand.transform.position,redBrickPosition));
                Debug.LogWarning(Vector3.Distance(targetHand.transform.GetChild(0).position, redBrickPosition));
                if (Vector3.Distance(targetHand.transform.GetChild(0).position, redBrickPosition) < 0.1f)
                    _state = GuidanceState.MoveToEnd;
                break;
            
            case GuidanceState.GrabBrick:
                
                
                break;
            
            case GuidanceState.MoveToEnd:
                StartCoroutine(MoveHand(redBrickPosition,finalGoal));
                Debug.LogWarning(Vector3.Distance(targetHand.transform.GetChild(0).position, finalGoal));
                if (Vector3.Distance(targetHand.transform.GetChild(0).position, finalGoal) < 0.1f)
                    _state = GuidanceState.GrabBrick;
                
                break;
        }

    }

    private IEnumerator MoveHand(Vector3 startPos, Vector3 endPos)
    {
        if (!_animated)
        {
            startPos = startPos - Vector3.up * 0.05f;
            _animated = true;
            _guidingHand.SetActive(true);
            var renderer = _guidingHand.GetComponentInChildren<Renderer>();
            float startAlpha = 0;
            _guidingHand.transform.rotation = targetHand.transform.rotation;
            Color color =  renderer.material.color;

            var timeToMove = Vector3.Distance(endPos, startPos) / movementSpeed + 0.5f;
        
            float t = 0;
            while (t <= 1)
            {
                if (t < 0.6)
                {
                    color.a = t/2;
                    renderer.material.color = color;
                }
                t+= Time.deltaTime/timeToMove;
            
                _guidingHand.transform.position = Vector3.Lerp(startPos,endPos,t);
                yield return null;
            }


            yield return new WaitForSeconds(0.5f);
            _guidingHand.SetActive(false);
            yield return new WaitForSeconds(1.5f);
        
            _animated = false;
        }
    }
    
}

public enum GuidanceState
{
    Setup,
    MoveToBrick,
    GrabBrick,
    MoveToEnd,
}
