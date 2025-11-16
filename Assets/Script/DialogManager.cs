using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogManager : MonoBehaviour
{
    [System.Serializable]
    public class DialogSequence
    {
        public string characterName;
        public string dialogText;
        public Sprite characterSprite; 
        public float delayBeforeNext = 0f;
    }

    [System.Serializable]
    public struct RectOffsets
    {
        public float Left;
        public float Right;
        public float Top;
        public float Bottom;
    }

    public enum CharacterEntranceType { FadeScale, SlideInLeft, SlideInRight, BounceScale, PopScale, Flip }
    public enum DialogBackgroundAnimation { FadeIn, PopUp, SlideUp, ScaleUp }

    [Header("UI References")]
    public GameObject characterImage; 
    public TextMeshProUGUI dialogText;
    public Image dialogBackground; 
    
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

    [Header("Dialog Sequences")]
    public List<DialogSequence> dialogSequences = new List<DialogSequence>();

    [Header("Panel Animation Event")]
    public RectTransform panelToAnimate; 
    public float animationDuration = 0.5f;

    [Header("Scale Animation")]
    public bool animateScale = true; 
    public Vector3 targetScale = new Vector3(1, 1, 1);

    [Header("Rect Offsets Animation")]
    public bool animateRectOffsets = false; 
    public RectOffsets targetRectOffsets; 

    private bool hasPanelAnimated = false; 

    [Header("External References")]
    public FadeToScene fadeManager; 

    [Header("Scene Control")]
    public string sceneToLoadAfter = "";

    private CanvasGroup characterCanvasGroup;
    private RectTransform characterRectTransform;
    private Image characterImageComponent; 
    private bool isAnimating = false;
    private bool isWaitingForClick = false;
    private int currentDialogIndex = 0;
    private bool isDialogDone = false;
    private bool isSkippingTyping = false; 

    void Awake()
    {
        EnsureReferences();
    }

    void Start()
    {
        if (characterImage != null)
            characterImage.SetActive(false);
            
        if (dialogText != null)
            dialogText.text = "";

        StartCoroutine(StartSequence());
    }

    void Update()
    {
        if (isDialogDone) return; 

        if (Input.GetMouseButtonDown(0))
        {
            if (isWaitingForClick)
            {
                isWaitingForClick = false; 
                
                if (currentDialogIndex == 2 && !hasPanelAnimated)
                {
                    hasPanelAnimated = true; 
                    StartCoroutine(AnimatePanel(panelToAnimate, animationDuration));
                }

                currentDialogIndex++;

                if (currentDialogIndex < dialogSequences.Count)
                {
                    StartCoroutine(ShowNextDialog());
                }
                else
                {
                    Debug.Log("Dialog sequence completed!");
                    isDialogDone = true;

                    if (fadeManager != null && !string.IsNullOrEmpty(sceneToLoadAfter))
                    {
                        fadeManager.PindahSceneDenganFade(sceneToLoadAfter);
                    }
                    else
                    {
                        Debug.LogWarning("Dialog selesai, tapi FadeManager atau SceneToLoadAfter belum di-set di Inspector.");
                    }
                }
            }
            else
            {
                // KASUS 2: Teks sedang mengetik (isWaitingForClick == false), player klik untuk SKIP
                isSkippingTyping = true; // Aktifkan flag untuk skip
            }
        }
    }

    void EnsureReferences()
    {
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
                Debug.LogWarning("DialogManager: characterImage (atau child-nya) tidak punya Image component!");
        }
        if (dialogBackground != null)
        {
            Color bgColor = dialogBackground.color;
            dialogBackground.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0);
        }
    }

    GameObject FindChildByName(Transform parent, string name)
    {
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

    // --- COROUTINE UTAMA DIALOG ---

    IEnumerator StartSequence()
    {
        // --- MEMANGGIL FADEIN DARI "BOS" ---
        if (fadeManager != null)
        {
            yield return StartCoroutine(fadeManager.FadeIn());
        }
        else
        {
            Debug.LogWarning("FadeManager belum di-set di DialogManager! Tidak ada FadeIn.");
        }
        // --- BATAS PERUBAHAN ---

        if (characterImage != null)
            characterImage.SetActive(true);
        yield return StartCoroutine(PlayDialogSequence());
    }

    IEnumerator PlayDialogSequence()
    {
        if (dialogSequences.Count == 0)
        {
            Debug.LogWarning("DialogManager: No dialog sequences assigned!");
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
        yield return StartCoroutine(TypeText(dialog.dialogText)); // Panggil TypeText
        
        // PENTING: Pindahkan isWaitingForClick ke SETELAH TypeText selesai
        isWaitingForClick = true;
    }

    IEnumerator ShowNextDialog()
    {
        yield return StartCoroutine(ShowDialog(currentDialogIndex));
    }

    // --- COROUTINE ANIMASI KARAKTER ---
    IEnumerator AnimateCharacterEntrance() { /* ... kode Anda ... */ 
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
    IEnumerator FadeScaleEntrance() { /* ... kode Anda ... */ 
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
    IEnumerator SlideInLeftEntrance() { /* ... kode Anda ... */ 
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
    IEnumerator SlideInRightEntrance() { /* ... kode Anda ... */ 
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
    IEnumerator BounceScaleEntrance() { /* ... kode Anda ... */ 
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
    IEnumerator FlipEntrance() { /* ... kode Anda ... */ 
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

    IEnumerator AnimateDialogBackground() { /* ... kode Anda ... */ 
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
    
    // --- TYPETEXT BARU DENGAN LOGIKA SKIP ---
    IEnumerator TypeText(string text)
    {
        if (dialogText == null)
            yield break;

        isSkippingTyping = false; // Reset flag di awal
        dialogText.text = "";
        string displayedText = "";

        foreach (char letter in text)
        {
            // Cek apakah player mau skip
            if (isSkippingTyping)
            {
                break; // Keluar dari loop
            }

            displayedText += letter;
            dialogText.text = displayedText;
            yield return new WaitForSeconds(typingSpeed);
        }

        // Tampilkan teks penuh (baik karena selesai atau di-skip)
        dialogText.text = text;
        isSkippingTyping = false; // Reset flag untuk jaga-jaga
    }

    IEnumerator SwitchCharacterImage(Sprite newSprite) { /* ... kode Anda ... */ 
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

    // --- COROUTINE PANEL ANIMATION ---
    
    IEnumerator AnimatePanel(RectTransform panel, float duration)
    {
        if (panel == null)
        {
            Debug.LogWarning("Panel To Animate belum di-set di Inspector!");
            yield break;
        }

        // Ambil nilai awal
        Vector3 startScale = panel.localScale;
        Vector2 startOffsetMin = panel.offsetMin; // (Left, Bottom)
        Vector2 startOffsetMax = panel.offsetMax; // (-Right, -Top)

        // Tentukan nilai akhir dari Inspector
        Vector3 endScale = targetScale;
        Vector2 endOffsetMin = new Vector2(targetRectOffsets.Left, targetRectOffsets.Bottom);
        Vector2 endOffsetMax = new Vector2(-targetRectOffsets.Right, -targetRectOffsets.Top);

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress); // Gerakan mulus

            // Animasikan HANYA jika dicentang
            if (animateScale)
            {
                panel.localScale = Vector3.Lerp(startScale, endScale, easedProgress);
            }
            if (animateRectOffsets)
            {
                panel.offsetMin = Vector2.Lerp(startOffsetMin, endOffsetMin, easedProgress);
                panel.offsetMax = Vector2.Lerp(startOffsetMax, endOffsetMax, easedProgress);
            }

            yield return null;
        }

        // Pastikan sampai di nilai akhir
        if (animateScale)
        {
            panel.localScale = endScale;
        }
        if (animateRectOffsets)
        {
            panel.offsetMin = endOffsetMin;
            panel.offsetMax = endOffsetMax;
        }
    }

    // --- FUNGSI HELPER ---
    public void AddDialog(string text, float delay = 0f) { /* ... kode Anda ... */ 
        DialogSequence newDialog = new DialogSequence
        {
            characterName = "",
            dialogText = text,
            characterSprite = null,
            delayBeforeNext = delay
        };
        dialogSequences.Add(newDialog);
    }
    
    // Fungsi ini tidak dipakai di logika skip, tapi bisa dipakai untuk tombol "Skip All"
    public void SkipAnimation() 
    { 
        StopAllCoroutines();
        if (dialogText != null && dialogSequences.Count > 0 && currentDialogIndex < dialogSequences.Count)
            dialogText.text = dialogSequences[currentDialogIndex].dialogText;
        
        // Langsung set ke state menunggu
        isWaitingForClick = true;
        isSkippingTyping = false;
    }
}