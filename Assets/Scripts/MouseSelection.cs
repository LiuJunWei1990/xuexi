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
    /// 当前选中的静态对象
    /// </summary>
    /// <remarks>
    /// 公开静态属性，可被其他类访问以获取当前选中对象
    /// </remarks>
    [HideInInspector]
    static public StaticObject current;
    
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
        // 检查鼠标位置是否在对象边界内
        if (bounds.Contains(mousePos)) current = obj;
    }
}