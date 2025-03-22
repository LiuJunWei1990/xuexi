// ����ϵͳ���������ռ䣬����ʹ�ü�������
using System.Collections;

// ����ϵͳ���Ϸ��������ռ䣬����ʹ�÷��ͼ�������
using System.Collections.Generic;

// ���� Unity ����������ռ䣬���ڷ��� Unity �ĺ��Ĺ���
using UnityEngine;



/// <summary>
/// ���� Iso �࣬���ڴ���Ⱦ��������������֮���ת��,���Ƶ�ͼ����(���ڵ���ģʽ,��Ѱ·�ڵ�)
/// </summary>
/// ����:���ڱ༭ģʽ�����иýű�
[ExecuteInEditMode]
/// ����:�ýű�Я��һ�����
[RequireComponent(typeof(SpriteRenderer))]
public class Iso : MonoBehaviour
{

    /// <summary>
    /// ��̬��������Ƭ�ĳߴ磨���������ȣ�
    /// </summary>
    static public float tileSize = 0.2f;

    /// <summary>
    /// ��̬��������Ƭ�ĳߴ磨��������߶ȣ�Ϊ��ȵ�һ�룩
    /// </summary>
    static public float tileSizeY = tileSize / 2;
    /// <summary>
    /// ��ǰ�����λ�ã��Ⱦ����꣩
    /// </summary>
    [Tooltip("λ��(�Ⱦ�)")]
    public Vector2 pos;

    /// <summary>
    /// ��ǰ����ĵ�Ԫ������
    /// </summary>
    [Tooltip("������������")]
    public Vector2 tilePos;

    /// <summary>
    /// ���������Ⱦ��
    /// </summary>
    SpriteRenderer spriteRenderer;

    /// <summary>
    /// �Ƿ���Ƭ,��Ƭ�϶�ʱ����Ƭ����,�����������
    /// </summary>
    [Tooltip("����Ƭ����")]
    public bool macro = false;

    /// <summary>
    /// �Ƿ�����(��Ⱦ�㼶),Ĭ���������,��Ϊ��Щ���岻��Ҫ����,����ذ�,����ͼ�㶼�ǵذ�,�ذ��ǲ��ᵲס���������,���Բ���Ҫ����
    /// </summary>
    [Tooltip("��ͬ�㼶����������")]
    public bool sort = true;


    /// <summary>
    /// ���Ⱦ�����ת��Ϊ��������
    /// </summary>
    /// <param name="iso">�Ⱦ�����</param>
    /// <returns></returns>
    static public Vector3 MapToWorld(Vector3 iso)
    {
        // ���ݵȾ����������������
        return new Vector3(iso.x - iso.y, (iso.x + iso.y) / 2) * tileSize;
    }

    /// <summary>
    /// ����������ת��Ϊ�Ⱦ�����
    /// </summary>
    /// <param name="world">��������</param>
    /// <returns></returns>
    static public Vector3 MapToIso(Vector3 world)
    {
        // ���������������Ⱦ�����
        return new Vector3(world.y + world.x / 2, world.y - world.x / 2) / tileSize;
    }

    /// <summary>
    /// ���Ʊ��ߵĵ�����Ϣ��������ɫ������߾�(��ҪӦ���ǻ�������ģ�����Ĭ����0.5f.��ȻҪ������Ƭ�߽�Ҳ���ǲ��У���ƫ�ƼӸ�������ʹ�䳬��0.5����)
    /// </summary>
    /// <param name="pos">�Ⱦ�����</param>
    /// <param name="color">������ɫ</param>
    /// <param name="margin">ƫ�ƣ����������м��С���飬A*Ѱ·�õ�����</param>
    static public void DebugDrawTile(Vector3 pos, Color color, float margin = 0)
    {
        // ���Ⱦ�����ת��Ϊ��������
        pos = Iso.MapToWorld(pos);

        // ������������һ��߳�
        float d = 0.5f - margin;

        // ���������������
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(d, -d)), color);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(-d, -d)), pos + Iso.MapToWorld(new Vector2(-d, d)), color);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(-d, d)), color);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, -d)), pos + Iso.MapToWorld(new Vector2(-d, -d)), color);
    }
    /// <summary>
    /// ר�Ÿ�����ģʽ�õ�,������Ƭ�߽磬��ɫ��Gizmos����ɫ�������߳��ļ��㷽ʽ�е�仯�������Ǳ߳�����0.5
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="size"></param>
    static public void GizmosDrawTile(Vector3 pos,float size = 1.0f)
    {
        // ���Ⱦ�����ת��Ϊ��������
        pos = Iso.MapToWorld(pos);
        // ������������һ��߳�
        float d = 0.5f * size;
        // ���������������
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(d, -d)));
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(-d, -d)), pos + Iso.MapToWorld(new Vector2(-d, d)));
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(-d, d)));
        Gizmos.DrawLine(pos + Iso.MapToWorld(new Vector2(d, -d)), pos + Iso.MapToWorld(new Vector2(-d, -d)));
    }

    /// <summary>
    /// ���ṩ��ɫ������debug���ߣ�Ĭ����ɫΪ��ɫ
    /// </summary>
    /// <param name="pos">�Ⱦ�����</param>
    /// <param name="margin">ƫ��</param>
    static public void DebugDrawTile(Vector3 pos, float margin = 0)
    {
        DebugDrawTile(pos, Color.white, margin);
    }

    /// <summary>
    /// ���������ȡ������
    /// </summary>
    /// <param name="pos">�Ⱦ�����</param>
    /// <returns></returns>
    static public Vector3 Snap(Vector3 pos)
    {
        pos.x = Mathf.Round(pos.x);
        pos.y = Mathf.Round(pos.y);
        return pos;
    }

    /// <summary>
    /// ������ߴ���Ƭ������ȡ��
    /// </summary>
    /// ���������5��������������Ĵ����������5����֤����һֱ��5�ı������Ӷ�������Ƭ����
    /// <param name="pos">�Ⱦ�����</param>
    /// <returns>XY��ֱ����5��ȡ���Ľ��</returns>
    static public Vector3 MacroTile(Vector3 pos)
    {
        //����Z�᲻��
        var macroTlie = pos;
        //X,Y�����5��ȡ��
        macroTlie.x = Mathf.Round(pos.x / 5);
        macroTlie.y = Mathf.Round(pos.y / 5);
        //���ش���������
        return macroTlie;
    }

    private void Awake()
    {
        //��ȡ��ǰ��������
        pos = MapToIso(transform.position);
        //ȡ��
        tilePos = Snap(pos);
        //ȡ���,�����������������ר�Ÿ���������׼����,�ҹ��ƺ��滥����ش������֤spriteRenderer�Ƿ�Ϊ��
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start ����������Ϸ��ʼʱ����
    void Start()
    {
        // �շ�����δʵ�־����߼�
    }
    // Update ������ÿ֡����һ��
    void Update()
    {
        // �����ǰ��������Ϸ״̬
        if (Application.isPlaying)
        {
            // ÿ֡����ǰ����ĵȾ�����ת��Ϊ�������꣬�����ö����λ��
            transform.position = MapToWorld(pos);
        }
        // �����ǰ�����ڱ༭״̬
        else
        {
            //>>>>>>>>>>������������ʱ,�ڱ༭ģʽ��,�����϶���Ϸ����λ��,�����Զ���������,һ��һ��Ķ�<<<<<<<<<<<<
            //����Ǵ�ߴ���Ƭ,ÿ�������Ƭ��С
            if (macro)
            {
                //MacroTile�ѵȾ����������֮��ȡ��,�ٳ�����,��֤����һֱ��5�ı���,�������Ǵ�ߴ���Ƭ��������.
                transform.position = MapToWorld(MacroTile(MapToIso(transform.position))) * 5;
            }
            //�������,ÿ����������С
            else
            {
                // ����ǰ�����λ��ת��Ϊ�Ⱦ����꣬��ȡ����ת��Ϊ�������꣬���ö����λ��.(�����Ƕ�������)
                transform.position = MapToWorld(Snap(MapToIso(transform.position)));
            }
            
            //�������ɵ�ǰ������������ת��Ϊ�Ⱦ�����������pos,ԭ����pos��������λ��,����������λ�ø���pos,��Ϊ�༭ģʽ��,����λ���ǿ����϶���
            pos = MapToIso(transform.position);
        }

        //�Ƿ�����,Ĭ���������,��Ϊ��Щ���岻��Ҫ����,����ذ�,�ذ��ǲ��ᵲס���������,���Բ���Ҫ����
        if (sort)
        {
            //�����������Ⱦ�㼶,��ֵԽ��Խ��ǰ,��Ϊ����-��,�����ֵԽСԽ��ǰ
            spriteRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y / tileSizeY);
            //����Ƭ������ֵ����Ƭ������ڹ���ĵذ���Ƭ��˵��,����������Ⱦ�㼶����ֵ���,ʹ��ֿ���Ⱦ,�Դ�����������
            var macroTile = MacroTile(pos);
            int macroTileOrder = -Mathf.RoundToInt((MapToWorld(macroTile)).y / tileSizeY);
            spriteRenderer.sortingOrder += macroTileOrder * 1000;
        }
    }

    //// �� Unity �༭���л��� Gizmos��������Ϣ��
    //void OnDrawGizmosSelected()
    //{
    //    // ���Ƶ�ǰ��Ϸ��������������Ϣ
    //    DebugDrawTile(pos);
    //}
}