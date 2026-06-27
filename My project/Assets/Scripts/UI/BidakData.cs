using UnityEngine;

[CreateAssetMenu(fileName = "BaruBidakData", menuName = "Game Catur/Bidak Data")]
public class BidakData : ScriptableObject
{
    public string namaBidak;
    [TextArea(2, 5)]
    public string deskripsiBidak;
    public Sprite fotoBidak;

    // Kamu bisa menambah status lain di sini nanti, contoh:
    // public int jarakJalan;
    // public int attackPower;
}