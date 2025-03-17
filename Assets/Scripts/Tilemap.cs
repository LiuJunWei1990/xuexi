using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瓦片/网格容器
/// 这里要注意网格和瓦片是有5倍的系数.一个瓦片是5*5=25个网格,Tile是瓦片的组件,字段里的map存的是网格
/// </summary>
public class Tilemap : MonoBehaviour
{
    /// <summary>
    /// 类的实例
    /// </summary>
    static public Tilemap instance;

    /// <summary>
    /// 长
    /// </summary>
    private int widht = 1024;
    /// <summary>
    /// 宽
    /// </summary>
    private int height = 1024;
    /// <summary>
    /// 原点
    /// </summary>
    private int origin;
    /// <summary>
    /// 容器
    /// </summary>
    private bool[] map;

    private void Awake()
    {
        //初始化容器,容量等于长乘以宽
        map = new bool[widht * height];
        //初始化原点,容器最中间的那个网格就是原点
        origin = map.Length / 2;
        //初始话实例
        instance = this;
    }
    /// <summary>
    /// 需要使用有条件的排序,故继承IComparer接口
    /// 这个排序是带Floor层级的瓦片排末尾
    /// </summary>
    class TileOrderComparer : IComparer<Tile>
    {
        /// <summary>
        /// 接口方法,比较两个瓦片的层级名,是否为为Floor,a是,b不是,返回否.反之返回是
        /// </summary>
        /// <param name="a">A地板</param>
        /// <param name="b">B地板</param>
        /// <returns>这样的结果就是,地板会排序到后面</returns>
        public int Compare(Tile a,Tile b)
        {
            bool floor1 = a.GetComponent<SpriteRenderer>().sortingLayerName == "Floor";
            bool floor2 = b.GetComponent<SpriteRenderer>().sortingLayerName == "Floor";
            return -floor1.CompareTo(floor2);
        }
    }
    private void Start()
    {
        //找到所有瓦片
        Tile[] tiles = GameObject.FindObjectsOfType<Tile>();
        //按照是否瓦片层级名为Floor排序,Floor排后面
        Array.Sort(tiles, new TileOrderComparer());
        //遍历所有瓦片
        foreach (Tile tile in tiles)
        {
            //当前瓦片坐标转等距
            Vector3 pos = Iso.MapToIso(tile.transform.position);
            //获取瓦片最下方网格的等距坐标
            pos.x -= tile.width / 2;
            pos.y -= tile.height / 2;
            //遍历瓦片的所有网格
            for (int x = 0; x < tile.height; ++x)
            {
                for (int y = 0; y < tile.width; ++y)
                {
                    //根据瓦片可否通行给网格打可否通行标记
                    Tilemap.instance[pos + new Vector3(x, y)] = tile.passable;
                }
            }
        }
    }

    private void Update()
    {
        //下面都是绘制网格红线的代码

        //准备颜色,白
        Color color = new Color(1, 1, 1, 0.07f);
        //准备颜色,红
        Color redColor = new Color(1, 0, 0, 0.2f);

        //取屏幕中心点的等距坐标
        Vector3 pos = Iso.Snap(Iso.MapToIso(Camera.main.transform.position));
        //设定长宽
        int debugWidth = 100;
        int debugHeight = 100;
        //pos在屏幕中心,这里-=长宽,就是取最底部坐标
        pos.x -= debugWidth / 2;
        pos.y -= debugHeight / 2;

        //绘制网格标线,
        for (int x = 1; x < debugWidth; ++x)
        {
            for (int y = 1; y < debugHeight; ++y)
            {
                //形参1,当前网格坐标,形参2,获取当前网格状态,可通行的白色不可的红色
                Iso.DebugDrawTile(pos + new Vector3(x, y), this[pos + new Vector3(x, y)] ? color : redColor);
            }
        }
    }

    /// <summary>
    /// 地图转索引
    /// </summary>
    /// <param name="tilePos">网格坐标</param>
    /// <returns>返回索引下标</returns>
    private int MapToIndex(Vector3 tilePos)
    {
        //Mathf.Round四舍五入取整,保证坐标精度
        return origin + (int)Mathf.Round(tilePos.x + tilePos.y * widht);
    }



    /// <summary>
    /// 索引器
    /// </summary>
    /// <param name="tilePos">网格坐标</param>
    /// <returns>网格通行状态</returns>
    public bool this[Vector3 tilePos]
    {
        get
        {
            //通过等距坐标计算出索引
            return map[MapToIndex(tilePos)];
        }

        set
        {
            //通过等距坐标计算出索引
            map[MapToIndex(tilePos)] = value;
        }
    }


}
