using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ȫˮ���
/// </summary>
public class Spring : MonoBehaviour
{
    /// <summary>
    /// [Range(0, 2)]����fullness��һ����0-2�Ļ�����
    /// �ֶ��Ǵ���Ȫˮ�Ƿ���/һ��/��
    /// </summary>
    [Range(0, 2)]
    public int fullness = 2;
    /// <summary>
    /// ����
    /// </summary>
    Animator animator;
    /// <summary>
    /// �������
    /// </summary>
    Usable usable;

    private void Awake()
    {
        //��ȡ���
        animator = GetComponent<Animator>();
        usable = GetComponent<Usable>();
    }

    private void Start()
    {
        //��ֵ������0,�򼤻�
        usable.active = fullness != 0;
        //�������ĳ̶Ȳ��Ŷ�Ӧ����
        animator.Play(fullness.ToString());
    }
    /// <summary>
    /// ��Ȫˮ
    /// </summary>
    void OnUse()
    {
        //���̶�-1
        fullness -= 1;
        //���Ŷ�Ӧ���̶ȵĶԻ�
        animator.Play(fullness.ToString());
        //�л�����Ӧ�ļ���״̬
        usable.active = fullness != 0;
    }
}
