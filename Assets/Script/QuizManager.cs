using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class QuizManager : MonoBehaviour
{
    // --- DATA SOAL ---
    [Tooltip("Isi semua soal kuis di sini dari Inspector")]
    public List<QuestionData> questions; 
    private int currentQuestionIndex = 0;
    private int score = 0;

    // --- REFERENSI UI (Drag & Drop di Inspector) ---
    [Header("Referensi UI Kuis")]
    public GameObject quizPanel;
    [Tooltip("Tambahkan komponen 'CanvasGroup' ke quizPanel Anda dan seret ke sini")]
    public CanvasGroup quizCanvasGroup; // <-- MODIFIKASI: Untuk fade in/out
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI[] answerTexts;
    public Button[] answerButtons;

    [Header("Referensi UI Feedback")]
    [Tooltip("Isi 4 gambar 'Benar' sesuai urutan A, B, C, D")]
    public GameObject[] correctImages;
    [Tooltip("Isi 4 gambar 'Salah' sesuai urutan A, B, C, D")]
    public GameObject[] wrongImages;
    public float feedbackDuration = 1.0f;

    [Header("Referensi UI Hasil")]
    public GameObject resultsPanel;
    public TextMeshProUGUI finalScoreText; 
    
    [Tooltip("Panel yang muncul jika skor >= 80")]
    public GameObject successPanel;
    [Tooltip("Panel yang muncul jika skor < 80")]
    public GameObject failPanel;
    
    public float fadeDuration = 0.5f; // <-- MODIFIKASI: Durasi fade

    // --- MODIFIKASI: Start() sekarang hanya menyembunyikan panel ---
    void Start()
    {
        // Sembunyikan semua panel hasil
        resultsPanel.SetActive(false);
        successPanel.SetActive(false); 
        failPanel.SetActive(false);
        
        // Sembunyikan panel kuis menggunakan CanvasGroup
        if (quizCanvasGroup != null)
        {
            quizCanvasGroup.alpha = 0;
            quizCanvasGroup.interactable = false;
            quizCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            quizPanel.SetActive(false); // Fallback jika CanvasGroup tidak di-set
        }

        // Sembunyikan semua gambar feedback
        SembunyikanSemuaFeedback();
    }

    // --- MODIFIKASI: Fungsi ini dipanggil oleh DialogManager ---
    public void TampilkanKuis()
    {
        StartCoroutine(FadeInAndStart());
    }

    // --- MODIFIKASI: Coroutine untuk fade in lalu memulai kuis ---
    IEnumerator FadeInAndStart()
    {
        // Pastikan panel hasil tersembunyi
        resultsPanel.SetActive(false);
        successPanel.SetActive(false); 
        failPanel.SetActive(false);

        // Tampilkan panel kuis (tapi masih transparan)
        quizPanel.SetActive(true); 

        if (quizCanvasGroup != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                quizCanvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
                yield return null;
            }
            quizCanvasGroup.alpha = 1;
            quizCanvasGroup.interactable = true;
            quizCanvasGroup.blocksRaycasts = true;
        }
        
        // Panggil logika Start() Anda yang lama
        MulaiKuis();
    }

    // --- MODIFIKASI: Logika Start() Anda yang lama dipindah ke sini ---
    void MulaiKuis()
    {
        // Sembunyikan semua gambar feedback
        SembunyikanSemuaFeedback();

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
            QuestionData q = questions[currentQuestionIndex];
            questionText.text = q.questionText;

            for (int i = 0; i < answerTexts.Length; i++)
            {
                if (i < q.answers.Length)
                {
                    answerTexts[i].text = q.answers[i];
                }
            }
            SetButtonsInteractable(true);
        }
        else
        {
            // Kuis selesai
            ShowResults();
        }
    }

    // Fungsi ini akan dipanggil oleh 4 tombol jawaban
    public void OnAnswerSelected(int answerIndex)
    {
        SetButtonsInteractable(false);
        bool isCorrect = (answerIndex == questions[currentQuestionIndex].correctAnswerIndex);

        if (isCorrect)
        {
            score++;
            if (answerIndex < correctImages.Length && correctImages[answerIndex] != null)
            {
                StartCoroutine(ShowFeedback(correctImages[answerIndex]));
            }
        }
        else
        {
            if (answerIndex < wrongImages.Length && wrongImages[answerIndex] != null)
            {
                StartCoroutine(ShowFeedback(wrongImages[answerIndex]));
            }
        }
    }

    // Coroutine untuk menampilkan feedback (Benar/Salah) lalu lanjut
    IEnumerator ShowFeedback(GameObject feedbackImage)
    {
        feedbackImage.SetActive(true);
        yield return new WaitForSeconds(feedbackDuration);
        feedbackImage.SetActive(false);

        currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    // --- MODIFIKASI: ShowResults() sekarang memanggil fade out ---
    void ShowResults()
    {
        StartCoroutine(FadeOutQuiz()); // Sembunyikan panel kuis
        resultsPanel.SetActive(true); // Tampilkan panel hasil utama

        int finalScore = 0;
        if (questions.Count > 0)
        {
            finalScore = Mathf.RoundToInt(((float)score / questions.Count) * 100);
        }

        finalScoreText.text = "Skor Kamu: " + finalScore + "/100";

        if (finalScore >= 80)
        {
            successPanel.SetActive(true);
            failPanel.SetActive(false);
        }
        else
        {
            successPanel.SetActive(false);
            failPanel.SetActive(true);
        }
    }

    // --- MODIFIKASI: Coroutine untuk fade out panel kuis ---
    IEnumerator FadeOutQuiz()
    {
        if (quizCanvasGroup != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                quizCanvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
                yield return null;
            }
            quizCanvasGroup.alpha = 0;
            quizCanvasGroup.interactable = false;
            quizCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            quizPanel.SetActive(false); // Fallback
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

    // --- MODIFIKASI: RestartQuiz() memanggil MulaiKuis() ---
    public void RestartQuiz()
    {
        // Sembunyikan panel hasil
        resultsPanel.SetActive(false);
        successPanel.SetActive(false); 
        failPanel.SetActive(false);
        
        // Tampilkan panel kuis (sudah di-set Active(true) oleh ShowResults, jadi kita pastikan Alpha-nya)
        quizPanel.SetActive(true);
        if(quizCanvasGroup != null)
        {
            quizCanvasGroup.alpha = 1;
            quizCanvasGroup.interactable = true;
            quizCanvasGroup.blocksRaycasts = true;
        }

        // Mulai ulang kuis
        MulaiKuis();
    }

    // Helper-function untuk menyembunyikan feedback
    void SembunyikanSemuaFeedback()
    {
        foreach (GameObject img in correctImages)
        {
            if (img != null) img.SetActive(false);
        }
        foreach (GameObject img in wrongImages)
        {
            if (img != null) img.SetActive(false);
        }
    }
}