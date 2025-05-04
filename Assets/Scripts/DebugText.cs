using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// DebugText类，用于在屏幕上显示调试文本
public class DebugText : MonoBehaviour
{
    // 可自定义的文本内容，如果为空则显示游戏对象名称
    public string text = null;
	
    // 每帧调用，用于绘制GUI元素
    void OnGUI ()
    {
        // 设置GUI颜色为白色
        GUI.color = Color.white;
        // 获取摄像机中心点在屏幕上的位置
        var center = Camera.main.WorldToScreenPoint(Camera.main.transform.position);
        // 获取当前对象在屏幕上的位置
        var pos = Camera.main.WorldToScreenPoint(transform.position);
        // 将Z轴设为0，因为2D GUI不需要深度
        pos.z = 0;
        // 计算Y轴位置，使其相对于屏幕中心对称
        pos.y = center.y * 2 - pos.y;
        // 决定要显示的文本：如果text为空则显示游戏对象名称
        var renderText = (text == null) || text == "" ? gameObject.name : text;
        // 在计算出的位置绘制文本标签
        GUI.Label(new Rect(pos, new Vector2(200, 200)), renderText);
	}
}
