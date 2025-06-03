using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 玩家控制器组件
/// </summary>
/// <remarks>
/// 挂游戏管理器对象上,Awake时自动设置当前玩家控制角色
/// </remarks>
public class PlayerController : MonoBehaviour {
    /// <summary>
    /// 当前玩家控制角色引用
    /// </summary>
	public Character character;
    static public PlayerController instance;
    /// <summary>
    /// 鼠标悬停目标
    /// </summary>
    [HideInInspector]
    static public GameObject hover;
    /// <summary>
    /// 玩家控制角色的等距坐标
    /// </summary>
    Iso iso;
    /// <summary>
    /// 鼠标悬停位置碰撞器数组
    /// </summary>
    Collider2D[] hoverColliders = new Collider2D[4];


    void Awake() {
        instance = this;

        if (character == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                SetCharacter(player.GetComponent<Character>());
        }   
	}

	void Start () {
	}

	public void SetCharacter (Character character) {
		this.character = character;
		iso = character.GetComponent<Iso>();
	}
    /// <summary>
    /// 更新悬停目标
    /// </summary>
    /// <remarks>
    /// 把悬停的目标亮度提高, 把原悬停目标亮度恢复正常, UI界面血条显示悬停目标的值
    /// </remarks>
    void UpdateHover()
    {
        if (Input.GetMouseButton(0))
        {
            return;
        }

        GameObject newHover = null;
        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //返回向量上的所有碰撞器数量,所有碰撞器赋值给形参2
        int overlapCount = Physics2D.OverlapPointNonAlloc(mousePos, hoverColliders);
        if (overlapCount > 0)
        {
            newHover = hoverColliders[0].gameObject;
        }
        if (newHover != hover)
        {
            if (hover != null)
            {
                var spriteRenderer = hover.GetComponent<SpriteRenderer>();
                spriteRenderer.material.SetFloat("_SelfIllum", 1.0f);
            }
            hover = newHover;
            if (hover != null)
            {
                var spriteRenderer = hover.GetComponent<SpriteRenderer>();
                spriteRenderer.material.SetFloat("_SelfIllum", 1.75f);

                EnemyBar.instance.character = hover.GetComponent<Character>();
            }
            else
            {
                EnemyBar.instance.character = null;
            }
        }
    }
    /// <summary>
    /// 更新
    /// </summary>
    /// <remarks>
    /// [表驱动]外设控制的代码,由此驱动玩家角色的行为
    /// </remarks>
	void Update () {
        if (character == null)
            return;

        UpdateHover();
        //实时更新人物朝向,鼠标悬停目标的路径辅助线
        Vector3 targetPosition;
		if (hover != null) {
			targetPosition = Iso.MapToIso(hover.transform.position);
		} else {
			targetPosition = IsoInput.mousePosition;
		}
        var path = Pathing.BuildPath(iso.pos, targetPosition, character.directionCount);
        Pathing.DebugDrawPath(iso.pos, path);

        character.LookAt(IsoInput.mousePosition);

        //各种按键控制,触发角色行为
        if (Input.GetKeyDown(KeyCode.F4))
        {
            character.Teleport(IsoInput.mouseTile);
        }

        if (Input.GetMouseButton(1) || (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0)))
        {
            character.Attack(IsoInput.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            if (hover != null)
            {
                character.target = hover;
            }
            else {
                character.GoTo(IsoInput.mousePosition);
            }
        }

		if (Input.GetKeyDown(KeyCode.Tab)) {
			foreach (Character character in GameObject.FindObjectsOfType<Character>()) {
				if (this.character != character) {
					SetCharacter(character);
					return;
				}
			}
		}
	}
}
