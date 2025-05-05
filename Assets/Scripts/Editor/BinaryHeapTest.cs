using System.Collections.Generic;
using NUnit.Framework;

// 二叉堆测试类
public class BinaryHeapTest
{
    // 简单测试用例
    [Test]
    public void SimpleTest()
    {
        // 创建一个最大容量为1024的二叉堆
        var heap = new BinaryHeap<int>(1024);
        // 测试数据集合
        var testCollection = new List<int>(new int[]{ 1, 3, 4, 2 });

        // 将测试数据逐个添加到堆中
        foreach(var item in testCollection)
        {
            heap.Add(item);
            // 打印当前堆状态
            System.Console.Write(heap);
        }

        // 验证堆中元素数量与测试数据数量一致
        Assert.AreEqual(testCollection.Count, heap.Count);

        // 对测试数据进行排序
        testCollection.Sort();

        // 逐个取出堆顶元素并与排序后的测试数据比较
        foreach (var item in testCollection)
        {
            var gotItem = heap.Take();
            // 打印当前堆状态
            System.Console.Write(heap);
            // 验证取出的元素与预期一致
            Assert.AreEqual(gotItem, item);
        }
    }

    // 压力测试用例
    [Test]
    public void StressTest()
    {
        // 创建一个最大容量为128的二叉堆
        var heap = new BinaryHeap<int>(128);
        // 测试数据集合
        var testCollection = new List<int>();
        // 测试次数
        const int TrialCount = 100;
        // 随机数生成器，固定种子666
        var random = new System.Random(666);

        // 进行多次测试
        for (int trial = 0; trial < TrialCount; ++trial)
        {
            // 清空测试数据和堆
            testCollection.Clear();
            heap.Clear();

            // 生成随机测试数据
            for(int i = 0; i < heap.MaxSize; ++i)
            {
                testCollection.Add(random.Next());
            }

            // 将测试数据逐个添加到堆中
            foreach (var item in testCollection)
            {
                heap.Add(item);
            }

            // 验证堆中元素数量与测试数据数量一致
            Assert.AreEqual(testCollection.Count, heap.Count);

            // 对测试数据进行排序
            testCollection.Sort();

            // 逐个取出堆顶元素并与排序后的测试数据比较
            foreach (var item in testCollection)
            {
                var gotItem = heap.Take();
                // 验证取出的元素与预期一致
                Assert.AreEqual(gotItem, item);
            }
        }
    }
}
