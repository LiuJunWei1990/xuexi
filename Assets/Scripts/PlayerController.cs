using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// 等距坐标类型
    /// </summary>
    Iso iso;
    /// <summary>
    /// 角色类型
    /// </summary>
    Character character;

    private void Start()
    {
        //获取等距坐标对象
        iso = GetComponent<Iso>();
        //获取角色对象
        character = GetComponent<Character>();
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
            targetTile = Iso.MouseTile();
        }
        //画目标网格的边框,坐标是targetTile,可通行画绿框,不可通行画红框
        Iso.DebugDrawTile(targetTile, Tilemap.instance[targetTile] ? Color.green : Color.red, 0.1f);
        //生成路径,当前坐标--目标网格
        Pathing.BuildPath(iso.tilePos, targetTile);

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
            character.Teleport(Iso.MouseTile());
        }
    }
}
