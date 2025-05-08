using UnityEngine;

/// <summary>
/// 类用于管理静态对象的动画和行为。
/// </summary>
class StaticObject : MonoBehaviour
{
    // 对象的方向
    public int direction = 0;
    // 对象数据
    public Obj obj;
    // 对象信息
    public ObjectInfo objectInfo;

    // 当前模式
    int mode;
    // COF 动画控制器
    COFAnimator animator;

    // Awake 方法，在对象初始化时调用
    void Awake()
    {
        // 获取 COFAnimator 组件
        animator = GetComponent<COFAnimator>();
    }

    // Start 方法，在对象首次激活时调用
    void Start()
    {
        // 设置对象的初始模式
        SetMode(obj.mode);
    }

    // 动画完成时的回调方法
    void OnAnimationFinish()
    {
        // 如果当前模式为 1，切换到 "ON" 模式
        if (mode == 1)
        {
            SetMode("ON");
        }
    }

    // 设置对象模式的方法
    void SetMode(string modeName)
    {
        // 获取模式名称对应的索引
        mode = System.Array.IndexOf(COF.ModeNames[2], modeName);
        // 如果对象需要绘制
        if (objectInfo.draw)
        {
            // 加载 COF 文件
            var cof = COF.Load(obj, modeName);
            // 设置 COF 动画控制器
            animator.SetCof(cof);
            // 设置动画方向
            animator.direction = direction;
            // 设置动画是否循环
            animator.loop = objectInfo.cycleAnim[mode];
            // 设置动画帧范围
            animator.SetFrameRange(objectInfo.start[mode], objectInfo.frameCount[mode]);
        }
    }
}

