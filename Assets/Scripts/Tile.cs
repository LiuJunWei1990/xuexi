using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瓦片类,用于标记地图上的瓦片,并且标记是否可通行
/// </summary>
/// 特性:会在编辑模式下运行该脚本
[ExecuteInEditMode]
public class Tile : MonoBehaviour
{
    /// <summary>
    /// 可通行
    /// </summary>
    public bool passable = true;

    /// <summary>
    /// 瓦片宽度
    /// </summary>
    public int width = 5;
    /// <summary>
    /// 瓦片高度
    /// </summary>
     public int height = 5;

    /// <summary>
    /// 滑动条,用于调整可通行网格的X偏移
    /// </summary>
    [Range(-5, 5)]
    public int offsetX = 0;

    /// <summary>
    /// 滑动条,用于调整可通行网格的Y偏移
    /// </summary>
    [Range(-5, 5)]
    public int offsetY = 0;

    private void Start()
    {

    }

    private void Update()
    {
    }

    /// <summary>
    /// 瓦片被选中时绘制该瓦片的网格
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        //获取瓦片的等距坐标
        Vector3 pos = Iso.MapToIso(transform.position);
        //获取瓦片的左上角坐标
        pos.x -= width / 2;
        pos.y -= height / 2;
        //加上瓦片的偏移量
        pos.x += offsetX;
        pos.y += offsetY;
        //遍历瓦片的每一个网格
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //根据可通行性设置颜色
                Gizmos.color = passable ? new Color(1, 1, 1, 0.2f) : new Color(1, 0, 0, 0.3f);
                //绘制网格,大小为0.9f
                Iso.GizmosDrawTile(pos + new Vector3(x, y), 0.9f);
            }
        }
    }
}
