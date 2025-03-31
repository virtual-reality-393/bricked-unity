using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameUtils
{
    public static Dictionary<string, Color> nameToColor = new Dictionary<string, Color>()
    {
        {"green", Color.green },
        {"blue", Color.blue },
        {"yellow", Color.yellow},
        {"red", Color.red},
        {"magenta", Color.magenta},
    };
    public static string GetColorName(int h, int s, int v)
    {
        h = h * 2;
        s = s * 2;
        v = v * 2;

        // Uncomment and adjust if needed for other conditions
        // if (v < 50)
        // {
        //     return "black";
        // }
        // else if (v > 200 && s < 100)
        // {
        //     return "white";
        // }

        if (h < 30 || h > 330)
        {
            return "red";
        }
        else if (h >= 30 && h < 90)
        {
            return "yellow";
        }
        else if (h >= 90 && h < 150)
        {
            return "green";
        }
        // Uncomment and adjust if needed for other conditions
        // else if (h >= 150 && h < 210)
        // {
        //     return "cyan";
        // }
        else if (h >= 210 && h < 270)
        {
            return "blue";
        }
        // Uncomment and adjust if needed for other conditions
        // else if (h >= 270 && h < 330)
        // {
        //     return "magenta";
        // }

        return "magenta"; // Default to magenta if none of the conditions matchedW
    }

    public static Color GetColorByName(string colorName)
    {
        return nameToColor[colorName];
    }

    public static (double H, double S, double V) RgbToHsv(int r, int g, int b)
    {
        double h = 0, s = 0, v = 0;

        double rNormalized = r / 255.0;
        double gNormalized = g / 255.0;
        double bNormalized = b / 255.0;

        double max = Math.Max(rNormalized, Math.Max(gNormalized, bNormalized));
        double min = Math.Min(rNormalized, Math.Min(gNormalized, bNormalized));
        double delta = max - min;

        v = max; // Value is the max component

        if (delta > 0)
        {
            s = delta / max; // Saturation is the difference divided by the max value

            if (rNormalized == max)
            {
                h = (gNormalized - bNormalized) / delta; // Red is the max
            }
            else if (gNormalized == max)
            {
                h = 2.0 + (bNormalized - rNormalized) / delta; // Green is the max
            }
            else
            {
                h = 4.0 + (rNormalized - gNormalized) / delta; // Blue is the max
            }

            h *= 60; // Convert to degrees

            if (h < 0)
            {
                h += 360; // Make sure hue is positive
            }
        }

        return (h, s * 100, v * 100); // Return H, S, and V (S and V in percentage)
    }

    //public static (double H, double S, double V) RgbToHsv(Color color)
    //{
    //    return RgbToHsv((int)(color.r * 255), (int)(color.g * 255), (int)(color.b * 255));
    //}


    public static string GetAverageColorName(Texture2D texture, BoundingBox box, Vector2Int screenpoint)
    {
        int x = screenpoint.x;
        int y = screenpoint.y;

        float scale = 0.1f;

        //var hvsTex = ConvertRGBToHSV(texture);

        Color[] pixels = texture.GetPixels(x, y, (int)(box.Width * scale), (int)(box.Height * scale));

        double h = 0, s = 0, v = 0;

        // Loop through each pixel to calculate the average HSV
        int totalPixels = pixels.Length;
        foreach (var pixel in pixels)
        {
            // Assuming ConvertRGBToHSV returns a Color object with H, S, and V properties
            var hsv = RGBToHSV(pixel); // Convert the color pixel to HSV
            h += hsv.H;
            s += hsv.S;
            v += hsv.V;
        }

        // Calculate average values for H, S, V
        h /= totalPixels;
        s /= totalPixels;
        v /= totalPixels;


        return GetColorName((int)h, (int)s, (int)v);
    }

    public static (float H, float S, float V) RGBToHSV(Color color)
    {
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        return (h, s, v);
    }

    public static string GetClosestColorName(Color color)
    {
        // Get the closest color name based on the given color
        string closestColorName = "unknown";
        float closestDistance = float.MaxValue;
        foreach (var kvp in nameToColor)
        {
            float distance = GetColorDistance(color, kvp.Value);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestColorName = kvp.Key;
            }
        }
        return closestColorName;
    }

    private static float GetColorDistance(Color color1, Color color2)
    {
        return Mathf.Sqrt(Mathf.Pow(color1.r - color2.r, 2) + Mathf.Pow(color1.g - color2.g, 2) + Mathf.Pow(color1.b - color2.b, 2));
    }

    public static Texture2D ConvertRGBToHSV(Texture2D inputTexture)
    {
        // Get pixel data from the input texture
        Color[] pixels = inputTexture.GetPixels();

        // Create an array to store the converted HSV values (will store them as Color objects)
        Color[] hsvPixels = new Color[pixels.Length];

        // Loop through each pixel and convert RGB to HSV
        for (int i = 0; i < pixels.Length; i++)
        {
            Color rgb = pixels[i];

            // Convert RGB to HSV (Color class provides RGB -> HSV conversion)
            float h, s, v;
            Color.RGBToHSV(rgb, out h, out s, out v);

            // Store the HSV value back in the new array as a Color
            hsvPixels[i] = new Color(h, s, v);
        }

        // Create a new Texture2D with the same dimensions as the input
        Texture2D hsvTexture = new Texture2D(inputTexture.width, inputTexture.height);

        return hsvTexture;

    }

    public static List<string> GenetateStack(List<string> sortedList)
    {
        System.Random rng = new System.Random();
        List<string> shuffled = sortedList.OrderBy(x => rng.Next()).ToList();

        List<string> res = new List<string>();
        for (int i = 0; i < shuffled.Count; i++)
        {
            if (i == 0)
            {
                res.Add(shuffled[i]);
            }
            else if (shuffled[i] == shuffled[i - 1])
            {
                continue;
            }
            else
            {
                res.Add(shuffled[i]);
            }
        }
        return res;
    }

    public static List<string> GenetateStack(Dictionary<string, int> input)
    {
        List<string> sortedList = GenerateListFromDict(input);
        return GenetateStack(sortedList);
    }

    public static List<string> GenerateListFromDict(Dictionary<string, int> input)
    {
        List<string> result = new List<string>();

        // Loop through each key-value pair in the dictionary
        foreach (var pair in input)
        {
            // Add the key to the result list "value" number of times
            for (int i = 0; i < pair.Value; i++)
            {
                result.Add(pair.Key);
            }
        }

        return result;
    }
}
