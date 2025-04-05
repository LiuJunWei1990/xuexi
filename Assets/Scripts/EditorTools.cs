using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EditorTools : MonoBehaviour
{
    /// <summary>
    /// ��Ӳ˵���Ŀ:����16�򶯻�
    /// </summary>
    [MenuItem("Assets/Create/16�򶯻�")]
    static public void CreateAnimation16Way()
    {
        CreateAnimation(16);
    }

    /// <summary>
    /// ��Ӳ˵���Ŀ:����8�򶯻�
    /// </summary>
    [MenuItem("Assets/Create/8�򶯻�")]
    static public void CreateAnimation8Way()
    {
        CreateAnimation(8);
    }
    /// <summary>
    /// ���ɶ���
    /// </summary>
    /// <param name="directionCount">X�򶯻�</param>
    static public void CreateAnimation(int directionCount)
    {

        
        #region >>>>>>>>>>>>>��ͼƬ��Դ��ȡΪ��������<<<<<<<<<<<<<<<<<<<

        //1.���ñ༭����ѡ�еĶ���,ǿתΪͼƬ����,���ѡ�еĲ���ͼƬ,����null
        var texture = Selection.activeObject as Texture2D;
        //2.��ȡͼƬ������ļ�·��
        var texturePath = AssetDatabase.GetAssetPath(texture);
        //�ָ�·���ַ���,��ȡ�ļ�����(��һ���������õ�)
        string dir = texturePath.Split('/')[2];
        //3.����·���µ�������Դ,��ת��ΪSprite���͵�����
        //���һ��ÿ�δ���
        //AssetDatabase.LoadAllAssetsAtPath(texturePath)��texturePath·���������ļ�,����ͼƬ�ļ������о���
        //OfType<Sprite>()֧ȡ�����ļ��еľ���
        //OrderBy(s => s.name.Length)������,����������ֵĳ���
        //.ThenBy(s => s.name)������,���ֳ���һ�µİ���������(��ĸ˳��)
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().OrderBy(s => s.name.Length).ThenBy(s => s.name).ToArray();

        #endregion



        #region >>>>>>>>>>>>>>>��������������ָ�ɶ������,��Ϊ�Ķ����ļ�<<<<<<<<<<<<<<<<<<

        //����ÿ����������֡��,����ÿ������֡������ȵ�,���Կ���ֱ�ӳ��Է�����
        int framesPerAnimation = sprites.Length / directionCount;
        //�����¼���,�Ƕ����ļ�������
        var eventName = texture.name;

        //�������з���
        for (int i = 0; i < directionCount; ++i)
        {
            //�������ļ�ȡ�����֣������ļ���+������,����walk_0
            var name = texture.name + "_" + i.ToString();
            //�����Ÿ�ֵ
            int direction = i;
            //�����8�򶯻��������˳����Ҫ��������ת��
            //ʵ�ַ������Ǽ�һ��Ҳ����4��Ȼ���ٳ��Ա���Ҳ����8ȡ�ࡣ
            //0--4,1--5,2--6,3--7,4--0,5--1,6--2,7--3,�պ��Ƿ�ת��Ч��
            if (directionCount == 8) direction = (direction + 4) % directionCount;
            //��ȡ��ǰ��������ж���֡
            //sprites.Skip(direction * framesPerAnimation) --- �����β�������Ԫ��,�β��Ƿ����ų���֡��,���ǵ�ǰ����ĵ�һ֡
            //Take(framesPerAnimation) --- ȡ���β�������Ԫ��,���ǵ�ǰ���������֡
            //ToArray() --- ת��Ϊ����
            Sprite[] animSprites = sprites.Skip(direction * framesPerAnimation).Take(framesPerAnimation).ToArray();
            //���ɲ�ͬ����Ķ���·��
            var assetPath = "Assets/Animations/" + dir + "/" + name + ".anim";
            //����·���µ�AnimationClip�ļ�����ֵ��animationClip,���Ǽ��ص����ļ���,���������һ���ļ��Ĳ�ͬ
            var animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            //Ϊ��,�������ʧ����,Ҫ����һ���µ�
            if(animationClip == null)
            {
                //��ʼ��
                animationClip = new AnimationClip();
                //��·������������ļ�
                AssetDatabase.CreateAsset(animationClip, assetPath);
            }
            //����ֵ
            animationClip.name = name;
            animationClip.frameRate = 12;
            //���¶����ļ�
            FillAnimationClip(animationClip, animSprites, eventName);
        }

        #endregion
    }

    /// <summary>
    /// ���¾��鶯������,���Ƕ����ļ�
    /// </summary>
    /// �������ɵ���һ����������,����walk_0,walk_1,walk_2,walk_3,walk_4,walk_5
    /// AnimationClip����Unity�Ķ����ļ�,�������ڲ��Ŷ���,�ļ���׺��.anim
    /// <param name="clip">�����ļ�������</param>
    /// <param name="sprites">��������</param>
    /// <param name="eventName">�����¼�</param>
    /// <returns>��������AnimationClip,��������Ϊ�����ļ�</returns>
    static private void FillAnimationClip(AnimationClip clip, Sprite[] sprites, string eventName)
    {
        #region >>>>>>>>>>>>>>>>���ɶ����ļ�����һЩ������ֵ<<<<<<<<<<<<<<<<<<<<<<

        //����֡��,��������ĳ���
        int frameCount = sprites.Length;
        //1���Զ���֡��,�ȵ�ÿ֡��ʱ�䳤��
        float frameLength = 1f / clip.frameRate;

        #endregion



        #region >>>>>>>>>>>>>>>>ȷ�������ļ����԰󶨵�����(�������������Ŀ��,����ֱ�����ļ���ȥ���Ǹ�)<<<<<<<<<<<<<<<<<<<<<<

        //EditorCurveBinding����༭������ϵ�һ������,���԰󶨶����ļ�
        EditorCurveBinding curveBinding = new EditorCurveBinding();
        //���ð󶨵Ķ���(���)
        curveBinding.type = typeof(SpriteRenderer);
        //���ð󶨵�����
        curveBinding.propertyName = "m_Sprite";


        #region >>>>>>>>>>>>>>>>�趨�����Ĺؼ�֡(ʵ��ÿһ֡���趨��)<<<<<<<<<<<<<<<<<<<<<<

        //����һ���ؼ�֡����,���Ⱦ���֡������
        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            //����һ���ؼ�֡
            ObjectReferenceKeyframe kf = new ObjectReferenceKeyframe();
            //0,0.083,0.166,0.25,0.333,0.416,0.5,0.583,0.666,0.75,0.833,0.916,ÿһ֡���ǹؼ�֡
            kf.time = i * frameLength;  // ���ùؼ�֡��ʱ��
            kf.value = sprites[i];  // ���ùؼ�֡��ֵ
            keyFrames[i] = kf;  // ���ؼ�֡��ӵ��ؼ�֡������
        }
        //�����ǰ���������е�֡(����),ע������ǰ���½���һ��AnimationClip,�������������֮֡ǰ�����һ��
        clip.ClearCurves();
        //���ؼ�֡����󶨵������ļ���
        //AnimationUtility.SetObjectReferenceCurve�� Unity ����ϵͳ�ĺ��ķ���֮һ�����ڽ������������͵Ĺؼ�֡���� Sprite��Material �ȣ��󶨵�����
        //clip    AnimationClip Ҫ�޸ĵ�Ŀ�궯���ļ�
        //curveBinding EditorCurveBinding  ���԰���Ϣ�����԰��ĸ������ʲô���ԣ�
        //keyFrames ObjectReferenceKeyframe[]   �������ùؼ�֡����
        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);

        #endregion


        #endregion


        #region >>>>>>>>>>>>>>>>>>>�������ļ�����������,����������ʵҲ�������ö�������<<<<<<<<<<<<<<<<<<<<<<<<
        //SerializedObject��������/��������
        //�޸� Unity ������������صĲ������� m_IsActive��
        SerializedObject serializedClip = new SerializedObject(clip);
        //�Զ��������ڴ������ļ�������
        //�βη�����һ��SerializedProperty����,��������Ƕ����ļ�������
        AnimationClipSettings clipSettings = new AnimationClipSettings(serializedClip.FindProperty("m_AnimationClipSettings"));
        clipSettings.loopTime = true; // ����ѭ��ʱ��
        serializedClip.ApplyModifiedProperties(); // Ӧ���޸�
        #endregion

        //���������������,���¼���ӵ������ļ�,����������������д������ķ�ʽ��Ӷ���¼�
        AnimationUtility.SetAnimationEvents(clip, new[]
        {
            new AnimationEvent()
            {
                //�����¼���ʱ���Ƕ�������,�Ǿ���ĩβ
                time = clip.length,
                //�¼����õķ�������functionName
                functionName = "On"+eventName+"Finish"
            },
            new AnimationEvent()
            {
                //ͬ��
                time = clip.length,
                functionName = "OnAnimationFinish"
            },
        }
        );
    }
}


/// <summary>
/// ����һ�� AnimationClipSettings �࣬���ڲ�����������������
/// </summary>
/// ���л�����:Unity����������ϵ�����,����ͨ��SerializedObject��������
class AnimationClipSettings
{
    #region >>>>>>>>>>>>>>>>>��ʼ���Ĳ���,��������˸����Ե�����<<<<<<<<<<<<<<<<<<<<<<

    // �����˶����ļ���m_AnimationClipSettings����,�����������Եĸ�����
    SerializedProperty m_Property;

    /// <summary>
    /// ��ȡָ������,��m_StartTime,m_StopTime��,�����������Ե�get��set������
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    private SerializedProperty Get(string property)
    {
        //ͨ�������ԣ����ڲ���Ƕ������
        return m_Property.FindPropertyRelative(property);
    }

    /// <summary>
    /// ���캯��,��һ�����õĸ�����
    /// </summary>
    /// <param name="prop">serializedClip.FindProperty("m_AnimationClipSettings")�����Ҹ�����</param>
    public AnimationClipSettings(SerializedProperty prop)
    {
        m_Property = prop; // �洢���������ã��������в����������������
    }

    #endregion

    #region ���ָ����ԵĻ�ȡ������
    // === ʱ��������� ===
    public float startTime
    {
        get { return Get("m_StartTime").floatValue; } // ��ȡ��ʼʱ��
        set { Get("m_StartTime").floatValue = value; } // ������ʼʱ��
    }

    public float stopTime
    {
        get { return Get("m_StopTime").floatValue; }  // ��ȡ����ʱ��
        set { Get("m_StopTime").floatValue = value; } // ���ý���ʱ��
    }

    // === ����ƫ������ ===
    public float orientationOffsetY
    {
        get { return Get("m_OrientationOffsetY").floatValue; } // ��ȡY����תƫ��
        set { Get("m_OrientationOffsetY").floatValue = value; } // ����Y����תƫ��
    }

    public float level
    {
        get { return Get("m_Level").floatValue; } // ��ȡ�㼶ֵ
        set { Get("m_Level").floatValue = value; } // ���ò㼶ֵ
    }

    public float cycleOffset
    {
        get { return Get("m_CycleOffset").floatValue; } // ��ȡѭ��ƫ��
        set { Get("m_CycleOffset").floatValue = value; } // ����ѭ��ƫ��
    }

    // === ѭ���������� ===
    public bool loopTime
    {
        get { return Get("m_LoopTime").boolValue; } // ��ȡ�Ƿ�ѭ��
        set { Get("m_LoopTime").boolValue = value; } // �����Ƿ�ѭ��
    }

    public bool loopBlend
    {
        get { return Get("m_LoopBlend").boolValue; } // ��ȡ�Ƿ���ѭ��
        set { Get("m_LoopBlend").boolValue = value; } // �����Ƿ���ѭ��
    }

    // === ���ģʽ���� ===
    public bool loopBlendOrientation
    {
        get { return Get("m_LoopBlendOrientation").boolValue; } // ��ȡ������
        set { Get("m_LoopBlendOrientation").boolValue = value; } // ���÷�����
    }

    public bool loopBlendPositionY
    {
        get { return Get("m_LoopBlendPositionY").boolValue; } // ��ȡY��λ�û��
        set { Get("m_LoopBlendPositionY").boolValue = value; } // ����Y��λ�û��
    }

    public bool loopBlendPositionXZ
    {
        get { return Get("m_LoopBlendPositionXZ").boolValue; } // ��ȡXZƽ��λ�û��
        set { Get("m_LoopBlendPositionXZ").boolValue = value; } // ����XZƽ��λ�û��
    }

    // === ԭʼ״̬�������� ===
    public bool keepOriginalOrientation
    {
        get { return Get("m_KeepOriginalOrientation").boolValue; } // ��ȡ�Ƿ���ԭʼ��ת
        set { Get("m_KeepOriginalOrientation").boolValue = value; } // �����Ƿ���ԭʼ��ת
    }

    public bool keepOriginalPositionY
    {
        get { return Get("m_KeepOriginalPositionY").boolValue; } // ��ȡ�Ƿ���ԭʼYλ��
        set { Get("m_KeepOriginalPositionY").boolValue = value; } // �����Ƿ���ԭʼYλ��
    }

    public bool keepOriginalPositionXZ
    {
        get { return Get("m_KeepOriginalPositionXZ").boolValue; } // ��ȡ�Ƿ���ԭʼXZλ��
        set { Get("m_KeepOriginalPositionXZ").boolValue = value; } // �����Ƿ���ԭʼXZλ��
    }

    // === ����Ч������ ===
    public bool heightFromFeet
    {
        get { return Get("m_HeightFromFeet").boolValue; } // ��ȡ�Ƿ�ӽŲ�����߶�
        set { Get("m_HeightFromFeet").boolValue = value; } // �����Ƿ�ӽŲ�����߶�
    }

    public bool mirror
    {
        get { return Get("m_Mirror").boolValue; } // ��ȡ�Ƿ��񶯻�
        set { Get("m_Mirror").boolValue = value; } // �����Ƿ��񶯻�
    }

    #endregion
}