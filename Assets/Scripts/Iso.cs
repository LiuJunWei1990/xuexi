// ����ϵͳ���������ռ䣬����ʹ�ü�������
using System.Collections;

// ����ϵͳ���Ϸ��������ռ䣬����ʹ�÷��ͼ�������
using System.Collections.Generic;

// ���� Unity ����������ռ䣬���ڷ��� Unity �ĺ��Ĺ���
using UnityEngine;

/// <summary>
/// ���� Iso �࣬���ڴ���Ⱦ��������������֮���ת��,���Ƶ�ͼ����(���ڵ���ģʽ,��Ѱ·�ڵ�)
/// </summary>
/// �ýű�Я��һ�����
[RequireComponent(typeof(SpriteRenderer))]
public class Iso : MonoBehaviour
{

    /// <summary>
    /// ��̬��������ʾÿ����Ƭ�Ĵ�С��Ĭ��ֵΪ 0.2
    /// </summary>
    static public float tileSize = 0.2f;

    /// <summary>
    /// ��ǰ�����λ�ã��Ⱦ����꣩
    /// </summary>
    public Vector2 pos;

    /// <summary>
    /// ��ǰ����ĵ�Ԫ������
    /// </summary>
    public Vector2 tilePos;

    /// <summary>
    /// ���������Ⱦ��
    /// </summary>
    SpriteRenderer spriteRenderer;

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
    /// ���Ʊ��ߵĵ�����Ϣ��������ɫ������߾�(����һС���������)
    /// </summary>
    /// <param name="pos">�Ⱦ�����</param>
    /// <param name="color">������ɫ</param>
    /// <param name="margin">ƫ��</param>
    static public void DebugDrawTile(Vector3 pos, Color color, float margin = 0)
    {
        // ���Ⱦ�����ת��Ϊ��������
        pos = Iso.MapToWorld(pos);

        // �����������ı߽�
        float d = 0.5f - margin;

        // ���������������
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(d, -d)), color);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(-d, -d)), pos + Iso.MapToWorld(new Vector2(-d, d)), color);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, d)), pos + Iso.MapToWorld(new Vector2(-d, d)), color);
        Debug.DrawLine(pos + Iso.MapToWorld(new Vector2(d, -d)), pos + Iso.MapToWorld(new Vector2(-d, -d)), color);
    }

    /// <summary>
    /// ������Ϸ�����������ĵ�����Ϣ��Ĭ����ɫΪ��ɫ(���µ�С����)
    /// </summary>
    /// <param name="pos">�Ⱦ�����</param>
    /// <param name="margin">ƫ��</param>
    static public void DebugDrawTile(Vector3 pos, float margin = 0)
    {
        DebugDrawTile(pos, Color.white, margin);
    }

    /// <summary>
    /// ��ȡ�������λ�õĵȾ�����(����Ѱ·������)
    /// </summary>
    /// <returns>���ĵȾ�����(ȡ��)</returns>
    static public Vector3 MouseTile()
    {
        // ��ȡ������������
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // ��������������ת��Ϊ�Ⱦ����꣬������ȡ��
        return Snap(MapToIso(mousePos));
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
        // ����ǰ����ĵȾ�����ת��Ϊ�������꣬�����ö����λ��
        transform.position = MapToWorld(pos);
        //�����������Ⱦ�㼶,��ֵԽ��Խ��ǰ,��Ϊ����-��,�����ֵԽСԽ��ǰ
        //y�᲻�ý�����,�����Ǹ��Ǿ�������ظ߶�,������֤ͬһ������,��ͼ��ľ�����ӿ���
        spriteRenderer.sortingOrder = -(int)(transform.position.y * spriteRenderer.sprite.pixelsPerUnit);
    }

    // �� Unity �༭���л��� Gizmos��������Ϣ��
    void OnDrawGizmosSelected()
    {
        // ���Ƶ�ǰ��Ϸ��������������Ϣ
        DebugDrawTile(pos);
    }
}