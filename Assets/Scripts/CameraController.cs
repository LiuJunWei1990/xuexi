using UnityEngine;
using System.Collections;
using System;
/// <summary>
/// 相机控制器组件
/// </summary>
public class CameraController : MonoBehaviour
{
	/// <summary>
	/// 后期更新
	/// </summary>
	/// <remarks>
	/// 相机每帧跟随玩家,没玩家就不跟随
	/// </remarks>
	void LateUpdate () {
        if (PlayerController.instance.character == null)
            return;

        transform.position = CalcTargetPos();
	}
	/// <summary>
	/// 跟随目标坐标
	/// </summary>
	/// <returns></returns>
	/// <remarks>
	/// 就是Z轴不跟随,其他都跟随
	/// </remarks>
	Vector3 CalcTargetPos() {
		Vector3 targetPos = PlayerController.instance.character.transform.position;
		targetPos.z = transform.position.z;

		return targetPos;
	}
}