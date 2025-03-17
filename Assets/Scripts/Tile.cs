using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    ///// <summary>
    ///// 半透明白色,可通行瓦片颜色
    ///// </summary>
    //Color color = new Color(1, 1, 1, 0.07f);

    ///// <summary>
    ///// 半透明红色,不可通行瓦片颜色
    ///// </summary>
    //Color redColor = new Color(1, 0, 0, 0.2f);

    private void Start()
    {
        ////获取当前瓦片的坐标
        //Vector3 pos = Iso.MapToIso(transform.position);
        ////取左上角坐标
        //pos.x -= width / 2;
        //pos.y -= height / 2;
        ////遍历瓦片中的每个单元格
        //for (int x = 0; x < height; x++)
        //{
        //    for (int y = 0; y < width; y++)
        //    {
        //        //Tilemap类的索引器,是将等距坐标转换为对应的容器中的网格对象(布尔类型);
        //        Tilemap.instance[pos + new Vector3(x, y)] = passable;
        //    }
        //}
    }

    private void Update()
    {
        ////获取当前瓦片的坐标
        //Vector3 pos = Iso.MapToIso(transform.position);
        ////取左上角坐标
        //pos.x -= width / 2;
        //pos.y -= height / 2;
        ////遍历瓦片中的每个单元格
        //for (int x = 0; x < height; x++)
        //{
        //    for (int y = 0; y < width; y++)
        //    {
        //        //debug画线,可通行的画半透明白,不可通行的画半透明红
        //        Iso.DebugDrawTile(pos + new Vector3(x, y), passable ? color : redColor);
        //    }
        //}
    }
}
