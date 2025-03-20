using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��Ƭ��,���ڱ�ǵ�ͼ�ϵ���Ƭ,���ұ���Ƿ��ͨ��
/// </summary>
/// ����:���ڱ༭ģʽ�����иýű�
[ExecuteInEditMode]
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

    /// <summary>
    /// ������,���ڵ�����ͨ�������Xƫ��
    /// </summary>
    [Range(-5, 5)]
    public int offsetX = 0;

    /// <summary>
    /// ������,���ڵ�����ͨ�������Yƫ��
    /// </summary>
    [Range(-5, 5)]
    public int offsetY = 0;

    private void Start()
    {

    }

    private void Update()
    {
    }

    /// <summary>
    /// ��Ƭ��ѡ��ʱ���Ƹ���Ƭ������
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        //��ȡ��Ƭ�ĵȾ�����
        Vector3 pos = Iso.MapToIso(transform.position);
        //��ȡ��Ƭ�����Ͻ�����
        pos.x -= width / 2;
        pos.y -= height / 2;
        //������Ƭ��ƫ����
        pos.x += offsetX;
        pos.y += offsetY;
        //������Ƭ��ÿһ������
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //���ݿ�ͨ����������ɫ
                Gizmos.color = passable ? new Color(1, 1, 1, 0.2f) : new Color(1, 0, 0, 0.3f);
                //��������,��СΪ0.9f
                Iso.GizmosDrawTile(pos + new Vector3(x, y), 0.9f);
            }
        }
    }
}
