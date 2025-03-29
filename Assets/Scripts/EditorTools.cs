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
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().ToArray();

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
            //�����÷�������ﶯ������(�༭����Ķ����ļ�)
            AnimationClip animation = CreateSpriteAnimationClip(name, animSprites,eventName); // ������������
            // ���ɳɶ����ļ�
            AssetDatabase.CreateAsset(animation, "Assets/Animations/" + dir + "/" + name + ".anim"); 
        }

        #endregion
    }

    /// <summary>
    /// ���ɾ��鶯������,���Ƕ����ļ�
    /// </summary>
    /// �������ɵ���һ����������,����walk_0,walk_1,walk_2,walk_3,walk_4,walk_5
    /// AnimationClip����Unity�Ķ����ļ�,�������ڲ��Ŷ���,�ļ���׺��.anim
    /// <param name="name">������������</param>
    /// <param name="sprites">��������</param>
    /// <param name="eventName">�����¼�</param>
    /// <param name="fps">FPS</param>
    /// <returns>��������AnimationClip,��������Ϊ�����ļ�</returns>
    static private AnimationClip CreateSpriteAnimationClip(string name, Sprite[] sprites, string eventName, int fps = 12)
    {
        #region >>>>>>>>>>>>>>>>���ɶ����ļ�����һЩ������ֵ<<<<<<<<<<<<<<<<<<<<<<

        //����֡��,��������ĳ���
        int frameCount = sprites.Length;
        //����ÿ֡��ʱ�䳤��,����1�����֡��
        float frameLength = 1.0f / fps;

        //�����µĶ���
        AnimationClip clip = new AnimationClip();
        clip.name = name;  // ���ö���������
        clip.frameRate = fps;   // ���ö�����֡��
        clip.wrapMode = WrapMode.Loop;   //���ö�����ѭ��ģʽ,���ѭ��ģʽ�����ȶ�,�����õ���serializedClip��������ѭ��ģʽ�ĸ������

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

        return clip; // ���ش����Ķ�������
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
    /* ========== ʱ�䷶Χ�������� ========== */

    // ��ȡ�����ö�����ʼʱ�䣨��λ���룩
    public float startTime
    {
        get => Get("m_StartTime").floatValue;  // ��ȡfloat���͵�����ֵ
        set => Get("m_StartTime").floatValue = value; // ��������ֵ
    }

    // ��ȡ�����ö�������ʱ�䣨��λ���룩
    public float stopTime
    {
        get => Get("m_StopTime").floatValue;
        set => Get("m_StopTime").floatValue = value;
    }

    // ��ȡ������Y����תƫ��������λ���ȣ�
    public float orientationOffsetY
    {
        get => Get("m_OrientationOffsetY").floatValue;
        set => Get("m_OrientationOffsetY").floatValue = value;
    }

    // ��ȡ�����ö����㼶�����ڻ�϶�����
    public float level
    {
        get => Get("m_Level").floatValue;
        set => Get("m_Level").floatValue = value;
    }

    // ��ȡ������ѭ��ƫ��������׼��ʱ�䣬0-1��Χ��
    public float cycleOffset
    {
        get => Get("m_CycleOffset").floatValue;
        set => Get("m_CycleOffset").floatValue = value;
    }

    /* ========== ѭ���������� ========== */

    // ��ȡ�������Ƿ����ö���ʱ��ѭ��
    public bool loopTime
    {
        get => Get("m_LoopTime").boolValue;
        set => Get("m_LoopTime").boolValue = value;
    }

    // ��ȡ�������Ƿ�����ѭ�����
    public bool loopBlend
    {
        get => Get("m_LoopBlend").boolValue;
        set => Get("m_LoopBlend").boolValue = value;
    }

    /* ========== ��ת��Ͽ��� ========== */

    // ��ȡ�������Ƿ�������ת���ѭ��
    public bool loopBlendOrientation
    {
        get => Get("m_LoopBlendOrientation").boolValue;
        set => Get("m_LoopBlendOrientation").boolValue = value;
    }

    /* ========== λ�û�Ͽ��� ========== */

    // ��ȡ�������Ƿ�����Y��λ�û��ѭ��
    public bool loopBlendPositionY
    {
        get => Get("m_LoopBlendPositionY").boolValue;
        set => Get("m_LoopBlendPositionY").boolValue = value;
    }

    // ��ȡ�������Ƿ�����XZƽ��λ�û��ѭ��
    public bool loopBlendPositionXZ
    {
        get => Get("m_LoopBlendPositionXZ").boolValue;
        set => Get("m_LoopBlendPositionXZ").boolValue = value;
    }

    /* ========== ԭʼ���ݱ������� ========== */

    // ��ȡ�������Ƿ���ԭʼ��ת
    public bool keepOriginalOrientation
    {
        get => Get("m_KeepOriginalOrientation").boolValue;
        set => Get("m_KeepOriginalOrientation").boolValue = value;
    }

    // ��ȡ�������Ƿ���ԭʼY��λ��
    public bool keepOriginalPositionY
    {
        get => Get("m_KeepOriginalPositionY").boolValue;
        set => Get("m_KeepOriginalPositionY").boolValue = value;
    }

    // ��ȡ�������Ƿ���ԭʼXZƽ��λ��
    public bool keepOriginalPositionXZ
    {
        get => Get("m_KeepOriginalPositionXZ").boolValue;
        set => Get("m_KeepOriginalPositionXZ").boolValue = value;
    }

    /* ========== ����������� ========== */

    // ��ȡ�������Ƿ�ӽŲ�����߶�
    public bool heightFromFeet
    {
        get => Get("m_HeightFromFeet").boolValue;
        set => Get("m_HeightFromFeet").boolValue = value;
    }

    // ��ȡ�������Ƿ����þ��񶯻�
    public bool mirror
    {
        get => Get("m_Mirror").boolValue;
        set => Get("m_Mirror").boolValue = value;
    }

    #endregion
}