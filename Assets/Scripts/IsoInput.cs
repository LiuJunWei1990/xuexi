using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ʵʱ��ȡ����ڵȾ������е�λ��
/// </summary>
public class IsoInput : MonoBehaviour
{
    /// <summary>
    /// ����ڵȾ������е�λ��
    /// </summary>
    static public Vector2 mousePosition;
    /// <summary>
    /// ���ָ�����Ƭ����
    /// </summary>
    static public Vector2 mouseTile;

    void Update()
    {
        //��ȡ��������������е�λ��
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //ת��Ϊ�Ⱦ�����
        mousePosition = Iso.MapToIso(mousePos);
        //ȡ��,�õ����ָ�����Ƭ����
        mouseTile = Iso.Snap(mousePosition);
    }
}
