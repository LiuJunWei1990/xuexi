using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// ����
/// </summary>
//�ýű�Я���������
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class Usable : MonoBehaviour
{
    /// <summary>
    /// ��Ϊ��ǰĿ��Ķ���
    /// </summary>
    static public Usable hot;

    /// <summary>
    /// ��ͼ����
    /// </summary>
    SpriteRenderer spriteRenderer;

    /// <summary>
    /// ����
    /// </summary>
    public bool active = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// ��껬��ʱ����
    /// </summary>
    void OnMouseEnter()
    {
        //�������ʹ����,��껬����û��Ӧ
        if (!active) return;
        //����Ϊ��ǰĿ��
        hot = this;
        //�޸Ĳ�������
        spriteRenderer.material.SetFloat("_SelfIllum", 1.0f);
    }

    /// <summary>
    /// ��껬��ʱ����
    /// </summary>
    void OnMouseExit()
    {
        //�������ʹ����,��껬����û��Ӧ
        if (!active) return;
        //��ǰĿ���ÿ�
        hot = null;
        //�޸Ĳ�������
        spriteRenderer.material.SetFloat("_SelfIllum", 0.75f);
    }

    /// <summary>
    /// ʹ��,�������Ͱ�Ķ���
    /// </summary>
    public void Use()
    {
        //������ǰ��Ϸ�����ϵ�OnUse����
        SendMessage("OnUse");
        //��ǰĿ���ÿ�
        hot = null;
    }
}
