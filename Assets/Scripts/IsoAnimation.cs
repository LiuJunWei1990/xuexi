using UnityEngine;

/// <summary>
/// 等距场景下的动画文件
/// </summary>
/// <remarks>
/// [表参数]这个类就是储存一个角色的动画数据
/// 这个脚本继承ScriptableObject,可以把一个类保存成一个文件
/// </remarks>
/// 特性可序列化
[System.Serializable]
public class IsoAnimation : ScriptableObject
{
    /// <summary>
    /// 动画姿态
    /// </summary>
    /// <remarks>
    /// 储存一个姿态的动画数据,如行走中[姿态]的动画
    /// </remarks>
    /// 特性:可序列化
    [System.Serializable]
    public class State
    {
        /// <summary>
        /// 姿态的名字
        /// </summary>
        public string name;
        /// <summary>
        /// 动画是否循环播放
        /// </summary>
        /// <remarks>
        /// 比如死亡中就不能循环播放,不然人物一直不停重复倒地的动作
        /// </remarks>
        public bool loop = true;
        /// <summary>
        /// 动画的帧率
        /// </summary>
        /// <remarks>
        /// 就是由几张精灵组成,12帧就是12张精灵
        /// </remarks>
        public float fps = 12.0f;
        /// <summary>
        /// 动画的原图片
        /// </summary>
        /// <remarks>
        /// 是由这张图片切成的精灵数组(Unity自带工具切的)
        /// </remarks>
        public Texture2D texture;
        /// <summary>
        /// 动画的精灵数组
        /// </summary>
        /// <remarks>
        /// 组成当前动画的精灵数组
        /// </remarks>
        public Sprite[] sprites;
    }
    /// <summary>
    /// 动画的方向数量
    /// </summary>
    /// <remarks>
    /// 精灵切片会根据方向数量进行切图,8方向就是12*8张精灵
    /// </remarks>
    public int directionCount = 8;
    /// <summary>
    /// 动画的方向偏移量
    /// </summary>
    /// <remarks>
    /// 如果你的精灵切片是从左下角开始的,那么你需要偏移一下,让它从左上角开始
    /// </remarks>
    public int directionOffset = 0;
    /// <summary>
    /// 动画的姿态数组
    /// </summary>
    /// <remarks>
    /// 这个数组就是储存当前动画的所有姿态
    /// </remarks>
    public State[] states;
}
