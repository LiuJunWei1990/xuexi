using System.Collections;
using System.Collections.Generic;
using System.IO;
//这个命名空间会使脚本只在编辑器模式下运行,在游戏打包后不会被打包进游戏
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity 编辑器扩展工具集
/// </summary>
public class EditorTools
{
    [MenuItem("Assets/Create/Iso Animation动画切片生成器")]
    static public void CreateAnimation()
    {
       ScriptableObjectUtility.CreateAsset<IsoAnimation>();
    }
}

/// <summary>
/// ScriptableObject的工具类
/// ScriptableObject类是Unity自带的一个类,用于把一个类型转换为资源文件,前提是这个类型继承自ScriptableObject
/// 这个类的序列化字段可以直接在Inspector面板上显示和修改
/// </summary>
public static class ScriptableObjectUtility
{
    /// <summary>
    /// 创建一个ScriptableObject资源文件
    /// 把任意一个派生的类型转换为资源文件
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
        //激活资源窗口(就是鼠标点中的那个效果)
        EditorUtility.FocusProjectWindow();
        //当前选择项改为刚才创建的文件
        Selection.activeObject = asset;
        //返回创建的资源 
        return asset;
    }
}

