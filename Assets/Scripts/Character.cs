using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    /// <summary>
    /// ��������
    /// </summary>
    [Tooltip("��ɫ������������")]
    public int directionCount = 8;
    /// <summary>
    /// �ٶ�
    /// </summary>
    public float speed = 3.5f;
    /// <summary>
    /// �����ٶ�
    /// </summary>
    public float attackSpeed = 1.0f;
    /// <summary>
    /// �Ƿ����ܶ�
    /// </summary>
    [Tooltip("�Ƿ���")]
    public bool run = false;
    /// <summary>
    /// ��ǰ����������
    /// </summary>
    ///����:��Inspector���������
    [HideInInspector]
    public Usable usable;
    /// <summary>
    /// ����(����)
    /// </summary>
    /// ����:��Inspector���������
    [HideInInspector]
    public int direction = 0;
    /// <summary>
    /// �Ⱦ���������
    /// </summary>
    Iso iso;
    /// <summary>
    /// �������
    /// </summary>
    Animator animator;

    /// <summary>
    /// ·��,����Ϊһ��װ���������
    /// </summary>
    List<Pathing.Step> path = new List<Pathing.Step>();

    /// <summary>
    /// �Ѿ��ƶ��ľ���
    /// </summary>
    float traveled = 0;

    /// <summary>
    /// Ŀ�귽��
    /// </summary>
    int targetDirection = 0;
    /// <summary>
    /// �Ƿ����ڹ���
    /// </summary>
    bool attack = false;
    /// <summary>
    /// ��������(���)
    /// </summary>
    int attackAnimation;

    private void Start()
    {
        //��ȡ�������
        iso = GetComponent<Iso>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// ʹ��/����
    /// </summary>
    /// <param name="usable">ʹ�õ�Ŀ����</param>
    public void Use(Usable usable)
    {
        //׼��Ҫʹ�õ����������ʹ�õ�����ʱͬһ��������,����Ҫִ���κδ���.
        if (this.usable == usable) return;
        //�������������·��
        GoTo(usable.GetComponent<Iso>().tilePos);
        //����·����ѵ�ǰ��������Ϊ����ʹ�õ����,��Ϊ����·�������õ�ǰ��������Ϊ��,���Է��ں���
        this.usable = usable;
    }

    /// <summary>
    /// ������(����·��,�ƶ�����Move()ʵ�ֵ�)
    /// </summary>
    /// <param name="target">Ŀ���(�������)</param>
    public void GoTo(Vector2 target)
    {
        //�������ִ�й�������,������,ֱ������
        if (attack) return;
        //����·��֮ǰ,���õ�ǰ����������
        this.usable = null;
        //////////////��һ���������ԭ��·��,��Ϊ������һ����Ŀ�ĵ�.

        //ȡ��ǰ��Ϸ����ĵȾ�����,��Iso��������ȡ
        Vector2 startPos = iso.tilePos;
        //·����������0,���Ǵ�����·��û����(��Ҫ���ԭ�е�·��)
        if(path.Count > 0)
        {
            //��ȡԭ��һ��·��
            var firstStep = path[0];
            //�����·������Ŀ���(ԭ��һ��·��)
            startPos = firstStep.pos;
            //���·��
            path.Clear();
            //��ԭ���ĵ�һ��·��������ӵ��ղ��Ѿ���յ�·����,��������Ŀ����Ϊ�˱�������ͻȻ��ס,Ʈ��,���ֵ�����
            path.Add(firstStep);
        }
        else
        {
            //·�������˾��Ǵ���״̬,ֱ����վ�����.
            path.Clear();
            traveled = 0;
        }

        ////////////////�ڶ�����,�Ǵ����µ�·��

        //�������ɲ����·��,ע��ǰ���Ѿ���firstStep��Ϊ��һ��·����.
        path.AddRange(Pathing.BuildPath(Iso.Snap(startPos), target,directionCount));
    }

    /// <summary>
    /// ˲��
    /// </summary>
    /// <param name="target">Ŀ���</param>
    public void Teleport(Vector2 target)
    {
        if (attack) return;
        //�ж�Ŀ�������Ƿ��ͨ��
        if (Tilemap.instance[target])
        {
            //��ͨ�о�ֱ��˲��
            iso.pos = target;
            iso.tilePos = target;
        }
        else
        {
            //����ͨ�оͻ�·��,׼��˲�Ƶ�����Ѱ·�Ĺ����Ŀ������
            var pathToTarget = Pathing.BuildPath(Iso.Snap(iso.tilePos), target,directionCount);
            //·����Ϊ��,Ϊ�վͷ���
            if (pathToTarget.Count == 0) return;
            //����-1,����·�������е����һ��·��,˲�ƹ�ȥ
            iso.pos = pathToTarget[pathToTarget.Count - 1].pos;
            iso.tilePos = iso.pos;
        }
        //��Ȼ��˲��,�Ͱ�·�����
        path.Clear();
        //���߾���Ҳ����
        traveled = 0;
    }

    private void Update()
    {
        //��������վ����������
        Iso.DebugDrawTile(iso.tilePos);
        //��·����
        Pathing.DebugDrawPath(path);
        //�ƶ���ɫ
        Move();

        //ִ���굱ǰ֡��Move()��,���·��Ϊ��,�����е�ǰ����������
        if(path.Count == 0 && usable)
        {
            //ʹ�õ�ǰ����
            usable.Use();
            //��ǰ��������Ϊ��
            usable = null;
        }
        //���¸�������
        UpdateAnimation();
    }
    /// <summary>
    /// �ƶ���ɫ
    /// </summary>
    private void Move()
    {
        //��֧1.·��Ϊ�վͷ���
        if (path.Count == 0) return;


        //��ȡ��һ��·��
        Vector2 step = path[0].direction;
        //�����һ·���ĳ���;
        float stepLen = step.magnitude;

        //���㵱ǰ֡���ƶ�����
        float distance = speed * Time.deltaTime;

        //��֧2.����һ֡Ҫ������һ·����,���߾�����ϵ�ǰ֡���볬����һ·������
        while (traveled + distance >= stepLen)
        {
            //������һ��·��ľ���
            float firstPart = stepLen - traveled;
            //��ɫ�ƶ�����һ·��,��������ǰѵ�һ·����һ�����Ծ���.��һ�����ǰ�·����ɷ���,���Գ��Ⱥ�ͻ���һ��·��.
            iso.pos += step.normalized * firstPart;
            //��ǰ֡����,Ҫ��ȥ�����ɫ�ƶ����ľ���.
            distance -= firstPart;
            //�����Ѿ��ƶ��˵ľ���(������ΪʲôҪ��ȥ��һ��·���ĳ���)
            traveled += firstPart - stepLen;
            //���½�ɫ���������λ��
            iso.tilePos += step;
            //��һ��·���Ѿ�������,ɾ��
            path.RemoveAt(0);
            //·����Ϊ�վͼ�����ȡ�����·��
            if (path.Count > 0)
            {
                step = path[0].direction;
            }
        }
        //��֧3.·����Ϊ�վͿ���
        if (path.Count > 0)
        {
            traveled += distance;
            iso.pos += step.normalized * distance;
        }
        //��֧4.�������ִ����֮��·������,תΪ����
        if (path.Count == 0)
        {
            //����ȡ��
            iso.pos.x = Mathf.Round(iso.pos.x);
            iso.pos.y = Mathf.Round(iso.pos.y);
            //�ƶ��������
            traveled = 0;
        }
    }

    /// <summary>
    /// ���¶���
    /// </summary>
    private void UpdateAnimation()
    {
        //�Ƿ�ά�ֶ�����ʱ�����,�������²��ŵ�ǰ����ʱ,����֮ǰ�Ľ��ȼ�������
        bool preserveTime = false;
        //��������
        string animation;
        //�������������ʼֵ
        animator.speed = 1.0f;
        //������ڹ���
        if (attack)
        {
            //���������Ƹ�ֵ
            animation = "Attack" + attackAnimation;
            //�������ٶȸ�ֵ
            animator.speed = attackSpeed;
        }

        //û��·�����Ǵ���״̬
        else if(path.Count == 0)
        {
            //����������ֵ
            animation = "Idle";
            //�˶�����Ҫά��,ʱ�����
            preserveTime = true;
        }
        //�����������
        else
        {
            //ͨ�����ܱ�ǩ�ж����ܻ�����
            animation = run ? "Run" : "Walk";
            //�˶�����Ҫά��,ʱ�����
            preserveTime = true;
            //Ŀ�귽����·���ĵ�һ���ķ���
            targetDirection = path[0].directionIndex;
        }

        //������ﳯ���Ŀ�귽��һ��,��ת��
        if (!attack && direction != targetDirection)
        {

            //���㵱ǰ�����Ŀ�귽��ļн�,��ȡ�нǵ�����,��������˳ʱ��,����������ʱ��
            int diff = (int)Mathf.Sign(Tools.ShortestDelta(direction, targetDirection, directionCount));
            //ƽ���ĸ��µ�ǰ����,ȷ������ֵ�� [0, directionCount - 1] ��Χ��
            direction = (direction + diff + directionCount) % directionCount;
        }
        //�������Ƽ��Ϸ�����ַ���
        animation += "_" + direction.ToString();
        //GetCurrentAnimatorStateInfo(0),���ض���״̬����Ϣ,0�ǲ������,0�����Ĭ�ϲ�,�ǵ�ǰ������״̬��Ϣ
        //IsName()�жϵ�ǰ�������Ƿ����β���ͬ,�����β���ʽת��Ϊ������
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(animation))
        {
            //���Ҫ���Ŷ���,�뵱ǰ������ͬ�Ͳ���
            //�����ά�ֶ������ȵ�,��������,�ᰴ����ǰ�Ķ������Ȳ���
            //�β�1:������,�β�2:�������(0���ǵ�ǰ����),�β�3:�����Ĺ�һ��ʱ��(���ǵ�ǰ���Ž�����)
            if (preserveTime) animator.Play(animation, 0, animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            //����Ҫά�ֲ��Ž��ȵĶ���,ֱ�Ӿʹ�ͷ��ʼ��
            else animator.Play(animation);
        }
    }

    /// <summary>
    /// �۲�(ת��)
    /// </summary>
    /// <param name="target">���</param>
    public void LookAt(Vector3 target)
    {
        //���㷽��,Ŀ���ȥ��ǰλ��(�Ⱦ�)
        var dir = target - (Vector3)iso.pos;
        //����Ƕ�,Vector3.Angle()������������֮��ļн�,���ص��ǻ���,����Mathf.Sign()��Ϊ���ж�����,������1,��-1.
        var angle = Vector3.Angle(new Vector3(-1, -1), dir) * Mathf.Sign(dir.y - dir.x);
        //���㷽��Ķ���,360���Է�������
        var dierctionDegrees = 360.0f / directionCount;
        //Ŀ�귽������������ĽǶȳ���360���Է�������,ȡ�෽������
        targetDirection = Mathf.RoundToInt((angle + 360) % 360 / directionCount) % directionCount;
    }

    /// <summary>
    /// ����
    /// </summary>
    public void Attack()
    {
        //������ڹ�����,���ﳯ��ʱĿ��,·��Ϊ����ô�Ϳ�ʼ������
        if (!attack && direction == targetDirection && path.Count == 0)
        {
            //���빥��״̬
            attack = true;
            //��һ����������,���
            attackAnimation = Random.Range(1, 3);
        }
    }

    /// <summary>
    /// �ڶ������ʱִ�еķ���(����ϵͳ��api��,�����Լ�ȡ������)
    /// </summary>
    void OnAnimationFinish()
    {
        //�Ƿ񹥻�״̬,�ڹ���״̬���÷�
        if (attack) attack = false;
    }

    void OnAttack1Finish()
    {

    }
    void OnAttack2Finish()
    {

    }
}
