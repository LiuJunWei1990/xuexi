using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家控制器组件
/// </summary>
public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// 角色组件
    /// </summary>
    public Character character;
    //当前鼠标悬停的游戏对象
    //特性:不显示在面板上
    [HideInInspector]
    static public GameObject hover;
    /// <summary>
    /// 等距坐标组件
    /// </summary>
    Iso iso;

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

        //单击右键
        if (Input.GetMouseButtonDown(1))
        {
            //调用瞬移方法
            character.Teleport(IsoInput.mouseTile);
        }
        //单击左键+左Shift
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0))
        {
            //执行攻击
            character.Attack();
        }

        //单击左键
        else if (Input.GetMouseButton(0))
        {
            //被玩家关注的互动物体不为空
            if (Usable.hot != null)
            {
                //设置当前为玩家关注
                character.Use(Usable.hot);
            }
            //为空就是走路
            else
            {
                character.GoTo(targetTile);
            }
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

        //>>>>>>>>>>鼠标悬停高亮逻辑代码<<<<<<<<<<<<<<<<
        //声明鼠标悬停目标
        GameObject newHover = null;
        //通过当前相机的渲染获得鼠标当前位置(这里seepseek建议换成Camera.mian主相机,因为当前相机在Update中容易为空)
        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //2D射线检测
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        //如射线检测到对象
        if (hit)
        {
            //获取这个对象
            newHover = hit.collider.gameObject;
        }

        //如这个对象不等于之前的悬停对象
        if (newHover != hover)
        {
            //如之前的悬停对象不为空
            if (hover != null)
            {
                //获取原有悬停对象的渲染器
                var spriteRenderer = hover.GetComponent<SpriteRenderer>();
                //把高亮恢复
                spriteRenderer.material.SetFloat("_SelfIllum", 1.0f);
            }

            //更新当前悬停对象
            hover = newHover;
            //更新过后悬停对象不为空
            if(hover != null)
            {
                var spriteRenderer = hover.GetComponent<SpriteRenderer>();
                spriteRenderer.material.SetFloat("_SelfIllum", 1.75f);
            }
        }

    }
}
