using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathing
{
    /// <summary>
    /// 步子,把每一步具象成一个结构,包含方向和坐标
    /// </summary>
    public struct Step
    {
        public Vector2 direction;
        public Vector2 pos;
    }

    /// <summary>
    /// 路径,存储所有的步子
    /// </summary>
    static private List<Step> path = new List<Step>();

    /// <summary>
    /// 节点类,A*算法的节点
    /// </summary>
    //IEquatable,==相等运算符.IComparable,><比较运算符
    class Node : IEquatable<Node>, IComparable<Node>
    {
        public float score;                      // 节点的评分(成本 + 启发式距离)
        public Vector2 pos;                      // 节点的位置
        public Node parent;                      // 父节点
        public Vector2 direction;                // 移动方向

        private Node()
        {
        }

        /// <summary>
        /// 实现 IComparable 接口，用于排序
        /// </summary>
        /// <param name="other">用来比较的节点</param>
        /// <returns>大小-1,0,1</returns>
        public int CompareTo(Node other)
        {
            return score.CompareTo(other.score);
        }

        /// <summary>
        /// 实现 IEquatable 接口，用于比较节点
        /// </summary>
        /// <param name="other">用来比较的节点</param>
        /// <returns>是否相同</returns>
        public bool Equals(Node other)
        {
            return this.pos == other.pos;
        }

        /// <summary>
        /// 节点池,仅用于优化内存分配,没有实际用途
        /// </summary>
        static private List<Node> pool = new List<Node>();

        /// <summary>
        /// 获取池里的第一个节点
        /// </summary>
        /// <returns>返回第一个节点</returns>
        static public Node Get()
        {
            //池不为空,就开始获取
            if (pool.Count > 0)
            {
                //获取第一个节点
                Node node = pool[0];
                //池里删除掉这个节点
                pool.RemoveAt(0);
                //返回
                return node;
            }
            //如果池为空就返回一个新的空节点
            else return new Node();
        }

        /// <summary>
        /// 回收(列表)
        /// </summary>
        /// <param name="nodes">节点列表</param>
        static public void Recycle(List<Node> nodes)
        {
            //把列表中的节点都收到池里,并清空该列表
            pool.AddRange(nodes);
            nodes.Clear();
        }

        /// <summary>
        /// 回收(个体)
        /// </summary>
        public void Recycle()
        {
            //单个节点收到池里
            pool.Add(this);
        }
    }

    //A*算法相关变量
    /// <summary>
    /// 目标位置
    /// </summary>
    static private Vector2 target;
    /// <summary>
    /// 开放列表
    /// </summary>
    static private List<Node> openNodes = new List<Node>();
    /// <summary>
    /// 关闭列表
    /// </summary>
    static private List<Node> closeNodes = new List<Node>();
    /// <summary>
    /// 8个可能的移动方向
    /// </summary>
    static private Vector2[] directions =
    {
        new Vector2(1, 0),
        new Vector2(0, 1),
        new Vector2(1, 1),
        new Vector2(-1, 0),
        new Vector2(0, -1),
        new Vector2(-1, -1),
        new Vector2(1, -1),
        new Vector2(-1, 1)

    };

    /// <summary>
    /// 遍历八个方向的节点
    /// </summary>
    /// <param name="node">中心节点</param>
    static private void StepTo(Node node)
    {
        //中心节点添加到关闭节点列表里
        closeNodes.Add(node);
        //添加一个空的节点对象,做为新节点
        Node newNode = null;
        //遍历八个方向的路径
        foreach (Vector2 direction in directions)
        {
            //中心节点+某一方向路径,等于该方向的节点坐标(用的是等距坐标)
            Vector2 pos = node.pos + direction;

            //网格库实例的索引器就是取按坐标对应的网格,获取的是一个bool,代表网格是否可通行,不通行就不添加进开放列表
            if (Tilemap.instance[pos])
            {
                //前面那个空节点,要是还为空,就取节点池里面的第一个节点,不为空就不做这一步
                if (newNode == null) newNode = Node.Get();

                //新节点的坐标赋值,就是当前这个方向的节点坐标
                newNode.pos = pos;

                //Contains列表中是否包含形参,这里就是判断两个列表中是否都没有newNode,都没有就执行代码
                if (!closeNodes.Contains(newNode) && !openNodes.Contains(newNode))
                {
                    //节点评分:移动成本+启发式距离(移动成本是1,因为每次只遍历中心点周围一圈的距离)
                    newNode.score = CalcScore(newNode.pos, target);
                    //中心点为新节点的父节点
                    newNode.parent = node;
                    //方向,因为只移动1的距离,方向和路径是一样的
                    newNode.direction = direction;
                    //添加到开放节点中
                    openNodes.Add(newNode);
                    //新节点置空
                    newNode = null;
                }
            }
        }
        //八个方向都遍历完了之后,如果新节点还未置空就回收该节点,就是添加到pool节点池里面.
        if (newNode != null) newNode.Recycle();
    }

    /// <summary>
    /// //节点评分:移动成本+启发式距离(移动成本是1,因为每次只遍历中心点周围一圈的距离)
    /// </summary>
    /// <param name="src">节点位置</param>
    /// <param name="target">目标位置</param>
    /// <returns></returns>
    static private float CalcScore(Vector2 src, Vector2 target)
    {
        return 1 + Vector2.Distance(src, target);
    }

    /// <summary>
    /// 从目标节点回溯,生成路径
    /// </summary>
    /// <param name="node">A*寻路的最终节点</param>
    static private void TraverseBack(Node node)
    {
        //找父节点,父节点不为空就执行代码
        while (node.parent != null)
        {
            //新建一个步子
            Step step = new Step();
            //把节点的属性赋值给步子
            step.direction = node.direction;
            step.pos = node.pos;
            //把步子插入到路径的第一个
            path.Insert(0, step);
            //把父节点赋过来,递进
            node = node.parent;
        }
    }

    /// <summary>
    /// A*算法路径生成
    /// </summary>
    /// <param name="from">来自</param>
    /// <param name="target">目标</param>
    /// <returns>返回路径列表</returns>
    static public List<Step> BuildPath(Vector2 from, Vector2 target)
    {
        //////////前期准备

        //先把目标点赋给类中的目标点
        Pathing.target = target;
        //回收内存,开放节点和关闭节点的
        Node.Recycle(openNodes);
        Node.Recycle(closeNodes);
        //清空路径列表,准备生成新的路径
        path.Clear();
        //来自和目标重合就不用寻了,直接返回空路径吧.
        if (from == target) return path;
        //创建起始节点,从池里面取节点,优化内存分配
        Node startNode = Node.Get();
        //给起始节点初始化,各种赋值,添入开放列表
        startNode.parent = null;
        startNode.pos = from;
        startNode.score = CalcScore(from, target);
        openNodes.Add(startNode);


        ////////////开始生成
        //循环计数器,避免死循环的
        int iterCount = 0;
        //开放列表不为空,就开始吧
        while (openNodes.Count > 0)
        {
            //开放列表排序,使用IComparable端口,比较的是score节点评分,就是按评分从小到大排序
            openNodes.Sort();
            //处理第一个,处理完的会移至关闭列表,所以,永远都是处理第一个
            Node node = openNodes[0];
            //如果目标不可通行 并且 节点有父节点 并且 节点的评分大于等于父节点(越寻越远了)
            if (!Tilemap.instance[target] && node.parent != null && node.score >= node.parent.score)
            {
                //那还寻个屁,回溯,生成路径
                TraverseBack(node.parent);
                //跳出
                break;
            }

            //到目标点就结束了,开始回溯路径,然后跳出循环
            if (node.pos == target)
            {
                TraverseBack(node);
                break;
            }

            //没到目标点就删掉
            openNodes.RemoveAt(0);
            //这个方法会把node加入关闭列表,并且把它的八个方向加入开放列表
            StepTo(node);

            iterCount += 1;
            if (iterCount > 1000)
            {
                //避免陷入死循环
                Debug.LogWarning("异常多的路径生成迭代,中止");
                //暂停编辑器并跳转到这一行
                Debug.Break();
                //跳出
                break;
            }
        }

        ///////////画线
        //绘制关闭列表和开放列表中的节点
        foreach (Node node in closeNodes)
        {
            Iso.DebugDrawTile(node.pos, Color.magenta, 0.3f);
        }
        foreach (Node node in openNodes)
        {
            Iso.DebugDrawTile(node.pos, Color.green, 0.3f);
        }

        //返回路径
        return path;
    }

    //绘制路径线
    static public void DebugDrawPath(List<Step> path)
    {
        //遍历所有路径
        for (int i = 0; i < path.Count - 1; ++i)
        {
            //画线从当前点到下一步点
            Debug.DrawLine(Iso.MapToWorld(path[i].pos), Iso.MapToWorld(path[i+1].pos));
        }
    }
}
