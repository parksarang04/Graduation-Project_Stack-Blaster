# 🚀 STACK EVO (Stack Blaster)

> **Web Audio API와 Canvas API를 활용한 하이테크 스타일의 어댑티브 적재 웹 게임**
>
> 본 프로젝트는 외부 게임 엔진(Unity, Unreal 등)이나 라이브러리(Three.js, Pixi.js)를 일절 사용하지 않고, 오직 브라우저 순수 순정 기술(**Vanilla JS, HTML5 Canvas, Web Audio API**)만을 이용하여 구현한 그래픽스 및 사운드 인터랙션 게임입니다.

---

## 🎮 게임 스크린샷
![STACK EVO 게임 실행 화면](screenshot.png)

---

## 🌟 핵심 개발 및 기술 특징

### 1. 동적 어댑티브 오디오 시스템 (Adaptive Audio Engine)
* **멀티레이어 사운드 빌드업:** 외부 `.mp3` 에셋 없이 브라우저 내장 주파수 발생기(`OscillatorNode`)와 게인 조절기(`GainNode`)를 시퀀서 형태로 제어합니다.
* **점수 연동형 사운드 진화:** 플레이어의 점수가 10단위(Stage)를 돌파할 때마다 메인 루프 사운드가 끊기지 않고 **실시간으로 악기 레이어(스네어 노이즈, 아르페지에이터 멜로디, 옥타브 쉬프팅 베이스)가 결합**되는 인터랙션 사운드를 구현했습니다.
* **클리핑 방지:** 지수 감쇠 함수(`exponentialRampToValueAtTime`)를 적용하여 레트로 사운드 특유의 틱 노이즈(오디오 튐 현상)를 공학적으로 차단했습니다.

### 2. 델타 타임(dt) 보정형 물리 및 특수효과
* **기기 독립적 환경 보정:** 모니터 주사율(60Hz, 144Hz 등)에 따라 게임 속도가 달라지는 문제를 방지하기 위해 프레임 간 격차를 보정하는 **델타 타임(Delta Time) 알고리즘**을 메인 루프에 이식했습니다.
* **자유낙하 파편 연산:** 잘려 나가는 자투리 블록에 오일러 적분법(Euler Integration) 기반의 중력 가속도 물리 모델을 적용하여 자연스러운 비산 파편을 묘사했습니다.
* **도파민 극대화 연출:** 퍼펙트(연속 성공) 판정 시 사인파(Sin) 기반의 화면 진동(Screen Shake) 효과와 선형 보간 이징(Easing)을 적용한 원형 충격파(Shockwave) 벡터 그래픽스를 구현하여 타격감을 높였습니다.

### 3. 의사 3D 투영 기하학 (Pseudo-3D Isometric Canvas)
* 무거운 3D 연산 행렬 없이, Canvas 2D 컨텍스트 상에서 정점 시차 왜곡(Polygon Vertex Offset) 측면 드로잉 꼼수를 활용하여 저사양 환경에서도 입체적인 3D Box 느낌을 재현하고 하드웨어 가속을 최적화했습니다.

---

## 🕹️ 조작 방법

* **PC:** `SpaceBar` 또는 `위쪽 화살표(↑)` 또는 `마우스 왼쪽 클릭`
* **Mobile / Tablet:** 화면 어디든 `터치(Tab)`
* **사운드 제어:** 우측 하단의 스피커 아이콘 토글을 통해 실시간 음소거 가능

---

## 🛠️ 기술 스택 (Tech Stack)

* **Language:** JavaScript (ES6+), HTML5, CSS3
* **Graphics:** HTML5 Canvas 2D Context API
* **Audio:** Web Audio API (Synthesized Audio)
* **Tools:** Git, Fork, VS Code