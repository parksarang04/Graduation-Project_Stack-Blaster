// ====================================================
// UI 매니저
// 점수/스테이지/콤보/오버레이 등 화면에 보이는 거 전부 여기서 처리
// 각 UI 요소는 Inspector에서 직접 드래그해서 연결해야 함
// 코루틴으로 페이드/스케일 애니메이션 처리 (DOTween 안 써도 됨)
// ====================================================
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD - Top Center")]
    public Text scoreText;
    public Text audioStageText;

    [Header("HUD - Right")]
    public Text bestText;
    public Image speedFillImage; // Fill Amount 방식 권장

    [Header("Combo / Milestone")]
    public Text comboText;
    public CanvasGroup comboGroup;
    public GameObject milestoneToast;
    public Text milestoneTitleText;

    [Header("Overlays")]
    public GameObject startOverlay;
    public GameObject gameOverOverlay;
    public GameObject pauseOverlay;
    public Text finalScoreText;
    public GameObject newRecordLabel;
    public Image missFlashImage;

    [Header("Buttons")]
    public Button playButton;
    public Button retryButton;
    public Button pauseButton;
    public Button resumeButton;
    public Button muteButton;
    public Text playButtonLabel;

    private Coroutine comboRoutine;
    private Coroutine milestoneRoutine;
    private bool muted = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        if (retryButton != null) retryButton.onClick.AddListener(OnPlayClicked);
        if (pauseButton != null) pauseButton.onClick.AddListener(() => StackGameManager.Instance.TogglePause());
        if (resumeButton != null) resumeButton.onClick.AddListener(() => StackGameManager.Instance.ResumeGame());
        if (muteButton != null) muteButton.onClick.AddListener(OnMuteClicked);

        gameOverOverlay.SetActive(false);
        pauseOverlay.SetActive(false);
        comboGroup.alpha = 0f;
        milestoneToast.SetActive(false);
    }

    void OnPlayClicked()
    {
        StackGameManager.Instance.StartGame();
    }

    void OnMuteClicked()
    {
        muted = !muted;
        AudioManager.Instance.SetMuted(muted);
        Text label = muteButton.GetComponentInChildren<Text>();
        if (label != null) label.text = muted ? "MUTE OFF" : "MUTE";
    }

    //  점수 / 스테이지 (가운데 상단) 
    public void UpdateScore(int score)
    {
        scoreText.text = score.ToString();
        StartCoroutine(PopScale(scoreText.transform));
    }

    IEnumerator PopScale(Transform t)
    {
        t.localScale = Vector3.one * 1.25f;
        float duration = 0.12f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.one, elapsed / duration);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    public void UpdateAudioStage(int stage)
    {
        audioStageText.text = "AUDIO STAGE: " + stage;
    }

    //  우측 정보 
    public void UpdateBest(int best)
    {
        bestText.text = best.ToString();
    }

    public void UpdateSpeedGauge(float pct)
    {
        if (speedFillImage != null) speedFillImage.fillAmount = Mathf.Clamp01(pct);
    }

    //  콤보 
    private static readonly string[] COMBO_MSG = { "", "", "DOUBLE!", "TRIPLE!", "QUAD!", "PENTA!", "HEXA!", "LUCKY!" };

    public void ShowCombo(int streak)
    {
        if (streak < 2) { HideCombo(); return; }
        if (comboRoutine != null) StopCoroutine(comboRoutine);

        comboText.text = streak < COMBO_MSG.Length ? COMBO_MSG[streak] : ("x" + streak + " PERFECT!");
        comboRoutine = StartCoroutine(ComboAnim());
    }

    IEnumerator ComboAnim()
    {
        comboGroup.alpha = 1f;
        comboGroup.transform.localScale = Vector3.one * 1.15f;
        float t = 0f;
        while (t < 0.15f) { t += Time.deltaTime; comboGroup.transform.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, t / 0.15f); yield return null; }

        yield return new WaitForSecondsRealtime(0.9f);

        t = 0f;
        while (t < 0.2f) { t += Time.deltaTime; comboGroup.alpha = 1f - t / 0.2f; yield return null; }
        comboGroup.alpha = 0f;
    }

    public void HideCombo()
    {
        if (comboRoutine != null) StopCoroutine(comboRoutine);
        comboGroup.alpha = 0f;
    }

    //  마일스톤 토스트 (10점마다) 
    public void ShowMilestone(int stage)
    {
        if (milestoneRoutine != null) StopCoroutine(milestoneRoutine);
        milestoneTitleText.text = "STAGE " + stage;
        milestoneRoutine = StartCoroutine(MilestoneAnim());
    }

    IEnumerator MilestoneAnim()
    {
        milestoneToast.SetActive(true);
        CanvasGroup cg = milestoneToast.GetComponent<CanvasGroup>();
        if (cg == null) cg = milestoneToast.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < 0.35f) { t += Time.deltaTime; cg.alpha = t / 0.35f; yield return null; }
        cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(1.4f);

        t = 0f;
        while (t < 0.35f) { t += Time.deltaTime; cg.alpha = 1f - t / 0.35f; yield return null; }
        cg.alpha = 0f;
        milestoneToast.SetActive(false);
    }

    public void HideMilestone()
    {
        if (milestoneRoutine != null) StopCoroutine(milestoneRoutine);
        milestoneToast.SetActive(false);
    }

    //  오버레이 
    public void ShowStartOverlay()
    {
        startOverlay.SetActive(true);
        gameOverOverlay.SetActive(false);
        if (playButtonLabel != null) playButtonLabel.text = "PLAY";
    }

    public void HideOverlay()
    {
        startOverlay.SetActive(false);
        gameOverOverlay.SetActive(false);
    }

    public void ShowGameOverOverlay(int score, bool isRecord)
    {
        startOverlay.SetActive(false);
        gameOverOverlay.SetActive(true);
        finalScoreText.text = score.ToString();
        if (newRecordLabel != null) newRecordLabel.SetActive(isRecord);
        if (playButtonLabel != null) playButtonLabel.text = "RETRY";
    }

    //  일시정지 오버레이 
    public void ShowPauseOverlay()
    {
        pauseOverlay.SetActive(true);
    }

    public void HidePauseOverlay()
    {
        pauseOverlay.SetActive(false);
    }

    //  미스 플래시 
    public void FlashMiss()
    {
        StartCoroutine(MissFlashRoutine());
    }

    IEnumerator MissFlashRoutine()
    {
        Color c = missFlashImage.color;
        c.a = 0.45f;
        missFlashImage.color = c;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0.45f, 0f, t / 0.4f);
            missFlashImage.color = c;
            yield return null;
        }
    }
}