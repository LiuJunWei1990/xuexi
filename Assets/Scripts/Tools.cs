using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools
{
    /// <summary>
    /// ȡģ,ȡ����
    /// Mathf.Floor������ȡ��,����ֱ��ȥ��С������
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    static public float Mod(float a, float b)
    {
        return a - b * Mathf.Floor(a / b);
    }

    /// <summary>
    /// ��̾���,������Ƕȵ�
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
