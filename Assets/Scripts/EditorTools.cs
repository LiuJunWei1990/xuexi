using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 编辑器工具类
/// </summary>
public class EditorTools {
    // 在Unity的Assets菜单中添加一个选项 "Create/Iso Animation"
    [MenuItem("Assets/Create/Iso Animation动画切片生成器")]
    static public void CreateIsoAnimation()
    {
        // 调用ScriptableObjectUtility的CreateAsset方法，创建一个IsoAnimation类型的ScriptableObject资源
        ScriptableObjectUtility.CreateAsset<IsoAnimation>();
    }

    // 在Unity的Assets菜单中添加一个选项 "Load DS1"
    [MenuItem("Assets/读取DS1文件")]
    static public void LoadDS1()
    {
        // 获取当前选中的资源路径
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        // 调用DS1类的Import方法，加载并解析选中的DS1文件
        DS1.Import(assetPath);
    }

    // 验证 "Load DS1" 菜单项是否可用
    [MenuItem("Assets/读取DS1文件", true)]
    static public bool LoadDS1Validate()
    {
        // 获取当前选中的资源路径
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        // 检查资源路径是否以 "ds1" 结尾，如果是则返回true，否则返回false
        return assetPath.ToLower().EndsWith("ds1");
    }

    [MenuItem("Assets/将DT1转换为PNG")]
    static public void ConvertDT1ToPNG()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        DT1.ConvertToPng(assetPath);
    }

    [MenuItem("Assets/将DT1转换为PNG", true)]
    static public bool ConvertDT1ToPNGValidate()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        return assetPath.ToLower().EndsWith("dt1");
    }

    [MenuItem("Assets/将DDC转换为PNG")]
    static public void ConvertDCCToPNG()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        DCC.ConvertToPng(assetPath);
    }

    [MenuItem("Assets/将DDC转换为PNG", true)]
    static public bool ConvertDCCToPNGValidate()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        return assetPath.ToLower().EndsWith("dcc");
    }


    [MenuItem("Assets/重置DT1缓存")]
    static public void ResetDT1()
    {
        DT1.ResetCache();
    }

    [MenuItem("Assets/从DC6创建字体")]
    static public void CreateFontFromDC6()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        DC6.CreateFontFromDC6(assetPath);
    }

    [MenuItem("从DC6创建字体", true)]
    static public bool CreateFontFromDC6Validate()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        return assetPath.ToLower().EndsWith("dc6");
    }
    [MenuItem("Assets/测试序列化")]
    static public void TestSerialization()
    {
        var rb = Obj.Find(1, 2, 2);
        Debug.Log(rb.TR);
    }
}



/// <summary>
/// ScriptableObject类实例化
/// ScriptableObject类是一个用于把子类变成文件的类型的资源,上面的IsoAnimation就是它的子类
/// </summary>
public static class ScriptableObjectUtility
{
    //创建资源
    public static T CreateAsset<T>() where T : ScriptableObject
    {
        // 创建一个指定类型的ScriptableObject实例
        T asset = ScriptableObject.CreateInstance<T>();

        // 获取当前选中的资源路径
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        // 如果路径为空，则默认使用 "Assets" 目录
        if (path == "")
        {
            path = "Assets";
        }
        // 如果路径包含文件扩展名，则去掉文件名，只保留目录路径
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        // 生成唯一的资源路径和名称
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New" + typeof(T).ToString() + ".asset");

        // 在指定路径创建资源
        AssetDatabase.CreateAsset(asset, assetPathAndName);
        // 保存资源
        AssetDatabase.SaveAssets();
        // 刷新资源数据库(确保能立即看到创建的文件)
        AssetDatabase.Refresh();
        // 聚焦到项目窗口(就和鼠标点中这个框体的效果一样)
        EditorUtility.FocusProjectWindow();
        // 选中新创建的资源
        Selection.activeObject = asset;
        // 返回创建的资源
        return asset;
    }
}

/// <summary>
/// 资源导入处理器类
/// 这个类继承自AssetPostprocessor，用于处理资源导入事件。
/// 但是并没有被使用,而是直接在上面的方法实现了相同的功能
/// </summary>
public class PostProcessor : AssetPostprocessor
{
    // 当资源导入、删除、移动时调用此方法
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        // 遍历所有导入的资源
        foreach (string assetPath in importedAssets)
        {
            // 如果资源是 .dt1 文件
            if (assetPath.EndsWith(".dt1"))
            {
                // 注释掉的代码：调用 DT1.Import 方法加载 .dt1 文件
                //DT1.Import(assetPath);
            }
            // 如果资源是 .ds1 文件
            else if (assetPath.EndsWith(".ds1"))
            {
                // 注释掉的代码：调用 DS1.Import 方法加载 .ds1 文件
                //DS1.Import(assetPath);
            }
        }
    }
}
