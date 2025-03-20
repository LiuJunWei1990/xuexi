using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathing
{
    /// <summary>
    /// ����,��ÿһ�������һ���ṹ,�������������,��������
    /// </summary>
    public struct Step
    {
        public Vector2 direction;
        public int directionIndex;
        public Vector2 pos;
    }

    /// <summary>
    /// ·��,�洢���еĲ���
    /// </summary>
    static private List<Step> path = new List<Step>();

    /// <summary>
    /// �ڵ���,A*�㷨�Ľڵ�
    /// </summary>
    //IEquatable,==��������.IComparable,><�Ƚ������
    class Node : IEquatable<Node>, IComparable<Node>
    {
        public float score;                      // �ڵ������(�ɱ� + ����ʽ����)
        public Vector2 pos;                      // �ڵ��λ��
        public Node parent;                      // ���ڵ�
        public Vector2 direction;                // �ƶ�����
        public int dirctionIndex;                // ��������

        private Node()
        {
        }

        /// <summary>
        /// ʵ�� IComparable �ӿڣ���������
        /// </summary>
        /// <param name="other">�����ȽϵĽڵ�</param>
        /// <returns>��С-1,0,1</returns>
        public int CompareTo(Node other)
        {
            return score.CompareTo(other.score);
        }

        /// <summary>
        /// ʵ�� IEquatable �ӿڣ����ڱȽϽڵ�
        /// </summary>
        /// <param name="other">�����ȽϵĽڵ�</param>
        /// <returns>�Ƿ���ͬ</returns>
        public bool Equals(Node other)
        {
            return this.pos == other.pos;
        }

        /// <summary>
        /// �ڵ��,�������Ż��ڴ����,û��ʵ����;
        /// </summary>
        static private List<Node> pool = new List<Node>();

        /// <summary>
        /// ��ȡ����ĵ�һ���ڵ�
        /// </summary>
        /// <returns>���ص�һ���ڵ�</returns>
        static public Node Get()
        {
            //�ز�Ϊ��,�Ϳ�ʼ��ȡ
            if (pool.Count > 0)
            {
                //��ȡ��һ���ڵ�
                Node node = pool[0];
                //����ɾ��������ڵ�
                pool.RemoveAt(0);
                //����
                return node;
            }
            //�����Ϊ�վͷ���һ���µĿսڵ�
            else return new Node();
        }

        /// <summary>
        /// ����(�б�)
        /// </summary>
        /// <param name="nodes">�ڵ��б�</param>
        static public void Recycle(List<Node> nodes)
        {
            //���б��еĽڵ㶼�յ�����,����ո��б�
            pool.AddRange(nodes);
            nodes.Clear();
        }

        /// <summary>
        /// ����(����)
        /// </summary>
        public void Recycle()
        {
            //�����ڵ��յ�����
            pool.Add(this);
        }
    }

    //A*�㷨��ر���
    /// <summary>
    /// Ŀ��λ��
    /// </summary>
    static private Vector2 target;
    /// <summary>
    /// �����б�
    /// </summary>
    static private List<Node> openNodes = new List<Node>();
    /// <summary>
    /// �ر��б�
    /// </summary>
    static private List<Node> closeNodes = new List<Node>();

    /// <summary>
    /// ��Ҫ�����Ŀ����ƶ��ķ���,�����ж���������ֵΪ8��16������
    /// </summary>
    static private Vector2[] directions;
    /// <summary>
    /// 8�����ܵ��ƶ�����
    /// </summary>
    static private Vector2[] directions8 =
    {
        new Vector2(-1, -1),
        new Vector2(-1, 0),
        new Vector2(-1, 1),
        new Vector2(0, 1),
        new Vector2(1, 1),
        new Vector2(1, 0),
        new Vector2(1, -1),
        new Vector2(0, -1) };
    /// <summary>
    /// 16�����ܵ��ƶ�����
    /// </summary>
    static private Vector2[] directions16 =
    {
        new Vector2(-1, -1), 
        new Vector2(-2, -1), 
        new Vector2(-1, 0), 
        new Vector2(-2, 1), 
        new Vector2(-1, 1), 
        new Vector2(-1, 2), 
        new Vector2(0, 1), 
        new Vector2(1, 2), 
        new Vector2(1, 1), 
        new Vector2(2, 1), 
        new Vector2(1, 0), 
        new Vector2(2, -1), 
        new Vector2(1, -1), 
        new Vector2(1, -2), 
        new Vector2(0, -1), 
        new Vector2(-1, -2)

    };

    /// <summary>
    /// ����...(�������ܷ���)
    /// </summary>
    /// <param name="node">���Ľڵ�</param>
    static private void StepTo(Node node)
    {
        //���Ľڵ���ӵ��رսڵ��б���
        closeNodes.Add(node);
        //���һ���յĽڵ����,��Ϊ�½ڵ�
        Node newNode = null;
        //�������ܵķ����·��
        for(int i = 0;i < directions.Length;++i)
        {
            Vector2 direction = directions[i];
            //���Ľڵ�+ĳһ����·��,���ڸ÷���Ľڵ�����(�õ��ǵȾ�����)
            Vector2 pos = node.pos + direction;

            //�����ʵ��������������ȡ�������Ӧ������,��ȡ����һ��bool,���������Ƿ��ͨ��,��ͨ�оͲ���ӽ������б�
            if (Tilemap.instance[pos])
            {
                //ǰ���Ǹ��սڵ�,Ҫ�ǻ�Ϊ��,��ȡ�ڵ������ĵ�һ���ڵ�,��Ϊ�վͲ�����һ��
                if (newNode == null) newNode = Node.Get();

                //�½ڵ�����긳ֵ,���ǵ�ǰ�������Ľڵ�����
                newNode.pos = pos;

                //Contains�б����Ƿ�����β�,��������ж������б����Ƿ�û��newNode,��û�о�ִ�д���
                if (!closeNodes.Contains(newNode) && !openNodes.Contains(newNode))
                {
                    //���ĵ�Ϊ�½ڵ�ĸ��ڵ�
                    newNode.parent = node;
                    //����,��Ϊֻ�ƶ�1�ľ���,�����·����һ����
                    newNode.direction = direction;
                    newNode.dirctionIndex = i;
                    //�ڵ�����:�ƶ��ɱ�+����ʽ����(�ƶ��ɱ���1,��Ϊÿ��ֻ�������ĵ���ΧһȦ�ľ���)
                    newNode.score = CalcScore(target,newNode);
                    //��ӵ����Žڵ���
                    openNodes.Add(newNode);
                    //�½ڵ��ÿ�
                    newNode = null;
                }
            }
        }
        //�˸����򶼱�������֮��,����½ڵ㻹δ�ÿվͻ��ոýڵ�,������ӵ�pool�ڵ������.
        if (newNode != null) newNode.Recycle();
    }

    /// <summary>
    /// //�ڵ�����:�ƶ��ɱ�+����ʽ����(�ƶ��ɱ�����һ������ĳ���,Լ����1��������ȷ)
    /// </summary>
    /// <param name="src">�ڵ�λ��</param>
    /// <param name="target">Ŀ��λ��</param>
    /// <returns></returns>
    static private float CalcScore(Vector2 src, Node target)
    {
        return target.direction.magnitude + Vector2.Distance(src, target.pos);
    }

    /// <summary>
    /// ��Ŀ��ڵ����,����·��
    /// </summary>
    /// <param name="node">A*Ѱ·�����սڵ�</param>
    static private void TraverseBack(Node node)
    {
        //�Ҹ��ڵ�,���ڵ㲻Ϊ�վ�ִ�д���
        while (node.parent != null)
        {
            //�½�һ������
            Step step = new Step();
            //�ѽڵ�����Ը�ֵ������
            step.direction = node.direction;
            step.directionIndex = node.dirctionIndex;
            step.pos = node.pos;
            //�Ѳ��Ӳ��뵽·���ĵ�һ��
            path.Insert(0, step);
            //�Ѹ��ڵ㸳����,�ݽ�
            node = node.parent;
        }
    }

    /// <summary>
    /// A*�㷨·������
    /// </summary>
    /// <param name="from">����</param>
    /// <param name="target">Ŀ��</param>
    /// <returns>����·���б�</returns>
    static public List<Step> BuildPath(Vector2 from, Vector2 target,int directionCount = 8)
    {
        //////////ǰ��׼��
        //�ж���8������16����
        directions = directionCount == 8 ? directions8 : directions16;
        //�Ȱ�Ŀ��㸳�����е�Ŀ���
        Pathing.target = target;
        //�����ڴ�,���Žڵ�͹رսڵ��
        Node.Recycle(openNodes);
        Node.Recycle(closeNodes);
        //���·���б�,׼�������µ�·��
        path.Clear();
        //���Ժ�Ŀ���غϾͲ���Ѱ��,ֱ�ӷ��ؿ�·����.
        if (from == target) return path;
        //������ʼ�ڵ�,�ӳ�����ȡ�ڵ�,�Ż��ڴ����
        Node startNode = Node.Get();
        //����ʼ�ڵ��ʼ��,���ָ�ֵ,���뿪���б�
        startNode.parent = null;
        startNode.pos = from;
        startNode.score = 999; //���ǵ�һ���ڵ�,��������ν,������߷־���,��Ϊ������Զ��
        openNodes.Add(startNode);


        ////////////��ʼ����
        //ѭ��������,������ѭ����
        int iterCount = 0;
        //�����б�Ϊ��,�Ϳ�ʼ��
        while (openNodes.Count > 0)
        {
            //�����б�����,ʹ��IComparable�˿�,�Ƚϵ���score�ڵ�����,���ǰ����ִ�С��������
            openNodes.Sort();
            //�����һ��,������Ļ������ر��б�,����,��Զ���Ǵ����һ��
            Node node = openNodes[0];
            //���Ŀ�겻��ͨ�� ���� �ڵ��и��ڵ� ���� �ڵ�����ִ��ڵ��ڸ��ڵ�(ԽѰԽԶ��)
            if (!Tilemap.instance[target] && node.parent != null && node.score >= node.parent.score)
            {
                //�ǻ�Ѱ��ƨ,����,����·��
                TraverseBack(node.parent);
                //����
                break;
            }

            //��Ŀ���ͽ�����,��ʼ����·��,Ȼ������ѭ��
            if (node.pos == target)
            {
                TraverseBack(node);
                break;
            }

            //û��Ŀ����ɾ��
            openNodes.RemoveAt(0);
            //����������node����ر��б�,���Ұ����ķ�����뿪���б�
            StepTo(node);

            iterCount += 1;
            if (iterCount > 100)
            {
                //Ѱ·ѭ������100��,�ͻ���,����·��,����������ѭ��
                TraverseBack(node);
                //����
                break;
            }
        }

        ///////////����
        //���ƹر��б�Ϳ����б��еĽڵ�
        foreach (Node node in closeNodes)
        {
            Iso.DebugDrawTile(node.pos, Color.magenta, 0.3f);
        }
        foreach (Node node in openNodes)
        {
            Iso.DebugDrawTile(node.pos, Color.green, 0.3f);
        }

        //����·��
        return path;
    }

    //����·����
    static public void DebugDrawPath(List<Step> path)
    {
        //��������·��
        for (int i = 0; i < path.Count - 1; ++i)
        {
            //���ߴӵ�ǰ�㵽��һ����
            Debug.DrawLine(Iso.MapToWorld(path[i].pos), Iso.MapToWorld(path[i+1].pos));
        }
    }
}
