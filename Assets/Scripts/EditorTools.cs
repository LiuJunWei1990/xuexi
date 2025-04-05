using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EditorTools : MonoBehaviour
{
    /// <summary>
    /// 添加菜单项目:创建16向动画
    /// </summary>
    [MenuItem("Assets/Create/16向动画")]
    static public void CreateAnimation16Way()
    {
        CreateAnimation(16);
    }

    /// <summary>
    /// 添加菜单项目:创建8向动画
    /// </summary>
    [MenuItem("Assets/Create/8向动画")]
    static public void CreateAnimation8Way()
    {
        CreateAnimation(8);
    }
    /// <summary>
    /// 生成动画
    /// </summary>
    /// <param name="directionCount">X向动画</param>
    static public void CreateAnimation(int directionCount)
    {

        
        #region >>>>>>>>>>>>>将图片资源提取为精灵数组<<<<<<<<<<<<<<<<<<<

        //1.引用编辑器中选中的对象,强转为图片类型,如果选中的不是图片,返回null
        var texture = Selection.activeObject as Texture2D;
        //2.获取图片对象的文件路径
        var texturePath = AssetDatabase.GetAssetPath(texture);
        //分割路径字符串,获取文件夹名(这一步到最后才用到)
        string dir = texturePath.Split('/')[2];
        //3.加载路径下的所有资源,并转换为Sprite类型的数组
        //详解一下每段代码
        //AssetDatabase.LoadAllAssetsAtPath(texturePath)读texturePath路径下所有文件,包括图片文件下所有精灵
        //OfType<Sprite>()支取所有文件中的精灵
        //OrderBy(s => s.name.Length)主排序,按精灵的名字的长度
        //.ThenBy(s => s.name)次排序,名字长度一致的按名字排序(字母顺序)
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().OrderBy(s => s.name.Length).ThenBy(s => s.name).ToArray();

        #endregion



        #region >>>>>>>>>>>>>>>把整个精灵数组分割成多个朝向,行为的动画文件<<<<<<<<<<<<<<<<<<

        //计算每个方向动作的帧数,由于每个动作帧数是相等的,所以可以直接除以方向数
        int framesPerAnimation = sprites.Length / directionCount;
        //动画事件名,是动画文件的名字
        var eventName = texture.name;

        //遍历所有方向
        for (int i = 0; i < directionCount; ++i)
        {
            //给动画文件取个名字，材质文件名+方向编号,例如walk_0
            var name = texture.name + "_" + i.ToString();
            //方向编号赋值
            int direction = i;
            //如果是8向动画，精灵的顺序，需要将动画反转。
            //实现方法就是加一半也就是4，然后再除以本身也就是8取余。
            //0--4,1--5,2--6,3--7,4--0,5--1,6--2,7--3,刚好是反转的效果
            if (directionCount == 8) direction = (direction + 4) % directionCount;
            //获取当前方向的所有动画帧
            //sprites.Skip(direction * framesPerAnimation) --- 跳过形参数量的元素,形参是方向编号乘以帧数,就是当前方向的第一帧
            //Take(framesPerAnimation) --- 取出形参数量的元素,就是当前方向的所有帧
            //ToArray() --- 转换为数组
            Sprite[] animSprites = sprites.Skip(direction * framesPerAnimation).Take(framesPerAnimation).ToArray();
            //生成不同朝向的动画路径
            var assetPath = "Assets/Animations/" + dir + "/" + name + ".anim";
            //加载路径下的AnimationClip文件并赋值给animationClip,这是加载单个文件的,与上面加载一堆文件的不同
            var animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            //为空,代表加载失败了,要创建一个新的
            if(animationClip == null)
            {
                //初始化
                animationClip = new AnimationClip();
                //在路径下生成这个文件
                AssetDatabase.CreateAsset(animationClip, assetPath);
            }
            //给赋值
            animationClip.name = name;
            animationClip.frameRate = 12;
            //更新动画文件
            FillAnimationClip(animationClip, animSprites, eventName);
        }

        #endregion
    }

    /// <summary>
    /// 更新精灵动画剪辑,就是动画文件
    /// </summary>
    /// 这里生成的是一个动画动作,比如walk_0,walk_1,walk_2,walk_3,walk_4,walk_5
    /// AnimationClip就是Unity的动画文件,可以用于播放动画,文件后缀是.anim
    /// <param name="clip">动画文件的类型</param>
    /// <param name="sprites">精灵数组</param>
    /// <param name="eventName">动画事件</param>
    /// <returns>动画变量AnimationClip,可以生成为动画文件</returns>
    static private void FillAnimationClip(AnimationClip clip, Sprite[] sprites, string eventName)
    {
        #region >>>>>>>>>>>>>>>>生成动画文件并赋一些基本的值<<<<<<<<<<<<<<<<<<<<<<

        //计算帧数,就是数组的长度
        int frameCount = sprites.Length;
        //1除以动画帧数,等到每帧的时间长度
        float frameLength = 1f / clip.frameRate;

        #endregion



        #region >>>>>>>>>>>>>>>>确定动画文件可以绑定的属性(就是面板属性里的框框,可以直接拖文件上去的那个)<<<<<<<<<<<<<<<<<<<<<<

        //EditorCurveBinding代表编辑器面板上的一个属性,可以绑定动画文件
        EditorCurveBinding curveBinding = new EditorCurveBinding();
        //设置绑定的对象(组件)
        curveBinding.type = typeof(SpriteRenderer);
        //设置绑定的属性
        curveBinding.propertyName = "m_Sprite";


        #region >>>>>>>>>>>>>>>>设定动画的关键帧(实际每一帧都设定了)<<<<<<<<<<<<<<<<<<<<<<

        //声明一个关键帧数组,长度就是帧数长度
        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            //声明一个关键帧
            ObjectReferenceKeyframe kf = new ObjectReferenceKeyframe();
            //0,0.083,0.166,0.25,0.333,0.416,0.5,0.583,0.666,0.75,0.833,0.916,每一帧都是关键帧
            kf.time = i * frameLength;  // 设置关键帧的时间
            kf.value = sprites[i];  // 设置关键帧的值
            keyFrames[i] = kf;  // 将关键帧添加到关键帧数组中
        }
        //清除当前动画引用中的帧(曲线),注意这是前面新建的一个AnimationClip,这个稳妥起见添加帧之前先清除一遍
        clip.ClearCurves();
        //将关键帧数组绑定到动画文件上
        //AnimationUtility.SetObjectReferenceCurve是 Unity 动画系统的核心方法之一，用于将对象引用类型的关键帧（如 Sprite、Material 等）绑定到动画
        //clip    AnimationClip 要修改的目标动画文件
        //curveBinding EditorCurveBinding  属性绑定信息（可以绑定哪个对象的什么属性）
        //keyFrames ObjectReferenceKeyframe[]   对象引用关键帧数组
        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);

        #endregion


        #endregion


        #region >>>>>>>>>>>>>>>>>>>处理动画文件的隐藏属性,正常属性其实也可以设置都包含了<<<<<<<<<<<<<<<<<<<<<<<<
        //SerializedObject访问隐藏/正常属性
        //修改 Unity 内置组件的隐藏的参数（如 m_IsActive）
        SerializedObject serializedClip = new SerializedObject(clip);
        //自定义类用于处理动画文件的设置
        //形参返回了一个SerializedProperty对象,这个对象是动画文件的属性
        AnimationClipSettings clipSettings = new AnimationClipSettings(serializedClip.FindProperty("m_AnimationClipSettings"));
        clipSettings.loopTime = true; // 设置循环时间
        serializedClip.ApplyModifiedProperties(); // 应用修改
        #endregion

        //这个方法的作用是,把事件添加到动画文件,还可以像下面这样写成数组的方式添加多个事件
        AnimationUtility.SetAnimationEvents(clip, new[]
        {
            new AnimationEvent()
            {
                //动画事件的时间是动画长度,那就是末尾
                time = clip.length,
                //事件调用的方法名称functionName
                functionName = "On"+eventName+"Finish"
            },
            new AnimationEvent()
            {
                //同上
                time = clip.length,
                functionName = "OnAnimationFinish"
            },
        }
        );
    }
}


/// <summary>
/// 定义一个 AnimationClipSettings 类，用于操作动画剪辑的设置
/// </summary>
/// 序列化属性:Unity的属性面板上的属性,可以通过SerializedObject类来访问
class AnimationClipSettings
{
    #region >>>>>>>>>>>>>>>>>初始化的部分,在这完成了根属性的引用<<<<<<<<<<<<<<<<<<<<<<

    // 引用了动画文件的m_AnimationClipSettings属性,它是所有属性的根属性
    SerializedProperty m_Property;

    /// <summary>
    /// 获取指定属性,如m_StartTime,m_StopTime等,用在类型属性的get和set方法里
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    private SerializedProperty Get(string property)
    {
        //通过根属性，用于查找嵌套属性
        return m_Property.FindPropertyRelative(property);
    }

    /// <summary>
    /// 构造函数,这一个引用的根属性
    /// </summary>
    /// <param name="prop">serializedClip.FindProperty("m_AnimationClipSettings")用来找根属性</param>
    public AnimationClipSettings(SerializedProperty prop)
    {
        m_Property = prop; // 存储根属性引用，后续所有操作都基于这个属性
    }

    #endregion

    #region 各种根属性的获取和设置
    // === 时间控制属性 ===
    public float startTime
    {
        get { return Get("m_StartTime").floatValue; } // 获取起始时间
        set { Get("m_StartTime").floatValue = value; } // 设置起始时间
    }

    public float stopTime
    {
        get { return Get("m_StopTime").floatValue; }  // 获取结束时间
        set { Get("m_StopTime").floatValue = value; } // 设置结束时间
    }

    // === 动画偏移属性 ===
    public float orientationOffsetY
    {
        get { return Get("m_OrientationOffsetY").floatValue; } // 获取Y轴旋转偏移
        set { Get("m_OrientationOffsetY").floatValue = value; } // 设置Y轴旋转偏移
    }

    public float level
    {
        get { return Get("m_Level").floatValue; } // 获取层级值
        set { Get("m_Level").floatValue = value; } // 设置层级值
    }

    public float cycleOffset
    {
        get { return Get("m_CycleOffset").floatValue; } // 获取循环偏移
        set { Get("m_CycleOffset").floatValue = value; } // 设置循环偏移
    }

    // === 循环控制属性 ===
    public bool loopTime
    {
        get { return Get("m_LoopTime").boolValue; } // 获取是否循环
        set { Get("m_LoopTime").boolValue = value; } // 设置是否循环
    }

    public bool loopBlend
    {
        get { return Get("m_LoopBlend").boolValue; } // 获取是否混合循环
        set { Get("m_LoopBlend").boolValue = value; } // 设置是否混合循环
    }

    // === 混合模式属性 ===
    public bool loopBlendOrientation
    {
        get { return Get("m_LoopBlendOrientation").boolValue; } // 获取方向混合
        set { Get("m_LoopBlendOrientation").boolValue = value; } // 设置方向混合
    }

    public bool loopBlendPositionY
    {
        get { return Get("m_LoopBlendPositionY").boolValue; } // 获取Y轴位置混合
        set { Get("m_LoopBlendPositionY").boolValue = value; } // 设置Y轴位置混合
    }

    public bool loopBlendPositionXZ
    {
        get { return Get("m_LoopBlendPositionXZ").boolValue; } // 获取XZ平面位置混合
        set { Get("m_LoopBlendPositionXZ").boolValue = value; } // 设置XZ平面位置混合
    }

    // === 原始状态保留属性 ===
    public bool keepOriginalOrientation
    {
        get { return Get("m_KeepOriginalOrientation").boolValue; } // 获取是否保留原始旋转
        set { Get("m_KeepOriginalOrientation").boolValue = value; } // 设置是否保留原始旋转
    }

    public bool keepOriginalPositionY
    {
        get { return Get("m_KeepOriginalPositionY").boolValue; } // 获取是否保留原始Y位置
        set { Get("m_KeepOriginalPositionY").boolValue = value; } // 设置是否保留原始Y位置
    }

    public bool keepOriginalPositionXZ
    {
        get { return Get("m_KeepOriginalPositionXZ").boolValue; } // 获取是否保留原始XZ位置
        set { Get("m_KeepOriginalPositionXZ").boolValue = value; } // 设置是否保留原始XZ位置
    }

    // === 特殊效果属性 ===
    public bool heightFromFeet
    {
        get { return Get("m_HeightFromFeet").boolValue; } // 获取是否从脚部计算高度
        set { Get("m_HeightFromFeet").boolValue = value; } // 设置是否从脚部计算高度
    }

    public bool mirror
    {
        get { return Get("m_Mirror").boolValue; } // 获取是否镜像动画
        set { Get("m_Mirror").boolValue = value; } // 设置是否镜像动画
    }

    #endregion
}