using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 相机控制器组件
/// </summary>
public class CameraController : MonoBehaviour
{
    /// <summary>
    /// 目标
    /// </summary>
    PlayerController playerController;

    private void Awake()
    {
        //获取目标,按TAG找
        playerController = GameObject.FindObjectOfType<PlayerController>();
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
        Vector3 targetPos = playerController.character.transform.position;

        //修改保存坐标的Z轴,实际就是Z不动
        targetPos.z = transform.position.z;

        //返回保存的坐标
        return targetPos;
    }
}
