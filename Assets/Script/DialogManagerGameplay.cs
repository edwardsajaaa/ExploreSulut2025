using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogManagerGameplay : MonoBehaviour
{
    // --- STRUCT DIALOG ---
    [System.Serializable]
    public class DialogSequence
    {
        public string characterName;
        public string dialogText;
        public Sprite characterSprite; 
        public float delayBeforeNext = 0f;
    }

    // --- ENUM ANIMASI ---
    public enum CharacterEntranceType { FadeScale, SlideInLeft, SlideInRight, BounceScale, PopScale, Flip }
    public enum DialogBackgroundAnimation { FadeIn, PopUp, SlideUp, ScaleUp }

    // --- REFERENSI UI ---
    [Header("UI References")]
    public GameObject characterImage; 
    public TextMeshProUGUI dialogText;
    public Image dialogBackground; 
    
    // --- PENGATURAN ANIMASI ---
    [Header("Animation Settings")]
    public float characterAnimationDuration = 0.8f;
    public CharacterEntranceType characterEntrance = CharacterEntranceType.FadeScale;
    public float typingSpeed = 0.05f;
    public float delayBetweenDialogs = 0.5f;
    
    [Header("Dialog Background Animation")]
    public DialogBackgroundAnimation backgroundAnimation = DialogBackgroundAnimation.PopUp;
    public float backgroundAnimationDuration = 0.6f;
    public float backgroundDelay = 0.2f;
    
    [Header("Character Switch Animation")]
    public float characterSwitchDuration = 0.5f;

    // --- LIST DIALOG ---
    [Header("Dialog Sequences")]
    public List<DialogSequence> dialogSequences = new List<DialogSequence>();

    // --- REFERENSI EKSTERNAL (DIGANTI KE CANVASGROUP) ---
    [Header("External References (Canvas Groups)")]
    public CanvasGroup panelDialogCanvasGroup;
    public CanvasGroup panelToShowAfterCanvasGroup;
    public CanvasGroup panelGameCanvasGroup;

    // --- FADE SETTINGS ---
    [Header("Scene Fade Settings")]
    public CanvasGroup fadePanelCanvasGroup; // Panel hitam untuk fade
    public float fadeInDuration = 1.0f; // Durasi fade in

    [Header("End Transition Settings")]
    public float endFadeDuration = 0.5f; // Durasi pudar untuk dialog & panel
    public float delayBeforePanelGame = 1.0f; // Jeda "beberapa detik"

    // --- VARIABEL PRIVATE ---
    private CanvasGroup characterCanvasGroup;
    private RectTransform characterRectTransform;
    private Image characterImageComponent; 
    private bool isAnimating = false;
    private bool isWaitingForClick = false;
    private int currentDialogIndex = 0;
    private bool isDialogDone = false; 
    private bool isSkippingTyping = false; 

    // --- FUNGSI UNITY (AWAKE, START, UPDATE) ---

    void Awake()
    {
        EnsureReferences();
    }

    void Start()
    {
        // Setup UI dialog (sembunyikan/reset)
        if (characterImage != null)
            characterImage.SetActive(false);
        if (dialogText != null)
            dialogText.text = "";

        // --- PENGATURAN AWAL PANEL FADE ---
        // Pastikan panel-panel akhir NON-INTERAKTIF dan TRANSparan
        if (panelToShowAfterCanvasGroup != null)
        {
            panelToShowAfterCanvasGroup.alpha = 0f;
            panelToShowAfterCanvasGroup.interactable = false;
            panelToShowAfterCanvasGroup.blocksRaycasts = false;
            panelToShowAfterCanvasGroup.gameObject.SetActive(true); // HARUS AKTIF
        }
        if (panelGameCanvasGroup != null)
        {
            panelGameCanvasGroup.alpha = 0f;
            panelGameCanvasGroup.interactable = false;
            panelGameCanvasGroup.blocksRaycasts = false;
            panelGameCanvasGroup.gameObject.SetActive(true); // HARUS AKTIF
        }
        
        // Pastikan panel dialognya aktif dan terlihat di awal
        if (panelDialogCanvasGroup != null)
        {
            panelDialogCanvasGroup.alpha = 1f;
            panelDialogCanvasGroup.interactable = true;
            panelDialogCanvasGroup.blocksRaycasts = true;
            panelDialogCanvasGroup.gameObject.SetActive(true);
        }

        // Mulai urutan dengan FADE IN
        StartCoroutine(InitializeSceneWithFade());
    }

    // --- UPDATE DENGAN LOGIKA AKHIR DIALOG ---
    void Update()
    {
        if (isDialogDone) return; // Hentikan update jika transisi akhir dimulai

        if (Input.GetMouseButtonDown(0))
        {
            if (isWaitingForClick)
            {
                // KASUS 1: Teks sudah selesai, player klik untuk MELANJUTKAN
                isWaitingForClick = false; 
                
                currentDialogIndex++; 

                if (currentDialogIndex < dialogSequences.Count)
                {
                    StartCoroutine(ShowNextDialog());
                }
                else
                {
                    // --- LOGIKA AKHIR DIALOG (FADE SEQUENCE) ---
                    Debug.Log("Dialog sequence completed!");
                    isDialogDone = true; // Hentikan input di Update()

                    // Mulai Coroutine transisi akhir
                    StartCoroutine(EndDialogTransitionSequence());
                }
            }
            else
            {
                // KASUS 2: Teks sedang mengetik, player klik untuk SKIP
                isSkippingTyping = true; 
            }
        }
    }

    // --- FUNGSI SETUP ---
    void EnsureReferences()
    {
        // ... (Kode EnsureReferences Anda tidak berubah) ...
        if (characterImage == null)
        {
            characterImage = FindChildByName(transform, "CharacterImage");
        }
        if (dialogText == null)
        {
            GameObject dialogObj = GameObject.Find("DialogText");
            if (dialogObj != null)
                dialogText = dialogObj.GetComponent<TextMeshProUGUI>();
            else
            {
                GameObject found = FindChildByName(transform, "DialogText");
                if (found != null)
                    dialogText = found.GetComponent<TextMeshProUGUI>();
            }
        }
        if (dialogBackground == null)
        {
            GameObject bgObj = GameObject.Find("DialogBackground");
            if (bgObj != null)
                dialogBackground = bgObj.GetComponent<Image>();
            else
            {
                GameObject found = FindChildByName(transform, "DialogBackground");
                if (found != null)
                    dialogBackground = found.GetComponent<Image>();
            }
        }
        if (characterImage != null)
        {
            characterCanvasGroup = characterImage.GetComponent<CanvasGroup>();
            if (characterCanvasGroup == null)
                characterCanvasGroup = characterImage.AddComponent<CanvasGroup>();
            characterImageComponent = characterImage.GetComponent<Image>();
            if (characterImageComponent == null)
            {
                characterImageComponent = characterImage.GetComponentInChildren<Image>();
            }
            characterRectTransform = characterImage.GetComponent<RectTransform>();
            if (characterCanvasGroup != null)
                characterCanvasGroup.alpha = 0;
            if (characterRectTransform != null)
                characterRectTransform.localScale = new Vector3(0.8f, 0.8f, 1f);
            if (characterImageComponent == null)
                Debug.LogWarning("DialogManagerGameplay: characterImage (atau child-nya) tidak punya Image component!");
        }
        if (dialogBackground != null)
        {
            Color bgColor = dialogBackground.color;
            dialogBackground.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0);
        }
    }

    GameObject FindChildByName(Transform parent, string name)
    {
        // ... (Kode FindChildByName Anda tidak berubah) ...
        foreach (Transform child in parent)
        {
            if (child.gameObject.name == name)
                return child.gameObject;
            GameObject result = FindChildByName(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    // --- COROUTINE FADE DAN DIALOG ---

    // Coroutine helper umum untuk memudarkan CanvasGroup
    IEnumerator FadeCanvas(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
    }

    IEnumerator InitializeSceneWithFade()
    {
        if (fadePanelCanvasGroup != null)
        {
            fadePanelCanvasGroup.gameObject.SetActive(true);
            fadePanelCanvasGroup.alpha = 1f;

            // 1. Jalankan Fade In (dari hitam ke jelas)
            yield return StartCoroutine(FadeCanvas(fadePanelCanvasGroup, 1f, 0f, fadeInDuration));
            
            // 2. Sembunyikan panel fade
            fadePanelCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("FadePanelCanvasGroup belum di-set di Inspector!");
        }
        
        // 3. Setelah fade selesai, baru mulai dialog
        StartCoroutine(StartSequence());
    }
    
    // --- COROUTINE BARU: Transisi Akhir Dialog ---
    IEnumerator EndDialogTransitionSequence()
    {
        // 1. Pudarkan PanelDialog
        if (panelDialogCanvasGroup != null)
        {
            Debug.Log("Memudarkan PanelDialog...");
            yield return StartCoroutine(FadeCanvas(panelDialogCanvasGroup, 1f, 0f, endFadeDuration));
            panelDialogCanvasGroup.interactable = false;
            panelDialogCanvasGroup.blocksRaycasts = false;
            panelDialogCanvasGroup.gameObject.SetActive(false); // Matikan setelah pudar
        }

        // 2. Munculkan panelToShowAfter (dari pudar ke cerah)
        if (panelToShowAfterCanvasGroup != null)
        {
            Debug.Log("Memunculkan panelToShowAfter...");
            panelToShowAfterCanvasGroup.gameObject.SetActive(true); // Pastikan aktif
            yield return StartCoroutine(FadeCanvas(panelToShowAfterCanvasGroup, 0f, 1f, endFadeDuration));
            panelToShowAfterCanvasGroup.interactable = true;
            panelToShowAfterCanvasGroup.blocksRaycasts = true;
        }

        // 3. Tunggu "beberapa detik"
        Debug.Log("Menunggu jeda...");
        yield return new WaitForSeconds(delayBeforePanelGame);

        // 4. Munculkan panelGame (dari pudar ke cerah)
        if (panelGameCanvasGroup != null)
        {
            Debug.Log("Memunculkan panelGame...");
            panelGameCanvasGroup.gameObject.SetActive(true); // Pastikan aktif
            yield return StartCoroutine(FadeCanvas(panelGameCanvasGroup, 0f, 1f, endFadeDuration));
            panelGameCanvasGroup.interactable = true;
            panelGameCanvasGroup.blocksRaycasts = true;
        }

        Debug.Log("Transisi akhir selesai.");
    }

    IEnumerator StartSequence()
    {
        // ... (Kode StartSequence Anda tidak berubah) ...
        if (characterImage != null)
            characterImage.SetActive(true);
        yield return StartCoroutine(PlayDialogSequence());
    }

    IEnumerator PlayDialogSequence()
    {
        // ... (Kode PlayDialogSequence Anda tidak berubah) ...
        if (dialogSequences.Count == 0)
        {
            Debug.LogWarning("DialogManagerGameplay: No dialog sequences assigned!");
            yield break;
        }
        yield return StartCoroutine(AnimateCharacterEntrance());
        if (dialogSequences[0].characterSprite != null && characterImageComponent != null)
        {
            characterImageComponent.sprite = dialogSequences[0].characterSprite;
        }
        currentDialogIndex = 0;
        yield return StartCoroutine(ShowDialog(currentDialogIndex));
    }

    IEnumerator ShowDialog(int dialogIndex)
    {
        // ... (Kode ShowDialog Anda tidak berubah) ...
        if (dialogIndex < 0 || dialogIndex >= dialogSequences.Count)
            yield break;
        DialogSequence dialog = dialogSequences[dialogIndex];
        Debug.Log("=== Showing Dialog " + dialogIndex + " ===");
        if (!string.IsNullOrEmpty(dialog.characterName))
            Debug.Log("Character: " + dialog.characterName);
        if (dialog.characterSprite != null && characterImageComponent != null)
        {
            Debug.Log("Dialog " + dialogIndex + " image: " + dialog.characterSprite.name);
            if (dialogIndex == 0)
            {
                Debug.Log("Setting initial image (no transition)");
                characterImageComponent.sprite = dialog.characterSprite;
                characterImage.SetActive(true);
            }
            else
            {
                if (characterImageComponent.sprite != dialog.characterSprite)
                {
                    Debug.Log("Switching image with transition");
                    yield return StartCoroutine(SwitchCharacterImage(dialog.characterSprite));
                }
            }
        }
        else
        {
            Debug.LogWarning("Dialog " + dialogIndex + " image (sprite) kosong atau characterImageComponent null!");
        }
        yield return StartCoroutine(AnimateDialogBackground());
        yield return StartCoroutine(TypeText(dialog.dialogText)); 
        
        isWaitingForClick = true;
    }

    IEnumerator ShowNextDialog()
    {
        // ... (Kode ShowNextDialog Anda tidak berubah) ...
        yield return StartCoroutine(ShowDialog(currentDialogIndex));
    }

    // --- COROUTINE ANIMASI KARAKTER ---
    IEnumerator AnimateCharacterEntrance() { 
        // ... (Kode Anda tidak berubah) ...
        if (characterImage == null || characterCanvasGroup == null)
            yield break;
        switch (characterEntrance)
        {
            case CharacterEntranceType.FadeScale: yield return StartCoroutine(FadeScaleEntrance()); break;
            case CharacterEntranceType.SlideInLeft: yield return StartCoroutine(SlideInLeftEntrance()); break;
            case CharacterEntranceType.SlideInRight: yield return StartCoroutine(SlideInRightEntrance()); break;
            case CharacterEntranceType.BounceScale: yield return StartCoroutine(BounceScaleEntrance()); break;
            case CharacterEntranceType.PopScale: yield return StartCoroutine(PopScaleEntrance()); break;
            case CharacterEntranceType.Flip: yield return StartCoroutine(FlipEntrance()); break;
        }
    }
    IEnumerator FadeScaleEntrance() { 
        // ... (Kode Anda tidak berubah) ...
        float t = 0f;
        Vector3 startScale = new Vector3(0.8f, 0.8f, 1f);
        Vector3 endScale = Vector3.one;
        while (t < characterAnimationDuration)
        {
            t += Time.deltaTime;
            float progress = t / characterAnimationDuration;
            characterCanvasGroup.alpha = Mathf.Lerp(0, 1, progress);
            if (characterRectTransform != null)
                characterRectTransform.localScale = Vector3.Lerp(startScale, endScale, progress);
            yield return null;
        }
        characterCanvasGroup.alpha = 1;
        if (characterRectTransform != null)
            characterRectTransform.localScale = endScale;
    }
    IEnumerator SlideInLeftEntrance() { 
        // ... (Kode Anda tidak berubah) ...
        if (characterRectTransform == null) yield break;
        float t = 0f;
        Vector2 startPos = new Vector2(-500, characterRectTransform.anchoredPosition.y);
        Vector2 endPos = characterRectTransform.anchoredPosition;
        while (t < characterAnimationDuration)
        {
            t += Time.deltaTime;
            float progress = t / characterAnimationDuration;
            float easedProgress = Mathf.SmoothStep(0, 1, progress);
            characterCanvasGroup.alpha = Mathf.Lerp(0, 1, progress);
            characterRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, easedProgress);
            yield return null;
        }
        characterCanvasGroup.alpha = 1;
        characterRectTransform.anchoredPosition = endPos;
    }
    IEnumerator SlideInRightEntrance() { 
        // ... (Kode Anda tidak berubah) ...
        if (characterRectTransform == null) yield break;
        float t = 0f;
        Vector2 startPos = new Vector2(500, characterRectTransform.anchoredPosition.y);
        Vector2 endPos = characterRectTransform.anchoredPosition;
        while (t < characterAnimationDuration)
        {
            t += Time.deltaTime;
            float progress = t / characterAnimationDuration;
            float easedProgress = Mathf.SmoothStep(0, 1, progress);
            characterCanvasGroup.alpha = Mathf.Lerp(0, 1, progress);
            characterRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, easedProgress);
            yield return null;
        }
        characterCanvasGroup.alpha = 1;
        characterRectTransform.anchoredPosition = endPos;
    }
    IEnumerator BounceScaleEntrance() { 
        // ... (Kode Anda tidak berubah) ...
        float t = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;
        while (t < characterAnimationDuration)
        {
            t += Time.deltaTime;
            float progress = t / characterAnimationDuration;
            float bounce = Mathf.Sin(progress * Mathf.PI) * 0.2f;
            float easedScale = progress + bounce * (1 - progress);
            characterCanvasGroup.alpha = Mathf.Lerp(0, 1, progress);
            if (characterRectTransform != null)
                characterRectTransform.localScale = Vector3.Lerp(startScale, endScale, easedScale);
            yield return null;
        }
        characterCanvasGroup.alpha = 1;
        if (characterRectTransform != null)
            characterRectTransform.localScale = endScale;
    }
    IEnumerator PopScaleEntrance() { 
        // ... (Kode Anda tidak berubah) ...
        float t = 0f;
        Vector3 startScale = new Vector3(1.3f, 1.3f, 1f);
        Vector3 endScale = Vector3.one;
        while (t < characterAnimationDuration)
        {
            t += Time.deltaTime;
            float progress = t / characterAnimationDuration;
            float eased = Mathf.SmoothStep(0, 1, progress);
            characterCanvasGroup.alpha = Mathf.Lerp(0, 1, progress);
            if (characterRectTransform != null)
                characterRectTransform.localScale = Vector3.Lerp(startScale, endScale, eased);
            yield return null;
        }
        characterCanvasGroup.alpha = 1;
        if (characterRectTransform != null)
            characterRectTransform.localScale = endScale;
    }
    IEnumerator FlipEntrance() { 
        // ... (Kode Anda tidak berubah) ...
        float t = 0f;
        Vector3 startRotation = new Vector3(0, 90, 0);
        Vector3 endRotation = Vector3.zero;
        while (t < characterAnimationDuration)
        {
            t += Time.deltaTime;
            float progress = t / characterAnimationDuration;
            characterCanvasGroup.alpha = Mathf.Lerp(0, 1, progress);
            if (characterRectTransform != null)
            {
                characterRectTransform.localEulerAngles = Vector3.Lerp(startRotation, endRotation, progress);
                characterRectTransform.localScale = Vector3.one;
            }
            yield return null;
        }
        characterCanvasGroup.alpha = 1;
        if (characterRectTransform != null)
            characterRectTransform.localEulerAngles = endRotation;
    }

    // --- COROUTINE ANIMASI UI LAINNYA ---
    IEnumerator AnimateDialogBackground() { 
        // ... (Kode Anda tidak berubah) ...
        if (dialogBackground == null)
            yield break;
        yield return new WaitForSeconds(backgroundDelay);
        float t = 0f;
        Color startColor = dialogBackground.color;
        switch (backgroundAnimation)
        {
            case DialogBackgroundAnimation.FadeIn:
                while (t < backgroundAnimationDuration)
                {
                    t += Time.deltaTime;
                    float progress = t / backgroundAnimationDuration;
                    Color newColor = startColor;
                    newColor.a = Mathf.Lerp(0, 1, progress);
                    dialogBackground.color = newColor;
                    yield return null;
                }
                break;
            case DialogBackgroundAnimation.PopUp:
                RectTransform bgRect = dialogBackground.GetComponent<RectTransform>();
                Vector3 startScale = new Vector3(0.9f, 0.9f, 1f);
                while (t < backgroundAnimationDuration)
                {
                    t += Time.deltaTime;
                    float progress = t / backgroundAnimationDuration;
                    float eased = Mathf.SmoothStep(0, 1, progress);
                    Color newColor = startColor;
                    newColor.a = Mathf.Lerp(0, 1, progress);
                    dialogBackground.color = newColor;
                    if (bgRect != null)
                        bgRect.localScale = Vector3.Lerp(startScale, Vector3.one, eased);
                    yield return null;
                }
                if (bgRect != null)
                    bgRect.localScale = Vector3.one; 
                break;
        }
        Color finalColor = dialogBackground.color;
        finalColor.a = 1;
        dialogBackground.color = finalColor;
    }
    
    // --- TYPETEXT DENGAN LOGIKA SKIP ---
    IEnumerator TypeText(string text)
    {
        // ... (Kode Anda tidak berubah) ...
        if (dialogText == null)
            yield break;

        isSkippingTyping = false; 
        dialogText.text = "";
        string displayedText = "";

        foreach (char letter in text)
        {
            if (isSkippingTyping)
            {
                break; 
            }
            displayedText += letter;
            dialogText.text = displayedText;
            yield return new WaitForSeconds(typingSpeed);
        }
        dialogText.text = text;
        isSkippingTyping = false; 
    }

    IEnumerator SwitchCharacterImage(Sprite newSprite) { 
        // ... (Kode Anda tidak berubah) ...
        Debug.Log("Switching to sprite: " + (newSprite != null ? newSprite.name : "null"));
        float t = 0f;
        while (t < characterSwitchDuration / 2)
        {
            t += Time.deltaTime;
            float progress = t / (characterSwitchDuration / 2);
            characterCanvasGroup.alpha = Mathf.Lerp(1, 0, progress);
            yield return null;
        }
        characterImageComponent.sprite = newSprite;
        Debug.Log("Sprite switched to: " + (newSprite != null ? newSprite.name : "null"));
        t = 0f;
        Vector3 startScale = new Vector3(0.9f, 0.9f, 1f);
        Vector3 endScale = Vector3.one;
        while (t < characterSwitchDuration / 2)
        {
            t += Time.deltaTime;
            float progress = t / (characterSwitchDuration / 2);
            characterCanvasGroup.alpha = Mathf.Lerp(0, 1, progress);
            if (characterRectTransform != null)
                characterRectTransform.localScale = Vector3.Lerp(startScale, endScale, progress);
            yield return null;
        }
        characterCanvasGroup.alpha = 1;
        if (characterRectTransform != null)
            characterRectTransform.localScale = endScale;
    }

    // --- FUNGSI HELPER ---
    public void AddDialog(string text, float delay = 0f) { 
        // ... (Kode Anda tidak berubah) ...
        DialogSequence newDialog = new DialogSequence
        {
            characterName = "",
            dialogText = text,
            characterSprite = null,
            delayBeforeNext = delay
        };
        dialogSequences.Add(newDialog);
    }
    
    public void SkipAnimation() 
    { 
        // ... (Kode Anda tidak berubah) ...
        StopAllCoroutines();
        if (dialogText != null && dialogSequences.Count > 0 && currentDialogIndex < dialogSequences.Count)
            dialogText.text = dialogSequences[currentDialogIndex].dialogText;
        
        isWaitingForClick = true;
        isSkippingTyping = false;
    }
}