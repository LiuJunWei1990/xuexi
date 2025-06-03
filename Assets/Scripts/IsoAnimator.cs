using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 等距场景下的动画状态机
/// </summary>
/// <remarks>
/// [应用表][所有有动画效果的对象的组件]存处一个对象的动画文件的所有数据,Update主要用来回调动画事件,LateUpdate主要用来更新动画
/// </remarks>
public class IsoAnimator : MonoBehaviour {
    /// <summary>
    /// 引用角色的动画文件,在Unity面板中赋值
    /// </summary>
    public IsoAnimation anim;
    /// <summary>
    /// 动画的方向索引
    /// </summary>
    public int direction = 0;
    /// <summary>
    /// 动画速度
    /// </summary>
    /// <remarks>
    /// 目前就是攻击动画会修改一下
    /// </remarks>
    [HideInInspector]
    public float speed = 1.0f;
    /// <summary>
    /// 引用角色的渲染器
    /// </summary>
    SpriteRenderer spriteRenderer;
    /// <summary>
    /// 引用角色组件
    /// </summary>
    Character character;
    /// <summary>
    /// 记录Update的事件,没更新一帧动画,重置一次
    /// </summary>
    float time = 0;
    /// <summary>
    /// 状态机当前的动画姿态 -- 当前动画参数1
    /// </summary>
    State state;
    /// <summary>
    /// 状态机当前的动画(如Attack1) -- 当前动画参数2
    /// </summary>
    IsoAnimation.State variation;
    /// <summary>
    /// 当前动画帧数索引 -- 当前动画参数3
    /// </summary>
    int frameIndex = 0;
    /// <summary>
    /// 当前动画每帧的持续时间 -- 当前动画参数4
    /// </summary>
    float frameDuration;
    /// <summary>
    /// 当前动画每个方向有多少帧(这个项目中现在所有动画都是12帧) -- 当前动画参数5
    /// </summary>
    int spritesPerDirection;
    /// <summary>
    /// 动画状态字典
    /// </summary>
    /// <remarks>
    /// 这个字典成员的State其实时一个数组,储存了这个姿态的所有动画,如野蛮人的攻击有两个动画; 
    /// 这里的string也是姿态名字,和State类里面的name一样和[动画文件]的State类的name也一样
    /// </remarks>
    Dictionary<string, State> states = new Dictionary<string, State>();
    /// <summary>
    /// 动画姿态
    /// </summary>
    /// <remarks>
    /// 这是动画状态机的动画姿态类,和动画文件的姿态类不同
    /// 这个姿态类,是一个数组,储存了这个姿态的所有动画,如野蛮人的攻击有两个动画
    /// </remarks>
    public class State
    {
        /// <summary>
        /// 姿态的名字
        /// </summary>
        public string name;
        /// <summary>
        /// 这个姿态的所有动画数组,如Attack1,Attack2
        /// </summary>
        public List<IsoAnimation.State> variations = new List<IsoAnimation.State>();
    }

    void Start () {
        spriteRenderer = GetComponent<SpriteRenderer>();
        character = GetComponent<Character>();

        State firstState = null;
        //这里有点绕备注一下
        //遍历[动画文件]的姿态数组
        foreach(var state in anim.states)
        {
            //如果遍历的姿态的名称存在于[动画状态机]的姿态字典里
            if (states.ContainsKey(state.name))
            {
                //那么,把这个[动画文件]的姿态动画,收入到字典对应的姿态数组里
                states[state.name].variations.Add(state);
            }
            //字典里没有就新建一个姿态,并且把这个姿态做为firstState
            else
            {
                var newState = new State();
                newState.name = state.name;
                newState.variations.Add(state);
                states.Add(state.name, newState);
                if (firstState == null)
                    firstState = newState;
            }
        }
        //设置状态机的当前状态为firstState
        SetState(firstState);
    }
	/// <summary>
    /// 更新
    /// </summary>
    /// <remarks>
    /// 回调动画事件,如动画中间,动画结束
    /// </remarks>
	void Update () {
        //非循环的动画,索引到最后一帧了,就不更新了直接返回
        if (!variation.loop && frameIndex >= spritesPerDirection)
            return;
        //[注]:Time.deltaTime是Unity的每帧事件大概1/60-1/30秒左右; frameDuration是动画每帧的时间1/12秒; Unity的每帧时间是远小于动画的

        //按动画速度系数累加Unity每帧时间,直到时间超过1/12秒的动画每帧时间,就更新一帧动画
        time += Time.deltaTime * speed;
        while (time >= frameDuration)
        {
            //把动画更新的这一帧时间减掉,重新计数
            time -= frameDuration;
            //没到事件帧就索引加一, 到了事件根据不同事件做相应处理
            if (frameIndex < spritesPerDirection)
                frameIndex += 1;
            if (frameIndex == spritesPerDirection / 2)
                SendMessage("OnAnimationMiddle", SendMessageOptions.DontRequireReceiver);
            if (frameIndex == spritesPerDirection)
            {
                SendMessage("OnAnimationFinish", SendMessageOptions.DontRequireReceiver);
                //动画如果循环,那就重新装载动画
                if (variation.loop)
                    SetupState();
            }
        }
    }
    /// <summary>
    /// 后期更新
    /// </summary>
    /// <remarks>
    /// 更新动画
    /// </remarks>
    void LateUpdate()
    {
        UpdateAnimation();
    }
    /// <summary>
    /// 更新动画
    /// </summary>
    void UpdateAnimation()
    {
        int direction = this.direction;
        if (character)
            direction = (character.directionIndex + anim.directionOffset) % anim.directionCount; //方向索引余一下总数保证不溢出
        //精灵指针定位到动画当前帧的精灵
        int spriteIndex = direction * spritesPerDirection + Mathf.Min(frameIndex, spritesPerDirection - 1);
        //给渲染器的精灵赋值
        spriteRenderer.sprite = variation.sprites[spriteIndex];
    }
    /// <summary>
    /// 获取状态机的当前状态(State类版)
    /// </summary>
    /// <returns></returns>
    public State GetState()
    {
        return state;
    }
    /// <summary>
    /// 设置状态机的当前状态(string版)
    /// </summary>
    /// <param name="stateName"></param>
    /// <remarks>
    /// 这个string是状态机的状态的名字,和从动画状态字典里取对应的动画状态类,之后调重载的SetState(State类版)
    /// </remarks>
    public void SetState(string stateName)
    {
        if (stateName == state.name)
            return;

        SetState(states[stateName]);
    }
    /// <summary>
    /// 设置状态机的当前状态(State类版)
    /// </summary>
    /// <param name="state"></param>
    public void SetState(State state)
    {
        if (this.state == state)
            return;

        this.state = state;
        SetupState();
    }
    /// <summary>
    /// 装载动画,当前动画(如Attack1)
    /// </summary>
    void SetupState()
    {
        //帧索引
        frameIndex = 0;
        variation = state.variations[Random.Range(0, state.variations.Count)];
        //一个方向上动画动作的帧数,现在用的都是12帧
        spritesPerDirection = variation.sprites.Length / anim.directionCount;
        
        if (spritesPerDirection == 0)
            spritesPerDirection = 1;
        //当前动画姿态的每帧时间
        frameDuration = 1.0f / variation.fps;
    }
}
