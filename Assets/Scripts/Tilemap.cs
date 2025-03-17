using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��Ƭ/��������
/// ����Ҫע���������Ƭ����5����ϵ��.һ����Ƭ��5*5=25������,Tile����Ƭ�����,�ֶ����map���������
/// </summary>
public class Tilemap : MonoBehaviour
{
    /// <summary>
    /// ���ʵ��
    /// </summary>
    static public Tilemap instance;

    /// <summary>
    /// ��
    /// </summary>
    private int widht = 1024;
    /// <summary>
    /// ��
    /// </summary>
    private int height = 1024;
    /// <summary>
    /// ԭ��
    /// </summary>
    private int origin;
    /// <summary>
    /// ����
    /// </summary>
    private bool[] map;

    private void Awake()
    {
        //��ʼ������,�������ڳ����Կ�
        map = new bool[widht * height];
        //��ʼ��ԭ��,�������м���Ǹ��������ԭ��
        origin = map.Length / 2;
        //��ʼ��ʵ��
        instance = this;
    }
    /// <summary>
    /// ��Ҫʹ��������������,�ʼ̳�IComparer�ӿ�
    /// ��������Ǵ�Floor�㼶����Ƭ��ĩβ
    /// </summary>
    class TileOrderComparer : IComparer<Tile>
    {
        /// <summary>
        /// �ӿڷ���,�Ƚ�������Ƭ�Ĳ㼶��,�Ƿ�ΪΪFloor,a��,b����,���ط�.��֮������
        /// </summary>
        /// <param name="a">A�ذ�</param>
        /// <param name="b">B�ذ�</param>
        /// <returns>�����Ľ������,�ذ�����򵽺���</returns>
        public int Compare(Tile a,Tile b)
        {
            bool floor1 = a.GetComponent<SpriteRenderer>().sortingLayerName == "Floor";
            bool floor2 = b.GetComponent<SpriteRenderer>().sortingLayerName == "Floor";
            return -floor1.CompareTo(floor2);
        }
    }
    private void Start()
    {
        //�ҵ�������Ƭ
        Tile[] tiles = GameObject.FindObjectsOfType<Tile>();
        //�����Ƿ���Ƭ�㼶��ΪFloor����,Floor�ź���
        Array.Sort(tiles, new TileOrderComparer());
        //����������Ƭ
        foreach (Tile tile in tiles)
        {
            //��ǰ��Ƭ����ת�Ⱦ�
            Vector3 pos = Iso.MapToIso(tile.transform.position);
            //��ȡ��Ƭ���·�����ĵȾ�����
            pos.x -= tile.width / 2;
            pos.y -= tile.height / 2;
            //������Ƭ����������
            for (int x = 0; x < tile.height; ++x)
            {
                for (int y = 0; y < tile.width; ++y)
                {
                    //������Ƭ�ɷ�ͨ�и������ɷ�ͨ�б��
                    Tilemap.instance[pos + new Vector3(x, y)] = tile.passable;
                }
            }
        }
    }

    private void Update()
    {
        //���涼�ǻ���������ߵĴ���

        //׼����ɫ,��
        Color color = new Color(1, 1, 1, 0.07f);
        //׼����ɫ,��
        Color redColor = new Color(1, 0, 0, 0.2f);

        //ȡ��Ļ���ĵ�ĵȾ�����
        Vector3 pos = Iso.Snap(Iso.MapToIso(Camera.main.transform.position));
        //�趨����
        int debugWidth = 100;
        int debugHeight = 100;
        //pos����Ļ����,����-=����,����ȡ��ײ�����
        pos.x -= debugWidth / 2;
        pos.y -= debugHeight / 2;

        //�����������,
        for (int x = 1; x < debugWidth; ++x)
        {
            for (int y = 1; y < debugHeight; ++y)
            {
                //�β�1,��ǰ��������,�β�2,��ȡ��ǰ����״̬,��ͨ�еİ�ɫ���ɵĺ�ɫ
                Iso.DebugDrawTile(pos + new Vector3(x, y), this[pos + new Vector3(x, y)] ? color : redColor);
            }
        }
    }

    /// <summary>
    /// ��ͼת����
    /// </summary>
    /// <param name="tilePos">��������</param>
    /// <returns>���������±�</returns>
    private int MapToIndex(Vector3 tilePos)
    {
        //Mathf.Round��������ȡ��,��֤���꾫��
        return origin + (int)Mathf.Round(tilePos.x + tilePos.y * widht);
    }



    /// <summary>
    /// ������
    /// </summary>
    /// <param name="tilePos">��������</param>
    /// <returns>����ͨ��״̬</returns>
    public bool this[Vector3 tilePos]
    {
        get
        {
            //ͨ���Ⱦ�������������
            return map[MapToIndex(tilePos)];
        }

        set
        {
            //ͨ���Ⱦ�������������
            map[MapToIndex(tilePos)] = value;
        }
    }


}
