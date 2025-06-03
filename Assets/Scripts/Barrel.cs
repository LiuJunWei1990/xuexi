using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 桶物体组件
/// </summary>
/// <remarks>
/// 自带Usable组件，OnUse方法被Usable组件回调
/// </remarks>
[RequireComponent(typeof(Usable))]
public class Barrel : MonoBehaviour {
	/// <summary>
	/// 动画状态机组件引用
	/// </summary>
	IsoAnimator animator;
	/// <summary>
	/// Usable组件引用
	/// </summary>
	Usable usable;
	/// <summary>
	/// 精灵渲染器组件引用
	/// </summary>
	SpriteRenderer spriteRenderer;

	void Awake() {
		animator = GetComponent<IsoAnimator>();
		usable = GetComponent<Usable>();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}
	/// <summary>
	/// 交互方法
	/// </summary>
	/// <remarks>
	/// 被Usable组件回调,桶被交互后的一系列代码处理
	/// </remarks>
	void OnUse() {
		animator.SetState("Use");
		usable.active = false;
		Tilemap.SetPassable(Iso.MapToIso(transform.position), true);
		spriteRenderer.sortingLayerName = "OnFloor";
	}
}
