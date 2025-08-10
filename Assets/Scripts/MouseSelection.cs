using UnityEngine;

/// <summary>
/// 鼠标选择管理器
/// </summary>
/// <remarks>
/// 处理游戏中静态对象的鼠标选择逻辑，管理选中状态切换和高亮显示
/// </remarks>
class MouseSelection : MonoBehaviour
{
    /// <summary>
    /// 选中对象的专用字体
    /// </summary>
    public Font selectionFont;
    /// <summary>
    /// 当前选中的静态对象
    /// </summary>
    /// <remarks>
    /// 静态属性
    /// </remarks>
    [HideInInspector]
    static StaticObject current;
    
    /// <summary>
    /// 上一个选中的静态对象
    /// </summary>
    /// <remarks>
    /// 用于在切换选中对象时取消前一个对象的选中状态
    /// </remarks>
    static StaticObject previous;
    
    /// <summary>
    /// 鼠标在世界坐标系中的位置
    /// </summary>
    static Vector3 mousePos;

    /// <summary>
    /// 选中对象的碰撞边界
    /// </summary>
    static Bounds bounds;

    /// <summary>
    /// 每帧更新选中状态
    /// </summary>
    void Update()
    {
        // 如果存在上一个选中对象，取消其选中状态
        if (previous != null) previous.selected = false;
        
        // 如果存在当前选中对象，设置其选中状态
        if (current != null) current.selected = true;
        
        // 更新上一个选中对象为当前对象（用于下一帧比较）
        previous = current;
        
        // 重置当前选中对象（等待Submit方法重新设置）
        current = null;
        
        // 将鼠标屏幕坐标转换为世界坐标
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // 确保Z轴为0，保持在2D平面上
        mousePos.z = 0;
    }

    /// <summary>
    /// GUI绘制方法，用于在选中对象上方显示名称标签
    /// </summary>
    void OnGUI()
    {
        // 仅当存在选中对象时才绘制标签
        if (current != null)
        {
            // 设置标签文本对齐方式为底部居中
            GUI.skin.label.alignment = TextAnchor.LowerCenter;
            // 应用选中对象的专用字体
            GUI.skin.font = selectionFont;
            
            // 将3D世界坐标转换为屏幕坐标
            var pos = Camera.main.WorldToScreenPoint(bounds.center);
            // 调整Y轴坐标（因为屏幕坐标原点在左下角，而GUI原点在左上角）
            // 加上名称偏移量以调整垂直位置
            pos.y = Camera.main.pixelHeight - pos.y + current.info.nameOffset;
            
            // 定义标签的宽度和高度
            const int width = 500;
            const int height = 100;
            
            // 绘制标签，居中显示在计算出的位置
            GUI.Label(new Rect(pos - new Vector3(width / 2, height), new Vector2(width, height)), current.info.name);
        }
    }

    /// <summary>
    /// 提交对象进行选中检测
    /// </summary>
    /// <param name="obj">待检测的静态对象</param>
    /// <param name="bounds">对象的碰撞边界</param>
    /// <remarks>
    /// 由静态对象调用，用于判断自身是否被鼠标选中
    /// 如果鼠标位置在对象边界内，则将该对象设为当前选中对象
    /// </remarks>
    static public void Submit(StaticObject obj, Bounds bounds)
    {
        // 判断当前对象是否为空，或新对象位置是否更低（Y轴值更小表示更靠下）
        bool betterMatch = current == null || bounds.center.y < MouseSelection.bounds.center.y;
        // 检查鼠标位置是否在对象边界内
        if (betterMatch && bounds.Contains(mousePos))
        {
            // 更新当前选中对象
            current = obj;
            // 更新选中对象的边界信息
            MouseSelection.bounds = bounds;
        }
    }
}