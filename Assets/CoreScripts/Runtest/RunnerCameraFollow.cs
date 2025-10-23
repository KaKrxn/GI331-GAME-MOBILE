using UnityEngine;

public class RunnerCameraFollow : MonoBehaviour
{
    public Transform target;        // ��� Player
    public Vector3 offset = new Vector3(0f, 6f, -8f);
    public float moveSmooth = 8f;   // ���������ͧ��õ��
    public float rotSmooth = 8f;    // ���������ͧ�����ع���

    void LateUpdate()
    {
        if (!target) return;

        // ���˹�������� = ���˹觼����� + �Ϳ��� local space �ͧ������
        Vector3 desiredPos = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * moveSmooth);

        // ��ع���ͧ����ѹ��������� (���ѧ�ͧ仢�ҧ˹�Ңͧ������)
        Quaternion desiredRot = Quaternion.LookRotation(target.forward, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRot, Time.deltaTime * rotSmooth);
    }
}
