using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色组件
/// </summary>
/// 脚本实现的功能流程如下:
/// 1.行走->PlayerController中外设按钮调用->GoTo()->PathTo()生成路径->Update()刷新Move()方法移动角色
/// 2.互动->PlayerController中外设按钮调用->target(属性)赋值->Set中执行Use()->PathTo()生成路径->赋值usable->Update()中Move()后检测到路径空了并usable不为空就执行互动
/// 3.攻击->PlayerController中外设按钮调用->target(属性)赋值->Set中执行Attack()->PathTo()生成路径->赋值targetCharacter->Update()中Move()后检测到路径空了并targetCharacter不为空就执行攻击,并人物进入攻击状态
/// 4.瞬移->PlayerController中外设按钮调用->Teleport()执行瞬移,如果目的地不可通行就生成路径瞬移到目的地最近的可通行网格
/// 5.动画->和Move()基本类似,根据角色是否攻击状态,挨打状态,是否有路径来决定播放Idle,Walk,Run,Attack动画
/// 总结:除瞬移外,其他动作都是生成路径+赋值目标单位,然后在Update()中检测路径是否为空,如果为空就执行相应的动作
public class Character : MonoBehaviour
{
    /// <summary>
    /// 朝向数量
    /// </summary>
    [Tooltip("角色动画朝向数量")]
    public int directionCount = 8;
    /// <summary>
    /// 速度
    /// </summary>
    [Tooltip("角色移动速度")]
    public float speed = 3.5f;
    /// <summary>
    /// 攻击速度
    /// </summary>
    [Tooltip("角色攻击速度")]
    public float attackSpeed = 1.0f;
    /// <summary>
    /// 互动范围
    /// </summary>
    [Tooltip("寻路至互动物体会在这个距离停下并开始互动")]
    public float useRange = 1.5f;
    /// <summary>
    /// 攻击范围
    /// </summary>
    [Tooltip("寻路至敌人会在这个距离停下并开始攻击")]
    public float attackRange = 2.5f;
    /// <summary>
    /// 是否在跑动
    /// </summary>
    [Tooltip("是否奔跑")]
    public bool run = false;

    /// <summary>
    /// 角色的目标(属性)
    /// </summary>
    //特性:不显示在面板上
    [HideInInspector]
    public GameObject target
    {
        get
        {
            ///读取目标字段
            return m_Target;
        }
        set
        {
            //>>>>>>>>>>>>>>设定目标<<<<<<<<<<<<<<<
            //尝试获取将要做为目标的对象的互动组件
            var usable = value.GetComponent<Usable>();
            //如果互动组件不为空就代表是可互动的对象,就调<使用/互动>Use方法,<使用>目标
            if (usable != null) Use(usable);
            //否则,即目标没有互动组件
            else
            {
                //尝试获取将要做为目标的对象的角色组件
                var targetCharacter = value.GetComponent<Character>();
                //如果有角色组件,就代表是可攻击的对象,就调<攻击>Attack方法,<攻击>目标
                if (targetCharacter) Attack(targetCharacter);
                //都没有,就return不做任何动作
                else
                {
                    return;
                }
            }

            //无论上面代码执行任一动作,都要把目标赋值给当前角色的目标
            m_Target = value;
        }
    }
    /// <summary>
    /// 方向
    /// </summary>
    /// 特性:在Inspector面板中隐藏
    [HideInInspector]
    public int direction = 0;
    /// <summary>
    /// 等距坐标组件
    /// </summary>
    Iso iso;
    /// <summary>
    /// 动画组件
    /// </summary>
    Animator animator;

    /// <summary>
    /// 路径,表现为一个装坐标的容器
    /// </summary>
    List<Pathing.Step> path = new List<Pathing.Step>();

    /// <summary>
    /// 已经移动的距离
    /// </summary>
    float traveled = 0;

    /// <summary>
    /// 目标方向
    /// </summary>
    int targetDirection = 0;
    /// <summary>
    /// 是否正在攻击
    /// </summary>
    bool attack = false;
    /// <summary>
    /// 攻击动画(编号)
    /// </summary>
    int attackAnimation;
    /// <summary>
    /// 是否正在挨打
    /// </summary>
    bool takingDamage = false;
    /// <summary>
    /// 本角色的目标<物体或其他角色>
    /// </summary>
    GameObject m_Target;
    ///>>>>>>>>>>>>>>>>>>>>>下面这两位是被检测的,被检测到有互动组件
    /// <summary>
    /// 目标的互动组件
    /// </summary>
    Usable usable;
    /// <summary>
    /// 目标角色组件
    /// </summary>
    Character targetCharacter;

    private void Start()
    {
        //获取两个组件
        iso = GetComponent<Iso>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 寻路至...(生成路径)
    /// </summary>
    /// <param name="target">行至的目标</param>
    /// <param name="minRange">最小范围?</param>
    void PathTo(Vector2 target,float minRange = 0.1f)
    {
        //先放弃当前的行走动作(会保留最后一步做为path[0].pos)
        AbortMovement();
        //如果保留了最后一步,新路径起始坐标是path[0].pos.
        //如果路径为空,代表人物原本是静止的,起始坐标就是人物当前网格的坐标iso.tilePos
        Vector2 startPos = path.Count > 0 ? path[0].pos : iso.tilePos;
        //添加路径,生成路径,起始坐标是startPos,目标坐标是target,方向数量是directionCount,最小范围是minRange
        path.AddRange(Pathing.BuildPath(Iso.Snap(startPos), target, directionCount,minRange));
    }

    /// <summary>
    /// 使用/互动
    /// </summary>
    /// <param name="usable">使用的目标物</param>
    public void Use(Usable usable)
    {
        //正在攻击动作中就不能使用,直接返回
        if (attack) return;
        //生成路径,止步最小范围是互动范围
        PathTo(usable.GetComponent<Iso>().tilePos, useRange);
        //生成路径后把当前互动物置为现在使用的这个,因为生成路径会重置当前互动物体为空,所以放在后面
        this.usable = usable;
    }

    /// <summary>
    /// 行走至(生成路径,移动是尤Move()实现的)
    /// </summary>
    /// <param name="target">目标点(鼠标点击处)</param>
    public void GoTo(Vector2 target)
    {
        //如果正在执行攻击动作,则不能走,直接跳出
        if (attack) return;
        //生成路径
        PathTo(target);
    }

    /// <summary>
    /// 瞬移
    /// </summary>
    /// <param name="target">目标点</param>
    public void Teleport(Vector2 target)
    {
        if (attack) return;
        //判断目标网格是否可通行
        if (Tilemap.instance[target])
        {
            //可通行就直接瞬移
            iso.pos = target;
            iso.tilePos = target;
        }
        else
        {
            //不可通行就画路径,准备瞬移到按照寻路的规则的目标网格
            var pathToTarget = Pathing.BuildPath(Iso.Snap(iso.tilePos), target,directionCount);
            //路径不为空,为空就返回
            if (pathToTarget.Count == 0) return;
            //长度-1,就是路径容器中的最后一个路点,瞬移过去
            iso.pos = pathToTarget[pathToTarget.Count - 1].pos;
            iso.tilePos = iso.pos;
        }
        //既然是瞬移,就把路径清空
        path.Clear();
        //行走距离也清零
        traveled = 0;
    }

    /// <summary>
    /// 放弃行走动作
    /// </summary>
    private void AbortMovement()
    {
        //把关于路径的所有变量都清空
        m_Target = null;
        usable = null;
        targetCharacter = null;

        //如果路径还没走完,就走完当前这一步(一帧的行走距离)
        if(path.Count > 0)
        {
            //存路径的第一步的
            var firstStep = path[0];
            //清空路径
            path.Clear();
            //把第一步再添加到路径开头
            path.Add(firstStep);
        }

        //否则,即路径已经走完了,就把路径清空
        else
        {
            //路径清空
            path.Clear();
            //移动距离也清零
            traveled = 0;
        }
    }

    private void Update()
    {
        //画线人物站立的网格画线
        Iso.DebugDrawTile(iso.tilePos);
        //画路径线
        Pathing.DebugDrawPath(path);
        //移动角色
        Move();

        //>>>>>>>>>>>>>角色行为代码<<<<<<<<<<<<<<
        //行为的运作方式是在,诸如<行走>,<攻击>,<使用>等方法中给usable,targetCharacter字段赋值,当下面的代码检测到字段不为空时,就会执行相应的行为
        //当寻路结束
        if (path.Count == 0)
        {
            //如果目标有互动组件
            if (usable)
            {
                //如果目标的网格和角色的网格距离小于等于互动范围,就执行互动
                if (Vector2.Distance(usable.GetComponent<Iso>().tilePos, iso.tilePos) <= useRange) usable.Use();
                //执行完毕后,把目标置空
                usable = null;
            }
            //如果目标有角色组件
            if (targetCharacter)
            {
                //如果目标的网格和角色的网格距离小于等于攻击范围,就执行攻击
                Vector2 target = targetCharacter.GetComponent<Iso>().tilePos;
                if(Vector2.Distance(target,iso.tilePos) <= attackRange)
                {
                    //状态修改为攻击中
                    attack = true;
                    //随机赋值1-3用于选择攻击动画
                    attackAnimation = Random.Range(1, 3);
                    //获取到目标的方向的编号
                    direction = Iso.Direction(iso.tilePos, target, directionCount);
                }
                //执行完毕后,把目标置空
                targetCharacter = null;
            }
            //所有动作执行完毕后,把目标置空
            m_Target = null;
        }
        //更新个动画吧
        UpdateAnimation();
    }
    /// <summary>
    /// 移动角色
    /// </summary>
    private void Move()
    {
        //分支1.路径为空就返回
        if (path.Count == 0) return;


        //获取第一个路径
        Vector2 step = path[0].direction;
        //计算第一路径的长度;
        float stepLen = step.magnitude;

        //计算当前帧的移动距离
        float distance = speed * Time.deltaTime;

        //分支2.当下一帧要经过第一路径了,已走距离加上当前帧距离超过第一路径长度
        while (traveled + distance >= stepLen)
        {
            //算距离第一个路点的距离
            float firstPart = stepLen - traveled;
            //角色移动到第一路点,具体操作是把第一路径归一化乘以距离.归一化就是把路径变成方向,乘以长度后就会变成一端路径.
            iso.pos += step.normalized * firstPart;
            //当前帧距离,要减去上面角色移动掉的距离.
            distance -= firstPart;
            //更新已经移动了的距离(不明白为什么要减去第一条路径的长度)
            traveled += firstPart - stepLen;
            //更新角色所在网格的位置
            iso.tilePos += step;
            //第一段路径已经走完了,删除
            path.RemoveAt(0);
            //路径不为空就继续获取下面的路径
            if (path.Count > 0)
            {
                step = path[0].direction;
            }
        }
        //分支3.路径不为空就开走
        if (path.Count > 0)
        {
            traveled += distance;
            iso.pos += step.normalized * distance;
        }
        //分支4.上面代码执行完之后路径空了,转为待机
        if (path.Count == 0)
        {
            //坐标取整
            iso.pos.x = Mathf.Round(iso.pos.x);
            iso.pos.y = Mathf.Round(iso.pos.y);
            //移动距离归零
            traveled = 0;
        }
    }

    /// <summary>
    /// 更新动画
    /// </summary>
    private void UpdateAnimation()
    {
        //是否维持动画的时间进度,就是重新播放当前动画时,按照之前的进度继续播放
        bool preserveTime = false;
        //动画名称
        string animation;
        //给动画组件赋初始值
        animator.speed = 1.0f;
        //如果正在攻击
        if (attack)
        {
            //给动画名称赋值
            animation = "Attack" + attackAnimation;
            //给动画速度赋值
            animator.speed = attackSpeed;
        }

        //没有路径就是待机状态
        else if(path.Count == 0)
        {
            //给动画名赋值
            animation = "Idle";
            //此动画需要维持,时间进度
            preserveTime = true;
        }
        //否则就是行走
        else
        {
            //通过奔跑标签判断是跑还是走
            animation = run ? "Run" : "Walk";
            //此动画需要维持,时间进度
            preserveTime = true;
            //目标方向是路径的第一步的方向
            targetDirection = path[0].directionIndex;
        }

        //如果人物朝向和目标方向不一样,就转向
        if (!attack && direction != targetDirection)
        {

            //计算当前方向和目标方向的夹角,获取夹角的正负,正数就是顺时针,负数就是逆时针
            int diff = (int)Mathf.Sign(Tools.ShortestDelta(direction, targetDirection, directionCount));
            //平滑的更新当前方向,确保方向值在 [0, directionCount - 1] 范围内
            direction = (direction + diff + directionCount) % directionCount;
        }
        //动画名称加上方向的字符串
        animation += "_" + direction.ToString();
        //GetCurrentAnimatorStateInfo(0),返回动画状态的信息,0是层的索引,0层就是默认层,是当前动画的状态信息
        //IsName()判断当前动画名是否与形参相同,这里形参隐式转换为动画名
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(animation))
        {
            //如果要播放动画,与当前动画不同就播放
            //如果是维持动画进度的,就用这行,会按照先前的动画进度播放
            //形参1:动画名,形参2:层的索引(0就是当前动画),形参3:动画的归一化时间(就是当前播放进度了)
            if (preserveTime) animator.Play(animation, 0, animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            //不需要维持播放进度的动画,直接就从头开始播
            else animator.Play(animation);
        }
    }

    /// <summary>
    /// 观察(转向)
    /// </summary>
    /// <param name="target">鼠标</param>
    public void LookAt(Vector3 target)
    {
        //目标方向等于自身-目标的方向
        targetDirection = Iso.Direction(iso.tilePos, target,directionCount);
    }

    /// <summary>
    /// 攻击
    /// </summary>
    public void Attack()
    {
        //如果不在攻击中,人物朝向时目标,路径为空那么就开始攻击了
        if (!attack && direction == targetDirection && path.Count == 0)
        {
            //进入攻击状态
            attack = true;
            //随一个攻击动画,编号
            attackAnimation = Random.Range(1, 3);
        }
    }

    /// <summary>
    /// 攻击(重载,有目标版)
    /// </summary>
    /// <param name="targetCharacter">目标</param>
    public void Attack(Character targetCharacter)
    {
        //如果正在攻击就返回
        if (attack) return;
        //获取目标的Iso组件
        Iso targetIso = targetCharacter.GetComponent<Iso>();
        //行至目标出,以攻击范围做为止步范围
        PathTo(targetIso.tilePos, attackRange);
        //获取目标的角色组件
        this.targetCharacter = targetCharacter;
    }

    /// <summary>
    /// 挨打
    /// </summary>
    public void TakeDamage()
    {
        //挨打状态置为true
        takingDamage = true;
    }

    /// <summary>
    /// 在动画完成时执行的方法(不是系统的api哈,就是自己取得名字)
    /// </summary>
    void OnAnimationFinish()
    {
        //动画完成后,就把攻击状态和挨打状态置为false
        attack = false;
        takingDamage = false;
    }

    void OnAttack1Finish()
    {

    }
    void OnAttack2Finish()
    {

    }
}
