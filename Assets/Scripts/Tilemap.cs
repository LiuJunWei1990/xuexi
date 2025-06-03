using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单元格地图组件
/// </summary>
/// <remarks>
/// [单例][游戏管理器组件][静态类]用于初始化和提供管理地图的单元格静态方法
/// [单位]1等距单位=1单元格,5等距单位=1瓦片
/// </remarks>
/// [注]几乎没有对瓦片的管理,瓦片信息几乎都在Tile里
    public class Tilemap : MonoBehaviour {
    /// <summary>
    /// 实例
    /// </summary>
    static private Tilemap instance;
    /// <summary>
    /// 单元格结构体
    /// </summary>
    /// <remarks>
    /// 一个瓦片中的25个小格子,每个小格子就是一个单元格
    /// </remarks>
    public struct Cell
    {
        /// <summary>
        /// 单元格是否可通行
        /// </summary>
        public bool passable;
        /// <summary>
        /// 单元格对应的瓦片的对象
        /// </summary>
        public GameObject gameObject;
    }
    /// <summary>
    /// 单元格映射的宽
    /// </summary>
    private int width = 1024;
    /// <summary>
    /// 单元格映射的高
    /// </summary>
    private int height = 1024;
    /// <summary>
    /// 单元格映射的原点
    /// </summary>
    /// <remarks>
    /// 等于map长度/2,只有用来算等距坐标转索引一个功能
    /// </remarks>
    private int origin;
    /// <summary>
    /// 单元格映射
    /// </summary>
    /// <remarks>
    /// 单元格映射的宽高是1024*1024,好像没有溢出机制,地图太大应该会报错(现在地图大小基本都在500*500以内)
    /// 单元格映射的中心是(0,0),长度除以2就是原点
    /// </remarks>
    private Cell[] map;
    /// <summary>
    /// 初始化
    /// </summary>
    /// <remarks>
    /// 初始化单元格映射,给每个映射赋值,默认都是可通行的
    /// 初始化实例
    /// </remarks>
    void Awake() {
        map = new Cell[width * height];
        origin = map.Length / 2;
        instance = this;
        for (int i = 0; i < map.Length; ++i)
            map[i].passable = true;
    }
    /// <summary>
    /// 
    /// </summary>
    class TileOrderComparer : IComparer<Tile> {
        public int Compare(Tile a, Tile b) {
            bool floor1 = a.GetComponent<SpriteRenderer>().sortingLayerName == "Floor";
            bool floor2 = b.GetComponent<SpriteRenderer>().sortingLayerName == "Floor";
            return -floor1.CompareTo(floor2);
        }
    }
    /// <summary>
    /// 开始
    /// </summary>
    /// <remarks>
    /// 遍历所有瓦片,遍历瓦片的每个单元格,把瓦片的状态和对象赋值给单元格
    /// </remarks>
    void Start() {
        Tile[] tiles = GameObject.FindObjectsOfType<Tile>();
        //Array.Sort(tiles, new TileOrderComparer());
        foreach (Tile tile in tiles) {
            if (tile.passable)
                continue;
            //获取瓦片的等距坐标
            Vector3 pos = Iso.MapToIso(tile.transform.position);
            pos.x -= tile.width / 2;
            pos.y -= tile.height / 2;
            pos.x += tile.offsetX;
            pos.y += tile.offsetY;
            //遍历瓦片内的每一个单元格
            for (int x = 0; x < tile.width; ++x) {
                for (int y = 0; y < tile.height; ++y) {
                    int index = MapToIndex(pos + new Vector3(x, y));
                    //瓦片的状态赋值给单元格的状态
                    map[index].passable = tile.passable;
                    //瓦片的对象赋值给单元格的对象(通过单元格获取到的也是对应瓦片的对象)
                    map[index].gameObject = tile.gameObject;
                }
            }
        }
    }
    /// <summary>
    /// 刷新
    /// </summary>
    /// <remarks>
    /// 以屏幕为中心的长宽100单位以内的不可通行单元格画上辅助线
    void Update() {
        Color color = new Color(1, 0, 0, 0.3f);
        Vector3 pos = Iso.Snap(Iso.MapToIso(Camera.main.transform.position));
        int debugWidth = 100;
        int debugHeight = 100;
        pos.x -= debugWidth / 2;
        pos.y -= debugHeight / 2;
        int index = instance.MapToIndex(Iso.Snap(pos));
        for (int y = 0; y < debugHeight; ++y)
        {
            for (int x = 0; x < debugWidth; ++x)
            {
                if (!instance.map[index + x].passable)
                    Iso.DebugDrawTile(pos + new Vector3(x, y), color, 0.9f);
            }
            index += width;
        }
    }
    /// <summary>
    /// 等距坐标 转 单元格数组索引
    /// </summary>
    /// <param name="tilePos">单元格坐标(等距)</param>
    /// <returns></returns>
    /// <remarks>
    /// 注意这里是等距哦,不是世界坐标
    /// </remarks>
    private int MapToIndex(Vector3 tilePos) {
		return origin + Mathf.RoundToInt(tilePos.x + tilePos.y * width);
	}
    /// <summary>
    /// 按等距坐标获取map中的单元格
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static Cell GetCell(Vector3 pos)
    {
        var tilePos = Iso.Snap(pos);
        int index = instance.MapToIndex(tilePos);
        return instance.map[index];
    }
    /// <summary>
    /// 设定单元格
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="cell"></param>
    public static void SetCell(Vector3 pos, Cell cell)
    {
        var tilePos = Iso.Snap(pos);
        int index = instance.MapToIndex(tilePos);
        instance.map[index] = cell;
    }
    /// <summary>
    /// 获取等距坐标点的单元格可通行状态
    /// </summary>
    /// <param name="pos">鼠标点击的等距坐标</param>
    /// <param name="radius"></param>
    /// <param name="debug"></param>
    /// <returns></returns>
    /// <remarks>
    /// 把等距坐标取整调用PassableTile,因为单元格大小刚好是1单位
    /// </remarks>
    public static bool Passable(Vector3 pos, int radius = 0, bool debug = false)
    {
        var tilePos = Iso.Snap(pos);
        return PassableTile(tilePos, radius, debug);
    }
    /// <summary>
    /// 获取单元格对象坐标的可通行状态
    /// </summary>
    /// <param name="tilePos">单元格坐标(等距坐标)</param>
    /// <param name="radius"></param>
    /// <param name="debug"></param>
    /// <returns>注意是单元格的可通行状态</returns>
    /// <remarks>
    /// 坐标转索引 >> 0半径直接结果 || 非0半径就上下左右一起算结果,只要有一个阻挡就返回不可通行
    /// </remarks>
    public static bool PassableTile(Vector3 tilePos, int radius = 0, bool debug = false)
    {
        int index = instance.MapToIndex(tilePos);
        bool passable = instance.map[index].passable;
        if (radius == 0)
            //0半径的到这就返回了
            return passable;
        //下面是非0半径的
        for (int x = -radius; x <= radius; ++x)
        passable = passable && instance.map[index - 1].passable;
        passable = passable && instance.map[index + 1].passable;
        passable = passable && instance.map[index - instance.width].passable;
        passable = passable && instance.map[index + instance.width].passable;

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
    /// 设置单元格可通行状态
    /// </summary>
    /// <param name="tilePos"></param>
    /// <param name="passable"></param>
    public static void SetPassable(Vector3 tilePos, bool passable)
    {
        if (!passable)
        {
            int index = instance.MapToIndex(tilePos);
            instance.map[index].passable = passable;
        }
    }
    /// <summary>
    /// [射线检测]射线结构体(单元格版)
    /// </summary>
    /// <remarks>
    /// 自己写的射线检测Tilemap版,用法和自带的那个差不多,也是重载了隐式转换bool,可以直接当bool用
    /// </remarks>
    public struct RaycastHit
    {
        public bool hit;
        public GameObject gameObject;
        public Vector2 pos;
        public static implicit operator bool(RaycastHit value)
        {
            return value.hit;
        }
    }
    /// <summary>
    /// 射线检测方法(单元格版)
    /// </summary>
    /// <param name="from">起点</param>
    /// <param name="to">终点</param>
    /// <param name="rayLength">射线长度(默认无限大)</param>
    /// <param name="maxRayLength">射线极限长度(默认无限大)</param>
    /// <param name="ignore">忽略目标</param>
    /// <param name="debug">是否画辅助线</param>
    /// <returns></returns>
    /// <remarks>
    /// 和自带射线用法一样的,代码逻辑不同而已不用管,检测射线上有没有不可通行的单元格
    /// </remarks>
    static public RaycastHit Raycast(Vector2 from, Vector2 to, float rayLength = Mathf.Infinity, float maxRayLength = Mathf.Infinity, GameObject ignore = null, bool debug = false)
    {
        var hit = new RaycastHit();
        var diff = to - from;
        //这个步长和寻路节点之间的长度是不一样的,但是这里的不需要那么精确,因为总长是一样的
        var stepLen = 0.2f;
        if (rayLength == Mathf.Infinity)
            rayLength = Mathf.Min(diff.magnitude, maxRayLength);
        int stepCount = Mathf.RoundToInt(rayLength / stepLen);
        var step = diff.normalized * stepLen;
        var pos = from;
        //遍历所有步,遇到阻挡就返回那个对象,不然就返回空
        for (int i = 0; i < stepCount; ++i)
        {
            pos += step;
            if (debug)
                Iso.DebugDrawTile(Iso.Snap(pos), margin: 0.3f, duration: 0.5f);
            Cell cell = GetCell(pos);
            bool passable = Passable(pos, 2);
            if (!passable && (ignore == null || ignore != cell.gameObject))
            {
                hit.hit = !passable;
                hit.gameObject = cell.gameObject;
                break;
            }
        }
        return hit;
    }
    /// <summary>
    /// 检测方格内是否有游戏对象
    /// </summary>
    /// <param name="center">中心点</param>
    /// <param name="size">大小</param>
    /// <param name="result">结果</param>
    /// <returns></returns>
    /// <remarks>
    /// 遍历方格内所有单元格,如果有游戏对象就返回数量, 并存储结果
    /// </remarks>
    static public int OverlapBox(Vector2 center, Vector2 size, GameObject[] result)
    {
        int count = 0;
        if (result.Length == 0)
            return 0;
        int rows = Mathf.RoundToInt(size.y);
        int columns = Mathf.RoundToInt(size.x);
        int index = instance.MapToIndex(Iso.Snap(center - size / 2));
        for (int row = 0; row < rows; ++row)
        {
            for (int column = 0; column < columns; ++column)
            {
                var gameObject = instance.map[index + column].gameObject;
                if (gameObject != null)
                {
                    result[count] = gameObject;
                    count += 1;
                    if (count >= result.Length)
                        return count;
                }
            }
            index += instance.width;
        }
        return count;
    }
    /// <summary>
    /// 绘制
    /// </summary>
    /// <remarks>
    /// 和Update一样每帧调用,但是游戏发布后不会调用了,顺序在LateUpdate之后
    /// </remarks>
    void OnDrawGizmos()
    {
        var cameraTile = Iso.MacroTile(Iso.MapToIso(Camera.current.transform.position));
        Gizmos.color = new Color(0.35f, 0.35f, 0.35f);
        for (int x = -10; x < 10; ++x)
        {
            var pos = Iso.MapToWorld(cameraTile + new Vector3(x, 10) - new Vector3(0.5f, 0.5f)) / Iso.tileSize;
            Gizmos.DrawRay(pos, new Vector3(20, -10f));
        }

        for (int y = -10; y < 10; ++y)
        {
            var pos = Iso.MapToWorld(cameraTile + new Vector3(-10, y) - new Vector3(0.5f, 0.5f)) / Iso.tileSize;
            Gizmos.DrawRay(pos, new Vector3(20, 10f));
        }
    }
}
