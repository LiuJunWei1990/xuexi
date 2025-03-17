using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    /// <summary>
    /// Ŀ�����(ͨ������ҽ�ɫ)
    /// </summary>
    public Transform targrt;

    private void Start()
    {
        //���ÿĿ��Ͱ����
        if (!targrt)
        {
            targrt = GameObject.FindWithTag("Player").transform;
        }

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
        Vector3 targetPos = targrt.position;

        //�޸ı��������Z��,ʵ�ʾ���Z����
        targetPos.z = transform.position.z;

        //���ر��������
        return targetPos;
    }
}
