using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ҿ��������
/// </summary>
public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// ��ɫ���
    /// </summary>
    public Character character;
    //��ǰ�����ͣ����Ϸ����
    //����:����ʾ�������
    [HideInInspector]
    static public GameObject hover;
    /// <summary>
    /// �Ⱦ��������
    /// </summary>
    Iso iso;

    Collider2D[] hoverColliders = new Collider2D[4];

    private void Awake()
    {
        //�����ɫ���Ϊ��
        if (character == null)
        {
            //ͨ��Tag�ҵ���ɫ���
            character = GameObject.FindWithTag("Player").GetComponent<Character>();
        }
        //���ý�ɫ
        SetCharacter(character);
    }

    private void Start()
    {

    }

    /// <summary>
    /// �趨��ɫ
    /// </summary>
    /// <param name="character">Ŀ���ɫ</param>
    void SetCharacter(Character character)
    {
        //��Ŀ��Ľ�ɫ�����ֵ����ǰ��ɫ���
        this.character = character;
        //���Ŀ���ɫ����ĵȾ��������
        iso = character.GetComponent<Iso>();
    }

    /// <summary>
    /// ���������ͣ��Ŀ��
    /// </summary>
    void UpdateHover()
    {
        //����������ʱ������
        if (Input.GetMouseButton(0)) return;

        //������ͣĿ��ı���
        GameObject newHover = null;
        //��ȡ�������������ϵ�е�λ��
        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //�������,���ش��е�������ײ��������,����������ײ������hoverColliders����
        int overlapCount = Physics2D.OverlapPointNonAlloc(mousePos, hoverColliders);
        //���������ײ������
        if (overlapCount > 0)
        {
            //��ȡ��ײ��������������,���������ͣ��Ŀ��
            newHover = hoverColliders[0].gameObject;
        }

        //�������ͣĿ�겻���ڵ�ǰ��ͣĿ��
        if (newHover != hover)
        {
            //�����ǰ��ͣĿ�겻Ϊ��
            if (hover != null)
            {
                //��ȡ��ǰ��ͣĿ��ľ�����Ⱦ�����
                var spriteRenderer = hover.GetComponent<SpriteRenderer>();
                //��ԭ������
                spriteRenderer.material.SetFloat("_SelfIllum", 1.0f);
            }
            //������ͣĿ�긳ֵ����ǰ��ͣĿ��
            hover = newHover;
            //��ֵ��,�����ǰ��ͣĿ�겻Ϊ��
            if (hover != null)
            {
                //��ȡ��ǰ��ͣĿ��ľ�����Ⱦ�����
                var spriteRenderer = hover.GetComponent<SpriteRenderer>();
                //���������
                spriteRenderer.material.SetFloat("_SelfIllum", 1.75f);
            }
        }
    }

    private void Update()
    {
        UpdateHover();
        //Ŀ�������
        Vector3 targetTile;
        //�����ǰ�������岻Ϊ��
        if (hover != null)
        {
            //Ŀ������ֱ��ȡ��ǰ�������������
            targetTile = Iso.MapToIso(hover.transform.position);
        }
        //��ǰ��������Ϊ��
        else
        {
            //Ŀ��ȡ���λ�õ�����
            targetTile = IsoInput.mouseTile;
        }
        //��Ŀ������ı߿�,������targetTile,��ͨ�л��̿�,����ͨ�л����
        Iso.DebugDrawTile(targetTile, Tilemap.instance[targetTile] ? Color.green : Color.red, 0.1f);
        //����·��,��ǰ����--Ŀ������
        Pathing.BuildPath(iso.tilePos, targetTile,character.directionCount,character.useRange);

        //����F4
        if (Input.GetKeyDown(KeyCode.F4))
        {
            //����˲�Ʒ���
            character.Teleport(IsoInput.mouseTile);
        } 
        //�����Ҽ� ���� �������+��Shift
        if (Input.GetMouseButton(1) || (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0)))
            {
            //ִ�й���
            character.Attack();
        }

        //�������
        else if (Input.GetMouseButton(0))
        {
            //����ҹ�ע�Ļ������岻Ϊ��
            if (hover != null)
            {
                //���õ�ǰΪ��ҹ�ע
                character.target = hover;
            }
            //Ϊ�վ�����·
            else
            {
                character.GoTo(targetTile);
            }
        }




        character.LookAt(IsoInput.mousePosition);
        //����Tab��
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            //���������е����н�ɫ
            foreach (Character character in GameObject.FindObjectsOfType<Character>())
            {
                //�����ǰ��ɫ������ҿ������Ľ�ɫ
                if (this.character != character)
                {
                    //�趨�½�ɫ
                    SetCharacter(character);
                    return;
                }
            }
        }

    }
}
