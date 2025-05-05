using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathing 
{
    /// <summary>
    /// 步子,把每一步具象成一个结构,包含方向和坐标,方向索引
    /// </summary>
    public struct Step
    {
        /// <summary>
        /// 朝向(向量)
        /// </summary>
        public Vector2 direction;
        /// <summary>
        /// 朝向(索引)
        /// </summary>
        public int directionIndex;
        /// <summary>
        /// 坐标
        /// </summary>
        public Vector2 pos;
    }
    /// <summary>
    /// 节点类,A*算法的节点
    /// </summary>
    //IEquatable,==相等运算符.IComparable,><比较运算符
    class Node : IEquatable<Node>, IComparable<Node>
    {
        /// <summary>
        /// 节点的移动成本
        /// </summary>
        public float gScore;
        public float hScore;
        public float score;
        public Vector2 pos;
        public Node parent;

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
        /// 重写 GetHashCode 方法，用于哈希表
        /// 这个方法可以获取数值的哈希码
        /// 哈希码是一种耗能非常低的对比方法,在有大量对比的代码中使用对性能提升很大
        /// 这里重写的作用是区分X和Y坐标
        /// 如果Y轴不乘以100
        /// 那么(1,99)和(99,1)的哈希码是一样的
        /// 而这样重写后就是9901和199对比,这样就不一样了
        /// 当然哈希码对比的准确性也会降低,所以如果遇到相同的对比,就会使用Equals方法再确认一次
        /// </summary>
        /// <returns></returns>
        override public int GetHashCode()
        {
            return (int)(pos.x + pos.y * 100);
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
        /// <param name="nodes">ICollection列表容器的主要接口,主要功能就是增删改查,这里应该是为了兼容哈希列表</param>
        static public void Recycle(ICollection<Node> nodes)
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
    /// <summary>
    /// 路径
    /// </summary>
    static private List<Step> path = new List<Step>();

    //A*算法相关变量
    /// <summary>
    /// 目标位置
    /// </summary>
    static private Vector2 target;
    /// <summary>
    /// 开放列表
    /// </summary>
    static private BinaryHeap<Node> openNodes = new BinaryHeap<Node>(4096);
    /// <summary>
    /// 关闭列表
    /// </summary>
    static private HashSet<Node> closeNodes = new HashSet<Node>();

    /// <summary>
    /// 需要遍历的可能移动的方向,更具判断条件被赋值为8或16个方向
    /// </summary>
    static private Vector2[] directions;
    /// <summary>
    /// 8个可能的移动方向
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
    /// 16个可能的移动方向
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
    /// 方向数量
    /// </summary>
    static private int directionCount;

    /// <summary>
    /// 走向...(遍历可能方向)
    /// </summary>
    /// <param name="node">节点</param>
    static private void StepTo(Node node)
    {
        //添加一个空的节点对象,做为新节点
        Node newNode = null;
        //遍厉可能的方向的路径
        for (int i = 0; i < directions.Length; ++i)
        {
            //中心节点+某一方向路径,等于该方向的节点坐标(用的是等距坐标)
            Vector2 pos = node.pos + directions[i];

            //单元格库实例的索引器就是取按坐标对应的单元格,获取的是一个bool,代表单元格是否可通行,不通行就不添加进开放列表
            if (Tilemap.PassableTile(pos, 2))
            {
                //前面那个空节点,要是还为空,就取节点池里面的第一个节点,不为空就不做这一步
                if (newNode == null) newNode = Node.Get();

                //新节点的坐标赋值,就是当前这个方向的节点坐标
                newNode.pos = pos;

                //Contains:列表中是否包含形参,这里就是判断两个列表中是否都没有newNode,都没有就执行代码
                if (!closeNodes.Contains(newNode))
                {
                    //中心点为新节点的父节点
                    newNode.parent = node;
                    //>>>>>节点评分:移动成本+启发式距离<<<<<<<<<<
                    //移动成本是自己填写的g分数+这一步的距离,因为每次只遍历中心点周围一圈的距离
                    newNode.gScore = node.gScore + 1;
                    //启发是距离评分是目标点到新节点的距离
                    newNode.hScore = Mathf.Abs(target.x - newNode.pos.x) + Mathf.Abs(target.y - newNode.pos.y);
                    //移动成本+启发式距离
                    newNode.score = newNode.gScore + newNode.hScore;
                    //添加到开放节点中
                    openNodes.Add(newNode);
                    closeNodes.Add(newNode);
                    //新节点置空
                    newNode = null;
                }
            }
        }
        //方向都遍历完了之后,如果新节点还未置空就回收该节点,就是添加到pool节点池里面.
        if (newNode != null) newNode.Recycle();
    }
    /// <summary>
    /// 剔除掉不重要的节点
    /// :比如终点10,987654都可以用射线打到,代表这条路径上无阻挡,98765节点都不重要,可以剔除掉,10的父节点直接是4
    /// </summary>
    /// <param name="node"></param>
    static private void Collapse(Node node)
    {
        //如果父节点不为空,且父节点的父节点也不为空,就循环
        while (node.parent != null && node.parent.parent != null)
        {
            //当前节点到祖父节点之间,如不可通行就中止循环
            if (Tilemap.Raycast(node.pos, node.parent.parent.pos))
            {
                break;
            }
            //前面没跳出,继续回溯,把祖父节点变成父节点,继续循环
            node.parent = node.parent.parent;
        }

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
            //先剔除不重要的节点
            Collapse(node);
            //新建一个步子
            Step step = new Step();
            //把节点的属性赋值给步子
            step.direction = node.pos - node.parent.pos;
            step.directionIndex = Iso.Direction(node.parent.pos, node.pos, directionCount);
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
    static public List<Step> BuildPath(Vector2 from, Vector2 target, int directionCount = 8, float minRange = 0.1f)
    {
        //////////前期准备
        from = Iso.Snap(from);
        target = Iso.Snap(target);
        //清空路径列表,准备生成新的路径
        path.Clear();
        //来自和目标重合就不用寻了,直接返回空路径吧.
        if (from == target) return path;
        //回收内存,开放节点和关闭节点的
        openNodes.Clear();
        Node.Recycle(closeNodes);
        //寻路不再使用16向,只用8方向
        directions = directions8;
        Pathing.directionCount = directionCount;
        //先把目标点赋给类中的目标点
        Pathing.target = target;
        bool targetAccessible = Tilemap.Passable(target, 2);
        //创建起始节点,从池里面取节点,优化内存分配
        Node startNode = Node.Get();
        //给起始节点初始化,各种赋值,添入开放列表
        startNode.parent = null;
        startNode.pos = from;
        startNode.gScore = 0;
        //初始节点的评分无穷大,反正是起始,所以无穷大就好
        startNode.hScore = Mathf.Infinity;
        startNode.score = Mathf.Infinity;
        openNodes.Add(startNode);
        closeNodes.Add(startNode);


        ////////////开始生成
        //循环计数器,避免死循环的
        int iterCount = 0;
        //父节点,起始节点,做为当前的父节点
        Node bestNode = startNode;
        //开放列表不为空,就开始吧
        while (openNodes.Count > 0)
        {
            //处理第一个,处理完的会移至关闭列表,所以,永远都是处理第一个
            Node node = openNodes.Take();
            //如果当前节点的启发式距离评分小于父节点的启发式距离评分,就把当前节点做新的父节点
            if (node.hScore < bestNode.hScore) bestNode = node;
            //如果目标不可通行 并且 父节点不为空 并且 节点启发式距离的评分大于等于父节点(越寻越远了)
            if (!targetAccessible && node.parent != null && node.hScore > node.parent.hScore)
            {
                //那还寻个屁,回溯,从父节点的父节点开始生成路径
                TraverseBack(bestNode.parent);
                //跳出
                break;
            }

            //到目标点的停止距离就结束了,开始回溯路径,然后跳出循环
            if (node.hScore <= minRange)
            {
                TraverseBack(node);
                break;
            }
            //这个方法会把node加入关闭列表,并且把它的所有方向节点加入开放列表
            StepTo(node);

            iterCount += 1;
            if (iterCount > 500)
            {
                //寻路循环超过100次,就从父节点的父节点开始回溯,生成路径,避免陷入死循环
                TraverseBack(bestNode.parent);
                //跳出
                break;
            }
        }

        // ///////////画线
        // //绘制关闭列表和开放列表中的节点
        // foreach (Node node in closeNodes)
        // {
        //     Iso.DebugDrawTile(node.pos, Color.magenta, 0.3f);
        // }
        // foreach (Node node in openNodes)
        // {
        //     Iso.DebugDrawTile(node.pos, Color.green, 0.3f);
        // }

        //返回路径
        return path;
    }

    //绘制路径线
    static public void DebugDrawPath(Vector2 from, List<Step> path)
    {
        //如果路径不为空
        if (path.Count > 0)
        {
            //画路径线(灰色段),从人物到第一步
            Debug.DrawLine(Iso.MapToWorld(from), Iso.MapToWorld(path[0].pos), Color.grey);
        }
        //遍历所有路径,画路径线(默认颜色),每一步
        for (int i = 0; i < path.Count - 1; ++i)
        {
            //画路径线(默认颜色),从当前步到下一步
            Debug.DrawLine(Iso.MapToWorld(path[i].pos), Iso.MapToWorld(path[i + 1].pos));
        }
        //如果路径不为空
        if (path.Count > 0)
        {
            //画方格
            //路径终点的方格
            var center = Iso.MapToWorld(path[path.Count - 1].pos);
            Debug.DrawLine(center + Iso.MapToWorld(new Vector2(0, 0.15f)), center + Iso.MapToWorld(new Vector2(0, -0.15f)));
            Debug.DrawLine(center + Iso.MapToWorld(new Vector2(-0.15f, 0)), center + Iso.MapToWorld(new Vector2(0.15f, 0)));
        }
    }
}
