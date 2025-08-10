using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 静态游戏对象组件
/// </summary>
[System.Diagnostics.DebuggerDisplay("{name}")]
class StaticObject : MonoBehaviour
{
    /// <summary>
    /// 朝向
    /// </summary>
    public int direction = 0;
    /// <summary>
    /// 游戏对象属性
    /// </summary>
    public Obj obj;
    /// <summary>
    /// 游戏对象信息
    /// </summary>
    public ObjectInfo objectInfo;
    /// <summary>
    /// 
    /// </summary>
    int mode;
    /// <summary>
    /// COF动画器组件
    /// </summary>
    COFAnimator animator;

    /// <summary>
    /// 游戏对象信息
    /// </summary>
    public ObjectInfo info
    {
        get { return objectInfo; }
    }


    /// <summary>
    /// 初始化
    /// </summary>
    void Awake()
    {
        animator = GetComponent<COFAnimator>();
    }

    /// <summary>
    /// 开始
    /// </summary>
    void Start()
    {
        //
        SetMode(obj.mode);
    }

    void OnAnimationFinish()
    {
        if (mode == 1)
        {
            SetMode("ON");
        }
    }

    void SetMode(string modeName)
    {
        //返回modeName在COF.ModeNames[2]数组中的数组下标,如果不存在就返回-1
        mode = System.Array.IndexOf(COF.ModeNames[2], modeName);
        //游戏对象信息的draw属性为ture,就执行
        if (objectInfo.draw)
        {
            //设定静态游戏对象的动画
            var cof = COF.Load(obj, modeName);
            animator.SetCof(cof);
            animator.direction = direction;
            animator.loop = objectInfo.cycleAnim[mode];
            animator.SetFrameRange(objectInfo.start[mode], objectInfo.frameCount[mode]);
        }
    }

    /// <summary>
    /// Unity方法,相机渲染后调用
    /// </summary>
    void OnRenderObject()
    {
        if (objectInfo.draw && objectInfo.selectable[mode]) MouseSelection.Submit(this, animator.bounds);
    }

    public bool selected
    {
        get { return animator.selected; }
        set { animator.selected = value; }
    }
}

