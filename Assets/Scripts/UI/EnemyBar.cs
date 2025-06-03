using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 血条组件
/// </summary>
/// <remarks>
/// UI组件,显示鼠标的悬停目标血量
/// </remarks>
public class EnemyBar : MonoBehaviour {
    /// <summary>
    /// 血条
    /// </summary>
    [SerializeField]
    Slider slider;
    /// <summary>
    /// 文本
    /// </summary>
    [SerializeField]
    Text title;
    /// <summary>
    /// 角色组件引用
    /// </summary>
    [HideInInspector]
    public Character character;
    /// <summary>
    /// 实例
    /// </summary>
    static public EnemyBar instance;

    void Awake()
    {
        instance = this;
        slider.gameObject.SetActive(false);
    }
    /// <summary>
    /// 后期更新
    /// </summary>
    /// <remarks>
    /// 把UI界面血条显示角色的值
    /// </remarks>
	void LateUpdate () {
        slider.gameObject.SetActive(character != null);
        if (character)
        {
            title.text = character.name;
            slider.maxValue = character.maxHealth;
            slider.value = character.health;
        }
    }
}
