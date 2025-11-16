using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // Penting untuk List<>
using TMPro; // Penting untuk TextMeshPro

public class QuizManager : MonoBehaviour
{
    // --- DATA SOAL ---
    [Tooltip("Isi semua soal kuis di sini dari Inspector")]
    public List<QuestionData> questions; 
    private int currentQuestionIndex = 0;
    private int score = 0;

    // --- REFERENSI UI (Drag & Drop di Inspector) ---
    [Header("Referensi UI Kuis")]
    public GameObject quizPanel;         // Panel yang berisi soal dan jawaban
    public TextMeshProUGUI questionText;    // Teks untuk menampilkan soal
    public TextMeshProUGUI[] answerTexts;   // Array untuk 4 teks tombol jawaban (A, B, C, D)
    public Button[] answerButtons;       // Array untuk 4 tombol jawaban

    [Header("Referensi UI Feedback")]
    [Tooltip("Isi 4 gambar 'Benar' sesuai urutan A, B, C, D")]
    public GameObject[] correctImages; // Array untuk 4 gambar "Benar"
    [Tooltip("Isi 4 gambar 'Salah' sesuai urutan A, B, C, D")]
    public GameObject[] wrongImages;   // Array untuk 4 gambar "Salah"
    public float feedbackDuration = 1.0f; // Durasi feedback (1 detik)

    [Header("Referensi UI Hasil")]
    public GameObject resultsPanel;      // Panel utama yang muncul di akhir kuis
    public TextMeshProUGUI finalScoreText;  // Teks untuk menampilkan skor akhir
    
    [Tooltip("Panel yang muncul jika skor >= 80")]
    public GameObject successPanel; // Panel berisi teks "Selamat" & 2 tombol
    [Tooltip("Panel yang muncul jika skor < 80")]
    public GameObject failPanel;    // Panel berisi teks "Gagal" & 1 tombol

    void Start()
    {
        // Sembunyikan semua panel hasil saat mulai
        resultsPanel.SetActive(false);
        successPanel.SetActive(false); 
        failPanel.SetActive(false);    
        
        quizPanel.SetActive(true);

        // Sembunyikan semua gambar feedback saat mulai
        foreach (GameObject img in correctImages)
        {
            if (img != null) img.SetActive(false);
        }
        foreach (GameObject img in wrongImages)
        {
            if (img != null) img.SetActive(false);
        }

        // Reset state
        currentQuestionIndex = 0;
        score = 0;

        // Mulai kuis
        ShowCurrentQuestion();
    }

    // Menampilkan soal dan jawaban saat ini
    void ShowCurrentQuestion()
    {
        if (currentQuestionIndex < questions.Count)
        {
            // Ambil data soal saat ini
            QuestionData q = questions[currentQuestionIndex];

            // Setel teks soal
            questionText.text = q.questionText;

            // Setel teks untuk 4 tombol jawaban
            for (int i = 0; i < answerTexts.Length; i++)
            {
                if (i < q.answers.Length)
                {
                    answerTexts[i].text = q.answers[i];
                }
            }

            // Aktifkan kembali tombol
            SetButtonsInteractable(true);
        }
        else
        {
            // Kuis selesai
            ShowResults();
        }
    }

    // Fungsi ini akan dipanggil oleh 4 tombol jawaban
    // Kita setel di Inspector (OnClick)
    public void OnAnswerSelected(int answerIndex)
    {
        // Matikan semua tombol agar tidak bisa diklik dua kali
        SetButtonsInteractable(false);

        // Cek apakah jawaban benar
        bool isCorrect = (answerIndex == questions[currentQuestionIndex].correctAnswerIndex);

        if (isCorrect)
        {
            score++;
            // Tampilkan gambar "Benar" YANG SPESIFIK sesuai tombol
            if (answerIndex < correctImages.Length && correctImages[answerIndex] != null)
            {
                StartCoroutine(ShowFeedback(correctImages[answerIndex]));
            }
        }
        else
        {
            // Tampilkan gambar "Salah" YANG SPESIFIK sesuai tombol
            if (answerIndex < wrongImages.Length && wrongImages[answerIndex] != null)
            {
                StartCoroutine(ShowFeedback(wrongImages[answerIndex]));
            }
        }
    }

    // Coroutine untuk menampilkan feedback (Benar/Salah) lalu lanjut
    IEnumerator ShowFeedback(GameObject feedbackImage)
    {
        // Tampilkan gambar feedback
        feedbackImage.SetActive(true);

        // Tunggu selama 'feedbackDuration'
        yield return new WaitForSeconds(feedbackDuration);

        // Sembunyikan gambar feedback
        feedbackImage.SetActive(false);

        // Lanjut ke soal berikutnya
        currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    // Menampilkan panel hasil akhir
    void ShowResults()
    {
        quizPanel.SetActive(false); // Sembunyikan panel kuis
        resultsPanel.SetActive(true); // Tampilkan panel hasil utama

        // Hitung skor akhir (0-100)
        int finalScore = 0;
        if (questions.Count > 0)
        {
            finalScore = Mathf.RoundToInt(((float)score / questions.Count) * 100);
        }

        // Tampilkan skor
        finalScoreText.text = "Skor Kamu: " + finalScore + "/100";

        // Logika IF ELSE untuk SUKSES atau GAGAL
        if (finalScore >= 80)
        {
            // Player Lulus
            successPanel.SetActive(true); // Tampilkan panel sukses
            failPanel.SetActive(false);   // Sembunyikan panel gagal
        }
        else
        {
            // Player Gagal
            successPanel.SetActive(false); // Sembunyikan panel sukses
            failPanel.SetActive(true);     // Tampilkan panel gagal
        }
    }

    // Helper-function untuk mengaktifkan/menonaktifkan semua tombol
    void SetButtonsInteractable(bool state)
    {
        foreach (Button button in answerButtons)
        {
            button.interactable = state;
        }
    }
    public void RestartQuiz()
    {
        score = 0;
        currentQuestionIndex = 0;

        resultsPanel.SetActive(false);
        successPanel.SetActive(false); 
        failPanel.SetActive(false);    
        
        quizPanel.SetActive(true);
        ShowCurrentQuestion();
    }
}