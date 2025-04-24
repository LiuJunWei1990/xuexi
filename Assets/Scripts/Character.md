# Character代码说明
## 一. 字段:
### 公开
|字段名|类型|特性|说明|
|:----:|:--:|:--:|:--:|
|directionCount|int|[Tooltip("")]|角色的(行动动画 和 A*寻路)有多少朝向(8或者16)|
|direction|int|[Tooltip("")]|角色的当前朝向(0-7)或(0-15)|
|speed|float|[Tooltip("")]|角色的移动速度|
|attackSpeed|float|[Tooltip("")]|角色的攻击速度>>影响攻击动画的播放速度>>动画事件触发对方挨打方法>>以次实质的攻击速度也提升了|
|useRange|float|[Tooltip("")]|角色的互动范围,被互动对象进入这个距离寻路止步并开始互动|
|attackRange|float|[Tooltip("")]|角色的攻击范围,对受害者进入这个距离寻路止步并开始攻击|
|diameter|float|[Tooltip("")]|角色的直径,攻击/互动范围要加上这个直径的半径|
|run|bool|[Tooltip("")]|姿态:奔跑,姿态做为人物当前行为的标识,用于判断播放走路动画还是奔跑动画|
|TakeDamageHandler|delegate|无|委托:受到伤害|
|OnTakeDamage|event|无|事件:受到伤害|
### 私有
|字段名|类型|特性|说明|
|:----:|:--:|:--:|:--:|
|iso|Iso|无|角色的Iso组件|
|animator|Animator|无|角色的Animator组件|
|spriteRenderer|SpriteRenderer|无|角色的SpriteRenderer组件|
|usable|Usable|无|角色的Usable组件|
|targetCharacter|Character|无|角色目标的Character组件|
|path|List<Pathing.Step>|无|角色的寻路路径,存的是<步>,每帧一步|
|traveled|float|无|角色的已走距离,用于寻路,每次点击地图重新计算|
|desiredDirection|int|无|<待补充>|
|moving|bool|无|姿态:行走,姿态做为人物当前行为的标识,用于判断人物是否在移动中|
|attacking|bool|无|姿态:攻击,姿态做为人物当前行为的标识,用于判断人物是否在攻击中|
|takingDamage|bool|无|姿态:挨打,姿态做为人物当前行为的标识,用于判断人物是否在挨打中|
|dying|bool|无|姿态:死亡中,姿态做为人物当前行为的标识,用于判断人物是否在死亡中|
|dead|bool|无|姿态:死亡,姿态做为人物当前行为的标识,用于判断人物是否已经死亡|
|m_Target|GameObject|无|角色的目标,私有外部调用通过target[属性]实现|
|attackDamage|int|无|角色的攻击伤害|
|health|int|无|角色的生命值|
|maxHealth|int|无|角色的最大生命值|
|targetPoint|Vector2|无|角色的目标的坐标点|
## 二. 属性:
### 公开
|属性名|类型|特性|说明|
|:----:|:--:|:--:|:--:|
|target|GameObject|[HideInInspector]|角色的目标(属性),私有字段m_Target的外壳,在set时判断目标是物体还是人并调用相应方法|
## 三. 方法:
### 公开
|方法名|返回值|参数|说明|引用说明|
|:----:|:--:|:--:|:--:|:--:|
|PathTo|void|Vector2 target,float minRange = 0.1f|调用Pathing.BuildPath生成路径,把路径添加到path字段里面|GoTo,Attack,Use方法|
|GoTo|void|Vector2 target|调用PathTo生成路径|target[属性]的set方法|
|Attack|void|无|强制攻击按键触发的版本,直接进入攻击姿态|单击右键 或者 单击左键+左Shift|
|Attack|void|Character targetCharacter|寻路至目标,并将目标赋值给this.targetCharacter,当Update检测该值不为空时,靠近目标会发动攻击|target[属性]的set方法;DummyController的OnTakeDamage挨打事件|
|TakeDamage|void|Character originator,int damage|挨打,被攻击动画事件触发,扣血>>触发挨打事件/死亡>>行走姿态置否,如果是怪物,挨打事件还会执行攻击方法|target[属性]的set方法|
|GoToSmooth|void|Vector2 target|并未使用过的方法,把目标引用赋值给目标的坐标|
|Teleport|void|Vector2 target|瞬移,如果目标点可通行(是状态,不是能不能走过去),就直接瞬移,否则寻路,并瞬移到离目标最近的点|PlayerController.cs>>F4按键|
|LookAt|void|Vector3 target|面向目标,获取自身和目标之间的朝向索引,赋值给预定的方向|PlayerController的Update|
|LookAtImmidietly|void|Vector3 target|立即面向目标,获取自身和目标之间的朝向索引,赋值给当前的方向;这是用于攻击目标时用的,即可面向目标|Update方法|


### 私有
|方法名|返回值|参数|说明|引用说明|
|:----:|:--:|:--:|:--:|:--:|
|Awake|void|无|初始化了各种组件|
|Update|void|无|1.画路径线  2.按生成的路径移动角色  3.检测是否触发角色行为[攻击/互动]  4.更新朝向|
|LateUpdate|void|无|更新动画|
|AbortMovement|void|无|放弃移动,把目标信息和路径清空,如果路径没走完,保留下一步|PathTo方法|
|Turn|void|无|更新朝向,当前坐标每帧向目标坐标的方向+1索引|Update方法|
|MoveAlongPath|void|无|沿着路径移动角色,根据不同的状态给出5种相应的处理|Update方法|
|MoveToTargetPoint|void|无|强行移动到目标点,做为一个BUG的保底,理论上是不会触发的,因为它必须在路径不为空,并且不再行走状态下在能运行|Update方法|
|UpdateAnimation|void|无|更新动画,根据人物姿态把姿态传递给动画状态机|Update方法,OnAnimationFinish方法|
|OnAnimationMiddle|void|无|动画中间事件触发的方法:暂时只有一个功能,如果在攻击姿态下,对目标执行挨打方法|动画中间事件|
|OnAnimationFinish|void|无|动画结尾事件触发的方法:1.攻击姿态置否 2.挨打姿态置否 3.如果姿态是死亡中,转为死亡姿态,尸体图层 4.更新动画|动画结束事件|