using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 等距组件
/// </summary>
/// <remarks>
/// [角色的组件]将等距坐标转为为世界坐标,更新给角色的trensform.position; [Iso工具类]提供各种坐标转换的静态方法;
/// 特性:编辑模式下,不运行游戏也可生效; 会自动添加SpriteRenderer组件;
/// </remarks>
[ExecuteInEditMode]
[RequireComponent (typeof(SpriteRenderer))]
public class Iso : MonoBehaviour {
    /// <summary>
    /// 一个瓦片素材的像素单位大小,80像素单位
    /// </summary>
    public const float pixelsPerUnit = 80;
    /// <summary>
    /// 一个单元格的X轴,世界坐标中0.2单位
    /// </summary>
    public const float tileSize = 0.2f;
    /// <summary>
    /// 一个单元格的Y轴,世界坐标中0.1单位
    /// </summary>
    public const float tileSizeY = tileSize / 2;
    /// <summary>
    /// 游戏对象的等距坐标
    /// </summary>
    public Vector2 pos;
    /// <summary>
    /// 编辑模式下,该对象拖动时,吸附瓦片素材的边框
    /// </summary>
    public bool macro = false;
    /// <summary>
    /// 是否按等距地图坐标排序
    /// </summary>
    /// <remarks>
    /// 先排宏瓦片Y轴,同一个宏瓦片的再排世界坐标Y轴
    /// </remarks>
	public bool sort = true;
    /// <summary>
    /// 渲染器组件引用
    /// </summary>
	SpriteRenderer spriteRenderer;
    /// <summary>
    /// 等距坐标转世界坐标
    /// </summary>
    /// <param name="iso"></param>
    /// <returns></returns>
	static public Vector3 MapToWorld(Vector3 iso) {
		return new Vector3(iso.x - iso.y, (iso.x + iso.y) / 2) * tileSize;
	}
    /// <summary>
    /// 世界坐标转等距坐标
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
	static public Vector3 MapToIso(Vector3 world) {
		return new Vector3(world.y + world.x / 2, world.y - world.x / 2) / tileSize;
	}
    /// <summary>
    /// 等距地图排序
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    /// <remarks>
    /// 计算方式: 宏瓦片Y轴*100 再加上 角色以宏瓦片坐标为原点的世界坐标Y轴
    /// 解释一下就是: 先按宏瓦片Y轴排,同一宏瓦片的就比Y轴的世界坐标
    /// </remarks>
    static public int SortingOrder(Vector3 worldPosition)
    {
        var macroTile = MacroTile(MapToIso(worldPosition));
        var macroY = (MapToWorld(macroTile)).y / tileSizeY;
        int macroTileOrder = -Mathf.RoundToInt(macroY);
        int sortingOrder = -Mathf.RoundToInt(worldPosition.y / tileSizeY - macroY);
        sortingOrder += macroTileOrder * 100;
        return sortingOrder;
    }
    /// <summary>
    /// 画格子辅助线(Debug版)
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="color"></param>
    /// <param name="margin"></param>
    /// <param name="duration"></param>
    /// <remarks>
    /// Debug.DrawLine需要在运行模式才会在场景界面显示
    /// 这个重载可以自定义颜色
    /// </remarks>
	static public void DebugDrawTile(Vector3 pos, Color color, float margin = 0, float duration = 0f) {
		float d = 0.5f - margin;
        var topRight = MapToWorld(pos + new Vector3(d, d));
        var topLeft = MapToWorld(pos + new Vector3(-d, d));
        var bottomRight = MapToWorld(pos + new Vector3(d, -d));
        var bottomLeft = MapToWorld(pos + new Vector3(-d, -d));
        Debug.DrawLine(topRight, bottomRight, color, duration);
		Debug.DrawLine(bottomLeft, topLeft, color, duration);
		Debug.DrawLine(topRight, topLeft, color, duration);
		Debug.DrawLine(bottomRight, bottomLeft, color, duration);
	}
    /// <summary>
    /// 画直线辅助线(Debug版)
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    static public void DebugDrawLine(Vector3 from, Vector3 to)
    {
        Debug.DrawLine(MapToWorld(from), MapToWorld(to));
    }
    /// <summary>
    /// 画格子辅助线(Gizmos版)
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="size"></param>
    /// <remarks>
    /// 默认尺寸是画单元格的
    /// Gizmos.DrawLine即使不在运行模式也会在场景界面显示
    /// </remarks>
    static public void GizmosDrawTile(Vector3 pos, float size = 1.0f)
    {
        float d = 0.5f * size;
        var topRight = MapToWorld(pos + new Vector3(d, d));
        var topLeft = MapToWorld(pos + new Vector3(-d, d));
        var bottomRight = MapToWorld(pos + new Vector3(d, -d));
        var bottomLeft = MapToWorld(pos + new Vector3(-d, -d));
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(bottomRight, bottomLeft);
    }
    /// <summary>
    /// 画直线辅助线(Debug版)
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="color"></param>
    /// <param name="margin"></param>
    /// <param name="duration"></param>
    /// <remarks>
    /// Debug.DrawLine需要在运行模式才会在场景界面显示
    /// 这个重载颜色强制是白色
    /// </remarks>
    static public void DebugDrawTile(Vector3 pos, float margin = 0, float duration = 0f) {
		DebugDrawTile(pos, Color.white, margin, duration);
	}
    /// <summary>
    /// 坐标取整
    /// </summary>
    /// <param name="pos">用来取整的坐标</param>
    /// <returns></returns>
	static public Vector3 Snap(Vector3 pos) {
		pos.x = Mathf.Round(pos.x);
		pos.y = Mathf.Round(pos.y);
		return pos;
	}
    /// <summary>
    /// 宏瓦片坐标,等距坐标乘以5倍取整(就是瓦片素材的大小)
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    static public Vector3 MacroTile(Vector3 pos)
    {
        var macroTile = pos;
        macroTile.x = Mathf.Round(pos.x / 5);
        macroTile.y = Mathf.Round(pos.y / 5);
        return macroTile;
    }
    /// <summary>
    /// [Iso工具]计算两点之间的方向索引
    /// </summary>
    /// <param name="from"></param>
    /// <param name="target"></param>
    /// <param name="directionCount"></param>
    /// <returns></returns>
    /// <remarks>
    /// 方向索引对应表
    /// 0: 左上（(-1, -1)）
    /// 1: 左（(-1, 0)）
    /// 2: 左下（(-1, 1)）
    /// 3: 下（(0, 1)）
    /// 4: 右下（(1, 1)）
    /// 5: 右（(1, 0)）
    /// 6: 右上（(1, -1)）
    /// 7: 上（(0, -1)）
    /// </remarks>
    static public int Direction(Vector2 from, Vector3 target, int directionCount)
    {
        var dir = target - (Vector3)from;
        var angle = Vector3.Angle(new Vector3(-1, -1), dir) * Mathf.Sign(dir.y - dir.x);
        var directionDegrees = 360.0f / directionCount;
        return Mathf.RoundToInt((angle + 360) % 360 / directionDegrees) % directionCount;
    }

	void Awake() {
		pos = MapToIso(transform.position);
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	void Start () {
		
	}
    /// <summary>
    /// 运行模式下坐标转换，编辑器模式下坐标转换
    /// </summary>
    void Update () {
        //如果处在游戏运行状态下,等距坐标直接转成世界坐标,主要用于角色移动
        if (Application.isPlaying)
        {
            transform.position = MapToWorld(pos);
        }
        //否则就是在编辑模式下,这个坐标处理主要应用于编辑模式下拖动对象
        else
        {
            //宏瓦片对齐就是把等距坐标乘以5倍,于瓦片素材的大小一致
            if (macro)
            {
                transform.position = MapToWorld(MacroTile(MapToIso(transform.position))) * 5;
            }
            //否则就按瓦片单元格对齐就行
            else
            {
                transform.position = MapToWorld(Snap(MapToIso(transform.position)));
            }
            //角色的transform.position同时也会更新给iso.pos.以保证实时一致
            pos = MapToIso(transform.position);
        }
        //是否按等距地图坐标排序
		if (sort)
        {
            spriteRenderer.sortingOrder = SortingOrder(transform.position);
        }
    }
}
