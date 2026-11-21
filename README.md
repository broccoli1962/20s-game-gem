# Crypt Rush

20초 게임잼을 위해 만든 1인칭 던전 아케이드입니다. 화면의 화살표와 같은 방향키를 눌러 해골, 박쥐, 미믹을 베고 전진하세요. 같은 수의 몬스터를 잡아도 다음 화살표를 얼마나 빨리 맞히느냐에 따라 점수가 달라집니다.

**[itch.io에서 플레이](https://broccoli1962.itch.io/cryptrush)** · **[GitHub Pages에서 플레이](https://broccoli1962.github.io/20s-game-gem/)**

데스크톱 브라우저와 키보드로 플레이할 수 있습니다.

![미믹 앞에 오른쪽 화살표가 뜨고 난무 게이지가 24%까지 찬 전투 화면](20%27s%20Game%20Gem/ItchPage/02-mimic.png)

## 플레이 방법

| 언제 | 누르는 키 | 결과 |
| --- | --- | --- |
| 시작·결과 화면 | Enter 또는 Space | 20초 라운드를 시작하거나 다시 도전합니다. |
| 일반 전투 | 화면과 같은 방향키 ↑ → ↓ ← | 몬스터 한 마리를 베고 다음 화살표를 표시합니다. |
| 난무 중 (`ANY KEY`) | 아무 키나 반복 입력 | 누를 때마다 한 마리를 베고 5점을 얻습니다. |

방향을 맞힐 때마다 게이지가 20%p 차고, 곧바로 초당 20%p씩 줄어듭니다. 빠르게 이어 맞혀 게이지를 높게 유지할수록 한 번의 공격으로 더 많은 점수를 얻습니다. 방향을 틀리면 게이지와 연속 처치가 초기화되고 0.65초간 공격이 막힙니다.

게이지를 가득 채우면 **3초간 난무**가 발동합니다. 난무 중에도 라운드 타이머는 계속 흐르며, 20초가 끝나는 순간 점수가 확정됩니다.

## 개발 환경과 테스트

1. Unity Hub에서 저장소 안의 `20's Game Gem` 폴더를 Unity `6000.3.6f1`로 엽니다.
2. `Assets/Scenes/CryptRushScene.unity`를 열고 Play를 누르면 에디터에서 플레이할 수 있습니다.
3. Unity Test Runner의 **EditMode**에서 `DungeonRoundTests`를 실행하면 게이지의 즉시 감소, 오입력 경직, 난무 종료 시점의 점수 판정을 검증할 수 있습니다.

라운드 판정은 C#으로 작성했고 웹 버전은 Unity WebGL로 빌드했습니다. 그래픽 제작에는 ChatGPT 이미지 생성을, 프로그래밍에는 Codex를 활용했습니다.

[라운드 판정 코드](20%27s%20Game%20Gem/Assets/GameResource/Scripts/Object/GameSystems/DungeonRun/DungeonRound.cs) · [단위 테스트](20%27s%20Game%20Gem/Assets/GameResource/Tests/EditMode/DungeonRoundTests.cs)
