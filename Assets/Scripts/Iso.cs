// 引入系统集合命名空间，用于使用集合类型
using System.Collections;

// 引入系统集合泛型命名空间，用于使用泛型集合类型
using System.Collections.Generic;

// 引入 Unity 引擎的命名空间，用于访问 Unity 的核心功能
using UnityEngine;



/// <summary>
/// 定义 Iso 类，用于处理等距坐标和世界坐标之间的转换,绘制地图标线(用于调试模式,和寻路遮挡)
/// </summary>
/// 特性:会在编辑模式下运行该脚本
[ExecuteInEditMode]
/// 特性:该脚本携带一个组件
[RequireComponent(typeof(SpriteRenderer))]
public class Iso : MonoBehaviour
{

    /// <summary>
    /// 静态变量，瓦片的尺寸（世界坐标宽度）
    /// </summary>
    static public float tileSize = 0.2f;

    /// <summary>
    /// 静态变量，瓦片的尺寸（世界坐标高度，为宽度的一半）
    /// </summary>
    static public float tileSizeY = tileSize / 2;
    /// <summary>
    /// 当前对象的位置（等距坐标）
    /// </summary>
    [Tooltip("位置(等距)")]
    public Vector2 pos;

    /// <summary>
    /// 是否瓦片,瓦片拖动时按瓦片对齐,否则按网格对齐
    /// </summary>
    [Tooltip("与瓦片对齐")]
    public bool macro = false;

    /// <summary>
    /// 是否排序(渲染层级),默认是排序的,因为有些物体不需要排序,比如地板,整个图层都是地板,地板是不会挡住其他物体的,所以不需要排序
    /// </summary>
    [Tooltip("与同层级按分数排序")]
    public bool sort = true;

    /// <summary>
    /// 精灵材质渲染器
    /// </summary>
    SpriteRenderer spriteRenderer;


    /// <summary>
    /// 将等距坐标转换为世界坐标
    /// </summary>
    /// <param name="iso">等距坐标</param>
    /// <returns></returns>
    static public Vector3 MapToWorld(Vector3 iso)
    {
        // 根据等距坐标计算世界坐标
        return new Vector3(iso.x - iso.y, (iso.x + iso.y) / 2) * tileSize;
    }

    /// <summary>
    /// 将世界坐标转换为等距坐标
    /// </summary>
    /// <param name="world">世界坐标</param>
    /// <returns></returns>
    static public Vector3 MapToIso(Vector3 world)
    {
        // 根据世界坐标计算等距坐标
        return new Vector3(world.y + world.x / 2, world.y - world.x / 2) / tileSize;
    }

    /// <summary>
    /// 绘制网格标线的调试信息，带有颜色和网格边距(主要应该是绘制网格的，长度默认是0.5f.当然要绘制瓦片边界也不是不行，给偏移加负数长度使其超过0.5就行)
    /// </summary>
    /// <param name="pos">等距坐标</param>
    /// <param name="color">线条颜色</param>
    /// <param name="margin">偏移，就是网格中间的小方块，A*寻路用的那种</param>
    /// <param name="duration">画线的持续时间</param>
    static public void DebugDrawTile(Vector3 pos, Color color, float margin = 0, float duration = 0f)
    {
        // 将等距坐标转换为世界坐标
        pos = Iso.MapToWorld(pos);

        // 计算标线网格的一半边长
        float d = 0.5f - margin;

        // 绘制网格的四条边
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(d, -d)), color, duration);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(-d, -d)), pos + Iso.MapToWorld(new Vector2(-d, d)), color, duration);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(-d, d)), color, duration);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, -d)), pos + Iso.MapToWorld(new Vector2(-d, -d)), color, duration);
    }
    /// <summary>
    /// 专门给调试模式用的,绘制瓦片边界，颜色由Gizmos的颜色决定，边长的计算方式有点变化，这里是边长乘以0.5
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="size"></param>
    static public void GizmosDrawTile(Vector3 pos,float size = 1.0f)
    {
        // 将等距坐标转换为世界坐标
        pos = Iso.MapToWorld(pos);
        // 计算标线网格的一半边长
        float d = 0.5f * size;
        // 绘制网格的四条边
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(d, -d)));
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(-d, -d)), pos + Iso.MapToWorld(new Vector2(-d, d)));
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(-d, d)));
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(d, -d)), pos + Iso.MapToWorld(new Vector2(-d, -d)));
    }

    /// <summary>
    /// 不提供颜色的重载debug网格画线，默认颜色为白色
    /// </summary>
    /// <param name="pos">等距坐标</param>
    /// <param name="margin">偏移</param>
    /// <param name="duration">持续时间</param>
    static public void DebugDrawTile(Vector3 pos, float margin = 0, float duration = 0f)
    {
        DebugDrawTile(pos, Color.white, margin, duration);
    }

    /// <summary>
    /// 对坐标进行取整操作
    /// </summary>
    /// <param name="pos">等距坐标</param>
    /// <returns></returns>
    static public Vector3 Snap(Vector3 pos)
    {
        pos.x = Mathf.Round(pos.x);
        pos.y = Mathf.Round(pos.y);
        return pos;
    }

    /// <summary>
    /// 用作大尺寸瓦片，坐标取整
    /// </summary>
    /// 把坐标除以5。调用这个方法的代码后面会乘以5，保证坐标一直是5的倍数，从而对齐瓦片坐标
    /// <param name="pos">等距坐标</param>
    /// <returns>XY轴分别除以5并取整的结果</returns>
    static public Vector3 MacroTile(Vector3 pos)
    {
        //保持Z轴不变
        var macroTlie = pos;
        //X,Y轴除以5后取整
        macroTlie.x = Mathf.Round(pos.x / 5);
        macroTlie.y = Mathf.Round(pos.y / 5);
        //返回处理后的坐标
        return macroTlie;
    }

    /// <summary>
    /// 获取两点之间的方向的索引(directionCount:8向或者16向)
    /// </summary>
    /// <param name="from">来自</param>
    /// <param name="target">目标</param>
    /// <param name="directionCount">8向或者16向</param>
    /// <returns></returns>
    static public int Direction(Vector2 from, Vector3 target,int directionCount)
    {
        //终点减起点得到两点之间的向量
        var dir = target - (Vector3)from;
        //获取Vector3(-1,-1,-1)(左下方向)和向量dir之间的夹角
        //Mathf.Sign(dir.y - dir.x)判断的是-1,-1,-1到1,1,1之间,负数是下半,正数是上半
        //给夹角加上正负数,可以代表360度中的任一角度
        var angle = Vector3.Angle(new Vector3(-1,-1,-1), dir)*Mathf.Sign(dir.y - dir.x);
        //360°除以方向数,得到每一个方向的角度
        var directionDegrees = 360.0f / directionCount;
        //获取方向索引
        return Mathf.RoundToInt((angle + 360) % 360 / directionDegrees) % directionCount;
    }

    private void Awake()
    {
        //获取当前对象坐标
        pos = MapToIso(transform.position);
        //取组件,这和上面两条代码是专门给互动物体准备的,我估计后面互动相关代码会验证spriteRenderer是否为空
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start 方法，在游戏开始时调用
    void Start()
    {
        // 空方法，未实现具体逻辑
    }
    // Update 方法，每帧调用一次
    void Update()
    {
        // 如果当前对象处于游戏状态
        if (Application.isPlaying)
        {
            // 每帧将当前对象的等距坐标转换为世界坐标，并设置对象的位置
            transform.position = MapToWorld(pos);
        }
        // 如果当前对象处于编辑状态
        else
        {
            //>>>>>>>>>>这里代码的作用时,在编辑模式下,可以拖动游戏对象位置,并且自动对齐网格,一格一格的动<<<<<<<<<<<<
            //如果是大尺寸瓦片,每格就是瓦片大小
            if (macro)
            {
                //MacroTile把等距坐标除以五之后取整,再乘以五,保证坐标一直是5的倍数,这样就是大尺寸瓦片的坐标了.
                transform.position = MapToWorld(MacroTile(MapToIso(transform.position))) * 5;
            }
            //如果不是,每格就是网格大小
            else
            {
                // 将当前对象的位置转换为等距坐标，并取整再转换为世界坐标，设置对象的位置.(作用是对齐网格)
                transform.position = MapToWorld(Snap(MapToIso(transform.position)));
            }
            
            //反过来由当前对象世界坐标转换为等距坐标来更新pos,原本是pos更新人物位置,现在是人物位置更新pos,因为编辑模式下,人物位置是可以拖动的
            pos = MapToIso(transform.position);
        }

        //是否排序,默认是排序的,因为有些物体不需要排序,比如地板,地板是不会挡住其他物体的,所以不需要排序
        if (sort)
        {
            //管理物体的渲染层级,数值越大越靠前,因为加了-号,变成数值越小越靠前
            spriteRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y / tileSizeY);
            //宏瓦片的排序值宏瓦片是相对于规则的地板瓦片来说的,用来增加渲染层级的数值差距,使其分开渲染,以次来提升性能
            var macroTile = MacroTile(pos);
            int macroTileOrder = -Mathf.RoundToInt((MapToWorld(macroTile)).y / tileSizeY);
            spriteRenderer.sortingOrder += macroTileOrder * 1000;
        }
    }

    //// 在 Unity 编辑器中绘制 Gizmos（调试信息）
    //void OnDrawGizmosSelected()
    //{
    //    // 绘制当前游戏对象的网格调试信息
    //    DebugDrawTile(pos);
    //}
}