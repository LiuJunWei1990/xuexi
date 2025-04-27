using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 互动
/// </summary>
public class Usable : MonoBehaviour
{
    /// <summary>
    /// 激活
    /// </summary>
    public bool active = true;

    /// <summary>
    /// 使用,例如打碎桶的动作
    /// </summary>
    public void Use()
    {
        //触发当前游戏对象上的OnUse方法
        SendMessage("OnUse");
    }
}
