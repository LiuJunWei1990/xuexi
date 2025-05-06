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
    /// <summary>
    /// 状态(人物状态,就是动画,比如 idle,run,jump...)
    /// </summary>
    //特性:字段会显示在Inspector面板上
    [System.Serializable]
    public class State
    {
        /// <summary>
        /// 状态名字(跑,待命,挨打等)
        /// </summary>
        public string name;
        /// <summary>
        /// 是否循环播放动画
        /// </summary>
        public bool loop = true;
        /// <summary>
        /// 动画文件的fps
        /// </summary>
        public float fps = 12.0f;
        /// <summary>
        /// 动画文件引用(编辑器面板上那个框框),需要自己赋值,赋值这个texture后,会自动生成sprites数组
        /// </summary>
        public Texture2D texture; 
        /// <summary>
        /// texture的精灵文件数组,由生成按钮生成,不需要自己赋值
        /// </summary>
        public Sprite[] sprites;
    }
    /// <summary>
    /// 朝向数量
    /// </summary>
    public int directionCount = 8;
    /// <summary>
    /// 朝向偏移量
    /// </summary>
    public int directionOffset = 0;
    /// <summary>
    /// 状态数组,需手动添加
    /// </summary>
    public State[] states;
}