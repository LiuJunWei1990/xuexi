using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// 相机控制器组件
/// </summary>
public class CameraController : MonoBehaviour
{
    void LateUpdate()
    {
        //如果玩家控制器的实例为空,那就目标可以跟随,直接跳出
        if (PlayerController.instance.character == null) return;
        //每帧更新坐标,比Update晚
        transform.position = CalcTargetPos();
    }

    /// <summary>
    /// 计算目标坐标,目标就是玩家
    /// </summary>
    /// <returns>返回处理好的坐标,用于更新摄像机坐标位置</returns>
    Vector3 CalcTargetPos()
    {
        //保存目标坐标
        Vector3 targetPos = PlayerController.instance.character.transform.position;

        //修改保存坐标的Z轴,实际就是Z不动
        targetPos.z = transform.position.z;

        //返回保存的坐标
        return targetPos;
    }
}
