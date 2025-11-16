using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeToScene : MonoBehaviour
{
    public Image fadePanel;
    public float fadeDuration = 1.5f;
    
    // Ini bisa jadi scene default jika Anda masih ingin menggunakannya
    public string defaultNextScene = "GamePlayRL"; 

    void Awake()
    {
        EnsureFadePanel();
    }

    void Start()
    {
        // Pindahkan StartCoroutine(FadeIn()) dari sini
        // Biarkan DialogManager yang memanggilnya jika diperlukan.
        // Jika Anda ingin scene ini SELALU fade in, biarkan baris di bawah:
        // StartCoroutine(FadeIn()); 
        
        if (fadePanel != null && fadePanel.color.a != 1) // Hanya set jika belum fade in
            fadePanel.color = new Color(0, 0, 0, 0);
    }

    // --- FUNGSI BARU YANG FLEKSIBEL ---
    // Ini adalah fungsi yang akan Anda panggil dari tombol
    public void PindahSceneDenganFade(string namaScene)
    {
        // Memulai coroutine dengan nama scene yang diberikan
        StartCoroutine(FadeOutAndLoad(namaScene));
    }

    // --- FUNGSI LAMA (JIKA MASIH DIPERLUKAN) ---
    public void StartGame()
    {
        StartCoroutine(FadeOutAndLoad(defaultNextScene));
    }

    // --- COROUTINE YANG DIMODIFIKASI ---
    IEnumerator FadeOutAndLoad(string sceneToLoad) // <-- Diubah untuk menerima parameter
    {
        if (fadePanel == null)
        {
            Debug.LogError("Fade Panel tidak ditemukan!");
            yield break;
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 1);

        // --- Perubahan Utama di Sini ---
        if (!string.IsNullOrEmpty(sceneToLoad)) // <-- Menggunakan parameter
            SceneManager.LoadScene(sceneToLoad); // <-- Menggunakan parameter
        else
            Debug.LogWarning("FadeToScene: Nama scene kosong. Tidak ada scene yang dimuat.");
    }

    // (Opsional) Coroutine untuk Fade-In
    // *** DIBUAT MENJADI PUBLIC ***
    public IEnumerator FadeIn()
    {
        if (fadePanel == null)
        {
            Debug.LogError("Fade Panel tidak ditemukan!");
            yield break;
        }

        fadePanel.color = new Color(0, 0, 0, 1); // Pastikan mulai dari hitam
            
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, t / fadeDuration); // Balikkan Lerp
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 0); // Transparan penuh
    }


    // --- TIDAK ADA PERUBAHAN DI BAWAH SINI ---
    void EnsureFadePanel()
    {
        if (fadePanel != null)
            return;

        GameObject go = GameObject.Find("FadePanel");
        if (go != null)
            fadePanel = go.GetComponent<Image>();

        if (fadePanel == null)
        {
            Image[] images = FindObjectsOfType<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name.ToLower().Contains("fade"))
                {
                    fadePanel = img;
                    break;
                }
            }
        }

        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            // Jangan set color di sini, biarkan Start() yang mengatur
        }
        else
        {
            Debug.LogWarning("FadeToScene: fadePanel not assigned. Please assign an Image (FadePanel) in the inspector.");
        }
    }
}