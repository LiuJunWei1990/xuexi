using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 互动物体组件
/// </summary>
/// <remarks>
/// 回调OnUse方法
/// </remarks>
public class Usable : MonoBehaviour
{
    /// <summary>
    /// 是否可用
    /// </summary>
    public bool active = true;
    /// <summary>
    /// 交互方法
    /// </summary>
    /// <remarks>
    /// 调用OnUse方法
    /// </remarks>
    public void Use()
  {
    SendMessage("OnUse");
  }
}
