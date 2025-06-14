using System.Collections.Generic;
using UnityEngine;

class COFAnimator : MonoBehaviour
{
    /// <summary>
    /// 单个游戏对象的COF文件数据
    /// </summary>
    COF cof;
    /// <summary>
    /// 角色动画朝向(索引)
    /// </summary>
    public int direction = 0;
    /// <summary>
    /// 动画是否循环
    /// </summary>
    public bool loop = true;

    float time = 0;
    float speed = 1.0f;
    /// <summary>
    /// 动作的帧率
    /// </summary>
    float frameDuration = 1.0f / 12.0f;
    /// <summary>
    /// 帧计数器
    /// </summary>
    /// <remarks>
    /// 记录当前动作帧的索引
    /// </remarks>
    int frameCounter = 0;
    /// <summary>
    /// 帧的总数
    /// </summary>
    /// <remarks>
    /// 和Unity的动画切片的帧数是一个东西
    /// </remarks>
    int frameCount = 0;
    /// <summary>
    /// 帧的起始位置
    /// </summary>
    /// <remarks>
    /// 应该也是和Unity动画系统里面的类似,代表这个动作的动画是在第几帧开始的
    /// </remarks>
    int frameStart = 0;
    /// <summary>
    /// 游戏对象的图层容器
    /// </summary>
    /// <remarks>
    /// 储存游戏对象所有图层(用来挂spriteRenderer组件的子节点)
    /// </remarks>
    List<Layer> layers = new List<Layer>();

    /// <summary>
    /// 游戏对象图层,结构体
    /// </summary>
    /// <remarks>
    /// 游戏对象用来显示图像的那个子节点(用来挂spriteRenderer组件的子节点)(貌似可以有多个)
    /// </remarks>
    struct Layer
    {
        public SpriteRenderer spriteRenderer;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <remarks>
    /// 更新游戏对象图层容器配置
    /// </remarks>
    void Start()
    {
        UpdateConfiguration();
    }

    /// <summary>
    /// 设置新的COF文件数据
    /// </summary>
    /// <param name="cof"></param>
    public void SetCof(COF cof)
    {
        this.cof = cof;
        //设置之后
        UpdateConfiguration();
    }

    /// <summary>
    /// 设置动作动画的范围
    /// </summary>
    /// <param name="start">起始帧</param>
    /// <param name="count">该动作的总帧数</param>
    public void SetFrameRange(int start, int count)
    {
        //赋值
        frameStart = start;
        frameCount = count != 0 ? count : cof.framesPerDirection;
        //更新游戏对象图层容器
        UpdateConfiguration();
    }

    /// <summary>
    /// 更新游戏对象图层容器
    /// </summary>
    /// <remarks>
    /// 初始化时间,帧计数器,帧的总数; 如果cof文件数据比容器里多,就先开辟好新的游戏对象图层(不赋值图像信息),保证数量一致
    /// </remarks>
    void UpdateConfiguration()
    {
        //如果cof为空,则返回
        if (cof == null)
            return;
        
        //初始化时间
        time = 0;
        //初始化帧计数器
        frameCounter = 0;
        //如果帧的总数为0,就给它赋值 = 动画动作的帧数
        if (frameCount == 0)
            frameCount = cof.framesPerDirection;

        //如果cof文件存储的游戏对象图层比容器里的多,那就新增(只新增未赋值)
        for (int i = layers.Count; i < cof.layerCount; ++i)
        {
            //新增游戏对象图层
            Layer layer = new Layer();
            GameObject layerObject = new GameObject();
            layerObject.transform.position = new Vector3(0, 0, -i * 0.1f);
            layerObject.transform.SetParent(gameObject.transform, false);
            layer.spriteRenderer = layerObject.AddComponent<SpriteRenderer>();
            layer.spriteRenderer.sortingOrder = Iso.SortingOrder(gameObject.transform.position);
            //将新增的游戏对象图层添加到容器中
            layers.Add(layer);
        }
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <remarks>
    /// 计时,用来控制动画的播放进度和触发事件
    void Update()
    {
        //动画不循环并且计数器超过动作帧的总数,则返回
        if (!loop && frameCounter >= frameCount)
            return;
        //记录时间
        time += Time.deltaTime * speed;
        //Update时间,到了动画的每帧时间索引就+1,到事件节点还会执行动画事件(现在不会执行,因为组件不齐)
        while (time >= frameDuration)
        {
            time -= frameDuration;
            if (frameCounter < frameCount)
                frameCounter += 1;
            if (frameCounter == frameCount / 2)
                SendMessage("OnAnimationMiddle", SendMessageOptions.DontRequireReceiver);
            if (frameCounter == frameCount)
            {
                SendMessage("OnAnimationFinish", SendMessageOptions.DontRequireReceiver);
                if (loop)
                    frameCounter = 0;
            }
        }
    }

    /// <summary>
    /// 后期更新
    /// </summary>
    void LateUpdate()
    {
        if (cof == null)
            return;
        
        //遍历当前COF文件中的所有动画数据
        for (int i = 0; i < cof.layerCount; ++i)
        {
            //取出游戏对象图层
            Layer layer = layers[i];

            //帧索引 = 帧计数器,不能超过动作帧的总数
            int frameIndex = Mathf.Min(frameCounter, frameCount - 1);
            // 
            int layerIndex = cof.priority[(direction * cof.framesPerDirection * cof.layerCount) + (frameIndex * cof.layerCount) + i];
            var cofLayer = cof.layers[layerIndex];
            //读取对应的DCC文件
            var dcc = DCC.Load(cofLayer.dccFilename);

            //精灵索引 = 方向 * 动作帧数 + 帧起始位置 + 帧索引
            int spriteIndex = direction * dcc.framesPerDirection + frameStart + frameIndex;
            //刷新当前游戏对象图层的精灵
            layer.spriteRenderer.sprite = dcc.sprites[spriteIndex];
        }
    }
}
