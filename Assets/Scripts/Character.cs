using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色组件
/// </summary>
/// <remarks>
/// 表驱动,update通过扫描人物属性来判断是否执行对应的方法
/// </remarks>
/// <example>
/// 受害者属性:Update中根据目标的组件来判断,互动还是攻击
/// 姿态属性:Update中根据人物的姿态来执行动作和动画,移动,攻击,受击,死亡,死亡中
/// 
/// ----------------------------------------------------------------------------------------
/// 修改表[方法]:这类方法会修改人物的属性,然后Update会根据属性来判断是否执行对应的方法
/// 应用表[方法]:这类方法会更具人物的属性来判断是否执行对应的动作和动画
/// 表参数[字段]:即Character的所有字段,属性;例如Pathing类本身没有实例,向[表参数]path提供值得,会给它标上[表参数]
/// 也有即修改也应用的方法
/// </example>
public class Character : MonoBehaviour {
    /// <summary>
    /// 角色的朝向 -- 朝向属性1
    /// </summary>
    /// <remarks>
    /// 用于寻路,动画的相关赋值
    /// ||| 方向索引对应表
    /// 0: 左上（(-1, -1)）
    /// 1: 左（(-1, 0)）
    /// 2: 左下（(-1, 1)）
    /// 3: 下（(0, 1)）
    /// 4: 右下（(1, 1)）
    /// 5: 右（(1, 0)）
    /// 6: 右上（(1, -1)）
    /// 7: 上（(0, -1)）
    /// </remarks>
    public int directionCount = 8;
    /// <summary>
    /// 角色的移动速度 -- 移动属性5
    /// </summary>
	public float speed = 3.5f;
    /// <summary>
    /// 角色的攻击速度 -- 动画属性4/战斗属性1
    /// </summary>
    /// <remarks>
    /// 这个属性作用于攻击动画的播放速度,动画会触发攻击
    /// </remarks>
	public float attackSpeed = 1.0f;
    /// <summary>
    /// 互动范围 -- 移动属性1
    /// </summary>
    public float useRange = 1f;
    /// <summary>
    /// 攻击范围 -- 移动属性2
    /// </summary>
    public float attackRange = 1f;
    /// <summary>
    /// 角色直径 -- 移动属性3
    /// </summary>
    public float diameter = 1f;
    /// <summary>
    /// 是否奔跑 -- 移动属性4/动画属性3
    /// </summary>
    public bool run = false;
    /// <summary>
    /// 角色转向速度 -- 朝向属性5
    /// </summary>
    static float turnSpeed = 4f; // full rotations per second
    /// <summary>
    /// 挨打委托
    /// </summary>
    /// <param name="originator"></param>
    /// <param name="damage"></param>
    /// <remarks>
    /// 在傀儡控制器中被赋值响应,回调时OnTakeDamage方法
    /// </remarks>
    public delegate void TakeDamageHandler(Character originator, int damage);
    /// <summary>
    /// 挨打事件
    /// </summary>
    /// <remarks>
    /// 在傀儡控制器中被赋值响应,回调时OnTakeDamage方法
    /// </remarks>
    public event TakeDamageHandler OnTakeDamage;

    /// <summary>
    /// 目标[属性]
    /// </summary>
    /// <remarks>
    /// m_Target[字段]的壳子  
    /// set时会自动调用Use()或Attack();
    /// </remarks>
    [HideInInspector] //隐藏
    public GameObject target
    {
        get
        {
            return m_Target;
        }
        //取到哪个组件就调对应方法, 没有就返回
        set
        {
            var usable = value.GetComponent<Usable>();
            if (usable != null)
                Use(usable);
            else
            {
                var targetCharacter = value.GetComponent<Character>();
                if (targetCharacter) {
                    Attack(targetCharacter);
                }
                else
                {
                    return;
                }
            }
            //不管取没取到都赋值
            m_Target = value;
        }
    }
    /// <summary>
    /// 角色实际朝向的方向(索引) -- 朝向属性2
    /// </summary>
    /// <remarks>
    /// 会在Update中逐步的朝期望的方向desiredDirection[字段]变化
    /// </remarks>
    [HideInInspector]
	public int directionIndex = 0;
    /// <summary>
    /// 角色实际朝向的方向(浮点版) -- 朝向属性3
    /// </summary>
    /// <remarks>
    /// directionIndex的浮点版,directionIndex是由本字段计算来的,使用本字段使转向更加平滑
    /// </remarks>
    float direction = 0;
    /// <summary>
    /// 角色的坐标(等距) -- 移动属性8
    /// </summary>
    /// <remarks>
    /// 等距坐标,并不直接作用于角色,而是通过iso.pos来赋值给角色的transform.position
    /// </remarks>
    Iso iso;
    /// <summary>
    /// 角色的动画组件 -- 动画属性1
    /// </summary>
	IsoAnimator animator;
    /// <summary>
    /// 角色的渲染组件 -- 动画属性2
    /// </summary>
    SpriteRenderer spriteRenderer;
    /// <summary>
    /// 角色的路径 -- 移动属性6
    /// </summary>
    List<Pathing.Step> path = new List<Pathing.Step>();
    /// <summary>
    /// 记录一次步骤中,已经走过的距离 -- 移动属性7
    /// </summary>
	float traveled = 0;
    /// <summary>
    /// 角色期望朝向的方向(索引) -- 朝向属性4
    /// </summary>
    /// <remarks>
    /// 使转向更平滑,一般需要变更朝向会赋值这个字段,然后实际朝向会逐步向这个方向变化
    /// ||| 方向索引对应表
    /// 0: 左上（(-1, -1)）
    /// 1: 左（(-1, 0)）
    /// 2: 左下（(-1, 1)）
    /// 3: 下（(0, 1)）
    /// 4: 右下（(1, 1)）
    /// 5: 右（(1, 0)）
    /// 6: 右上（(1, -1)）
    /// 7: 上（(0, -1)）
    /// </remarks>
	int desiredDirection = 0;
    /// <summary>
    /// 移动中[姿态] -- 姿态属性2
    /// </summary>
    bool moving = false;
    /// <summary>
    /// 攻击[姿态] -- 姿态属性1
    /// </summary>
	bool attack = false;
    /// <summary>
    /// 受击[姿态] -- 姿态属性3
    /// </summary>
    bool takingDamage = false;
    /// <summary>
    /// 死亡中[姿态] -- 姿态属性4
    /// </summary>
    bool dying = false;
    /// <summary>
    /// 死亡[姿态] -- 姿态属性5
    /// </summary>
    bool dead = false;
    /// <summary>
    /// 目标[字段] -- 受害者属性1
    /// </summary>
    GameObject m_Target;
    /// <summary>
    /// 目标的互动组件 -- 受害者属性2
    /// </summary>
    Usable usable;
    /// <summary>
    /// 目标的角色组件 -- 受害者属性3
    /// </summary>
    Character targetCharacter;
    /// <summary>
    /// 攻击力 -- 战斗属性2
    /// </summary>
    public int attackDamage = 30;
    /// <summary>
    /// 生命值 -- 战斗属性3
    /// </summary>
    public int health = 100;
    /// <summary>
    /// 最大生命值 -- 战斗属性4
    /// </summary>
    public int maxHealth = 100;
    /// <summary>
    /// 目标的坐标(等距) -- 受害者属性4
    /// </summary>
    Vector2 targetPoint;



    /// <summary>
    /// 初始化
    /// </summary>
    /// <remarks>
    /// 获取组件
    /// </remarks>
    void Awake()
    {
		iso = GetComponent<Iso>();
		animator = GetComponent<IsoAnimator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    /// <summary>
    /// 设定互动对象
    /// </summary>
    /// <param name="usable">受害者的互动组件</param>
    /// <remarks>
    /// [修改表]把相应的目标属性修改为受害者的,Update刷新时会移动并与受害者互动
    /// </remarks>
	public void Use(Usable usable) {
        if (attack || takingDamage || dying || dead)
            return;
        targetPoint = usable.GetComponent<Iso>().pos;
		this.usable = usable;
        targetCharacter = null;
        moving = true;
    }
    /// <summary>
    /// 设定移动点位
    /// </summary>
    /// <param name="target">要移动到的目标坐标</param>
    /// <remarks>
    /// [修改表]仅保留目标点赋值,Update刷新时会移动至目标点
    /// </remarks>
    public void GoTo(Vector2 target)
    {
        if (attack || takingDamage || dying || dead)
            return;

        moving = true;
        targetPoint = target;
        usable = null;
        targetCharacter = null;
    }
    /// <summary>
    /// 实施瞬移
    /// </summary>
    /// <param name="target">目标点位(等距)</param>
    /// <remarks>
    /// [应用表]严格算不在表驱动范围内,目标单元格可通行就直接穿,不可通行就传到离目标点最近的位置,结束后离开行走中[姿态]
    /// </remarks>
    public void Teleport(Vector2 target) {
		if (attack || takingDamage)
			return;

		if (Tilemap.Passable(target)) {
			iso.pos = target;
		} 
        //目标点不可通行,那就找到里目标最近的点传送
        else 
        {
			var pathToTarget = Pathing.BuildPath(iso.pos, target, directionCount);
			if (pathToTarget.Count == 0)
				return;
			iso.pos = pathToTarget[pathToTarget.Count - 1].pos;
		}
        //清空路径
		path.Clear();
		traveled = 0;
        moving = false;
	}
    /// <summary>
    /// 设定强制攻击的坐标点
    /// </summary>
    /// <param name="target">被攻击的坐标点</param>
    /// <remarks>
    /// [修改表]进入攻击[姿态],设置强制攻击的坐标点,Update时会刷新攻击动作
    /// </remarks>
    public void Attack(Vector3 target)
    {
        if (!dead && !dying && !attack && !takingDamage && directionIndex == desiredDirection && !moving)
        {
            attack = true;
            targetPoint = target;
        }
    }
    /// <summary>
    /// 设定攻击对象
    /// </summary>
    /// <param name="targetCharacter">被攻击的游戏对象</param>
    /// <remarks>
    /// [修改表]进入行走中[姿态]这个攻击的重载是攻击角色的,Update时会根据条件刷新进入攻击动作
    /// </remarks>
    public void Attack(Character targetCharacter)
    {
        if (attack || takingDamage || dead || dying)
            return;

        Iso targetIso = targetCharacter.GetComponent<Iso>();
        targetPoint = targetIso.pos;
        this.targetCharacter = targetCharacter;
        usable = null;
        moving = true;
    }
    /// <summary>
    /// 放弃寻路
    /// </summary>
    /// <remarks>
    /// [修改表]放弃寻路,清空路径[字段]
    /// </remarks>
    void AbortPath()
    {
        m_Target = null;
        path.Clear();
        traveled = 0;
    }
    /// <summary>
    /// 更新
    /// </summary>
    /// <remarks>
    /// [应用表][每帧调用]应用表的核心,根据角色属性来判断是否执行对应的动作
    /// </remarks>
	void Update() {
        if (!takingDamage && !dead && !dying) {
            //互动动作,每帧打射线,打中就停下来互动
            if (usable)
            {
                var hit = Tilemap.Raycast(iso.pos, usable.GetComponent<Iso>().pos, maxRayLength: useRange + diameter / 2, ignore: gameObject);
                if (hit.gameObject == usable.gameObject)
                {
                    usable.Use();
                    moving = false;
                    usable = null;
                    m_Target = null;
                }
            }
            //有目标的攻击(非强制攻击),就进入攻击姿态
            if (targetCharacter && !attack)
            {
                Vector2 target = targetCharacter.GetComponent<Iso>().pos;
                if (Vector2.Distance(target, iso.pos) <= attackRange + diameter / 2 + targetCharacter.diameter / 2)
                {
                    moving = false;
                    attack = true;
                    LookAtImmidietly(target);
                }
            }
        }

        MoveToTargetPoint();
        Turn();
	}
    /// <summary>
    /// 后期更新
    /// </summary>
    /// <remarks>
    /// [应用表][每帧调用]应用表的另一核心,根据角色属性来判断是否执行对应的动画
    /// </remarks>
    void LateUpdate()
    {
        UpdateAnimation();
    }
    /// <summary>
    /// 转向
    /// </summary>
    /// <remarks>
    /// [应用表][每帧调用]实际朝向索引, 按照旋转速度, 朝期望朝向索引转动一帧的角度
    /// </remarks>
    void Turn()
    {
        if (!dead && !dying && !attack && !takingDamage && directionIndex != desiredDirection)
        {
            //计算两个方向索引之间的差值(逆时针为负数)
            float diff = Tools.ShortestDelta(directionIndex, desiredDirection, directionCount);
            //计算这个差值的绝对值
            float delta = Mathf.Abs(diff);
            //direction += 当前帧的旋转角度(范围不超过±差值绝对值)
            direction += Mathf.Clamp(Mathf.Sign(diff) * turnSpeed * Time.deltaTime * directionCount, -delta, delta);
            //对朝向模运算,避免超出方向总数
            direction = Tools.Mod(direction + directionCount, directionCount);
            //取个整再取余,得到实际朝向索引
            directionIndex = Mathf.RoundToInt(direction) % directionCount;
        }
    }
    /// <summary>
    /// 按着路径寻路
    /// </summary>
    /// <remarks>
    /// [应用表][每帧调用]如果当前帧会超过步骤长度,本帧就只走完当前步骤,并且轮换下一步骤,其他情况就正常叠数值就行
    /// </remarks>
	void MoveAlongPath() {
        //路径空了就返回
		if (path.Count == 0 || !moving || attack || takingDamage || dead || dying)
			return;
        //读取当前步骤
		Vector2 step = path[0].direction;
        //步骤长度
		float stepLen = step.magnitude;
        //每帧能走的长度
        float distance = speed * Time.deltaTime;
        //如果当前帧会超过步骤长度,本帧就只走完当前步骤,并且轮换下一步骤
		while (traveled + distance >= stepLen) {
            //计算并走到步骤的终点
			float firstPart = stepLen - traveled;
            Vector2 newPos = iso.pos + step.normalized * firstPart;
            iso.pos = newPos;
            //当前帧的移动距离,要去掉刚才走掉的距离
			distance -= firstPart;
            //清空当前步骤已走的距离
			traveled += firstPart - stepLen;
            //删除当前步骤
			path.RemoveAt(0);
            //如果还有路径载入下一步骤
            if (path.Count > 0)
            {
                step = path[0].direction;
            }
		}
        //如果还有路径,就正常走一步
		if (path.Count > 0) {
			traveled += distance;
			iso.pos += step.normalized * distance;
		}
        //没有路径了,步骤已走过距离清零
        if (path.Count == 0) {
			traveled = 0;
		}
        //BUG兜底,如果到了这里,至少保证人物朝向与最后寻路的方向一致
        else
        {
            desiredDirection = path[0].directionIndex;
        }
    }
    /// <summary>
    /// 移动到目标点
    /// </summary>
    /// <remarks>
    /// [应用表][每帧调用]这个版本的代码摒弃了直接移动,全部由生成路径移动了
    /// </remarks>
    void MoveToTargetPoint()
    {
        //[分支1]不再行走中姿态,就直接反回
        if (!moving)
            return;
        //尝试另外生成一个新路径
        var newPath = Pathing.BuildPath(iso.pos, targetPoint, directionCount);
        //[分支2]自身与目标点之间生成不出路径,就直接返回
        if (newPath.Count == 0)
        {
            moving = false;
            return;
        }
        //[分支3]原路径为空,新生成路径不为空,就用新路径
        if (path.Count == 0 || newPath[newPath.Count - 1].pos != path[path.Count - 1].pos)
        {
            AbortPath();
            path.AddRange(newPath);
        }
        //画路径的辅助线(鼠标点击后的)
        Pathing.DebugDrawPath(iso.pos, path);
        //[分支4]目标点与自身距离小于1,向目标点走一帧,准备停止移动
        if (path.Count == 1 && Vector2.Distance(path[0].pos, targetPoint) < 1.0f)
        {
            var dir = (targetPoint - iso.pos).normalized;
            iso.pos += dir * Time.deltaTime * speed;
            desiredDirection = Iso.Direction(iso.pos, targetPoint, directionCount);
        }
        //[分支5]以上条件都不是就正常按路径寻路
        else
        {
            MoveAlongPath();
        }
    }
    /// <summary>
    /// 更新动画
    /// </summary>
    /// <remarks>
    /// [应用表][每帧调用]根据角色属性来判断是否执行对应的动画, 没有任何姿态那就待命动画,现版本这个动画系统貌似只用于玩家了
    /// </remarks>
    void UpdateAnimation() {
        string animation;
		animator.speed = 1.0f;
        if (dying || dead)
        {
            animation = "Death";
        }
        else if (attack)
        {
            animation = "Attack";
			animator.speed = attackSpeed;
        }
        else if (takingDamage)
        {
            animation = "TakeDamage";
        }
        else if (moving)
        {
            animation = run ? "Run" : "Walk";
        }
        else
        {
            animation = "Idle";
        }

        animator.SetState(animation);
    }
    /// <summary>
    /// 注释
    /// </summary>
    /// <param name="target"></param>
    /// <remarks>
    /// [应用表][每帧调用]修改期望朝向,在update里实际朝向逐步转向至期望朝向
    /// </remarks>
	public void LookAt(Vector3 target)
    {
        if (!moving)
            desiredDirection = Iso.Direction(iso.pos, target, directionCount);
    }
    /// <summary>
    /// 立刻注视
    /// </summary>
    /// <param name="target">目标</param>
    /// <remarks>
    /// [应用表]立即转向,用于攻击和被杀死时,有即使性的要求,所以不再逐步转向
    /// </remarks>
    public void LookAtImmidietly(Vector3 target)
    {
        directionIndex = desiredDirection = Iso.Direction(iso.pos, target, directionCount);
    }
    /// <summary>
    /// 挨打
    /// </summary>
    /// <param name="originator">施暴者</param>
    /// <param name="damage">伤害</param>
    /// <remarks>
    /// [动画事件调用]扣血; 傀儡控制器触发反击; 离开攻击姿态(这个作用可能是后续打断施法用的,试不了这个版本怪物没有挂脚本,懒得试)
    /// </remarks>
    public void TakeDamage(Character originator, int damage)
    {
        health -= damage;
        if (health > 0)
        {
            // 回调正在挨打事件,只有怪物角色会生效,因为只有傀儡控制器会给这个事件赋值响应
            if (OnTakeDamage != null)
                OnTakeDamage(originator, damage);
            takingDamage = true;
            attack = false;
        }
        //死了尸体立即注视施暴者
        else
        {
            LookAtImmidietly(originator.iso.pos);
            dying = true;
            attack = false;
        }
        moving = false;
        targetCharacter = null;
    }
    /// <summary>
    /// 动画中期事件,回调方法
    /// </summary>
    /// <remarks>
    /// [应用表][动画事件调用]动画播放到一半就调用这个方法,这个方法用于攻击,被攻击,死亡等,这个版本中只有攻击和死亡
    /// </remarks>
    void OnAnimationMiddle()
    {
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

            if (targetCharacter)
            {
                Vector2 target = targetCharacter.GetComponent<Iso>().pos;
                if (Vector2.Distance(target, iso.pos) <= attackRange + diameter / 2 + targetCharacter.diameter / 2)
                {
                    //实际的攻击是在这个事件中调用的
                    targetCharacter.TakeDamage(this, attackDamage);
                }
                targetCharacter = null;
                m_Target = null;
            }
        }
        // 死了就修改下图层,躺地板上
        if (dying)
        {
            spriteRenderer.sortingLayerName = "OnFloor";
        }
    }
    /// <summary>
    /// 动画结尾事件,回调方法
    /// </summary>
    /// <remarks>
    /// [应用表][动画事件调用]动画播放到结束就调用这个方法, 用于: 结束攻击, 被攻击姿态; 死亡中姿态转入死亡姿态; 更新下一个动作动画
    /// </remarks>
    void OnAnimationFinish() {
        attack = false;
        takingDamage = false;
        if (dying)
        {
            dying = false;
            dead = true;
        }
        UpdateAnimation();
    }
}
