using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 路径规划类
/// </summary>
/// <remarks>
/// [表参数]提供角色组件表驱动的路径参数
/// 这个类是一个工具类, 用于计算路径,并传递参数给Character.path,没有实例
/// </remarks>
/// 没有实例,哪个对象调就给哪个对象的Character用,用之前会清空Pathing中的所有成员的值

public class Pathing {
    /// <summary>
    /// 路径步骤结构体
    /// </summary>
    /// <remarks>
    /// 路径中的一段, 包含方向和位置,是一个直线,一般方向变了就会新建一个Step
    /// </remarks>
	public struct Step {
        /// <summary>
        /// 方向(向量)
        /// </summary>
		public Vector2 direction;
        /// <summary>
        /// 方向(索引)
        /// </summary>
        /// <remarks>
        /// 方向索引对应表
        /// 0: 左上（(-1, -1)）
        /// 1: 左（(-1, 0)）
        /// 2: 左下（(-1, 1)）
        /// 3: 下（(0, 1)）
        /// 4: 右下（(1, 1)）
        /// 5: 右（(1, 0)）
        /// 6: 右上（(1, -1)）
        /// 7: 上（(0, -1)）
        /// </remarks>
		public int directionIndex;
        /// <summary>
        /// 路径步骤的终点
        /// </summary>
		public Vector2 pos;
	}
    /// <summary>
    /// 节点结构体
    /// </summary>
    /// <remarks>
    /// 开放和关闭列表中会遍历的单元格,用节点来代表单元格
    /// </remarks>
	class Node : IEquatable<Node>, IComparable<Node> {
        /// <summary>
        /// G: 从起点到当前节点的实际代价
        /// </summary>
		public float gScore;
        /// <summary>
        /// H: 从当前节点到目标节点的估计代价
        /// </summary>
        public float hScore;
        /// <summary>
        /// score: G + H
        /// </summary>
        public float score;
        /// <summary>
        /// 节点坐标
        /// </summary>
		public Vector2 pos;
        /// <summary>
        /// 父节点
        /// </summary>
		public Node parent;
		private Node() {
		}
        /// <summary>
        /// 比较接口,比大小的方法
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <remarks>
        /// 在Node排序或比较大小时,回调这个方法
        /// </remarks>
		public int CompareTo(Node other) {
			return score.CompareTo(other.score);
		}
        /// <summary>
        /// 对比接口,对比相同与否的方法
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <remarks>
        /// 在Node类对比是否相等时,回调这个方法
        /// </remarks>
		public bool Equals(Node other) {
			return this.pos == other.pos;
		}
        /// <summary>
        /// 对比接口,哈希码获取
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Equals不是单纯比较pos,会先对比两值得哈希码这样性能更好,本方法重载用来自定义哈希码如何取值.返回的值最后也会转成哈希码再来做对比
        /// </remarks>
        override public int GetHashCode()
        {
            return (int)(pos.x + pos.y * 100);
        }
        /// <summary>
        /// 节点池
        /// </summary>
        /// <remarks>
        /// 因为寻路生成的节点会很多,用于回收节点,减少内存分配
        /// </remarks>
        static private List<Node> pool = new List<Node>();
        /// <summary>
        /// 获取节点
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 节省内存的,池里有就池里取,没有就new一个 || 不用管原赋值,取了节点会重新赋值再用
        /// </remarks>
		static public Node Get() 
        {
			if (pool.Count > 0) 
            {
				Node node = pool[0];
				pool.RemoveAt(0);
				return node;
			} 
            else 
            {
				return new Node();
			}
		}
        /// <summary>
        /// 回收节点(容器版)
        /// </summary>
        /// <param name="nodes"></param>
        /// <remarks>
        /// 回收整个容器到池里,优化内存
        /// </remarks>
		static public void Recycle(ICollection<Node> nodes) {
			pool.AddRange(nodes);
			nodes.Clear();
		}
        /// <summary>
        /// 回收节点(节点版)
        /// </summary>
        /// <remarks>
        /// 回收单个节点到池里,优化内存
		public void Recycle() {
			pool.Add(this);
		}
	}
    /// <summary>
    /// 当前路径
    /// </summary>
    static private List<Step> path = new List<Step>();
    /// <summary>
    /// 当前路径的目标点
    /// </summary>
    static private Vector2 target;
    /// <summary>
    /// 开放列表
    /// </summary>
    /// <remarks>
    /// 二叉堆容器,特点是叉状树结构,并且永远按上小下大的顺序排列,任何修改都会重新排序
    /// </remarks>
	static private BinaryHeap<Node> openNodes = new BinaryHeap<Node>(4096);
    /// <summary>
    /// 关闭列表
    /// </summary>
    /// <remarks>
    /// 哈希集合容器,特点是不允许重复元素,重复元素无法加入,并且用哈希比对,极限的快速查找
    /// </remarks>
	static private HashSet<Node> closeNodes = new HashSet<Node>();
    /// <summary>
    /// 方向数组
    /// </summary>
    /// <remarks>
    /// 周围一圈单元格的向量数组, 用于计算路径.几向就几个单元格
    /// </remarks>
	static private Vector2[] directions;
    /// <summary>
    /// 方向数组预制体(8方向)
    /// </summary>
    /// <remarks>
    /// 确定是8向寻路后,会赋值给方向数组
    /// </remarks>
	static private Vector2[] directions8 = { new Vector2(-1, -1), new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(1, -1), new Vector2(0, -1) };
    /// <summary>
    /// 方向数组预制体(16方向)
    /// </summary>
    /// <remarks>
    /// 确定是16向寻路后,会赋值给方向数组
    /// </remarks>
	static private Vector2[] directions16 = { new Vector2(-1, -1), new Vector2(-2, -1), new Vector2(-1, 0), new Vector2(-2, 1), new Vector2(-1, 1), new Vector2(-1, 2), new Vector2(0, 1), new Vector2(1, 2), new Vector2(1, 1), new Vector2(2, 1), new Vector2(1, 0), new Vector2(2, -1), new Vector2(1, -1), new Vector2(1, -2), new Vector2(0, -1), new Vector2(-1, -2) };
    /// <summary>
    /// 当前规划的路径是几向的
    /// </summary>
    static private int directionCount;
    /// <summary>
    /// 下一步骤
    /// </summary>
    /// <param name="node"></param>
    /// <remarks>
    /// 把当前节点的四周的节点都加入开放列表, 并且计算它们的G,H,score;;
    /// 放入开放列表时会同时放入关闭列表,代表已经处理完了
    /// </remarks>
    static private void StepTo(Node node)
    {
		Node newNode = null;

		for (int i = 0; i < directions.Length; ++i)
        {
			Vector2 pos = node.pos + directions[i];

            if (Tilemap.PassableTile(pos, 2))
            {
                if (newNode == null)
                    newNode = Node.Get();
                newNode.pos = pos;
                //查关闭列表中是不是没有分数的节点,没有就添加,这样保证关闭列表中只有一条路线
                if (!closeNodes.Contains(newNode))
                { 
					newNode.parent = node;
                    newNode.gScore = node.gScore + 1;
                    newNode.hScore = Mathf.Abs(target.x - newNode.pos.x) + Mathf.Abs(target.y - newNode.pos.y);
                    newNode.score = newNode.gScore + newNode.hScore;
                    //开放列表这个容器Add时会自动排序
					openNodes.Add(newNode);
                    //关闭列表这个容器Add时如果有相同的会自动放弃,不会重复添加
                    closeNodes.Add(newNode);
                    newNode = null;
				}
			}
		}
        //弄完节点不为空,就把它回收到池里,优化内存
		if (newNode != null)
			newNode.Recycle();
	}
    /// <summary>
    /// 折叠节点
    /// </summary>
    /// <param name="node"></param>
    /// <remarks>
    /// 就是把直线上的Node只保留两端,中间都删掉
    /// </remarks>
    static private void Collapse(Node node)
    {
        while (node.parent != null && node.parent.parent != null)
        {
            if (Tilemap.Raycast(node.pos, node.parent.parent.pos))
            {
                break;
            }

            node.parent = node.parent.parent;
        }
    }
    /// <summary>
    /// 回溯路径
    /// </summary>
    /// <param name="node"></param>
	static private void TraverseBack(Node node)
    {
        while (node.parent != null)
        {
            //一次性折叠到能折叠的最远那个节点
            Collapse(node);
            Step step = new Step();
            step.direction = node.pos - node.parent.pos;
            step.directionIndex = Iso.Direction(node.parent.pos, node.pos, directionCount);
            step.pos = node.pos;
            //把元素从{0}位置插入列表,0就是开头
            path.Insert(0, step);
            //一直循环到起点
            node = node.parent;
        }
    }
    /// <summary>
    /// 生成路径
    /// </summary>
    /// <param name="from">起点</param>
    /// <param name="target">目标点</param>
    /// <param name="directionCount">几向移动</param>
    /// <param name="minRange">中止距离</param>
    /// <returns></returns>
    /// <remarks>
    /// 因为成员都是静态的,所以生成路径是要全部清理和赋值一遍
    /// </remarks>
	static public List<Step> BuildPath(Vector2 from, Vector2 target, int directionCount = 8, float minRange = 0.1f)
    {
        //初始化
        from = Iso.Snap(from);
        target = Iso.Snap(target);
        path.Clear();
        if (from == target)
            return path;
        openNodes.Clear();
        Node.Recycle(closeNodes);
        directions = directions8;
        Pathing.directionCount = directionCount;
        Pathing.target = target;
        bool targetAccessible = Tilemap.Passable(target, 2); // 目标点是否可通行,检测目标和上下左右单元格是否有阻挡
        Node startNode = Node.Get();
		startNode.parent = null;
		startNode.pos = from;
		startNode.gScore = 0;
        startNode.hScore = Mathf.Infinity;
        startNode.score = Mathf.Infinity;//起始节点肯定是离最远的,无所谓排序评分直接无限大
		openNodes.Add(startNode);
        closeNodes.Add(startNode);
        int iterCount = 0; //计数器,防止死循环的
        Node bestNode = startNode; //最优节点?
        // 因为成员都是静态的,所以生成路径是要全部清理一遍
        //-------------------------------------------------------------
        // 开始计算路径
		while (openNodes.Count > 0)
        {
			Node node = openNodes.Take();
            //更新最佳节点
            if (node.hScore < bestNode.hScore)
                bestNode = node;
            //目标点不可通行,并且节点分数越来越大了,就是寻到离目标最近的节点了,回溯
            if (!targetAccessible && node.parent != null && node.hScore > node.parent.hScore)
            {
                TraverseBack(bestNode.parent);
                break;
            }
            //节点分数小于最小距离,就是到目标点了,回溯
            if (node.hScore <= minRange)
            {
                TraverseBack(node);
				break;
			}
            //走一步,把周围格子都放进开放列表和关闭列表
            //放入开放列表,这一圈格子里最小的会排序到顶端
            //放入关闭节点,不会存相同分数的节点,省点内存
			StepTo(node);
            //指针加一
			iterCount += 1;
            ///溢出了,回溯
			if (iterCount > 500)
            {
                TraverseBack(bestNode.parent);
                break;
			}
		}
        //foreach (Node node in closeNodes)
        //{
        //    Iso.DebugDrawTile(node.pos, Color.magenta, 0.3f);
        //}
        //foreach (Node node in openNodes)
        //{
        //    Iso.DebugDrawTile(node.pos, Color.green, 0.3f);
        //}
        //循环完成后返回计算出的路径
        return path;
	}
    /// <summary>
    /// 路径画辅助线
    /// </summary>
    /// <param name="from">起点</param>
    /// <param name="path">路径</param>
    /// <remarks>
    /// 鼠标点击寻路的那个辅助线,鼠标指向和点击都会画线
    /// </remarks>
	static public void DebugDrawPath(Vector2 from, List<Step> path) {
        //第一段步骤画灰线
        if (path.Count > 0)
        {
            Debug.DrawLine(Iso.MapToWorld(from), Iso.MapToWorld(path[0].pos), Color.grey);
        }
        //其他步骤画白线(默认)
		for (int i = 0; i < path.Count - 1; ++i)
        {
			Debug.DrawLine(Iso.MapToWorld(path[i].pos), Iso.MapToWorld(path[i + 1].pos));
		}
        //目标点上画个X
        if (path.Count > 0)
        {
            var center = Iso.MapToWorld(path[path.Count - 1].pos);
            Debug.DrawLine(center + Iso.MapToWorld(new Vector2(0, 0.15f)), center + Iso.MapToWorld(new Vector2(0, -0.15f)));
            Debug.DrawLine(center + Iso.MapToWorld(new Vector2(-0.15f, 0)), center + Iso.MapToWorld(new Vector2(0.15f, 0)));
        }
	}
}
