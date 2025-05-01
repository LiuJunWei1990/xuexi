using System;
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
    public float useRange = 1f;
    /// <summary>
    /// 攻击范围
    /// </summary>
    [Tooltip("寻路至敌人会在这个距离停下并开始攻击")]
    public float attackRange = 1f;
    /// <summary>
    /// 角色模型直径
    /// </summary>
    [Tooltip("攻击/互动范围要加上这个直径的半径")]
    /// </summary>
    public float diameter = 1f;
    /// <summary>
    /// 姿态:奔跑
    /// </summary>
    [Tooltip("姿态:奔跑")]
    public bool run = false;
    /// <summary>
    /// 转向速度
    /// </summary>
    [Tooltip("人物转向的速度")]
    static float turnSpeed = 4f;
    /// <summary>
    /// 受到伤害委托
    /// </summary>
    /// <param name="orginator">施暴者</param>
    /// <param name="damage">伤害</param>
    public delegate void TakeDamageHandler(Character orginator, int damage);

    /// <summary>
    /// 受到伤害事件
    /// </summary>
    public event TakeDamageHandler OnTakeDamage;
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
    /// 角色的当前朝向(索引)
    /// </summary>
    /// 特性:在Inspector面板中隐藏
    [HideInInspector]
    public int directionIndex = 0;
    /// <summary>
    /// 角色的当前朝向(矢量)
    /// </summary>
    float direction = 0;
    /// <summary>
    /// 等距坐标组件
    /// </summary>
    Iso iso;
    /// <summary>
    /// 动画组件
    /// </summary>
    IsoAnimator animator;
    /// <summary>
    /// 渲染器组件
    /// </summary>
    SpriteRenderer spriteRenderer;
    /// <summary>
    /// 路径,表现为一个装坐标的容器,存的是Step<步>
    /// </summary>
    List<Pathing.Step> path = new List<Pathing.Step>();

    /// <summary>
    /// 当前步的已经移动的距离
    /// </summary>
    float traveled = 0;

    /// <summary>
    /// 预定的方向,角色应该面对的朝向;为了动画平滑,人物朝向会缓慢的改变至预定方向
    /// </summary>
    int desiredDirection = 0;
    /// <summary>
    /// 是否正在移动
    /// </summary>
    bool moving = false;
    /// <summary>
    /// 是否正在攻击
    /// </summary>
    bool attack = false;
    /// <summary>
    /// 是否正在挨打
    /// </summary>
    bool takingDamage = false;
    /// <summary>
    /// 是否正在死亡(播放死亡动画中)
    /// </summary>
    bool dying = false;
    /// <summary>
    /// 是否死亡(死亡动画播放完)
    /// </summary>
    bool dead = false;
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
    /// <summary>
    /// 攻击力
    /// </summary>
    public int attackDamage = 30;
    /// <summary>
    /// 生命值
    /// </summary>
    public int health = 100;
    /// <summary>
    /// 最大生命值
    /// </summary>
    public int maxHealth = 100;
    /// <summary>
    /// 目标的坐标点
    /// </summary>
    Vector2 targetPoint;

    private void Awake()
    {
        //获取组件
        iso = GetComponent<Iso>();
        animator = GetComponent<IsoAnimator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    /// <summary>
    /// 使用/互动
    /// </summary>
    /// <param name="usable">使用的目标物</param>
    public void Use(Usable usable)
    {
        //正在攻击动作中就不能使用,直接返回
        if (attack || takingDamage || dying || dead) return;
        //赋值目标的坐标
        targetPoint = usable.GetComponent<Iso>().pos;
        //生成路径后把当前互动物置为现在使用的这个,因为生成路径会重置当前互动物体为空,所以放在后面
        this.usable = usable;
        //既然是互动,删除目标角色
        targetCharacter = null;
        //进入行走中姿态
        moving = true;
    }

    /// <summary>
    /// 行走...至
    /// 点击地板或怪物巡逻调用的方法,通过赋值targetPoint来实现
    /// </summary>
    /// <param name="target">目标点</param>
    public void GoTo(Vector2 target)
    {
        //进入行走中姿态
        moving = true;
        //给目标的坐标点赋值
        targetPoint = target;
        //可互动物体和目标角色都置为空
        usable = null;
        targetCharacter = null;
    }
    /// <summary>
    /// 瞬移
    /// </summary>
    /// <param name="target">目标点</param>
    public void Teleport(Vector2 target)
    {
        if (attack || takingDamage) return;
        //判断目标网格是否可通行
        if (Tilemap.Passable(target))
        {
            //可通行就直接瞬移
            iso.pos = target;
        }
        else
        {
            //不可通行就画路径,准备瞬移到按照寻路的规则的目标网格
            var pathToTarget = Pathing.BuildPath(iso.pos, target,directionCount);
            //路径不为空,为空就返回
            if (pathToTarget.Count == 0) return;
            //长度-1,就是路径容器中的最后一个路点,瞬移过去
            iso.pos = pathToTarget[pathToTarget.Count - 1].pos;
        }
        //既然是瞬移,就把路径清空
        path.Clear();
        //行走距离也清零
        traveled = 0;
        //离开行走中姿态
        moving = false;
    }
    /// <summary>
    /// 攻击
    /// </summary>
    public void Attack(Vector3 target)
    {
        //如果不在攻击中,人物朝向时目标,路径为空那么就开始攻击了
        if (!dead && !dying && !attack && !takingDamage && directionIndex == desiredDirection && !moving)
        {
            //进入攻击姿态
            attack = true;
            //给目标坐标点赋值
            targetPoint = target;
        }
    }
    /// <summary>
    /// 攻击(重载,有目标版)
    /// </summary>
    /// <param name="targetCharacter">目标</param>
    public void Attack(Character targetCharacter)
    {
        //如果正在攻击就返回
        if (attack || takingDamage || dead || dying) return;
        //获取目标的Iso组件
        Iso targetIso = targetCharacter.GetComponent<Iso>();
        //获取目标的坐标
        targetPoint = targetIso.pos;
        //获取目标的角色组件
        this.targetCharacter = targetCharacter;
        //由于有了攻击目标可互动目标置空
        usable = null;
        //进入行走中姿态
        moving = true;
    }
    /// <summary>
    /// 放弃当前路径
    /// </summary>
    private void AbortPath()
    {
        //把关于路径的所有变量都清空
        m_Target = null;
        //路径清空
        path.Clear();
        //移动距离也清零
        traveled = 0;
    }

    private void Update()
    {
        //>>>>>>>>>>>>>角色行为代码<<<<<<<<<<<<<<
        //行为的运作方式是在,诸如<行走>,<攻击>,<使用>等方法中给usable,targetCharacter字段赋值,当下面的代码检测到字段不为空时,就会执行相应的行为
        //当没有挨打,死亡,死亡中姿态时,执行
        if (!takingDamage && !dead &&!dying)
        {
            //如果目标有互动组件
            if (usable)
            {
                //如果目标的网格和角色的网格距离小于等于互动范围,就执行
                //打瓦片版射线,自身>>可互动物体,最大长度为互动范围+直径/2;;;maxRayLength:(命名参数)代表指定这个新参赋值给maxRayLength:;;也可以单纯做一个装饰,提升代码可读性;;ignore射线忽略角色自身
                var hit = Tilemap.Raycast(iso.pos, usable.GetComponent<Iso>().pos, maxRayLength: useRange + diameter / 2, ignore: gameObject);
                //如果打中了物体(代表物体不可通行,不可通行代表是可互动状态)
                if(hit.gameObject == usable.gameObject)
                {
                    //执行可互动目标的互动方法
                    usable.Use();
                    //离开行走中姿态
                    moving = false;
                    //执行完毕后,把目标置空
                    usable = null;
                    m_Target = null;
                }
            }
            //如果目标有角色组件
            if (targetCharacter && !attack)
            {
                //获取目标角色的坐标点
                Vector2 target = targetCharacter.GetComponent<Iso>().pos;
                //如果目标和角色的距离 <= 攻击范围 + 角色直径 / 2 + 目标直径 / 2,就执行攻击
                if (Vector2.Distance(target, iso.pos) <= attackRange + diameter / 2 + targetCharacter.diameter / 2)
                {
                    //离开行走中姿态
                    moving = false;
                    //状态修改为攻击中
                    attack = true;
                    //获取到目标的方向的编号
                    LookAtImmidietly(target);
                }
            }
        }
        //朝目标移动
        MoveToTargetPoint();
        //更新个朝向吧
        Turn();
    }

    void LateUpdate()
    {
        UpdateAnimation();
    }
    /// <summary>
    /// 转头
    /// 每帧更新一次
    /// </summary>
    void Turn()
    {
        //不再死亡|死亡中|攻击中|挨打中|当前朝向不等于预定的朝向,就执行
        if(!dead &&!dying &&!attack &&!takingDamage && directionIndex != desiredDirection)
        {
            //Tools.ShortestDelta计算两个方向的角度差,如果是顺时针,返回值为正,逆时针为负
            float diff = Tools.ShortestDelta(directionIndex, desiredDirection, directionCount);
            //获取绝对值,去掉顺逆时针,只保留角度
            float delta = Mathf.Abs(diff);
            // 当前帧转向的角度 : (转向方向顺逆 * 转向速度 * 每帧事件 * 朝向数量)
            //Mathf.Clamp(..., -delta, delta): 限制形参1角度差的范围,防止超出形参2-形参3的范围
            //direction += ...: 把当前帧的角度差加到当前朝向上
            direction += Mathf.Clamp(Mathf.Sign(diff) * turnSpeed * Time.deltaTime * directionCount, -delta, delta);
            //取余数,防止超出方向数量
            direction = Tools.Mod(direction + directionCount, directionCount);
            //取整,获得最终的方向索引
            directionIndex = Mathf.RoundToInt(direction);
        }
    }
    /// <summary>
    /// 沿着路径移动角色
    /// </summary>
    private void MoveAlongPath()
    {
        //分支1.路径为空,攻击中,挨打中,死亡中,死亡了,就直接返回
        if (path.Count == 0 ||!moving || attack || takingDamage || dead || dying) return;


        //获取当前步
        Vector2 step = path[0].direction;
        //计算当前步的长度;
        float stepLen = step.magnitude;

        //计算当前帧的移动距离
        float distance = speed * Time.deltaTime;

        //分支2.如果当步已移动距离+当前帧的移动距离,超出了当前的步长就循环
        while (traveled + distance >= stepLen)
        {
            //算距离当前步的坐标点的距离
            float firstPart = stepLen - traveled;
            //角色移动到当前步的坐标点,具体操作是把当前步归一化(变成方向),乘以距离就成了到当前步坐标点的向量,加上角色当前坐标,就成了当前步的坐标了.
            Vector2 newPos = iso.pos + step.normalized * firstPart;
            //最后复制给角色坐标
            iso.pos = newPos;
            //因为前面移动掉了一部分,所以当前帧的移动距离要减去掉的那部分
            distance -= firstPart;
            //重置当前步的已移动距离firstPart - stepLen = -traveled
            //traveled += -traveled = 0;
            //这样的操作应该是担心到了float类型的精度问题,所以用这种方式来重置
            //当前步的已移动距离归零
            traveled += firstPart - stepLen;
            //当前步已经走完了,删除
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
            //移动距离归零
            traveled = 0;
        }
        //分支5.如果以上条件皆不成立,那么预定的朝向就是当前路径的朝向
        else
        {
            desiredDirection = path[0].directionIndex;
        }
    }

    /// <summary>
    /// 移动到目标点
    /// 处于没有路径的状态下,又有目标点,就移动到目标点,会在每一步的间隙中生效是移动更顺滑并且不会半路中断了
    /// </summary>
    void MoveToTargetPoint()
    {
        //如果不再行走中姿态就直接跳出
        if(!moving) return;

        //新建上一帧坐标点变量,先保存当前帧坐标点
        var prevPos = iso.pos;

        //瓦片的射线检测,角色当前位置>>>目标位置,射线最大长度2f,射线忽略自身
        bool directlyAccesible = !Tilemap.Raycast(iso.pos, targetPoint, maxRayLength: 2.0f, ignore: gameObject);
        //没打中就代表可通行,执行下面代码
        if(directlyAccesible)
        {
            //获取角色坐标到目标点的方向
            var dir = (targetPoint - iso.pos).normalized;
            //当前帧的移动距离
            float distance = speed * Time.deltaTime;
            //当前帧的移动
            iso.pos += dir * distance;
            //获取预定方向
            desiredDirection = Iso.Direction(iso.pos, targetPoint, directionCount);
        }
        //否则,就是不能直接移动到目标点,那就A*寻路
        else
        {
            //生成路径
            var newPath = Pathing.BuildPath(iso.pos, targetPoint, directionCount);
            //如果当前路径或者新路径未空,又或者两个路径的终点一致,那就执行下面的代码
            if(path.Count == 0 || newPath.Count == 0 || newPath[newPath.Count - 1].pos != path[path.Count - 1].pos)
            {
                //放弃当前路径
                AbortPath();
                //把新路径赋值给当前路径
                path.AddRange(newPath);
            }
            if(path.Count == 0) moving = false;

            Pathing.DebugDrawPath(iso.pos, path);
            //沿路径移动
            MoveAlongPath();
        }
        //到了这一步,能让角色移动的分支都执行完了
        //新建一个网格对象,赋值角色上一帧坐标的网格
        var cell = Tilemap.GetCell(prevPos);
        //前一帧网格的对象就是角色自身,代表网格状态没重置
        if (cell.gameObject == gameObject)
        {
            //重置网格状态
            cell.passable = true;
            cell.gameObject = null;
            //把新状态设置给容器中的对应网格
            Tilemap.SetCell(prevPos, cell);
        }
        //新建一个新网格对象,赋值角色当前站立的网格
        var newCell = Tilemap.GetCell(iso.pos);
        //网格如果为可通行,代表网格状态没更新
        if (newCell.passable)
        {
            //更新网格状态
            newCell.passable = false;
            newCell.gameObject = gameObject;
            //把新状态设置给容器中的对应网格
            Tilemap.SetCell(iso.pos, newCell);
        }        
        //如果没有互动对象和目标角色,并且距离目标点小于1.这种情况就是纯行走到了目的地,那么就离开行走中姿态
        if(usable == null && targetCharacter == null && Vector2.Distance(iso.pos, targetPoint) < 1f)
        {
            moving = false;
        }
    }
    /// <summary>
    /// 更新动画
    /// </summary>
    private void UpdateAnimation()
    {
        //动画名称
        string animation;
        //给动画组件赋初始值
        animator.speed = 1.0f;
        //如果死亡
        if (dying || dead)
        {
            //给动画名称赋值
            animation = "Death";
        }
        //如果正在攻击
        else if (attack)
        {
            //给动画名称赋值
            animation = "Attack";
            //给动画速度赋值
            animator.speed = attackSpeed;
        }
        //如果正在挨打
        else if (takingDamage)
        {
            //给动画名称赋值
            animation = "TakeDamage";
        }

        //没有路径就是待机状态
        else if(moving)
        {
            //给动画名赋值
            animation = run ? "Run" : "Walk";
        }
        //否则就是行走
        else
        {
            animation = "Idle";
        }
        animator.SetState(animation);
    }

    /// <summary>
    /// 注释(转向)
    /// 面向目标
    /// </summary>
    /// <param name="target">鼠标</param>
    public void LookAt(Vector3 target)
    {
        //如果不再行走中,目标方向 = 自身-目标的方向
        if(!moving) desiredDirection = Iso.Direction(iso.pos, target,directionCount);
    }
    /// <summary>
    /// 立刻注释(转向)
    /// 立刻面向目标不进行平滑处理
    /// </summary>
    /// <param name="target"></param>
    public void LookAtImmidietly(Vector3 target)
    {
        directionIndex = desiredDirection = Iso.Direction(iso.pos, target, directionCount);
    }
    /// <summary>
    /// 挨打
    /// </summary>
    /// <param name="originator">打人者</param>
    /// <param name="damage">伤害</param>
    public void TakeDamage(Character originator,int damage)
    {
        //每次动画播放时执行该方法

        //生命减去伤害
        health -= damage;
        //如果还有生命
        if (health > 0)
        {
            if(OnTakeDamage != null) OnTakeDamage(originator, damage);
            //挨打状态置是
            takingDamage = true;
            attack = false;
        }
        //如果生命打光了
        else
        {
            //目标方向(索引) = 人物朝向(索引) = 当前对象 向着 打人者的方向(索引)
            LookAtImmidietly(originator.iso.pos);
            //死亡状态置是
            dying = true;
            //攻击状态置否
            attack = false;
        }
        moving = false;
        targetCharacter = null;
    }
    /// <summary>
    /// 在动画播放时执行的方法(不是系统的api哈,就是自己取得名字)
    /// </summary>
    void OnAnimationMiddle()
    {
        //如果正在攻击
        if (attack)
        {
            if (targetCharacter == null)
            {
                var hit = Tilemap.Raycast(iso.pos, targetPoint, rayLength: diameter / 2 + attackRange, ignore: gameObject, debug: true);
                if (hit.gameObject != null)
                {
                    targetCharacter = hit.gameObject.GetComponent<Character>();
                }
            }
            //如果目标角色不为空
            if (targetCharacter)
            {
                Vector2 target = targetCharacter.GetComponent<Iso>().pos;
                if (Vector2.Distance(target, iso.pos) <= attackRange + diameter / 2 + targetCharacter.diameter / 2)
                {
                    targetCharacter.TakeDamage(this, attackDamage);
                }
                targetCharacter = null;                
                //目标也置空
                m_Target = null;
            }
        }
    }

    /// <summary>
    /// 在动画完成时执行的方法(不是系统的api哈,就是自己取得名字)
    /// </summary>
    void OnAnimationFinish()
    {
        //动画完成后,就把攻击状态和挨打状态置为false
        attack = false;
        takingDamage = false;
        //如果是死亡中(动画播放中),这是动画结束事件,所以就转入死亡状态
        if (dying)
        {
            //把渲染器的排序层名称改为"OnFloor"
            spriteRenderer.sortingLayerName = "OnFloor";
            //死亡中状态为否
            dying = false;
            //死亡状态置为true
            dead = true;
        }
        UpdateAnimation();
    }
}
