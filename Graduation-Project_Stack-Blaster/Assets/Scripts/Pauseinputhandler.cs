// ====================================================
// 일시정지 입력 처리
// ESC 누르면 GameManager의 TogglePause() 호출
// 모바일이면 Android 뒤로가기 버튼도 ESC로 인식됨
// ====================================================
using UnityEngine;

public class PauseInputHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StackGameManager.Instance.TogglePause();
        }
    }
}