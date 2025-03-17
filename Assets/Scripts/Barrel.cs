using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ͱ���
/// </summary>
[RequireComponent(typeof(Usable))]
public class Barrel : MonoBehaviour
{
    /// <summary>
    /// ����
    /// </summary>
    Animator animator;
    /// <summary>
    /// �������
    /// </summary>
    Usable usable;
    /// <summary>
    /// ����
    /// </summary>
    SpriteRenderer spriteRenderer;


    private void Awake()
    {
        //��ȡ�������
        animator = GetComponent<Animator>();
        usable = GetComponent<Usable>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// ʹ��,���пɻ������嶼�еķ���,�ᱻUsable�����������
    /// </summary>
    void OnUse()
    {
        //����ʹ�ö���
        animator.Play("Use");
        //ʹ������ļ����ֶ�,����Ϊ��.��Ϊʹ�ú�Ͱ������
        usable.active = false;
        //Ͱ����,������ȻҲ�Ϳ�ͨ����
        Tilemap.instance[Iso.MapToIso(transform.position)] = true;
        //�޸�ͼ��,�Ա�֤���ᵲס��������
        spriteRenderer.sortingLayerName = "OnFloor";
    }
}
