using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// IosAnimation的编辑器
/// 给IosAnimation添加一个编辑器
/// 编辑器类型Editor
/// 用来给资产文件添加选项,例子就是图片文件的选项那样,不是脚本,是直接在资源文件上添加选项
/// 这个脚本中是给IsoAnimation添加了一个按钮,用来更新动画
/// </summary>
/// 特性这个类是IsoAnimation的编辑器(就是IsoAnimation文件在Inspector面板上显示的属性)
[CustomEditor(typeof(IsoAnimation))]
public class IsoAnimationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //绘制默认面板并后续检测属性是否被修改
        //绘制默认面板就是把IsoAnimation的public属性都加到面板上并初始化
        //后续检测属性是否被修改,如果被修改就立即调用Build方法更新动画
        if(DrawDefaultInspector()) Build(); 

        //这是保底,如果属性修改没有正确更新,点击更新按钮强制更新
        if(GUILayout.Button("更新")) Build();
    }
    private void Build()
    {
        //这里指的应该是当前选中的isoAnimation文件
        var isoAnimation = target as IsoAnimation;
        //遍历isoAnimation文件中的states数组
        foreach(var state in isoAnimation.states)
        {
            //如果state的texture图片文件不为空,就继续
            if(state.texture)
            {
                //如果state的名字为空或者名字长度为0,就继续
                if(state.name == null || state.name.Length == 0)
                    //state的名字 = IsoAnimation文件中引用texture文件的名字
                    state.name = state.texture.name;
                //获取IsoAnimation文件中引用texture文件的路径
                var assetPath = AssetDatabase.GetAssetPath(state.texture);
                //加载路径下的所有资源,并先按照名字长度排序,次按照名字首字母排序,然后转成数组,赋值给state的sprites数组
                state.sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().OrderBy(s => s.name.Length).ThenBy(s => s.name).ToArray();
            }        
        }
        //保存修改到磁盘上,如果不保存修改,下次打开文件就会丢失修改
        EditorUtility.SetDirty(isoAnimation);
    }

    
}
