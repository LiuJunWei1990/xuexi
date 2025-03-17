using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// �Ⱦ���������
    /// </summary>
    Iso iso;
    /// <summary>
    /// ��ɫ����
    /// </summary>
    Character character;

    private void Start()
    {
        //��ȡ�Ⱦ��������
        iso = GetComponent<Iso>();
        //��ȡ��ɫ����
        character = GetComponent<Character>();
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
            targetTile = Iso.MouseTile();
        }
        //��Ŀ������ı߿�,������targetTile,��ͨ�л��̿�,����ͨ�л����
        Iso.DebugDrawTile(targetTile, Tilemap.instance[targetTile] ? Color.green : Color.red, 0.1f);
        //����·��,��ǰ����--Ŀ������
        Pathing.BuildPath(iso.tilePos, targetTile);

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
            character.Teleport(Iso.MouseTile());
        }
    }
}
