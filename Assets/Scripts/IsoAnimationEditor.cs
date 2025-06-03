using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// IsoAnimation的自定义编辑器
/// </summary>
/// <remarks>
/// 可以给IsoAnimation添加自定义的功能,比如这个类加了个按钮,和相关的功能,继承自Editor
/// </remarks>
[CustomEditor(typeof(IsoAnimation))]
public class IsoAnimationEditor : Editor
{
    /// <summary>
    /// 重写OnInspectorGUI方法,可以在Inspector面板中添加自定义的功能
    /// </summary>
    /// <remarks>
    /// 包含两个功能,一个是Inspector面板参数改变时自动更新,一个是更新按钮,都是调用Build方法
    /// </remarks>
    public override void OnInspectorGUI()
    {
        if (DrawDefaultInspector())
        {
            Build();
        }

        if (GUILayout.Button("更新"))
        {
            Build();
        }
    }
    /// <summary>
    /// 构建IsoAnimation的动画切片
    /// </summary>
    /// <remarks>
    /// 添加一个动画姿态,放入图片,填写帧数,方向数量,偏移,是否循环.之后,这个方法会把图片下的精灵导入到精灵数组
    /// </remarks>
    void Build()
    {
        //这里target是Editor的一个属性,代表正在编辑的对象,也就是IsoAnimation的文件
        var isoAnimation = target as IsoAnimation;

        foreach (var state in isoAnimation.states)
        {
            if (state.texture)
            {
                if (state.name == null || state.name.Length == 0)
                    state.name = state.texture.name;
                var spritesPath = AssetDatabase.GetAssetPath(state.texture);
                state.sprites = AssetDatabase.LoadAllAssetsAtPath(spritesPath).OfType<Sprite>().OrderBy(s => s.name.Length).ThenBy(s => s.name).ToArray();
            }
        }
        //标记当前文件为"脏"状态,意思是它被修改过.这样系统在自动保存时会把改动写入硬盘已保证不被丢失
        EditorUtility.SetDirty(isoAnimation);
    }
}