using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 傀儡控制器组件,用于控制傀儡的行为
/// </summary>
public class DummyController : MonoBehaviour
{
    /// <summary>
    /// 角色组件引用
    /// </summary>
    Character character;
    /// <summary>
    /// iso组件引用
    /// </summary>
    Iso iso;
    /// <summary>
    /// 目标的角色组件引用
    /// </summary>
    Character target;
    /// <summary>
    /// 静态数组,用于存储周围的傀儡
    /// </summary>
    static GameObject[] siblings = new GameObject[1024];
    void Awake()
    {
        //获取角色组件和iso组件
        character = GetComponent<Character>();
        iso = GetComponent<Iso>();
    }

    void Start()
    {
        //给角色组件的OnTakeDamage事件添加监听,当角色受到伤害时,执行OnTakeDamage方法
        character.OnTakeDamage += OnTakeDamage;
        //启动协程,执行Roam(闲逛)方法
        StartCoroutine(Roam());
    }
    /// <summary>
    /// 这个方法监听了角色的受伤事件,当角色受到伤害时,执行这个方法
    /// </summary>
    /// <param name="originator">施暴者</param>
    /// <param name="damage">伤害</param>
    void OnTakeDamage(Character originator, int damage)
    {
        //当前傀儡(自身)攻击施暴者
        Attack(originator);
        //计算附近傀儡数量并放入傀儡对象数组,Tilemap.OverlapBox检测区域中所有对象并存入数组(中心点,范围,用于保存的数组)
        int siblingsCount = Tilemap.OverlapBox(iso.pos, new Vector2(20, 20), siblings);
        //遍历数组中已保存的傀儡
        for(int i = 0; i < siblingsCount; ++i)
        {
            //获取对应的傀儡控制器组件
            DummyController sibling = siblings[i].GetComponent<DummyController>();
            //如果有这个组件,并且不是自己,就指挥它攻击施暴者(ADD)
            if (sibling != null && sibling != this)
            {
                sibling.Attack(originator);
            }
        }        
    }
    /// <summary>
    /// 携程,闲逛
    /// </summary>
    /// <returns></returns>
    IEnumerator Roam()
    {
        //等待一帧,等待协程执行完毕,再执行下面的代码
        yield return new WaitForEndOfFrame();
        //死循环,靠携程中止来跳出
        while (!this.target)
        {
            //生成一个周围8格的随机坐标,作为目标点
            var target = iso.pos + new Vector2(Random.Range(-8f, 8f), Random.Range(-8f, 8f));
            //行走至目标点
            character.GoTo(target);
            //等待1到3秒,再继续循环
            yield return new WaitForSeconds(Random.Range(1f, 3f));
        }
    }
    void Attack(Character target)
    {
        this.target = target;
        StartCoroutine(Attack());
    }
    /// <summary>
    /// 携程,攻击
    /// </summary>
    /// <returns></returns>
    IEnumerator Attack()
    {
        //死循环,靠携程中止来跳出
        while (true)
        {
            //攻击目标
            character.Attack(target);
            //等待0.15到1秒,再继续循环
            yield return new WaitForSeconds(Random.Range(0.15f, 1f));
        }
    }
}
