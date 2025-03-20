using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������������
/// </summary>
public class CameraController : MonoBehaviour
{
    /// <summary>
    /// Ŀ��
    /// </summary>
    PlayerController playerController;

    private void Awake()
    {
        //��ȡĿ��,��TAG��
        playerController = GameObject.FindObjectOfType<PlayerController>();
        //����һ������
        transform.position = CalcTargetPos();
    }

    private void LateUpdate()
    {
        //ÿ֡��������,��Update��
        transform.position = CalcTargetPos();
    }

    /// <summary>
    /// ����һ������ĸ�������,��ΪZ�᲻�ö�
    /// </summary>
    /// <returns>���ش���õ�����,���ڸ������������λ��</returns>
    private Vector3 CalcTargetPos()
    {
        //����Ŀ������
        Vector3 targetPos = playerController.character.transform.position;

        //�޸ı��������Z��,ʵ�ʾ���Z����
        targetPos.z = transform.position.z;

        //���ر��������
        return targetPos;
    }
}
