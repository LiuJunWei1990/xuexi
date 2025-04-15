using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
//这个命名空间会使脚本只在编辑器模式下运行,在游戏打包后不会被打包进游戏
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;

/// <summary>
/// Unity 编辑器扩展工具集
/// </summary>
public class EditorTools : MonoBehaviour
{
    ///// <summary>
    ///// 添加菜单项目:创建16向动画
    ///// </summary>
    //[MenuItem("Assets/Create/16向动画")]
    //static public void CreateAnimation16Way()
    //{
    //    CreateAnimation(16);
    //}

    ///// <summary>
    ///// 添加菜单项目:创建8向动画
    ///// </summary>
    //[MenuItem("Assets/Create/8向动画")]
    //static public void CreateAnimation8Way()
    //{
    //    CreateAnimation(8);
    //}


    /// <summary>
    /// 生成动画文件
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="directionCount">朝向数</param>
    /// <param name="dirOffset">朝向偏移</param>
    /// <param name="loop">动画是否循环</param>
    /// <returns>动画切片数组,动画切片就是资源里那个三角型的动画文件,是一个动画动作(如:向右下角攻击)</returns>
    static public AnimationClip[] CreateAnimation(Texture2D texture,int directionCount, int dirOffset, bool loop)
    {

        
        #region >>>>>>>>>>>>>将图片资源提取为精灵数组<<<<<<<<<<<<<<<<<<<
        //1.get图片文件的路径
        var spritesPath = AssetDatabase.GetAssetPath(texture);
        //1.新建一个切片数组
        var clips = new AnimationClip[directionCount];
        //3.加载路径下的所有资源,并转换为Sprite类型的数组
        //详解一下每段代码
        //AssetDatabase.LoadAllAssetsAtPath(texturePath)读spritesPath路径下所有文件,包括图片文件下所有精灵(返回GameObject)
        //OfType<Sprite>()筛选出Sprite类型的文件,返回Sprite类型的数组
        //OrderBy(s => s.name.Length)主排序,按精灵的名字的长度
        //.ThenBy(s => s.name)次排序,名字长度一致的按名字排序(字母顺序)
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(spritesPath).OfType<Sprite>().OrderBy(s => s.name.Length).ThenBy(s => s.name).ToArray();

        #endregion



        #region >>>>>>>>>>>>>>>把整个精灵数组分割成多个朝向,行为的动画文件<<<<<<<<<<<<<<<<<<

        //计算每个方向动作的帧数,由于每个动作帧数是相等的,所以可以直接除以方向数
        int framesPerAnimation = sprites.Length / directionCount;

        //取路径的文件名
        string textureName = ExtractFileName(spritesPath);
        //遍历所有方向
        for (int i = 0; i < directionCount; ++i)
        {
            //给动画文件取个名字，材质文件名+方向编号,例如walk_0
            var name = textureName + "_" + i.ToString();

            //生成方向编号,dirOffset是偏移量,比如是8方向,加上偏移量余出来也是0-7,八个方向
            int direction = (i + dirOffset) % directionCount;
            //获取当前方向的所有动画帧
            //sprites.Skip(direction * framesPerAnimation) --- 跳过形参数量的元素,形参是方向编号乘以帧数,就是当前方向的第一帧
            //Take(framesPerAnimation) --- 取出形参数量的元素,就是当前方向的所有帧
            //ToArray() --- 转换为数组
            Sprite[] animSprites = sprites.Skip(direction * framesPerAnimation).Take(framesPerAnimation).ToArray();
            clips[i] = new AnimationClip();
            var animationClip = clips[i];
            //给赋值
            animationClip.name = name;
            animationClip.frameRate = 12;
            //更新动画文件
            FillAnimationClip(animationClip, animSprites,loop);
        }

        #endregion
        return clips;
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
    static private void FillAnimationClip(AnimationClip clip, Sprite[] sprites,bool loop)
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
        clipSettings.loopTime = loop; // 设置循环播放
        serializedClip.ApplyModifiedProperties(); // 应用修改
        #endregion

        //这个方法的作用是,把事件添加到动画文件,还可以像下面这样写成数组的方式添加多个事件
        AnimationUtility.SetAnimationEvents(clip, new[]
        {
            new AnimationEvent()
            {
                //动画事件的时间是动画长度,那就是末尾
                time = clip.length / 2,
                //事件调用的方法名称functionName
                functionName = "OnAnimationMiddle"
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
    /// <summary>
    /// 取文件名
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>文件名</returns>
    static public string ExtractFileName(string path)
    {
        //提取文件名,去掉路径
        //按'/'分割路径,Last()是取最后一段,再按'.'分割,取第0段,就是文件名
        return path.Split('/').Last().Split('.')[0];
    }

    [MenuItem("Assets/Create/Iso Animation动画切片生成器")]
    static public void CreateAnimation()
    {
       ScriptableObjectUtility.CreateAsset<IsoAnimation>();
    }
}
/// <summary>
/// 定义一个IsoAnimation类,用于存储动画相关的属性
/// 它继承自ScriptableObject(编辑对象类),这个类对应资源文件，用来储存参数类似于XML，不过它可以直接调用，还会在Inspector面板上显示
/// 这个类可以直接在Inspector面板上显示,可以直接在Inspector面板上修改属性,但是要在Editor文件夹下
/// 这里没有放在Editor下，而是用特性实现了相同的效果
/// </summary>
//特性:字段会显示在Inspector面板上
[System.Serializable]
class IsoAnimation : ScriptableObject
{
    //朝向数量
    public int directionCount = 8;
    //朝向偏移
    public int directionOffset = 0;
    //是否循环播放动画
    public bool loop = true;
    //动画文件引用(编辑器面板上那个框框)
    public Texture2D[] texture;
    //动画状态机引用(编辑器面板上那个框框)
    public AnimatorController controller;
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
/// <summary>
/// 编辑对象工具包
/// 定义一个ScriptableObjectUtility类,用于创建ScriptableObject资源
/// </summary>
public static class ScriptableObjectUtility
{
    /// <summary>
    /// 创建一个ScriptableObject资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <returns>返回创建的资源</returns>
    public static T CreateAsset<T>() where T : ScriptableObject
    {
        //创建一个ScriptableObject资源实例
        T asset = ScriptableObject.CreateInstance<T>();
        //获取路径，形参是编辑器当前选中的目标
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        //如果没有选中对象,就创建到Assets根目录下
        if (path == "")
        {
            path = "Assets";
        }
        //返回路径的文件扩展名后缀 如果没有后缀,就返回空字符串
        //不为空就代表选中目标是个文件
        else if (Path.GetExtension(path) != "")
        {
            //把路径的文件名去掉,只剩下路径
            //AssetDatabase.GetAssetPath(Selection.activeObject)>>获取当前目标路径
            //Path.GetFileName>>获取路径的文件名(包括后缀)
            //path.Replace>>替换成空字符串
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }
        //>>>>如果上面的if都不成立,说明选中的是文件夹,就直接返回路径<<<<<

        
        //创建文件路径,尝试创建一个路径,如果路径存在就给文件名后面加个1,2,3.....
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New" + typeof(T).ToString() + ".asset");
        //>>>>>下面是创建文件的四个必要步骤
        //创建资源文件(文件的变量,文件路径)
        AssetDatabase.CreateAsset(asset, assetPathAndName);
        //确保写入磁盘
        AssetDatabase.SaveAssets();
        //刷新资源,保证新文件正确显示
        AssetDatabase.Refresh();
        //激活资源窗口
        EditorUtility.FocusProjectWindow();
        //当前选择项改为刚才创建的文件
        Selection.activeObject = asset;
        //返回创建的资源 
        return asset;
    }
}

/// <summary>
/// IosAnimation的编辑器
/// 给IosAnimation添加一个编辑器
/// 编辑器类型Editor
/// 用来给资产添加选项,例子就是图片文件的选项那样,不是脚本,是直接在资源文件上添加选项
/// </summary>
[CustomEditor(typeof(IsoAnimation))]
public class IsoAnimationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //绘制默认面板
        //就是把IsoAnimation的public属性都加到面板上
        DrawDefaultInspector(); 
        //获取当前正在被编辑的IsoAnimation
        var isoAnimation = target as IsoAnimation; 
        //添加一个生成按钮,按钮宽度占满
        if(GUILayout.Button("生成"))
        {
            //获取IsoAnimation文件的路径
            var assetPath = AssetDatabase.GetAssetPath(isoAnimation);
            //新建字典,映射了动画文件和名字
            var existingClips = new Dictionary<string, AnimationClip>();
            //遍历IsoAnimation文件中的所有动画文件,并添加到字典中
            //AssetDatabase.LoadAllAssetsAtPath(assetPath)读取isoAnimation文件同路径下的所有文件,范围Object[]
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                //强转赋值
                var clip = obj as AnimationClip;
                //如果clip不为空,就添加到字典中(强转失败就是空)
                if(clip) existingClips.Add(clip.name,clip);
            }
            //如果动画状态机为空,就在指定路径新建一个动画状态机
            if(isoAnimation.controller == null) isoAnimation.controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/" + isoAnimation.name + ".controller");
            //修改动画状态机的名字,使其和IsoAnimation文件的名字一致
            isoAnimation.controller.name = isoAnimation.name;
            //如果动画状态机没有层,就添加一个层(状态机里面那个图层)
            if(isoAnimation.controller.layers.Length < 1) isoAnimation.controller.AddLayer("Layer 1");
            //遍历IsoAnimation文件的texture数组(Texture2D图片文件数组),并生成动画文件
            foreach(var texture in isoAnimation.texture)
            {
                //读取每个图片文件下的精灵,生成动画文件,返回动画文件数组
                var clips = EditorTools.CreateAnimation(texture,isoAnimation.directionCount,isoAnimation.directionOffset,isoAnimation.loop);
                //遍历动画文件数组
                foreach(var clip in clips)
                {

                    //如果字典中有这个动画
                    if(existingClips.ContainsKey(clip.name))
                    {
                        //把新动画文件的参数覆盖字典中文件的参数
                        //这个方法的作用是仅复制序列化对象的值,就和在编辑器界面右键复制属性一样
                        EditorUtility.CopySerialized(clip,existingClips[clip.name]);
                        //删除字典中这个动画,不太懂为什么刚复制完还要删除
                        existingClips.Remove(clip.name);
                    }
                    else
                    {
                        //如果字典中没有这个动画,就创建这个动画文件
                        //把clip对象添加到isoAnimation文件(当前正在编辑的这个)中,就和图片切完精灵后,图片箭头后面就有一串精灵一样
                        AssetDatabase.AddObjectToAsset(clip, assetPath);
                    }
                    //把动画文件添加到动画状态机中,返回这个状态机的状态,形参1是动画文件,形参2是状态机的图层索引
                    var state = isoAnimation.controller.AddMotion(clip, 0);
                }
            }
            //遍历字典,删除字典中剩下的动画文件
            foreach(var clip in existingClips.Values)
            {
                DestroyImmediate(clip,true);
            }
            //刷新一下IosAnimation文件,确保它在Unity中生效
            AssetDatabase.ImportAsset(assetPath);
        }
    }  
}