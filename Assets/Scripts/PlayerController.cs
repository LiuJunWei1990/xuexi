using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ҿ��������
/// </summary>
public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// �Ⱦ��������
    /// </summary>
    Iso iso;
    /// <summary>
    /// ��ɫ���
    /// </summary>
    public Character character;

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

        //����
        if (Input.GetMouseButton(0))
        {
            if (Usable.hot != null)
            {
                character.Use(Usable.hot);
            }
            else
            {
                character.GoTo(targetTile);
            }
        }

        //�����Ҽ�
        if (Input.GetMouseButtonDown(1))
        {
            //����˲�Ʒ���
            character.Teleport(IsoInput.mouseTile);
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
