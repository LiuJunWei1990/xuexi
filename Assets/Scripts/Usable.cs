using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// ����
/// </summary>
public class Usable : MonoBehaviour
{
    /// <summary>
    /// ����
    /// </summary>
    public bool active = true;

    /// <summary>
    /// ʹ��,�������Ͱ�Ķ���
    /// </summary>
    public void Use()
    {
        //������ǰ��Ϸ�����ϵ�OnUse����
        SendMessage("OnUse");
    }
}
