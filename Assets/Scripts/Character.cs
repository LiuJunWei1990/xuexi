using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    /// <summary>
    /// 方向数量
    /// </summary>
    [Tooltip("角色动画朝向数量")]
    public int directionCount = 8;
    /// <summary>
    /// 速度
    /// </summary>
    public float speed = 3.5f;
    /// <summary>
    /// 攻击速度
    /// </summary>
    public float attackSpeed = 1.0f;
    /// <summary>
    /// 是否在跑动
    /// </summary>
    [Tooltip("是否奔跑")]
    public bool run = false;
    /// <summary>
    /// 当前互动的物体
    /// </summary>
    ///特性:在Inspector面板中隐藏
    [HideInInspector]
    public Usable usable;
    /// <summary>
    /// 方向(存疑)
    /// </summary>
    /// 特性:在Inspector面板中隐藏
    [HideInInspector]
    public int direction = 0;
    /// <summary>
    /// 等距坐标类型
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

    private void Start()
    {
        //获取两个组件
        iso = GetComponent<Iso>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 使用/互动
    /// </summary>
    /// <param name="usable">使用的目标物</param>
    public void Use(Usable usable)
    {
        //准备要使用的物体和正在使用的物体时同一个就跳出,不需要执行任何代码.
        if (this.usable == usable) return;
        //生成走向物体的路径
        GoTo(usable.GetComponent<Iso>().tilePos);
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
        //生成路径之前,重置当前互动的物体
        this.usable = null;
        //////////////第一部分是清空原有路径,因为鼠标点了一个新目的地.

        //取当前游戏对象的等距坐标,在Iso类型里面取
        Vector2 startPos = iso.tilePos;
        //路径容器大于0,就是代表还有路点没走完(需要清空原有的路径)
        if(path.Count > 0)
        {
            //获取原第一个路径
            var firstStep = path[0];
            //坐标加路径等于目标点(原第一个路点)
            startPos = firstStep.pos;
            //清空路径
            path.Clear();
            //把原本的第一个路点优先添加到刚才已经清空的路径中,这样做的目的是为了避免人物突然卡住,飘逸,闪现的问题
            path.Add(firstStep);
        }
        else
        {
            //路点走完了就是待机状态,直接清空就行了.
            path.Clear();
            traveled = 0;
        }

        ////////////////第二部分,是创建新的路径

        //重新生成并添加路点,注意前面已经把firstStep加为第一个路点了.
        path.AddRange(Pathing.BuildPath(Iso.Snap(startPos), target,directionCount));
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

    private void Update()
    {
        //画线人物站立的网格画线
        Iso.DebugDrawTile(iso.tilePos);
        //画路径线
        Pathing.DebugDrawPath(path);
        //移动角色
        Move();

        //执行完当前帧的Move()后,如果路径为空,并且有当前互动的物体
        if(path.Count == 0 && usable)
        {
            //使用当前物体
            usable.Use();
            //当前物体设置为空
            usable = null;
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
        //计算方向,目标减去当前位置(等距)
        var dir = target - (Vector3)iso.pos;
        //计算角度,Vector3.Angle()计算两个向量之间的夹角,返回的是弧度,乘以Mathf.Sign()是为了判断正负,返回正1,负-1.
        var angle = Vector3.Angle(new Vector3(-1, -1), dir) * Mathf.Sign(dir.y - dir.x);
        //计算方向的度数,360除以方向数量
        var dierctionDegrees = 360.0f / directionCount;
        //目标方向是四舍五入的角度除以360乘以方向数量,取余方向数量
        targetDirection = Mathf.RoundToInt((angle + 360) % 360 / directionCount) % directionCount;
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
    /// 在动画完成时执行的方法(不是系统的api哈,就是自己取得名字)
    /// </summary>
    void OnAnimationFinish()
    {
        //是否攻击状态,在攻击状态就置否
        if (attack) attack = false;
    }

    void OnAttack1Finish()
    {

    }
    void OnAttack2Finish()
    {

    }
}
