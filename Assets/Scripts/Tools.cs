using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 运算工具类
/// </summary>
/// <remarks>
/// 提供了两个运算方法
/// 1.模运算
/// 2.计算一个圆形中,两个节点之间的差值
/// </remarks>
public class Tools {
	/// <summary>
	/// 模运算
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns>就是结果始终未非负数的取余</returns>
	static public float Mod(float a, float b) {
		return a - b * Mathf.Floor(a / b);
	}
	/// <summary>
	/// 计算在一个圆中两个值之间的最短差值
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="range"></param>
	/// <returns>
	/// 这个值是平均分布的, 例如方向索引这样的. 这个方法在角色转向时非常有用，可以确保角色总是以最短路径转向目标方向。
	/// </returns>
	static public float ShortestDelta(float a, float b, float range) {
		return Mod(b - a + range / 2, range) - range / 2;
	}
}
