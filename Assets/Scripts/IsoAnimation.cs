using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// 定义一个IsoAnimation类,用于存储动画相关的属性,继承自ScriptableObject类
/// ScriptableObject类是Unity自带的一个类,用于把一个类型转换为资源文件,前提是这个类型继承自ScriptableObject
/// 这个类的序列化字段可以直接在Inspector面板上显示和修改
/// </summary>
//特性:字段会显示在Inspector面板上
[System.Serializable]
public class IsoAnimation : ScriptableObject
{
    //状态(人物状态,就是动画,比如 idle,run,jump...)
    //特性:字段会显示在Inspector面板上
    [System.Serializable]
    public class State
    {
        //状态名字(跑,待命,挨打等)
        public string name;
        //是否循环播放动画
        public bool loop = true;
        //动画文件引用(编辑器面板上那个框框),需要自己赋值,赋值这个texture后,会自动生成sprites数组
        public Texture2D texture; 
        //texture的精灵文件数组,由生成按钮生成,不需要自己赋值
        public Sprite[] sprites;
    }
    //动画文件的fps
    public float fps = 12.0f;
    //朝向数量
    public int directionCount = 8;
    //朝向偏移
    public int directionOffset = 0;
    //需手动添加
    public State[] states;
}