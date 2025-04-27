using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 等距人物动画组件
/// 管理多朝向的人物动作动画
/// </summary>
public class IsoAnimator : MonoBehaviour
{
    /// <summary>
    /// 获取IsoAnimation文件引用,界面上框框,直接拖
    /// </summary>
    public IsoAnimation anim;
    /// <summary>
    /// 动画播放速度系数
    /// </summary>
    /// 特性:字段会显示在Inspector面板上
    [HideInInspector]
    public float speed = 1.0f;
    /// <summary>
    /// 精灵渲染器组件引用
    /// </summary>
    SpriteRenderer spriteRenderer;
    /// <summary>
    /// 角色组件引用
    /// </summary>
    Character character;
    /// <summary>
    /// 累计动画时间
    /// </summary>
    float time = 0;
    /// <summary>
    /// 当前(状态)动作
    /// </summary>
    State state;

    /// <summary>
    /// 引用的isoAnimation文件中的状态(动作)
    /// </summary>
    IsoAnimation.State variation;
    /// <summary>
    /// 当前动画帧索引
    /// </summary>
    int frameIndex = 0;
    /// <summary>
    /// 每帧动画持续时间
    /// </summary>
    float frameDuration;
    /// <summary>
    /// 每个朝向的动画帧数量(当前动作动画的索引总数)
    /// </summary>
    int spritesPerDirection;

    /// <summary>
    /// 人物的状态(动作)库
    /// </summary>
    Dictionary<string, State> states = new Dictionary<string, State>();

    /// <summary>
    /// 动画状态类(动画状态机那个状态,就是动作),注意isoAnimation文件中的状态(动作)和这个类是不同的类,一个在IsoAnimation.cs文件中,一个在IsoAnimator.cs文件中
    /// </summary>
    public class State
    {
        /// <summary>
        /// 状态名称 (跑,待命,挨打等)
        /// </summary>
        public string name;
        /// <summary>
        /// 变化库
        /// isoAnimation文件中的状态(动作)变化的数组,比如向右跑,向左跑,向上跑,向下跑等
        /// </summary>
        public List<IsoAnimation.State> variations = new List<IsoAnimation.State>();
    }

    void Start()
    {
        //get渲染器和角色组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        character = GetComponent<Character>();


        //初始化当前状态
        State firstState = null;
        //遍历引用的isoAnimation文件中的states数组
        foreach (var state in anim.states)
        {
            //如果人物的动作库里有遍历的动作
            if (states.ContainsKey(state.name))
            {
                //把他加入到人物的这个动作的变化库里面(iosAnimation文件中的状态(动作)对应的是变化库
                states[state.name].variations.Add(state);
            }
            //否则
            else
            {
                //创建一个新的状态(动作)
                var newState = new State();
                //把遍历的动作的名字赋值给新的状态(动作)的名字
                newState.name = state.name;
                //把遍历到的动作赋值给新的状态(动作)的变化库;
                newState.variations.Add(state);
                //把新的状态(动作)加入到人物的动作库里面
                states.Add(state.name, newState);
                //如果第一个状态为空,就把新的状态(动作)赋值给第一个状态(动作)
                if (firstState == null)
                    firstState = newState;
            }
        }

        SetState(firstState);
    }

    void Update()
    {
        //如果当前动画帧的索引大于等于执行的动作的索引总数-1(就是当前动作动画执行完了)就跳出
        if(!variation.loop && frameIndex >= spritesPerDirection -1) return;
        //记录累计时间+=每帧时间*动画速度系数
        time += Time.deltaTime * speed;
        //如果累计时间大于等于每帧动画时间,就执行
        while (time >= frameDuration)
        {
            //累计时间-=每帧动画的时间
            time -= frameDuration;

            //>>>>帧数加一
            //如果动画当前帧索引<执行的动作的索引总数(就是当前动作动画还没执行完)
            if (frameIndex < spritesPerDirection)
            //那么动作索引+1(就是动作动画进行到下一帧)
                frameIndex += 1;

            //>>>>触发动画事件
            //如果动画当前帧索引==执行的动作的索引总数/2(就是当前动作动画执行到一半了)
            if (frameIndex == spritesPerDirection / 2)
            //向所有MonoBehaviour组件光比发"OnAnimationMiddle"消息,形参2的作用是不要求接受者必须有这个方法,没有就不执行不会报错
                SendMessage("OnAnimationMiddle", SendMessageOptions.DontRequireReceiver);
            //如果动画当前帧索引==执行的动作的索引总数(就是当前动作动画执行完了)
            if (frameIndex == spritesPerDirection)
            {
            //向所有MonoBehaviour组件光比发"OnAnimationFinish"消息,形参2的作用是不要求接受者必须有这个方法,没有就不执行不会报错
                SendMessage("OnAnimationFinish", SendMessageOptions.DontRequireReceiver);
                //如果动画循环播放,就重置动作索引为0(就是动作动画重新播放)
                if (variation.loop) SetupState();
            }
        }
    }

    void LateUpdate()
    {
        UpdateAnimation();
    }
    /// <summary>
    /// 更新动画
    /// </summary>
    void UpdateAnimation()
    {
        //初始化动画朝向
        int direction = 0;
        //如果角色组件不为空
        if (character)
        //动画朝向=(角色朝向+动画朝向偏移量)%动画朝向数量,这些量都是索引
            direction = (character.directionIndex + anim.directionOffset) % anim.directionCount;
        //精灵索引 = 动画朝向*每个朝向的动画帧数量(定位到当前的动作)+当前帧索引(定位到当前的动作的当前帧)%每个朝向的动画帧数量(定位到当前的动作的当前帧)
        int spriteIndex = direction * spritesPerDirection + frameIndex % spritesPerDirection;
        //渲染器中的精灵引用=当前动作的精灵数组[精灵索引](就是渲染器中的精灵引用=当前动作的精灵数组[当前帧索引])
        spriteRenderer.sprite = variation.sprites[spriteIndex];
    }
    /// <summary>
    /// 设置动画状态，跑步，攻击等状态(取状态名称做形参)
    /// </summary>
    /// <param name="stateName"></param>
    public void SetState(string stateName)
    {
        //获取的isoAnimation文件的状态，与当前状态相同，直接返回
        if (stateName == state.name) return;
        //不跳出就执行方法
        SetState(states[stateName]);
    }

    /// <summary>
    /// 设置动画状态，跑步，攻击等状态(取isoAnimation文件中的状态做形参)
    /// </summary>
    /// <param name="state">返回动画状态</param>
    public void SetState(State state)
    {
        //获取的isoAnimation文件的状态，与当前状态相同，直接返回
        if (this.state == state)
            return;
        //不跳出就赋值
        this.state = state;
        //上面换了新状态，初始化一下动画的相关设置
        SetupState();
    }
    /// <summary>
    /// 初始化状态参数，这个方法是在上面方法中赋值新状态后面，给新的状态初始化参数。
    /// </summary>

    void SetupState()
    {
        //帧索引赋值为0，动画播放重置到开头
        frameIndex = 0;

        variation = state.variations[Random.Range(0, state.variations.Count)];
        //重新计算没个朝向的精灵数量
        spritesPerDirection = variation.sprites.Length / anim.directionCount;
        //怕有空引用导致乘以0，所以如果是0 至少赋值个1保底
        if (spritesPerDirection == 0) spritesPerDirection = 1;
        //计算每帧动画持续时间
        frameDuration = 1.0f / variation.fps;
    }
}