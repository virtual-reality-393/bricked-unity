using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class StackGenerator : MonoBehaviour
{

    public static List<List<string>> GenerateStacks(Dictionary<string, int> brickFrequency, int minSlice = 1,
        int maxSlice = 1, SliceMethod sliceMethod = SliceMethod.Max)
    {
        var input = new List<string>();

        foreach (var v in brickFrequency)
        {
            for (int i = 0; i < v.Value; i++)
            {
                input.Add(v.Key);
            }
        }

        return GenerateStacks(input, minSlice, maxSlice, sliceMethod);
    }
    
    public static List<List<string>> GenerateStacks(List<string> bricks, int minSlice = 1, int maxSlice = 2, SliceMethod sliceMethod=SliceMethod.Random)
    {

        if (minSlice < 1 || maxSlice < 1)
        {
            throw new ArgumentException("Slices must be positive");
        }
        var validStack = GenerateValidStack(bricks);
        var res =  new List<List<string>>();
        
        switch (sliceMethod)
        {
            case SliceMethod.Max:
                return SliceMax(validStack, minSlice, maxSlice);
            case SliceMethod.Min:
                return SliceMin(validStack, minSlice, maxSlice);
            case SliceMethod.Random:
                return SliceRandomSize(validStack, minSlice, maxSlice);
            case SliceMethod.Equalize:
                return SliceEqualize(validStack,minSlice, maxSlice);
            case SliceMethod.Even:
                return SliceEven(validStack, minSlice, maxSlice);
            default:
                throw new NotSupportedException("Slice method not supported");
        }
    }


    static List<string> GenerateValidStack(List<string> inStack)
    {
        var counter = new Dictionary<string,int>();
        var res =  new List<string>();
        var stackCopy = inStack.ToList();
        
        foreach (var element in stackCopy)
        {
            if (!counter.ContainsKey(element))
            {
                counter.Add(element, 0);
            }
            counter[element]++;
        }

        var startIdx = Random.Range(0, inStack.Count);
        var startingElement = stackCopy[startIdx];
        var lastElement = stackCopy[startIdx];

        stackCopy.RemoveAt(startIdx);
        res.Add(lastElement);
        counter[lastElement]--;
        
        var candidates = new List<string>();

        do
        {
            var minCount = 1;
            candidates.Clear();
            
            foreach (var key in counter.Keys)
            {
                if (key == lastElement) continue;

                if (counter[key] < minCount) continue;
            
                if (minCount < counter[key])
                {
                    candidates.Clear();
                    minCount = counter[key];
                }
                candidates.Add(key);
            }

            if (candidates.Count == 0) break;
            
            var chosen = candidates[Random.Range(0,candidates.Count)];

            if (minCount == 1 && lastElement != startingElement && counter[startingElement] > 0)
            {
                chosen = startingElement;
            }

            res.Add(chosen);

            lastElement = chosen;
            
            counter[chosen]--;

        } while (candidates.Count > 0);

        if (lastElement == startingElement)
        {
            res.RemoveAt(res.Count-1);
        }
        
        return res.Roll(Random.Range(0, res.Count));
    }

    static List<List<string>> SliceMax(List<string> stack, int minSize, int maxSize)
    {
        var res = new List<List<string>>();
        var remaining = stack.Count;
        var idx = 0;

        while (idx < stack.Count)
        {
            var cutLength = Math.Min(maxSize,remaining);
            res.Add(stack.GetRange(idx,cutLength));
            remaining -= cutLength;
            idx += cutLength;
        }
        return res;
    }
    
    static List<List<string>> SliceMin(List<string> stack, int minSize, int maxSize)
    {
        var res = new List<List<string>>();
        var remaining = stack.Count;
        var idx = 0;

        while (idx < stack.Count)
        {
            var cutLength = Math.Min(minSize,remaining);
            res.Add(stack.GetRange(idx,cutLength));
            remaining -= cutLength;
            idx += cutLength;
        }
        return res;
    }

    static List<List<string>> SliceEqualize(List<string> stack, int minSize, int maxSize)
    {
        var res = new List<List<string>>();
        var remaining = stack.Count;
        var idx = 0;

        while (idx < stack.Count)
        {


            if (maxSize < minSize)
            {
                maxSize = minSize;
            }

            if (remaining - maxSize < minSize && remaining-maxSize > 0)
            {
                maxSize = remaining - minSize;
            }
            else if (remaining < maxSize)
            {
                maxSize = remaining;
            }

            res.Add(stack.GetRange(idx,maxSize));
            remaining -= maxSize;
            idx += maxSize;
        }
        return res;
    }
    
    static List<List<string>> SliceRandomSize(List<string> stack, int minSize, int maxSize)
    {
        var res = new List<List<string>>();
        var remaining = stack.Count;
        
        var idx = 0;

        while (idx < stack.Count)
        {
            var cutLength = Math.Min(Random.Range(minSize, maxSize),remaining);

            res.Add(stack.GetRange(idx,cutLength));
            remaining -= cutLength;
            idx += cutLength;
        }
        return res;
    }

    static List<List<string>> SliceEven(List<string> stack, int minSize, int maxSize)
    {
        var res = new List<List<string>>();
        var remaining = stack.Count;
        var idx = 0;
        var remainder = maxSize;
        var optimalSize = minSize;
        
        for (int i = maxSize; i >=minSize; i--)
        {
            if (stack.Count % i == 0)
            {
                remainder = stack.Count-stack.Count % i;
                optimalSize = i;
                break;
            }
            if (i-stack.Count % i < remainder)
            {
                remainder = stack.Count-stack.Count % i;
                optimalSize = i;
            }
        }

        var numCuts = (stack.Count - stack.Count % optimalSize)/optimalSize;

        for (int i = 0; i < numCuts; i++)
        {
            res.Add(stack.GetRange(idx,optimalSize));
            idx += optimalSize;
            remaining -= optimalSize;
        }
        
        res.Add(stack.GetRange(idx,remaining));
        
        
        return res;
    }
}

public enum SliceMethod
{
    Max,
    Random,
    Min,
    Equalize,
    Even
}

