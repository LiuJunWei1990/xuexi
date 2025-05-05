using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瓦片/单元格的容器
/// </summary>
/// 1.瓦片是地板模型的大小.单元格是等距长度为1的格子,1瓦片=5*5单元格
/// 2.容器并未实际储存瓦片或者单元格,而是通过储存单元格的通行状态来映射单元格
/// 3.单元格也是虚拟的,游戏环境内只是辅助线,并没有实际的游戏对象
public class Tilemap : MonoBehaviour
{
    /// <summary>
    /// 类的单例
    /// </summary>
    /// 容器类型,用一个单例来保证不会被多次实例化
    static private Tilemap instance;
    /// <summary>
    /// 结构:单元格
    /// </summary>
    public struct Cell
    {
        /// <summary>
        /// 标记该单元格是否可通行
        /// </summary>
        public bool passable;
        /// <summary>
        /// 该单元格对应的游戏对象
        /// </summary>
        public GameObject gameObject;
    }
    /// <summary>
    /// 容器的长
    /// </summary>
    private int width = 1024;
    /// <summary>
    /// 容器的宽
    /// </summary>
    private int height = 1024;
    /// <summary>
    /// 原点
    /// </summary>
    private int origin;
    /// <summary>
    /// Cell单元格容器
    /// </summary>
    private Cell[] map;

    void Awake()
    {
        //初始化容器,容量等于长乘以宽
        map = new Cell[width * height];
        //初始化原点,容器最中间的那个单元格就是原点
        origin = map.Length / 2;
        //初始话实例
        instance = this;
        for (int i = 0; i < map.Length; ++i) map[i].passable = true;
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
        public int Compare(Tile a, Tile b)
        {
            bool floor1 = a.GetComponent<SpriteRenderer>().sortingLayerName == "Floor";
            bool floor2 = b.GetComponent<SpriteRenderer>().sortingLayerName == "Floor";
            return -floor1.CompareTo(floor2);
        }
    }

    void Start()
    {
        //找到所有瓦片
        Tile[] tiles = GameObject.FindObjectsOfType<Tile>();
        // //按照是否瓦片层级名为Floor排序,Floor排后面,数组不能像List一样直接Sort,要用Array.Sort
        //Array.Sort(tiles, new TileOrderComparer());


        //>>>>>>>>>>>遍历所有瓦片,根据瓦片坐标标记容器里面的单元格是否可通行<<<<<<<<<<<<
        foreach (Tile tile in tiles)
        {
            //如果瓦片不可通行,中止foreach循环
            if (tile.passable) continue;
            //把pos定位到瓦片最下方的单元格的中心点
            //当前瓦片坐标转等距
            Vector3 pos = Iso.MapToIso(tile.transform.position);
            //获取瓦片最下方单元格的等距坐标
            pos.x -= tile.width / 2;
            pos.y -= tile.height / 2;
            //加上单元格的偏移量(这枚瓦片的所有单元格一起偏移)
            pos.x += tile.offsetX;
            pos.y += tile.offsetY;
            //遍历瓦片的所有单元格
            for (int x = 0; x < tile.width; ++x)
            {
                for (int y = 0; y < tile.height; ++y)
                {
                    //等距转索引下标
                    int index = MapToIndex(pos + new Vector3(x, y));
                    //给容器赋值
                    map[index].passable = tile.passable;
                    map[index].gameObject = tile.gameObject;
                }
            }
        }
    }

    void Update()
    {
        //下面都是绘制单元格红线的代码
        //准备颜色,红
        Color color = new Color(1, 0, 0, 0.3f);

        //取屏幕中心点的等距坐标
        Vector3 pos = Iso.Snap(Iso.MapToIso(Camera.main.transform.position));
        //设定长宽
        int debugWidth = 100;
        int debugHeight = 100;
        //pos在屏幕中心,这里-=长宽,就是取最底部坐标
        pos.x -= debugWidth / 2;
        pos.y -= debugHeight / 2;
        //获取pos屏幕中心点对应的索引下标
        int index = instance.MapToIndex(Iso.Snap(pos));

        //绘制单元格标线,单元格的小格子
        for (int y = 0; y < debugHeight; ++y)
        {
            for (int x = 0; x < debugWidth; ++x)
            {
                //如果当前单元格不可通行,就绘制红线;;[index + x]:index每循环1次+1024,代表的就是本行,+X就是这行的第几个单元格
                if (!instance.map[index + x].passable)
                    //单元格画红线
                    Iso.DebugDrawTile(pos + new Vector3(x, y), color, 0.9f);
            }
            //index += width,就是下一行
            index += width;
        }
    }

    /// <summary>
    /// 地图转索引
    /// </summary>
    /// <param name="tilePos">单元格坐标</param>
    /// <returns>返回索引下标</returns>
    private int MapToIndex(Vector3 tilePos)
    {
        //Mathf.Round四舍五入取整,保证坐标精度
        return origin + Mathf.RoundToInt(tilePos.x + tilePos.y * width);
    }
    /// <summary>
    /// 获取单元格
    /// </summary>
    /// <param name="pos">等距坐标</param>
    /// <returns>容器中对应的单元格</returns>
    public static Cell GetCell(Vector3 pos)
    {
        //坐标取整
        var tilePos = Iso.Snap(pos);
        //获取索引下标
        int index = instance.MapToIndex(tilePos);
        //返回容器中对应的单元格
        return instance.map[index];
    }
    /// <summary>
    /// 设置单元格
    /// </summary>
    /// <param name="pos">坐标</param>
    /// <param name="cell">单元格对象</param>
    public static void SetCell(Vector3 pos, Cell cell)
    {
        //输入的坐标取整
        var tilePos = Iso.Snap(pos);
        //获取索引下标
        int index = instance.MapToIndex(tilePos);
        //对应单元格赋值到单元格容器中
        instance.map[index] = cell;
    }
    /// <summary>
    /// 根据等距坐标判断是否可通行
    /// </summary>
    /// <param name="pos">等距坐标</param>
    /// <returns>是否可通行</returns>
    public static bool Passable(Vector3 pos, int radius = 0, bool debug = false)
    {
        //坐标取整
        var tilePos = Iso.Snap(pos);
        //返回对应下标单元格的通行状态
        return PassableTile(tilePos, radius, debug);
    }
    /// <summary>
    /// 根据单元格的等距坐标判断是否可通行
    /// </summary>
    /// <param name="tilePos">单元格的等距坐标</param>
    /// <returns>是否可通行</returns>
    public static bool PassableTile(Vector3 tilePos, int radius = 0, bool debug = false)
    {
        //获取瓦片坐标对应的数组索引
        int index = instance.MapToIndex(tilePos);
        //按索引获取瓦片的可通行性
        bool passable = instance.map[index].passable;
        //半径为0就返回
        if (radius == 0)
            return passable;
        
        //不为零就判断上下左右是否可通行,并赋值给passable
        passable = passable && instance.map[index - 1].passable;
        passable = passable && instance.map[index + 1].passable;
        passable = passable && instance.map[index - instance.width].passable;
        passable = passable && instance.map[index + instance.width].passable;

        //如果需要debug就绘制单元格
        if (debug)
        {
            Iso.DebugDrawTile(tilePos, 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(1, 0), 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(-1, 0), 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(0, 1), 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(0, -1), 0.1f);
        }
        return passable;
    }
    /// <summary>
    /// 修改可通行状态
    /// </summary>
    /// <param name="pos">等距坐标0</param>
    /// <param name="passable">输入可通行状态</param>
    public static void SetPassable(Vector3 tilePos, bool passable)
    {
        //获取到索引下标
        int index = instance.MapToIndex(tilePos);
        //修改对应下标单元格的通行状态
        instance.map[index].passable = passable; 
    }
    /// <summary>
    /// 射线(单元格版)
    /// :结构体
    /// </summary>
    public struct RaycastHit
    {
        /// <summary>
        /// 是否碰撞
        /// </summary>
        public bool hit;
        /// <summary>
        /// 碰撞的对象(瓦片/角色/物体等)
        /// </summary>
        public GameObject gameObject;
        /// <summary>
        /// 碰撞点
        /// </summary>
        public Vector2 pos;
        /// <summary>
        /// 自定义转换符'bool'
        /// RaycastHit类可以直接赋值给bool类型,内容是RaycastHit对象的hit属性
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator bool(RaycastHit value)
        {
            return value.hit;
        }
    }
    /// <summary>
    /// 射线检测(单元格版)
    /// :RaycastHit结构体的静态方法,用于检测射线是否碰撞
    /// </summary>
    /// <param name="from">射线起点</param>
    /// <param name="to">射线终点</param>
    /// <param name="rayLength">射线长度,默认赋值无限大</param>
    /// <param name="maxRayLength">射线极限长度,默认赋值无限大</param>
    /// <param name="ignore">忽略的对象,默认赋值null</param>
    /// <param name="debug">是否绘制射线,默认赋值false</param>
    /// <returns>返回RaycastHit,由于它自定义了bool类型的转换,基本上就是返回是否射中物体</returns>
    static public RaycastHit Raycast(Vector2 from, Vector2 to, float rayLength = Mathf.Infinity, float maxRayLength = Mathf.Infinity, GameObject ignore = null, bool debug = false)
    {
        //声明一个射线
        var hit = new RaycastHit();
        //计算射线的向量
        var diff = to - from;
        //设定每步长度,角色移动的步子
        var stepLen = 0.2f;
        //如果射线长度为无限大,就取射线长度和最大射线长度的较小的那个
        if (rayLength == Mathf.Infinity) rayLength = Mathf.Min(diff.magnitude, maxRayLength);
        //计算射线长度有多少步(取整)
        int stepCount = Mathf.RoundToInt(rayLength / stepLen);
        //计算一个步子的向量
        var step = diff.normalized * stepLen;
        //用射线起点初始化当前坐标
        var pos = from;
        //遍历每一步
        for (int i = 0; i < stepCount; ++i)
        {
            //每次当前坐标加上一步
            pos += step;
            //如果debug,那么给单元格画线白色,偏移0.3,就是会比单元格缩小一圈,持续时间0.5f
            if (debug) Iso.DebugDrawTile(Iso.Snap(pos), margin: 0.3f, duration: 0.5f);
            //获取当前坐标的单元格
            Cell cell = GetCell(pos);
            //如果当前坐标不可通行,并且不是忽略对象
            bool passable = Passable(pos, 2);
            if (!passable && (ignore == null || ignore != cell.gameObject))
            {
                //不可通行的反向为是,是赋值给hit代表射线击中阻挡
                hit.hit = !passable;
                //当前不可通行单元格的对象赋值成被击中对象
                hit.gameObject = cell.gameObject;
                //返回结果是
                break;
            }
        }
        //返回结果否
        return hit;
    }
    /// <summary>
    /// 检测指定区域内的所有游戏对象,返回数量
    /// </summary>
    /// <param name="center">中心点</param>
    /// <param name="size">区域大小</param>
    /// <param name="result">返回结果:对象存入的数组</param>
    /// <returns>对象数量</returns>
    static public int OverlapBox(Vector2 center, Vector2 size, GameObject[] result)
    {
        //新建计数器
        int count = 0;
        //数组长度为0,返回0
        if (result.Length == 0) return 0;
        //行长度:尺寸的Y轴取整
        int rows = Mathf.RoundToInt(size.y);
        //列长度:尺寸的X轴取整
        int columns = Mathf.RoundToInt(size.x);
        //获取正下方的单元格的数组下标
        int index = instance.MapToIndex(Iso.Snap(center - size / 2));
        //遍历范围内的单元格
        for(int row = 0; row < rows; ++row)
        {
            for(int column = 0; column < columns; ++column)
            {
                //新建对象 = 对应单元格的游戏对象,index是每行的开头,column是列数,就是每行的第几个单元格
                var gameObject = instance.map[index + column].gameObject;
                //如果对象不为空
                if (gameObject != null)
                {
                    //收集进数组
                    result[count] = gameObject;
                    //计数器+1
                    count += 1;
                    //如果计数器大于数组长度,返回计数器值
                    if (count >= result.Length)
                        return count;
                }
            }
            //index += width,就是下一行
            index += instance.width;
        }
        //返回计数器值
        return count;
    }
    
    /// <summary>
    /// 这个是编辑模式和运行模式都会调用的方法,用来绘制单元格线
    /// </summary>
    void OnDrawGizmos()
    {
        //这里把屏幕中心的等距坐标除以5,后面再除以瓦片尺寸0.2,就是乘以5.保证它一定是5的倍数,这样就能保证对齐瓦片
        var cameraTile = Iso.MacroTile(Iso.MapToIso(Camera.main.transform.position));
        //设置颜色
        Gizmos.color = new Color(0.35f, 0.35f, 0.35f);
        //遍历-10到9.作为瓦片的边框
        for (int x = -10; x < 10; ++x)
        {
            //画X轴线
            var pos = Iso.MapToWorld(cameraTile + new Vector3(x, 10) - new Vector3(0.5f, 0.5f)) / Iso.tileSize;
            Gizmos.DrawRay(pos, new Vector3(20, -10f));
        }
        //遍历-10到9.作为瓦片的边框
        for (int y = -10; y < 10; ++y)
        {
            //画Y轴线
            var pos = Iso.MapToWorld(cameraTile + new Vector3(-10, y) - new Vector3(0.5f, 0.5f)) / Iso.tileSize;
            Gizmos.DrawRay(pos, new Vector3(20, 10f));
        }
    }
}
