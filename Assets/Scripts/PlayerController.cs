using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家控制器组件
/// </summary>
/// 脚本实现功能流程如下：
/// 1.悬停->UpdateHover->每帧更新，指哪个哪个亮->赋值hover
/// 2.外设按钮事件->通过不同的操作分别出发，路径画线，攻击，互动，走路，瞬移，注视的功能
public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// 角色组件
    /// </summary>
    public Character character;
    /// <summary>
    /// 玩家控制器实例
    /// </summary>
    static public PlayerController instance;
    //当前鼠标悬停的游戏对象
    //特性:不显示在面板上
    [HideInInspector]
    static public GameObject hover;
    /// <summary>
    /// 等距坐标组件
    /// </summary>
    Iso iso;

    Collider2D[] hoverColliders = new Collider2D[4];

    void Awake()
    {
        instance = this;
        //如果角色组件为空
        if (character == null)
        {
            //通过Tag找到角色对象
            var player = GameObject.FindWithTag("Player");
            //如果通角色对象不为空,设置角色
            if (player != null)  SetCharacter(player.GetComponent<Character>());
        }

    }

    void Start ()
    {

    }

    /// <summary>
    /// 设定角色
    /// </summary>
    /// <param name="character">目标角色</param>
    public void SetCharacter (Character character)
    {
        //将目标的角色组件赋值给当前角色组件
        this.character = character;
        //获得目标角色对象的等距坐标组件
        iso = character.GetComponent<Iso>();
    }

    /// <summary>
    /// 更新鼠标悬停的目标
    /// </summary>
    void UpdateHover()
    {
        //鼠标左键按下时不更新
        if (Input.GetMouseButton(0)) return;

        //存新悬停目标的变量
        GameObject newHover = null;
        //获取鼠标在世界坐标系中的位置
        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //打出射线,返回打中的所有碰撞器的数量,并把所有碰撞器存如hoverColliders数组
        int overlapCount = Physics2D.OverlapPointNonAlloc(mousePos, hoverColliders);
        //如果射线碰撞到物体
        if (overlapCount > 0)
        {
            //获取碰撞到的最表面的物体,便是鼠标悬停的目标
            newHover = hoverColliders[0].gameObject;
        }

        //如果新悬停目标不等于当前悬停目标
        if (newHover != hover)
        {
            //如果当前悬停目标不为空
            if (hover != null)
            {
                //获取当前悬停目标的精灵渲染器组件
                var spriteRenderer = hover.GetComponent<SpriteRenderer>();
                //还原其亮度
                spriteRenderer.material.SetFloat("_SelfIllum", 1.0f);
            }
            //把新悬停目标赋值给当前悬停目标
            hover = newHover;
            //赋值后,如果当前悬停目标不为空
            if (hover != null)
            {
                //获取当前悬停目标的精灵渲染器组件
                var spriteRenderer = hover.GetComponent<SpriteRenderer>();
                //提高其亮度
                spriteRenderer.material.SetFloat("_SelfIllum", 1.75f);

                //敌人状态条的实例的角色组件引用当前悬停目标的角色组件
                EnemyBar.instance.character = hover.GetComponent<Character>();
            }
            else
            {
                //否则如果悬停目标为空,那么敌人状态条的实例的角色组件引用为空
                EnemyBar.instance.character = null;
            }
        }
    }

    void Update()
    {
        //如果玩家对象为空,那组件就不用运行了,直接返回这一帧
        if (character == null) return;
        UpdateHover();
        //目标的单元格
        Vector3 targetPosition;
        //如果当前互动物体不为空
        if (hover != null)
        {
            //目标单元格直接取当前互动物体的单元格
            targetPosition = Iso.MapToIso(hover.transform.position);
        }
        //当前互动物体为空
        else
        {
            //目标取鼠标位置的单元格
            targetPosition = IsoInput.mousePosition;
        }
        //给路径赋值:自身>>>目标
        var path = Pathing.BuildPath(iso.pos, targetPosition, character.directionCount);
        //画线,自身开始,整个路径
        Pathing.DebugDrawPath(iso.pos, path);
        //角色朝向跟随鼠标
        character.LookAt(IsoInput.mousePosition);

        //按下F4
        if (Input.GetKeyDown(KeyCode.F4))
        {
            //调用瞬移方法
            character.Teleport(IsoInput.mouseTile);
        }
        //单击右键 或者 单击左键+左Shift
        if (Input.GetMouseButton(1) || (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0)))
        {
            //执行攻击
            character.Attack(IsoInput.mousePosition);
        }

        //单击左键
        else if (Input.GetMouseButton(0))
        {
            //被玩家关注的互动物体不为空
            if (hover != null)
            {
                //设置当前为玩家关注
                character.target = hover;
            }
            //为空就是走路
            else
            {
                character.GoTo(IsoInput.mousePosition);
            }
        }
        //按下Tab键
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            //遍历场景中的所有角色
            foreach (Character character in GameObject.FindObjectsOfType<Character>())
            {
                //如果当前角色不是玩家控制器的角色
                if (this.character != character)
                {
                    //设定新角色
                    SetCharacter(character);
                    return;
                }
            }
        }

    }
}
