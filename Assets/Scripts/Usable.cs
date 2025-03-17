using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 互动
/// </summary>
//该脚本携带三个组件
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class Usable : MonoBehaviour
{
    /// <summary>
    /// 做为当前目标的对象
    /// </summary>
    static public Usable hot;

    /// <summary>
    /// 贴图材质
    /// </summary>
    SpriteRenderer spriteRenderer;

    /// <summary>
    /// 激活
    /// </summary>
    public bool active = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 鼠标滑过时触发
    /// </summary>
    void OnMouseEnter()
    {
        //如果正在使用中,鼠标滑过就没反应
        if (!active) return;
        //设置为当前目标
        hot = this;
        //修改材质亮度
        spriteRenderer.material.SetFloat("_SelfIllum", 1.0f);
    }

    /// <summary>
    /// 鼠标滑出时触发
    /// </summary>
    void OnMouseExit()
    {
        //如果正在使用中,鼠标滑出就没反应
        if (!active) return;
        //当前目标置空
        hot = null;
        //修改材质亮度
        spriteRenderer.material.SetFloat("_SelfIllum", 0.75f);
    }

    /// <summary>
    /// 使用,例如打碎桶的动作
    /// </summary>
    public void Use()
    {
        //触发当前游戏对象上的OnUse方法
        SendMessage("OnUse");
        //当前目标置空
        hot = null;
    }
}
