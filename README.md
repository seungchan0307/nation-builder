# Nation Builder (working title)

클래시 오브 클랜의 실시간 성장 루프와 문명의 선택형 국가 분기를 결합한 모바일 국가 빌더 게임. 전투는 아직 구현하지 않는다.

## 기획 개요

- **성장 루프**: 마을회관(수도) 레벨업이 진행의 중심축. 클래시 오브 클랜식 실시간 타이머로 자원이 축적되고, 건물 업그레이드도 실시간으로 진행된다.
- **노드 트리**: 모든 플레이어가 공유하는 단일 노드 트리(패스 오브 엑자일 방식)에 포인트를 투자한다. 어느 구역에 투자하느냐에 따라 결과적으로 나라의 스타일이 갈린다.
- **건물 도감**: 트리에서 노드를 해금하면 해당 건물이 도감에 등록되고, 그때부터 건설할 수 있게 된다.
- **진행 방식**: 평소에는 실시간 방치형으로 흘러가며, 마을회관 레벨업이나 포인트 획득 같은 마일스톤 시점에만 문명(Civilization)식 선택 화면이 뜬다.
- **전투**: 아직 구현하지 않음. 지금은 성장/선택 시스템 완성에 집중한다.
- **아트·네이밍**: 원본 게임들의 상표·에셋과 겹치지 않도록 직접 제작한다.

세부 밸런스 수치와 차별화 훅 후보는 별도 비공개 문서에서 관리한다 (아직 확정 전).

## 현재 상태

### 1단계: 실시간 자원 축적

- `Assets/Scripts/Core/ResourceManager.cs`: 시간이 지나면 골드가 자동으로 쌓이는 싱글턴 매니저.
- `Assets/Scripts/UI/GoldDisplay.cs`: 현재 골드 수치를 TextMeshPro 텍스트로 표시.

이 둘은 씬에 손으로 오브젝트를 만들어서 붙여야 한다 (`GameManager` + `ResourceManager`, `Text (TMP)` + `GoldDisplay`) — 아래 "Unity 에디터에서 확인하는 방법" 참고.

### 2단계: 노드 트리 / 건물 도감 / 건설·업그레이드 / 마을회관 마일스톤 / 오프라인 축적

`Assets/Scripts/Core/GameBootstrap.cs`가 게임 시작 시 자동으로 `SystemsRoot`라는 오브젝트를 만들고 아래 시스템들을 전부 붙여준다. **씬에 손으로 추가할 것 없음.**

- `NodeTreeManager` — `Assets/Resources/node-tree.txt`에서 읽어오는 공유 노드 트리. 포인트로 노드를 해금하면 해당 건물이 도감에 등록됨. 노드는 경제/군사/기반/문화 4개 구역(+ 합류 지점인 대회당)으로 나뉘어 있고, 어느 구역에 포인트를 많이 썼는지로 "나라 성향"이 계산됨 (`NodeTreeManager.LeadingCategory()`).
- `BuildingDex` — `Assets/Resources/buildings.txt`에서 읽어오는 전체 건물 목록(23종) + 그중 해금(등록)된 것들.
- `BuildingManager` — 건물 건설/업그레이드. 완료 시각을 절대 UTC 시각으로 저장해서, 앱이 꺼져있던 동안 지난 시간도 자동으로 반영됨.
- `TownHallManager` — 마을회관 레벨. 업그레이드 완료 시 노드 트리 포인트 +1 지급 + 마일스톤 선택 이벤트 발동.
- `MilestoneManager` — 마을회관 레벨업마다 뜨는 문명식 선택지 3개 (경제/건설/포인트 특화 — 프로토타입용 임시 옵션, 실제 차별화 훅 정해지면 교체 예정).
- `SaveSystem` — `Application.persistentDataPath/nation_save.json`에 저장. 30초마다 자동 저장 + 종료 시 저장. 재시작하면 꺼져있던 시간만큼 골드를 오프라인 보정으로 받음.
- `DevHudUI` — 위 시스템들을 조작해볼 수 있는 **임시 디버그 UI** (OnGUI, 꾸미지 않음). 실제 아트 방향이 정해지면 Canvas/TextMeshPro 기반 UI로 교체할 것.

밸런스 수치(`node-tree.txt`, `buildings.txt`의 포인트/골드/시간 값)는 전부 프로토타입용 임시값이다. 확정 수치는 `private-notes/DESIGN-PRIVATE.md`에서 관리.

### 3단계: 건물 3D 모델 (첫 배치)

- `Assets/Art/FantasyTownKit/` — [Kenney](https://kenney.nl)의 **Fantasy Town Kit** (CC0, 저작자 표시 불필요). FBX 모델 167개 + 텍스처 1장. 대부분 벽/지붕/계단 같은 조립용 부품이라, 건물 하나하나를 완성된 모델로 조립하는 건 에디터에서 직접 손으로 해야 하는 작업임.
- `Assets/Editor/BuildingPrefabGenerator.cs` — Fantasy Town Kit 부품으로 프리팹을 만드는 에디터 전용 스크립트. 세 가지 방식으로 **23개 건물 전부**를 커버함:
  - **단일 모델 매칭** (11종): 풍차→제분소, 시장 좌판→시장, 수레→교역소, 성벽 조각→성벽, 분수→사당, 돌기둥→기념비, 나무→제재소/목재소, 도로→도로, 계단→원형극장, 수차→수도교.
  - **부품 조립** (12종): farm/granary/bank/barracks/archery_range/fortress/war_camp/quarry/workshop/library/observatory/grand_hall — 발판+벽+지붕(+토퍼)을 세로로 쌓음.
  - **마을회관 3단계**: `town_hall_tier1`(초라한 목조 회관, Lv1~2) → `tier2`(아치형 회관, Lv3~5) → `tier3`(웅장한 회관, Lv6+).

  조립 위치는 손으로 정한 게 아니라, 각 조각을 인스턴스화한 뒤 실제 메쉬 크기(`Renderer.bounds`)를 읽어서 자동으로 정렬함 — 부품 크기를 몰라도 뜨거나 겹치지 않음. Unity 메뉴에서 **Nation Builder > Generate Building Prefabs (Fantasy Town Kit)** 실행하면 `Assets/Resources/Buildings/`와 `Assets/Resources/TownHall/`에 전부 생성됨.
- `Assets/Scripts/World/BuildingWorldView.cs` — 건물을 건설하면(또는 저장 파일에서 불러오면) 마을회관 앞쪽에 격자 배치로 실제 3D 오브젝트를 씬에 스폰함. 프리팹이 있으면 그 모델을, 없으면 노드 카테고리 색(경제=노랑/군사=빨강/기반=회색/문화=파랑)의 임시 큐브를 대신 세워둠.
- `Assets/Scripts/World/TownHallView.cs` — 마을 중앙(원점)에 마을회관을 세움. 마을회관 레벨에 따라 `town_hall_tier{1,2,3}` 프리팹을 갈아끼워서 외관 자체가 바뀜(프리팹 없으면 도형 임시 모델). 레벨업마다 빛 번쩍임 + 살짝 부풀어오르는 이펙트 재생.
- `Assets/Scripts/World/WorldDressing.cs` — 넓은 초록 바닥판을 깔고, 씬에 조명이 없으면 자동으로 방향광을 추가하고, 메인 카메라에 마우스 휠 확대/축소(`CameraZoomController`)를 붙임.
- `Assets/Scripts/World/CameraZoomController.cs` — 마우스 휠로 카메라를 자신의 정면 방향으로 이동시켜 확대/축소(FOV를 바꾸지 않아서 아이소메트릭 각도가 일그러지지 않음). 높이 4~26 사이로 제한.

프리팹 생성 메뉴를 실행하면 23개 건물 + 마을회관 3단계가 전부 실제 모델로 뜬다. 실행 안 했으면 여전히 색깔 큐브/도형으로 대체됨(안전한 폴백).

## Unity 에디터에서 확인하는 방법

1. Unity Hub에서 이 폴더(`게임개발2`)를 프로젝트로 열기. (`ProjectSettings/ProjectVersion.txt`에 Unity 6.3 LTS(6000.3.22f1)로 지정해뒀지만, 설치된 다른 버전으로 열어도 무방함 — Unity가 알아서 업그레이드를 제안함)
2. 씬에 아직 `GameManager`(+ `ResourceManager`), `Text (TMP)`(+ `GoldDisplay`)가 없다면 만들어준다 (1단계 참고 — 이미 되어 있다면 생략).
3. 새로 추가된 `Fantasy Town Kit` 임포트가 끝날 때까지 기다린 다음(에디터 하단 로딩 아이콘), 메뉴에서 **Nation Builder > Generate Building Prefabs (Fantasy Town Kit)** 를 한 번 실행한다 (3단계 참고).
4. Play 버튼을 눌러본다. 화면 왼쪽 위에 노드 트리 패널, 건물 도감 패널, 마을회관 패널이 뜬다. 마을 중앙에 마을회관이 서 있고 바닥이 깔려 있는지 확인.
5. 노드를 해금해보고(포인트가 부족하면 마을회관을 업그레이드해서 포인트를 얻는다), 도감에 등록된 건물을 건설/업그레이드해본다 — 마을회관 앞쪽에 건물이 격자로 세워지는지 확인.
6. 씬 저장 없이 Play를 멈췄다가 다시 켜도(또는 에디터를 완전히 재시작해도) 진행 상황이 `nation_save.json`에서 복원된다.

## 다음 단계

- [ ] `DevHudUI`를 Canvas 기반 정식 UI로 교체
- [ ] 차별화 훅 확정 후 마일스톤 선택지 재설계 (`private-notes/DESIGN-PRIVATE.md` 참고)
- [ ] 밸런스 수치 조정 (`node-tree.txt`, `buildings.txt`)
- [ ] 전투 시스템

## 사용한 무료 에셋

- [Fantasy Town Kit](https://kenney.nl/assets/fantasy-town-kit) by Kenney — CC0 (저작자 표시 의무 없음, 그래도 감사한 마음에 남겨둠). 라이선스 전문: `Assets/Art/FantasyTownKit/LICENSE.txt`
