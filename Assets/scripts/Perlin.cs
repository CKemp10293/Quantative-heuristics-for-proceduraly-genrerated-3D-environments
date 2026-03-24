using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
public static class Perlin
{
    private static readonly int[] Permutation;

    static Perlin(){
        Permutation = MakePermutation();
    }


    private static void Shuffle(int[] arrayToShuffle,System.Random random){
        for(int e = arrayToShuffle.Length -1; e > 0;e--){
            int index = random.Next(0,e);
            (arrayToShuffle[index], arrayToShuffle[e]) = (arrayToShuffle[e], arrayToShuffle[index]);
        }
    }

    private static int[] MakePermutation(){
        int[] permutation = new int[256];

        for (int i =0;i < 256;i++){
            permutation[i] = i;
        }

        System.Random random = new System.Random();
        Shuffle(permutation,random);

        // Duplicate array to avoid overflow issues

        int[] doublePermutation = new int[512];
        permutation.CopyTo(doublePermutation,0);
        permutation.CopyTo(doublePermutation,256);

        return doublePermutation;
    }

    private static Vector2 getConstantVector(int v){
        // v is the value from the permutation table
        int h = v & 3;
        switch (h){
            case 0: return new Vector2(1.0f,1.0f);
            case 1: return new Vector2(-1.0f,1.0f);
            case 2: return new Vector2(-1.0f,-1.0f);
            case 3: return new Vector2(1.0f,-1.0f);
            default: return Vector2.zero; // will never happen
        }
    }

    private static float Fade(float t){
         return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private static float Lerp(float t,float a1, float a2){
        return a1 + t * (a2-a1);
    }

    public static float perlinNoise(float x, float y){
        int X = Mathf.FloorToInt(x) & 255;
        int Y = Mathf.FloorToInt(y) & 255;

        float xf = x - Mathf.Floor(x);
        float yf = y - Mathf.Floor(y);

        Vector2 topRight =  new Vector2(xf - 1.0f,yf - 1.0f);
        Vector2 topLeft = new Vector2(xf,yf - 1.0f);
        Vector2 bottomRight = new Vector2(xf -1.0f, yf);
        Vector2 bottomLeft = new Vector2(xf,yf);

        int valueTopRight = Permutation[Permutation[X + 1] + Y + 1];
        int valueTopLeft = Permutation[Permutation[X] + Y + 1];
        int valueBottomRight = Permutation[Permutation[X + 1] + Y];
        int valueBottomLeft = Permutation[Permutation[X] + Y];

        float dotTopRight = Vector2.Dot(topRight,getConstantVector(valueTopRight));
        float dotTopLeft = Vector2.Dot(topLeft,getConstantVector(valueTopLeft));
        float dotBottomRight = Vector2.Dot(bottomRight,getConstantVector(valueBottomRight));
        float dotBottomLeft = Vector2.Dot(bottomLeft,getConstantVector(valueBottomLeft));

        float u = Fade(xf);
        float v = Fade(yf);

        return Lerp(u,Lerp(v,dotBottomLeft,dotTopLeft), Lerp(v,dotBottomRight,dotTopRight));
    }

}
