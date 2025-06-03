using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 泉水物体组件
/// </summary>
public class Spring : MonoBehaviour {
	/// <summary>
	/// 泉水的状态
	/// </summary>
	/// <remarks>
	/// 特性0-2的滑动条,代表泉水的水位
	/// </remarks>
	[Range(0, 2)]
	public int fullness = 2;
	/// <summary>
	/// 动画状态机组件引用
	/// </summary>
	Animator animator;
	/// <summary>
	/// Usable组件引用
	/// </summary>
	Usable usable;

	void Awake() {
		animator = GetComponent<Animator>();
		usable = GetComponent<Usable>();
	}
	/// <summary>
	/// 开始方法
	/// </summary>
	/// <remarks>
	/// 根据水位,确定是否可用
	/// 根据水位,播放对应动画(其实就3张精灵图,代表不同水位)
	/// </remarks>
	void Start() {
		usable.active = fullness != 0;
		animator.Play(fullness.ToString());
	}
	/// <summary>
	/// 交互方法
	/// </summary>
	/// <remarks>
	/// 被Usable组件回调,泉水被交互后的一系列代码处理逻辑
	/// </remarks>
	void OnUse() {
        if (fullness == 0)
            return;
		fullness -= 1;
		animator.Play(fullness.ToString());
		usable.active = fullness != 0;
	}
}
