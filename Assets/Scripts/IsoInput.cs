using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 等距输入组件
/// </summary>
public class IsoInput : MonoBehaviour {
	/// <summary>
	/// 鼠标指向的等距坐标
	/// </summary>
    static public Vector2 mousePosition;
	/// <summary>
	/// 鼠标指向的单元格的等距坐标
	/// </summary>
    static public Vector3 mouseTile;

    // 用于在Inspector面板上显示静态字段的值
    [SerializeField]
    private Vector2 _displayMousePosition;
    [SerializeField]
    private Vector3 _displayMouseTile;
	/// <summary>
	/// 刷新
	/// </summary>
	/// <remarks>
	/// 每帧刷新:鼠标坐标 >> 转等距 >> 取整 || 因为单元格刚好1单位,所以取整后就是单元格坐标
	/// </remarks>
    void Update ()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition = Iso.MapToIso(mousePos);
        mouseTile = Iso.Snap(mousePosition);

        // 更新显示字段的值
        _displayMousePosition = mousePosition;
        _displayMouseTile = mouseTile;
    }
}
