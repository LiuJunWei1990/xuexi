using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瓦片/网格的容器
/// </summary>
/// 1.瓦片是地板模型的大小.网格是等距长度为1的格子,1瓦片=5*5网格
/// 2.容器并未实际储存瓦片或者网格,而是通过储存网格的通行状态来映射网格
/// 3.网格也是虚拟的,游戏环境内只是辅助线,并没有实际的游戏对象
public class Tilemap : MonoBehaviour
{
    /// <summary>
    /// 类的单例
    /// </summary>
    /// 容器类型,用一个单例来保证不会被多次实例化
    static public Tilemap instance;

    /// <summary>
    /// 容器的长
    /// </summary>
    private int widht = 1024;
    /// <summary>
    /// 容器的宽
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
        for (int i = 0; i < map.Length; ++i) map[i] = true;
    }


    /// <summary>
    /// 瓦片渲染层级排序器,自定义排序器
    /// </summary>
    /// IComparer接口用于自定义的排序
    /// 实际使用list.Sort(new TileOrderComparer()); 容器变量.Sort(new 自定义排序器类名());
    class TileOrderComparer : IComparer<Tile>
    {
        /// <summary>
        /// 按渲染层级排序
        /// </summary>
        /// <param name="a">A瓦片</param>
        /// <param name="b">B瓦片</param>
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
        //按照是否瓦片层级名为Floor排序,Floor排后面,数组不能像List一样直接Sort,要用Array.Sort
        Array.Sort(tiles, new TileOrderComparer());


        //>>>>>>>>>>>遍历所有瓦片,根据瓦片坐标标记容器里面的网格是否可通行<<<<<<<<<<<<
        foreach (Tile tile in tiles)
        {
            //把pos定位到瓦片最下方的网格的中心点
            //当前瓦片坐标转等距
            Vector3 pos = Iso.MapToIso(tile.transform.position);
            //获取瓦片最下方网格的等距坐标
            pos.x -= tile.width / 2;
            pos.y -= tile.height / 2;
            //加上网格的偏移量(这枚瓦片的所有网格一起偏移)
            pos.x += tile.offsetX;
            pos.y += tile.offsetY;
            //遍历瓦片的所有网格
            for (int x = 0; x < tile.width; ++x)
            {
                for (int y = 0; y < tile.height; ++y)
                {
                    //根据瓦片可否通行给网格打可否通行标记(创建网格)
                    Tilemap.instance[pos + new Vector3(x, y)] = tile.passable;
                }
            }
        }



    }

    private void Update()
    {
        //下面都是绘制网格红线的代码

        //准备颜色,白
        Color color = new Color(1, 1, 1, 0.15f);
        //准备颜色,红
        Color redColor = new Color(1, 0, 0, 0.3f);

        //取屏幕中心点的等距坐标
        Vector3 pos = Iso.Snap(Iso.MapToIso(Camera.main.transform.position));
        //设定长宽
        int debugWidth = 100;
        int debugHeight = 100;
        //pos在屏幕中心,这里-=长宽,就是取最底部坐标
        pos.x -= debugWidth / 2;
        pos.y -= debugHeight / 2;

        //绘制网格标线,网格的小格子
        for (int x = 0; x < debugWidth; ++x)
        {
            for (int y = 0; y < debugHeight; ++y)
            {
                //获取当前网格的可通行状态
                bool passable = this[pos + new Vector3(x, y)];
                //如果不可通行,就绘制红线
                if (!passable)
                    //这里不太理解,都已经if了passable,为什么还要判断一下,不是多余的吗?好像这样只会画红线
                    Iso.DebugDrawTile(pos + new Vector3(x, y), passable ? color : redColor, 0.9f);
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
    /// <summary>
    /// 这个是编辑模式和运行模式都会调用的方法,用来绘制网格线
    /// </summary>
    private void OnDrawGizmos()
    {
        //这里把屏幕中心的等距坐标除以5,后面再除以瓦片尺寸0.2,就是乘以5.保证它一定是5的倍数,这样就能保证对齐瓦片
        var cameraTile = Iso.MacroTile(Iso.MapToIso(Camera.main.transform.position));
        //设置颜色
        Gizmos.color = new Color(0.35f, 0.35f, 0.35f);
        //遍历-10到9.作为瓦片的边框
        for (int x = -10; x < 10; ++x)
        {
            for (int y = -10; y < 10; ++y)
            {
                //不太明白怎么算的,反正是画瓦片的四条边
                //算出瓦片的中心点，然后转换为世界坐标，再除以瓦片长度
                var pos = Iso.MapToWorld(cameraTile + new Vector3(x, y) - new Vector3(0.5f, 0.5f)) / Iso.tileSize;
                //绘制瓦片的四条边
                Gizmos.DrawRay(pos, new Vector3(20, 10));
                Gizmos.DrawRay(pos, new Vector3(20, -10));
            }
        }
    }
}
