// ====================================================
// 게임 핵심 로직
// - 블록 스폰, 좌우(또는 앞뒤) 이동, 떨어뜨려서 쌓기
// - 겹치는 부분만 살리고 나머지는 잘림 처리
// - 점수 계산, 속도 증가, 퍼펙트 판정까지 여기서 다 함
// 다른 매니저들(Audio/UI/Camera/Effects)은 여기서 Instance로 호출해서 씀
// ====================================================
using System.Collections.Generic;
using UnityEngine;

public class StackGameManager : MonoBehaviour
{
    public static StackGameManager Instance;

    [Header("References")]
    public GameObject blockPrefab;
    public Transform cameraRig;          // CameraFollow.cs가 붙은 카메라 부모 오브젝트

    [Header("Block Settings")]
    public float blockHeight = 0.5f;
    public float baseSize = 3f;
    public float perfectThreshold = 0.05f;

    [Header("Speed Settings")]
    public float moveSpeedBase = 3.5f;
    public float moveSpeedMax = 12f;
    public float speedStep = 0.45f;
    public float moveRange = 4f;

    [Header("Color Palette (8단 그라데이션)")]
    public Color[] paletteA;
    public Color[] paletteB;

    //  내부 상태 
    private List<GameObject> stack = new List<GameObject>();
    private GameObject currentBlock;
    private Vector3 currentSize;
    private int moveAxis = 0;   // 0 = X축, 1 = Z축
    private int moveDir = 1;
    private float moveSpeed;
    public int score { get; private set; }
    public int best { get; private set; }
    public bool running { get; private set; }
    public bool gameOver { get; private set; }
    public bool paused { get; private set; }

    private int perfectStreak = 0;
    private int lastStage = 0;

    void Awake()
    {
        Instance = this;
        if (paletteA.Length == 0)
        {
            // 기본 팔레트 (HTML 버전과 동일한 색상)
            paletteA = new Color[] {
                HexColor("#00f5a0"), HexColor("#ff6b6b"), HexColor("#a29bfe"), HexColor("#00cec9"),
                HexColor("#f7971e"), HexColor("#56ccf2"), HexColor("#e96c6c"), HexColor("#b8f400")
            };
            paletteB = new Color[] {
                HexColor("#00c8ff"), HexColor("#feca57"), HexColor("#fd79a8"), HexColor("#6c5ce7"),
                HexColor("#ffd200"), HexColor("#2f80ed"), HexColor("#ff8c42"), HexColor("#00f5a0")
            };
        }
    }

    void Start()
    {
        best = PlayerPrefs.GetInt("StackBest", 0);
        UIManager.Instance.UpdateBest(best);
        UIManager.Instance.ShowStartOverlay();
    }

    void Update()
    {
        if (!running || gameOver || paused) return;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            PlaceBlock();
        }

        MoveCurrentBlock();
    }


    public void StartGame()
    {
        ClearStack();

        score = 0;
        moveSpeed = moveSpeedBase;
        moveDir = 1;
        moveAxis = 0;
        perfectStreak = 0;
        lastStage = 0;
        gameOver = false;
        paused = false;
        running = true;

        UIManager.Instance.UpdateScore(0);
        UIManager.Instance.UpdateSpeedGauge(0f);
        UIManager.Instance.UpdateAudioStage(1);
        UIManager.Instance.HideOverlay();
        UIManager.Instance.HideMilestone();

        GameObject baseBlock = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity);
        baseBlock.transform.localScale = new Vector3(baseSize, blockHeight, baseSize);
        SetColor(baseBlock, GetColorForLayer(0));
        stack.Add(baseBlock);

        currentSize = baseBlock.transform.localScale;
        SpawnNextBlock();

        AudioManager.Instance.StartBGM();
    }

    void ClearStack()
    {
        foreach (var b in stack) if (b != null) Destroy(b);
        stack.Clear();
        if (currentBlock != null) Destroy(currentBlock);
        currentBlock = null;
        cameraRig.position = new Vector3(cameraRig.position.x, 4f, cameraRig.position.z);
    }


    void SpawnNextBlock()
    {
        int layer = stack.Count;
        Vector3 prevPos = stack[stack.Count - 1].transform.position;

        Vector3 spawnPos = prevPos + Vector3.up * blockHeight;
        moveAxis = layer % 2; // 층마다 X축 / Z축 번갈아 이동

        float startOffset = moveRange * moveDir;
        if (moveAxis == 0) spawnPos.x = prevPos.x + startOffset;
        else spawnPos.z = prevPos.z + startOffset;

        currentBlock = Instantiate(blockPrefab, spawnPos, Quaternion.identity);
        currentBlock.transform.localScale = currentSize;
        SetColor(currentBlock, GetColorForLayer(layer));

        moveDir *= -1;

        // 카메라가 따라 올라가도록 목표 높이 전달
        CameraFollow.Instance.SetTargetHeight(spawnPos.y + 4f);
    }

    void MoveCurrentBlock()
    {
        if (currentBlock == null) return;

        Vector3 pos = currentBlock.transform.position;
        float delta = moveSpeed * Time.deltaTime * (moveDir > 0 ? -1 : 1);

        if (moveAxis == 0) pos.x += delta;
        else pos.z += delta;

        currentBlock.transform.position = pos;

        Vector3 prevPos = stack[stack.Count - 1].transform.position;
        float coord = (moveAxis == 0) ? pos.x : pos.z;
        float prevCoord = (moveAxis == 0) ? prevPos.x : prevPos.z;

        if (Mathf.Abs(coord - prevCoord) > moveRange)
        {
            moveDir *= -1;
        }
    }


    void PlaceBlock()
    {
        GameObject prev = stack[stack.Count - 1];
        Vector3 prevPos = prev.transform.position;
        Vector3 prevScale = prev.transform.localScale;

        Vector3 curPos = currentBlock.transform.position;
        Vector3 curScale = currentBlock.transform.localScale;

        float axisCur = (moveAxis == 0) ? curPos.x : curPos.z;
        float axisPrev = (moveAxis == 0) ? prevPos.x : prevPos.z;
        float curSize = (moveAxis == 0) ? curScale.x : curScale.z;
        float prevSize = (moveAxis == 0) ? prevScale.x : prevScale.z;

        float diff = axisCur - axisPrev;

        // 겹치는 구간 계산 (선분 교차 구하는 거랑 같음)
        // 두 블록의 min/max 중에서 더 안쪽 값들끼리 빼면 겹친 길이 나옴
        float curMin = axisCur - curSize / 2f;
        float curMax = axisCur + curSize / 2f;
        float prevMin = axisPrev - prevSize / 2f;
        float prevMax = axisPrev + prevSize / 2f;

        float overlapMin = Mathf.Max(curMin, prevMin);
        float overlapMax = Mathf.Min(curMax, prevMax);
        float overlapSize = overlapMax - overlapMin;

        // 겹치는 부분이 없으면(0 이하) = 완전히 빗나간 거 = 게임오버
        if (overlapSize <= 0f)
        {
            TriggerGameOver();
            return;
        }

        bool perfect = Mathf.Abs(diff) <= perfectThreshold;

        if (perfect)
        {
            // 퍼펙트면 위치/크기를 이전 블록이랑 완전히 똑같이 맞춤 (스냅)
            // 그래야 오차가 누적 안 되고 계속 퍼펙트 칠 수 있음
            if (moveAxis == 0) curPos.x = prevPos.x;
            else curPos.z = prevPos.z;
            currentBlock.transform.position = curPos;

            perfectStreak++;
            UIManager.Instance.ShowCombo(perfectStreak);
            AudioManager.Instance.PlayPerfectSound(perfectStreak);
            CameraFollow.Instance.Shake(0.12f, 7);
            EffectsManager.Instance.SpawnShockwave(currentBlock.transform.position, currentBlock.GetComponent<Renderer>().material.color);
        }
        else
        {
            perfectStreak = 0;
            UIManager.Instance.HideCombo();

            float overlapCenter = (overlapMin + overlapMax) / 2f;
            Vector3 newScale = curScale;
            Vector3 newPos = curPos;
            if (moveAxis == 0) { newScale.x = overlapSize; newPos.x = overlapCenter; }
            else { newScale.z = overlapSize; newPos.z = overlapCenter; }

            // 잘려나가는 조각 스폰
            float cutSize = curSize - overlapSize;
            if (cutSize > 0.01f)
            {
                float cutCenter = (curMin < prevMin) ? (curMin + (prevMin - curMin) / 2f) : (curMax - (curMax - prevMax) / 2f);
                Vector3 cutPos = curPos;
                Vector3 cutScale = curScale;
                if (moveAxis == 0) { cutPos.x = cutCenter; cutScale.x = cutSize; }
                else { cutPos.z = cutCenter; cutScale.z = cutSize; }

                EffectsManager.Instance.SpawnFallingPiece(blockPrefab, cutPos, cutScale, currentBlock.GetComponent<Renderer>().material.color);
            }

            currentBlock.transform.position = newPos;
            currentBlock.transform.localScale = newScale;
            currentSize = newScale;

            AudioManager.Instance.PlayPlaceSound(score);
        }

        stack.Add(currentBlock);
        currentBlock = null;

        score++;
        UIManager.Instance.UpdateScore(score);

        // 점수 오를수록 블록 이동속도 점점 빨라짐 (최대값 캡 걸어둠)
        moveSpeed = Mathf.Min(moveSpeedBase + score * speedStep, moveSpeedMax);
        float pct = (moveSpeed - moveSpeedBase) / (moveSpeedMax - moveSpeedBase);
        UIManager.Instance.UpdateSpeedGauge(pct);

        // 10점마다 오디오 스테이지 단계 올라감 (최대 4단계, AudioManager의 stage 0~3과 매칭)
        int newStage = Mathf.Min(3, score / 10);
        if (newStage > lastStage)
        {
            lastStage = newStage;
            UIManager.Instance.UpdateAudioStage(newStage + 1);
            UIManager.Instance.ShowMilestone(newStage + 1);
        }
        AudioManager.Instance.SetStage(lastStage);

        SpawnNextBlock();
    }


    void TriggerGameOver()
    {
        gameOver = true;
        running = false;

        if (currentBlock != null)
        {
            Rigidbody rb = currentBlock.AddComponent<Rigidbody>();
            rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 5f, ForceMode.Impulse);
            Destroy(currentBlock, 3f);
        }

        AudioManager.Instance.StopBGM();
        AudioManager.Instance.PlayGameOverSound();
        UIManager.Instance.FlashMiss();

        bool isRecord = score > best;
        if (isRecord)
        {
            best = score;
            PlayerPrefs.SetInt("StackBest", best);
            PlayerPrefs.Save();
        }
        UIManager.Instance.UpdateBest(best);
        UIManager.Instance.ShowGameOverOverlay(score, isRecord);
    }


    // 일시정지 기능
    public void TogglePause()
    {
        if (!running || gameOver) return;

        paused = !paused;
        if (paused)
        {
            Time.timeScale = 0f;
            AudioManager.Instance.SetPaused(true);
            UIManager.Instance.ShowPauseOverlay();
        }
        else
        {
            Time.timeScale = 1f;
            AudioManager.Instance.SetPaused(false);
            UIManager.Instance.HidePauseOverlay();
        }
    }

    public void ResumeGame()
    {
        if (!paused) return;
        paused = false;
        Time.timeScale = 1f;
        AudioManager.Instance.SetPaused(false);
        UIManager.Instance.HidePauseOverlay();
    }


    Color GetColorForLayer(int layer)
    {
        int idx = (layer / 8) % paletteA.Length;
        float t = (layer % 8) / 7f;
        return Color.Lerp(paletteA[idx], paletteB[idx], t);
    }

    void SetColor(GameObject obj, Color color)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(rend.material);
            rend.material.color = color;
        }
    }

    public static Color HexColor(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }
}