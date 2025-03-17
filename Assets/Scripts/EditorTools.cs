using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ��������
/// ��Unity�༭����Ӳ˵���ĿIso/�������
/// </summary>
public class EditorTools : MonoBehaviour
{
    /// <summary>
    /// ����ȡ�����ķ���,Ҳ��ȡģ.���������֤�������Ȳ���,��֮ǰС�����λ���Ǽ�λ
    /// </summary>
    /// <param name="a">��</param>
    /// <param name="b">����</param>
    /// <returns>a����b�����</returns>
    static float fmod(float a,float b)
    {
        //����ķ�������������ȡ��,��֤���������ᵼ�¸ı����ľ���
        return a - b * Mathf.Round(a / b);
    }

    /// <summary>
    /// []��������ݴ���,��Unity�༭���������һ���˵�Iso,�����б����һ����ĿSnap
    /// ���������ͨ��ȡ����,��������������
    /// </summary>
    [MenuItem("Iso/��������")]
    static public void SnapToIsoGrid()
    {
        //�����ڱ༭����ѡ�е�������Ϸ����(��ѡ,��ѡ)(Selection.gameObjects����ѡ�е����ж���)
        foreach (GameObject gameObject in Selection.gameObjects)
        {
            //��������
            Snap(gameObject.transform);
        }
    }
    /// <summary>
    /// ������Ϸ���������,ʹ�����������
    /// </summary>
    /// <param name="transform">��ǰ�����Ŀ��</param>
    public static void Snap(Transform transform)
    {
        //��ȡ��ǰ����ı�������
        var pos = transform.localPosition;
        //��������ĺ��Ĵ���,
        //����:0.7X��>>0.733-(0.733-0.2*(0.733/0.2��ȡ��))>>0.733-(0.733-0.2*(4))>>0.733-(0.733-0.8)>>0.733--0.067>>0.8
        //�����Ͱ�0.733X����뵽��0.8X��
        transform.localPosition = new Vector3(pos.x - fmod(pos.x, 0.2f), pos.y - fmod(pos.y, 0.1f), pos.z);

        //�ݹ�������,�����Ӷ���ȫ��Ҫ����
        for (int i = 0; i < transform.childCount; ++i)
        {
            Snap(transform.GetChild(i));
        }
    }
}
