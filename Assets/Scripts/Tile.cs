using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 瓦片组件
/// </summary>
/// <remarks>
/// 1瓦片=5等距单位
/// 1网格=1等距单位
/// </remarks>
[ExecuteInEditMode]
[RequireComponent(typeof(Iso))]
public class Tile : MonoBehaviour {
    /// <summary>
    /// 可否通行
    /// </summary>
    /// <remarks>
    /// true: 通行
    /// false: 不可通行
    /// </remarks>
	public bool passable = true;
    /// <summary>
    /// 瓦片宽度
    /// </summary>
    /// <remarks>
    /// 5等距单位,5单元格(一般不改,谨慎修改)
    /// </remarks>
	public int width = 5;
        /// <summary>
    /// 瓦片高度
    /// </summary>
    /// <remarks>
    /// 5等距单位,5单元格(一般不改,谨慎修改)
    /// </remarks>
	public int height = 5;
    /// <summary>
    /// 偏移X轴
    /// </summary>
    /// <remarks>
    /// 给墙面这种需要移阻挡位置用的
    /// </remarks>
    [Range(-5, 5)]
    public int offsetX = 0;
    /// <summary>
    /// 偏移Y轴
    /// </summary>
    /// <remarks>
    /// 给墙面这种需要移阻挡位置用的
    /// </remarks>
    [Range(-5, 5)]
    public int offsetY = 0;

    void Start () {

	}

	void Update () {
	}
    /// <summary>
    /// Unity自带在被选中时显示绘制Gizmos（辅助图形）
    /// </summary>
    /// <remarks>
    /// 场景界面选中瓦片时,给这个瓦片画单元格辅助线
    /// </remarks>
    void OnDrawGizmosSelected ()
    {
        Vector3 pos = Iso.MapToIso(transform.position);
        pos.x -= width / 2;
        pos.y -= height / 2;
        pos.x += offsetX;
        pos.y += offsetY;
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                Gizmos.color = passable ? new Color(1, 1, 1, 0.2f) : new Color(1, 0, 0, 0.3f);
                Iso.GizmosDrawTile(pos + new Vector3(x, y), 0.9f);
            }
        }
    }
}
