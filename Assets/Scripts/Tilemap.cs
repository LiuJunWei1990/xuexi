using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��Ƭ/���������
/// </summary>
/// 1.��Ƭ�ǵذ�ģ�͵Ĵ�С.�����ǵȾ೤��Ϊ1�ĸ���,1��Ƭ=5*5����
/// 2.������δʵ�ʴ�����Ƭ��������,����ͨ�����������ͨ��״̬��ӳ������
/// 3.����Ҳ�������,��Ϸ������ֻ�Ǹ�����,��û��ʵ�ʵ���Ϸ����
public class Tilemap : MonoBehaviour
{
    /// <summary>
    /// ��ĵ���
    /// </summary>
    /// ��������,��һ����������֤���ᱻ���ʵ����
    static public Tilemap instance;

    /// <summary>
    /// �����ĳ�
    /// </summary>
    private int widht = 1024;
    /// <summary>
    /// �����Ŀ�
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
    /// ��Ƭ��Ⱦ�㼶������,�Զ���������
    /// </summary>
    /// IComparer�ӿ������Զ��������
    /// ʵ��ʹ��list.Sort(new TileOrderComparer()); ��������.Sort(new �Զ�������������());
    class TileOrderComparer : IComparer<Tile>
    {
        /// <summary>
        /// ����Ⱦ�㼶����
        /// </summary>
        /// <param name="a">A��Ƭ</param>
        /// <param name="b">B��Ƭ</param>
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
        //�����Ƿ���Ƭ�㼶��ΪFloor����,Floor�ź���,���鲻����Listһ��ֱ��Sort,Ҫ��Array.Sort
        Array.Sort(tiles, new TileOrderComparer());


        //>>>>>>>>>>>����������Ƭ,������Ƭ��������������������Ƿ��ͨ��<<<<<<<<<<<<
        foreach (Tile tile in tiles)
        {
            //��pos��λ����Ƭ���·�����������ĵ�
            //��ǰ��Ƭ����ת�Ⱦ�
            Vector3 pos = Iso.MapToIso(tile.transform.position);
            //��ȡ��Ƭ���·�����ĵȾ�����
            pos.x -= tile.width / 2;
            pos.y -= tile.height / 2;
            //���������ƫ����(��ö��Ƭ����������һ��ƫ��)
            pos.x += tile.offsetX;
            pos.y += tile.offsetY;
            //������Ƭ����������
            for (int x = 0; x < tile.width; ++x)
            {
                for (int y = 0; y < tile.height; ++y)
                {
                    //������Ƭ�ɷ�ͨ�и������ɷ�ͨ�б��(��������)
                    Tilemap.instance[pos + new Vector3(x, y)] = tile.passable;
                }
            }
        }



    }

    private void Update()
    {
        //���涼�ǻ���������ߵĴ���

        //׼����ɫ,��
        Color color = new Color(1, 1, 1, 0.15f);
        //׼����ɫ,��
        Color redColor = new Color(1, 0, 0, 0.3f);

        //ȡ��Ļ���ĵ�ĵȾ�����
        Vector3 pos = Iso.Snap(Iso.MapToIso(Camera.main.transform.position));
        //�趨����
        int debugWidth = 100;
        int debugHeight = 100;
        //pos����Ļ����,����-=����,����ȡ��ײ�����
        pos.x -= debugWidth / 2;
        pos.y -= debugHeight / 2;

        //�����������,�����С����
        for (int x = 1; x < debugWidth; ++x)
        {
            for (int y = 1; y < debugHeight; ++y)
            {
                //��ȡ��ǰ����Ŀ�ͨ��״̬
                bool passable = this[pos + new Vector3(x, y)];
                //�������ͨ��,�ͻ��ƺ���
                if (!passable)
                {
                    //���ﲻ̫���,���Ѿ�if��passable,Ϊʲô��Ҫ�ж�һ��,���Ƕ������?��������ֻ�ử����
                    Iso.DebugDrawTile(pos + new Vector3(x, y), passable ? color : redColor, 0.9f);
                }
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
    /// <summary>
    /// ����Ǳ༭ģʽ������ģʽ������õķ���,��������������
    /// </summary>
    private void OnDrawGizmos()
    {
        //�������Ļ���ĵĵȾ��������5,�����ٳ�����Ƭ�ߴ�0.2,���ǳ���5.��֤��һ����5�ı���,�������ܱ�֤������Ƭ
        var cameraTile = Iso.MacroTile(Iso.MapToIso(Camera.main.transform.position));
        //������ɫ
        Gizmos.color = new Color(0.35f, 0.35f, 0.35f);
        //����-10��9.��Ϊ��Ƭ�ı߿�
        for (int x = -10; x < 10; ++x)
        {
            for (int y = -10; y < 10; ++y)
            {
                //��̫������ô���,�����ǻ���Ƭ��������
                //�����Ƭ�����ĵ㣬Ȼ��ת��Ϊ�������꣬�ٳ�����Ƭ����
                var pos = Iso.MapToWorld(cameraTile + new Vector3(x, y) - new Vector3(0.5f, 0.5f)) / Iso.tileSize;
                //������Ƭ��������
                Gizmos.DrawLine(pos, pos + new Vector3(20, 10));
                Gizmos.DrawLine(pos, pos + new Vector3(20, -10));
            }
        }
    }


}
