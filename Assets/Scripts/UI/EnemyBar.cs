using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人状态条组件
/// </summary>
public class EnemyBar : MonoBehaviour
{
    /// <summary>
    /// 滑动条组件
    /// </summary>
    /// 特性:显示在面板上
    [SerializeField]
    public Slider slider;
    /// <summary>
    /// 滑动条名字组件
    /// </summary>
    /// 特性:显示在面板上
    [SerializeField]
    Text title;
    /// <summary>
    /// 角色组件
    /// </summary>
    /// 特性:不显示在面板上
    [HideInInspector]
    public Character character;
    /// <summary>
    /// 敌人状态条组件实例
    /// </summary>
    static public EnemyBar instance;
    
    void Awake()
    {
        //实例初始化
        instance = this;
        //隐藏滑动条
        slider.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        //人物不为空滑动条显示,为空隐藏
        slider.gameObject.SetActive(character != null);
        //如果人物不为空
        if(character)
        {
            //设置滑动条名字
            title.text = character.name;
            //设置滑动条的最大值和当前值
            slider.maxValue = character.maxHealth;
            slider.value = character.health;
        }
    }
}
