using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    /// <summary>
    /// 目标对象(通常是玩家角色)
    /// </summary>
    public Transform targrt;

    private void Start()
    {
        //如果每目标就绑玩家
        if (!targrt)
        {
            targrt = GameObject.FindWithTag("Player").transform;
        }

        //更新一下坐标
        transform.position = CalcTargetPos();
    }

    private void LateUpdate()
    {
        //每帧更新坐标,比Update晚
        transform.position = CalcTargetPos();
    }

    /// <summary>
    /// 处理一下相机的跟随坐标,因为Z轴不用动
    /// </summary>
    /// <returns>返回处理好的坐标,用于更新摄像机坐标位置</returns>
    private Vector3 CalcTargetPos()
    {
        //保存目标坐标
        Vector3 targetPos = targrt.position;

        //修改保存坐标的Z轴,实际就是Z不动
        targetPos.z = transform.position.z;

        //返回保存的坐标
        return targetPos;
    }
}
