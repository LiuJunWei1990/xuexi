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
    /// 静态变量，瓦片的尺寸（宽度）
    /// </summary>
    static public float tileSize = 0.2f;

    /// <summary>
    /// 静态变量，瓦片的尺寸（高度，为宽度的一半）
    /// </summary>
    static public float tileSizeY = tileSize / 2;
    /// <summary>
    /// 当前对象的位置（等距坐标）
    /// </summary>
    public Vector2 pos;

    /// <summary>
    /// 当前对象的单元格坐标
    /// </summary>
    public Vector2 tilePos;

    /// <summary>
    /// 精灵材质渲染器
    /// </summary>
    SpriteRenderer spriteRenderer;

    /// <summary>
    /// 宏瓦片的排序值,用来增加渲染层级的数值差距,使其分开渲染,以次来提升性能
    /// </summary>
    public int macroTileOrder;

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
    /// 绘制标线的调试信息，带有颜色和网格边距(绘制一小格的四条边)
    /// </summary>
    /// <param name="pos">等距坐标</param>
    /// <param name="color">线条颜色</param>
    /// <param name="margin">偏移</param>
    static public void DebugDrawTile(Vector3 pos, Color color, float margin = 0)
    {
        // 将等距坐标转换为世界坐标
        pos = Iso.MapToWorld(pos);

        // 计算标线网格的边界
        float d = 0.5f - margin;

        // 绘制网格的四条边
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(d, -d)), color);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(-d, -d)), pos + Iso.MapToWorld(new Vector2(-d, d)), color);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(-d, d)), color);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, -d)), pos + Iso.MapToWorld(new Vector2(-d, -d)), color);
    }

    static public void GizmosDrawTile(Vector3 pos,float size = 1.0f)
    {
        // 将等距坐标转换为世界坐标
        pos = Iso.MapToWorld(pos);
        // 计算标线网格的边界
        float d = 0.5f * size;
        // 绘制网格的四条边
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(d, -d)));
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(-d, -d)), pos + Iso.MapToWorld(new Vector2(-d, d)));
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(-d, d)));
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(d, -d)), pos + Iso.MapToWorld(new Vector2(-d, -d)));
    }

    /// <summary>
    /// 绘制游戏对象地面网格的调试信息，默认颜色为白色(脚下的小格子)
    /// </summary>
    /// <param name="pos">等距坐标</param>
    /// <param name="margin">偏移</param>
    static public void DebugDrawTile(Vector3 pos, float margin = 0)
    {
        DebugDrawTile(pos, Color.white, margin);
    }

    /// <summary>
    /// 获取鼠标所在位置的等距坐标(用作寻路的坐标)
    /// </summary>
    /// <returns>鼠标的等距坐标(取整)</returns>
    static public Vector3 MouseTile()
    {
        // 获取鼠标的世界坐标
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // 将鼠标的世界坐标转换为等距坐标，并对其取整
        return Snap(MapToIso(mousePos));
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
    /// 计算宏瓦片坐标（将坐标按 5x5 分块）,用来增加渲染层级的数值差距,使其分开渲染,以次来提升性能
    /// </summary>
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

    private void Awake()
    {
        //获取当前对象坐标
        pos = MapToIso(transform.position);
        //取整
        tilePos = Snap(pos);
        //取组件,这和上面两条代码是专门给互动物体准备的,我估计后面互动相关代码会验证spriteRenderer是否为空
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start 方法，在游戏开始时调用
    void Start()
    {
        // 空方法，未实现具体逻辑
    }

    /// <summary>
    /// 用来取余数的方法,也叫取模.这个方法保证余数精度不变,除之前小数点后几位就是几位
    /// </summary>
    /// <param name="a">除</param>
    /// <param name="b">被除</param>
    /// <returns>a除以b余多少</returns>
    static float fmod(float a, float b)
    {
        return a - b * Mathf.Round(a / b);
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
            // 将当前对象的位置转换为等距坐标，并取整再转换为世界坐标，设置对象的位置.(作用是对齐网格)
            transform.position = MapToWorld(Snap(MapToIso(transform.position)));
            //反过来由当前对象世界坐标转换为等距坐标来更新pos,原本是pos更新人物位置,现在是人物位置更新pos,因为编辑模式下,人物位置是可以拖动的
            pos = MapToIso(transform.position);
        }

        //管理物体的渲染层级,数值越大越靠前,因为加了-号,变成数值越小越靠前
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y / tileSizeY);
        //宏瓦片的排序值,用来增加渲染层级的数值差距,使其分开渲染,以次来提升性能
        var macroTile = MacroTile(pos);
        macroTileOrder = -Mathf.RoundToInt((MapToWorld(macroTile)).y / tileSizeY);
        spriteRenderer.sortingOrder += macroTileOrder * 1000;
    }

    //// 在 Unity 编辑器中绘制 Gizmos（调试信息）
    //void OnDrawGizmosSelected()
    //{
    //    // 绘制当前游戏对象的网格调试信息
    //    DebugDrawTile(pos);
    //}
}