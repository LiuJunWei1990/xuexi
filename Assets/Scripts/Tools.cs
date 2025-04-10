using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools
{
    /// <summary>
    /// 取模,取余数
    /// Mathf.Floor是向下取整,就是直接去掉小数部分
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    static public float Mod(float a, float b)
    {
        return a - b * Mathf.Floor(a / b);
    }

    /// <summary>
    /// 最短距离,用来算角度的
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    static public float ShortestDelta(float a, float b, float range)
    {
        return Mod(b - a + range / 2, range) - range / 2;
    }
}
