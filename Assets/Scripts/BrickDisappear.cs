using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BrickDisappear : MonoBehaviour
{
    public static void DisappearStacks(List<List<GameObject>> stacks)
    {
        foreach (var stack in stacks)
        {
            if (stack.Count > 0)
            {
                var circle = Instantiate(GameManager.Instance.cylinderPrefab, stack[0].transform.position, Quaternion.identity);
                circle.GetComponent<Renderer>().material.color = Color.black;
                var circleScale = circle.transform.localScale;
                circle.transform.localScale = new Vector3(0,0,0);
                circle.transform.position -= Vector3.up * 0.05f;
                float circleHeight = circle.transform.position.y;
                float timeTaken = (0.0208f * (stack.Count - 1) + 0.4f)*3;
                circle.transform.DOScale(circleScale*1.6f, 1f).SetEase(Ease.OutCubic).Play().onComplete = () =>
                {
                    float maxTime = 0;
                    for (var i = 0; i < stack.Count; i++)
                    {
                        var brick = stack[i];
                        var renderers = brick.GetComponentsInChildren<Renderer>(true);

                        foreach (var renderer in renderers)
                        {
                            renderer.material.SetFloat("_YCutoff",circleHeight);
                        }

                        GameManager.Instance.StartCoroutine(FallingBrickEffect(brick,
                            brick.transform.position,
                            brick.transform.position - (0.0208f * (stack.Count - 1) + 0.4f) * Vector3.up,
                            circleHeight,
                            -0.25f * i,
                            i < stack.Count - 1,
                            i > 0));
                    }
                    
                    circle.transform.DOScale(Vector3.zero, 1f).SetEase(Ease.InCubic).SetDelay(0.25f * (stack.Count)+0.2f+timeTaken).Play().onComplete = () => Destroy(circle,0.2f);

                    
                };
            }
        }
    }

    public static IEnumerator FallingBrickEffect(GameObject brick, Vector3 startPos, Vector3 endPos,
        float yCutoff,
        float t = 0f, bool disableTop = false, bool disableBottom = false)
    {
        var spawned = false;
        var timeToComplete = Vector3.Distance(startPos, endPos) * 3;

        while (t < 1f)
        {
            if (t < 0)
            {
                t += Time.deltaTime / 0.6f;
            }
            if (t >= 0)
            {
                t += Time.deltaTime / timeToComplete;
                if (!spawned)
                {
                    if (disableBottom)
                    {
                        brick.GetComponent<Brick>().bottom.SetActive(true);
                    }

                    if (disableTop)
                    {
                        brick.GetComponent<Brick>().top.SetActive(true);
                    }

                    spawned = true;
                }


                brick.transform.position = Vector3.Lerp(startPos, endPos, Mathf.Clamp(t, 0f, 1f));
            }

            if (brick.transform.position.y < yCutoff - 0.02f)
            {
                // INSERT BRICK DONE EVENT HERE
            }

            yield return null;
        }
        
        Destroy(brick,0.3f);
    }
}
