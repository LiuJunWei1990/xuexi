using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泉水组件
/// </summary>
public class Spring : MonoBehaviour
{
    /// <summary>
    /// [Range(0, 2)]代表fullness是一个从0-2的滑动条
    /// 字段是代表泉水是否满/一半/空
    /// </summary>
    [Range(0, 2)]
    public int fullness = 2;
    /// <summary>
    /// 动画
    /// </summary>
    Animator animator;
    /// <summary>
    /// 互动组件
    /// </summary>
    Usable usable;

    private void Awake()
    {
        //获取组件
        animator = GetComponent<Animator>();
        usable = GetComponent<Usable>();
    }

    private void Start()
    {
        //满值不等于0,则激活
        usable.active = fullness != 0;
        //根据满的程度播放对应动画
        animator.Play(fullness.ToString());
    }
    /// <summary>
    /// 用泉水
    /// </summary>
    void OnUse()
    {
        //满程度-1
        fullness -= 1;
        //播放对应满程度的对话
        animator.Play(fullness.ToString());
        //切换成相应的激活状态
        usable.active = fullness != 0;
    }
}
