using UnityEngine;

public class SplitChoiceTrigger : MonoBehaviour
{
    public TileSpawner spawner;
    public Tile splitTile;     // ��ҧ Tile ����� SplitLR
    public bool chooseLeft = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        spawner.ChooseSplitExit(splitTile, chooseLeft);
        // (�ͻ�ѹ) ����ʤ�Ի����ع Player/���ͧ����ѹ����ҧ����Ẻ���� � ������
    }
}
