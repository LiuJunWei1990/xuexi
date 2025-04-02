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

    private void Update()
    {
        //Ŀ�������
        Vector3 targetTile;
        //�����ǰ�������岻Ϊ��
        if (Usable.hot != null)
        {
            //Ŀ������ֱ��ȡ��ǰ�������������
            targetTile = Iso.MapToIso(Usable.hot.transform.position);
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
        Pathing.BuildPath(iso.tilePos, targetTile,character.directionCount);

        //�����Ҽ�
        if (Input.GetMouseButtonDown(1))
        {
            //����˲�Ʒ���
            character.Teleport(IsoInput.mouseTile);
        }
        //�������+��Shift
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0))
        {
            //ִ�й���
            character.Attack();
        }

        //�������
        else if (Input.GetMouseButton(0))
        {
            //����ҹ�ע�Ļ������岻Ϊ��
            if (Usable.hot != null)
            {
                //���õ�ǰΪ��ҹ�ע
                character.Use(Usable.hot);
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

        //>>>>>>>>>>�����ͣ�����߼�����<<<<<<<<<<<<<<<<
        //���������ͣĿ��
        GameObject newHover = null;
        //ͨ����ǰ�������Ⱦ�����굱ǰλ��(����seepseek���黻��Camera.mian�����,��Ϊ��ǰ�����Update������Ϊ��)
        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //2D���߼��
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        //�����߼�⵽����
        if (hit)
        {
            //��ȡ�������
            newHover = hit.collider.gameObject;
        }

        //��������󲻵���֮ǰ����ͣ����
        if (newHover != hover)
        {
            //��֮ǰ����ͣ����Ϊ��
            if (hover != null)
            {
                //��ȡԭ����ͣ�������Ⱦ��
                var spriteRenderer = hover.GetComponent<SpriteRenderer>();
                //�Ѹ����ָ�
                spriteRenderer.material.SetFloat("_SelfIllum", 1.0f);
            }

            //���µ�ǰ��ͣ����
            hover = newHover;
            //���¹�����ͣ����Ϊ��
            if(hover != null)
            {
                var spriteRenderer = hover.GetComponent<SpriteRenderer>();
                spriteRenderer.material.SetFloat("_SelfIllum", 1.75f);
            }
        }

    }
}
