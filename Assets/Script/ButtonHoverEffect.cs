using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Wajib untuk deteksi hover
using UnityEngine.Events; // Wajib untuk OnClick
using System.Collections;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Referensi Komponen")]
    public Image buttonBackground; // Gambar yang berubah warna (BackgroundTombol)
    public RectTransform popupImageRect; // Gambar yang akan popup (GambarPopup)

    [Header("Pengaturan Efek")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.grey;
    public float animationDuration = 0.3f;

    [Header("Pengaturan Posisi Popup")]
    public float hiddenYPosition = -100f; // Posisi Y saat tersembunyi
    public float visibleYPosition = 50f;   // Posisi Y saat muncul

    [Header("Event Klik")]
    public UnityEvent OnClick; // Agar bisa di-set di Inspector seperti tombol biasa

    private Coroutine activeCoroutine; // Untuk menyimpan animasi yang sedang berjalan

    void Start()
    {
        // Set warna awal
        if (buttonBackground != null)
        {
            buttonBackground.color = normalColor;
        }

        // Set posisi awal gambar (tersembunyi)
        if (popupImageRect != null)
        {
            popupImageRect.anchoredPosition = new Vector2(popupImageRect.anchoredPosition.x, hiddenYPosition);
        }
    }

    // Terpicu saat kursor masuk ke area
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Ganti warna
        if (buttonBackground != null)
        {
            buttonBackground.color = hoverColor;
        }

        // Hentikan animasi sebelumnya (jika ada)
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }

        // Mulai animasi "Pop Up"
        if (popupImageRect != null)
        {
            activeCoroutine = StartCoroutine(AnimatePopup(visibleYPosition));
        }
    }

    // Terpicu saat kursor keluar dari area
    public void OnPointerExit(PointerEventData eventData)
    {
        // Kembalikan warna
        if (buttonBackground != null)
        {
            buttonBackground.color = normalColor;
        }

        // Hentikan animasi sebelumnya (jika ada)
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }

        // Mulai animasi "Pop Down"
        if (popupImageRect != null)
        {
            activeCoroutine = StartCoroutine(AnimatePopup(hiddenYPosition));
        }
    }

    // Terpicu saat di-klik
    public void OnPointerClick(PointerEventData eventData)
    {
        // Panggil semua fungsi yang terdaftar di event OnClick
        OnClick.Invoke();
    }

    // Coroutine untuk menganimasikan popup
    IEnumerator AnimatePopup(float targetY)
    {
        float t = 0f;
        Vector2 startPos = popupImageRect.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, targetY);

        while (t < animationDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, t / animationDuration);
            popupImageRect.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
            yield return null;
        }

        popupImageRect.anchoredPosition = endPos; // Pastikan sampai di posisi akhir
    }
}