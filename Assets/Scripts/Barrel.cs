using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 桶组件
/// </summary>
[RequireComponent(typeof(Usable))]
public class Barrel : MonoBehaviour
{
    /// <summary>
    /// 动画
    /// </summary>
    Animator animator;
    /// <summary>
    /// 互动组件
    /// </summary>
    Usable usable;
    /// <summary>
    /// 材质
    /// </summary>
    SpriteRenderer spriteRenderer;


    private void Awake()
    {
        //获取各种组件
        animator = GetComponent<Animator>();
        usable = GetComponent<Usable>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 使用,所有可互动物体都有的方法,会被Usable互动组件调用
    /// </summary>
    void OnUse()
    {
        //播放使用动画
        animator.Play("Use");
        //使用组件的激活字段,设置为否.因为使用后桶打碎了
        usable.active = false;
        //桶碎了,网格自然也就可通行了
        Tilemap.instance[Iso.MapToIso(transform.position)] = true;
        //修改图层,以保证不会挡住其他物体
        spriteRenderer.sortingLayerName = "OnFloor";
    }
}
