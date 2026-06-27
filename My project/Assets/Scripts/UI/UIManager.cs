using UnityEngine;
using UnityEngine.UI;
using TMPro; // Gunakan ini jika teks UI kamu memakai TextMeshPro

public class UIManager : MonoBehaviour
{
    // Singleton pattern agar mudah dipanggil dari script bidak manapun
    public static UIManager Instance { get; private set; }

    [Header("Komponen UI Kiri Bawah")]
    public GameObject panelInfoBidak; // Panel induk kiri bawah
    public Image[] fotoBidakUI;          // Komponen Image untuk foto
    public TextMeshPro[] namaBidakText; // Komponen Text untuk nama (Ganti ke 'Text' jika pakai UI biasa)
    public TextMeshPro[] deskripsiBidakText; // Komponen Text untuk deskripsi

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Sembunyikan panel di awal game karena belum ada bidak yang dipilih
        SembunyikanInfoBidak();
    }

    public void TampilkanInfoBidak(BidakData data)
    {
        if (data == null) return;

        panelInfoBidak.SetActive(true);

        namaBidakText[0].text = data.namaBidak;
        deskripsiBidakText[0].text = data.deskripsiBidak;
        fotoBidakUI[0].sprite = data.fotoBidak;
    }

    public void SembunyikanInfoBidak()
    {
        panelInfoBidak.SetActive(false);
    }
}