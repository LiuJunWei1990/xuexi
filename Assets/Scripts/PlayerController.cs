using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家控制器组件
/// </summary>
public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// 等距坐标组件
    /// </summary>
    Iso iso;
    /// <summary>
    /// 角色组件
    /// </summary>
    public Character character;

    private void Awake()
    {
        //如果角色组件为空
        if (character == null)
        {
            //通过Tag找到角色组件
            character = GameObject.FindWithTag("Player").GetComponent<Character>();
        }
        //设置角色
        SetCharacter(character);
    }

    private void Start()
    {

    }

    /// <summary>
    /// 设定角色
    /// </summary>
    /// <param name="character">目标角色</param>
    void SetCharacter(Character character)
    {
        //将目标的角色组件赋值给当前角色组件
        this.character = character;
        //获得目标角色对象的等距坐标组件
        iso = character.GetComponent<Iso>();
    }

    private void Update()
    {
        //目标的网格
        Vector3 targetTile;
        //如果当前互动物体不为空
        if (Usable.hot != null)
        {
            //目标网格直接取当前互动物体的网格
            targetTile = Iso.MapToIso(Usable.hot.transform.position);
        }
        //当前互动物体为空
        else
        {
            //目标取鼠标位置的网格
            targetTile = IsoInput.mouseTile;
        }
        //画目标网格的边框,坐标是targetTile,可通行画绿框,不可通行画红框
        Iso.DebugDrawTile(targetTile, Tilemap.instance[targetTile] ? Color.green : Color.red, 0.1f);
        //生成路径,当前坐标--目标网格
        Pathing.BuildPath(iso.tilePos, targetTile,character.directionCount);

        //单击
        if (Input.GetMouseButton(0))
        {
            if (Usable.hot != null)
            {
                character.Use(Usable.hot);
            }
            else
            {
                character.GoTo(targetTile);
            }
        }

        //单击右键
        if (Input.GetMouseButtonDown(1))
        {
            //调用瞬移方法
            character.Teleport(IsoInput.mouseTile);
        }


        character.LookAt(IsoInput.mousePosition);
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
