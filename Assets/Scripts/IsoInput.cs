using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 实时获取鼠标在等距坐标中的位置
/// </summary>
public class IsoInput : MonoBehaviour
{
    /// <summary>
    /// 鼠标在等距坐标中的位置
    /// </summary>
    static public Vector2 mousePosition;
    /// <summary>
    /// 鼠标指向的瓦片坐标
    /// </summary>
    static public Vector2 mouseTile;

    void Update()
    {
        //获取鼠标在世界坐标中的位置
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //转换为等距坐标
        mousePosition = Iso.MapToIso(mousePos);
        //取整,得到鼠标指向的瓦片坐标
        mouseTile = Iso.Snap(mousePosition);
    }
}
