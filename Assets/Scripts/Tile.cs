using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    /// <summary>
    /// ��ͨ��
    /// </summary>
    public bool passable = true;

    /// <summary>
    /// ��Ƭ���
    /// </summary>
    public int width = 5;
    /// <summary>
    /// ��Ƭ�߶�
    /// </summary>
     public int height = 5;

    ///// <summary>
    ///// ��͸����ɫ,��ͨ����Ƭ��ɫ
    ///// </summary>
    //Color color = new Color(1, 1, 1, 0.07f);

    ///// <summary>
    ///// ��͸����ɫ,����ͨ����Ƭ��ɫ
    ///// </summary>
    //Color redColor = new Color(1, 0, 0, 0.2f);

    private void Start()
    {
        ////��ȡ��ǰ��Ƭ������
        //Vector3 pos = Iso.MapToIso(transform.position);
        ////ȡ���Ͻ�����
        //pos.x -= width / 2;
        //pos.y -= height / 2;
        ////������Ƭ�е�ÿ����Ԫ��
        //for (int x = 0; x < height; x++)
        //{
        //    for (int y = 0; y < width; y++)
        //    {
        //        //Tilemap���������,�ǽ��Ⱦ�����ת��Ϊ��Ӧ�������е��������(��������);
        //        Tilemap.instance[pos + new Vector3(x, y)] = passable;
        //    }
        //}
    }

    private void Update()
    {
        ////��ȡ��ǰ��Ƭ������
        //Vector3 pos = Iso.MapToIso(transform.position);
        ////ȡ���Ͻ�����
        //pos.x -= width / 2;
        //pos.y -= height / 2;
        ////������Ƭ�е�ÿ����Ԫ��
        //for (int x = 0; x < height; x++)
        //{
        //    for (int y = 0; y < width; y++)
        //    {
        //        //debug����,��ͨ�еĻ���͸����,����ͨ�еĻ���͸����
        //        Iso.DebugDrawTile(pos + new Vector3(x, y), passable ? color : redColor);
        //    }
        //}
    }
}
