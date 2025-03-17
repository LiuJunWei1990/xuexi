using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 对齐网格
/// 给Unity编辑器添加菜单项目Iso/网格对齐
/// </summary>
public class EditorTools : MonoBehaviour
{
    /// <summary>
    /// 用来取余数的方法,也叫取模.这个方法保证余数精度不变,除之前小数点后几位就是几位
    /// </summary>
    /// <param name="a">除</param>
    /// <param name="b">被除</param>
    /// <returns>a除以b余多少</returns>
    static float fmod(float a,float b)
    {
        //下面的方法是四舍五入取整,保证除不尽不会导致改变结果的精度
        return a - b * Mathf.Round(a / b);
    }

    /// <summary>
    /// []里面的内容代表,在Unity编辑器里面添加一个菜单Iso,下拉列表添加一个项目Snap
    /// 这个方法是通过取余数,来对齐所有网格
    /// </summary>
    [MenuItem("Iso/对齐网格")]
    static public void SnapToIsoGrid()
    {
        //遍历在编辑器中选中的所有游戏对象(多选,单选)(Selection.gameObjects返回选中的所有对象)
        foreach (GameObject gameObject in Selection.gameObjects)
        {
            //对其坐标
            Snap(gameObject.transform);
        }
    }
    /// <summary>
    /// 对齐游戏对象的坐标,使其与网格对其
    /// </summary>
    /// <param name="transform">当前处理的目标</param>
    public static void Snap(Transform transform)
    {
        //获取当前对象的本地坐标
        var pos = transform.localPosition;
        //对齐网格的核心代码,
        //例子:0.7X轴>>0.733-(0.733-0.2*(0.733/0.2的取整))>>0.733-(0.733-0.2*(4))>>0.733-(0.733-0.8)>>0.733--0.067>>0.8
        //这样就把0.733X轴对齐到了0.8X轴
        transform.localPosition = new Vector3(pos.x - fmod(pos.x, 0.2f), pos.y - fmod(pos.y, 0.1f), pos.z);

        //递归走起来,所有子对象全部要对齐
        for (int i = 0; i < transform.childCount; ++i)
        {
            Snap(transform.GetChild(i));
        }
    }
}
