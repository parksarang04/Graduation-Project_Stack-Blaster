// ====================================================
// 이펙트 매니저
// 1) 블록 잘릴 때 떨어지는 조각 - Rigidbody 붙여서 물리로 떨어지게
// 2) 퍼펙트일 때 생기는 원형 충격파 - Quad 스케일 키우면서 알파 줄임
// ====================================================
using UnityEngine;

public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance;

    [Header("Shockwave")]
    public GameObject shockwavePrefab; // 속이 빈 링 형태 Quad/Sprite 권장 (없으면 자동 생성됨)
    public float shockwaveLife = 0.6f;
    public float shockwaveMaxScale = 4f;

    void Awake()
    {
        Instance = this;
    }

    //  잘린 블록 조각 (물리 적용 후 자동 삭제) 
    public void SpawnFallingPiece(GameObject blockPrefab, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject piece = Instantiate(blockPrefab, pos, Quaternion.identity);
        piece.transform.localScale = scale;

        Renderer rend = piece.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(rend.material);
            rend.material.color = color;
        }

        Rigidbody rb = piece.AddComponent<Rigidbody>();
        rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 5f, ForceMode.Impulse);
        rb.AddForce(new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f)) * 2f, ForceMode.Impulse);

        Destroy(piece, 2.5f);
    }

    //  퍼펙트 충격파 (확장하는 링) 
    public void SpawnShockwave(Vector3 pos, Color color)
    {
        GameObject ring;
        if (shockwavePrefab != null)
        {
            ring = Instantiate(shockwavePrefab, pos, Quaternion.Euler(90, 0, 0));
        }
        else
        {
            // 프리팹 없으면 기본 Quad로 자동 생성
            ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.transform.position = pos + Vector3.up * 0.01f;
            ring.transform.rotation = Quaternion.Euler(90, 0, 0);
            Destroy(ring.GetComponent<Collider>());

            Renderer r = ring.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Sprites/Default"));
            r.material.color = color;
        }

        StartCoroutine(ShockwaveAnim(ring, color));
    }

    System.Collections.IEnumerator ShockwaveAnim(GameObject ring, Color color)
    {
        Renderer rend = ring.GetComponent<Renderer>();
        float t = 0f;
        Vector3 startScale = Vector3.one * 0.2f;
        Vector3 endScale = Vector3.one * shockwaveMaxScale;

        while (t < shockwaveLife)
        {
            t += Time.deltaTime;
            float p = t / shockwaveLife;
            ring.transform.localScale = Vector3.Lerp(startScale, endScale, p);

            if (rend != null)
            {
                Color c = color;
                c.a = 1f - p;
                rend.material.color = c;
            }
            yield return null;
        }
        Destroy(ring);
    }
}