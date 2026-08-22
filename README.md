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

## Unity 에디터에서 확인하는 방법

1. Unity Hub에서 이 폴더(`게임개발2`)를 프로젝트로 열기. (`ProjectSettings/ProjectVersion.txt`에 Unity 6.3 LTS(6000.3.22f1)로 지정해뒀지만, 설치된 다른 버전으로 열어도 무방함 — Unity가 알아서 업그레이드를 제안함)
2. 씬에 아직 `GameManager`(+ `ResourceManager`), `Text (TMP)`(+ `GoldDisplay`)가 없다면 만들어준다 (1단계 참고 — 이미 되어 있다면 생략).
3. Play 버튼을 눌러본다. 화면 왼쪽 위에 노드 트리 패널, 건물 도감 패널, 마을회관 패널이 뜬다.
4. 노드를 해금해보고(포인트가 부족하면 마을회관을 업그레이드해서 포인트를 얻는다), 도감에 등록된 건물을 건설/업그레이드해본다.
5. 씬 저장 없이 Play를 멈췄다가 다시 켜도(또는 에디터를 완전히 재시작해도) 진행 상황이 `nation_save.json`에서 복원된다.

## 다음 단계

- [ ] 실제 아트(건물 스프라이트/모델) 적용, `DevHudUI`를 Canvas 기반 정식 UI로 교체
- [ ] 차별화 훅 확정 후 마일스톤 선택지 재설계 (`private-notes/DESIGN-PRIVATE.md` 참고)
- [ ] 밸런스 수치 조정 (`node-tree.txt`, `buildings.txt`)
- [ ] 전투 시스템
