# Nation Builder (working title)

클래시 오브 클랜의 실시간 성장 루프와 문명의 선택형 국가 분기를 결합한 모바일 국가 빌더 게임. 전투는 아직 구현하지 않는다.

## 현재 상태 (1단계: 실시간 자원 축적)

- `Assets/Scripts/Core/ResourceManager.cs`: 시간이 지나면 골드가 자동으로 쌓이는 싱글턴 매니저. 오프라인(앱이 꺼져 있는 동안) 자원 축적은 아직 구현하지 않았다.
- `Assets/Scripts/UI/GoldDisplay.cs`: 현재 골드 수치를 TextMeshPro 텍스트로 표시.

## Unity 에디터에서 확인하는 방법

1. Unity Hub에서 이 폴더(`nation-builder`)를 프로젝트로 열기. (`ProjectSettings/ProjectVersion.txt`에 Unity 6.3 LTS(6000.3.22f1)로 지정해뒀지만, 설치된 다른 버전으로 열어도 무방함 — Unity가 알아서 업그레이드를 제안함)
2. `Assets/Scenes` 폴더에 새 씬(`Main`)을 생성.
3. 빈 GameObject를 만들고 이름을 `GameManager`로 지정, `ResourceManager` 컴포넌트를 추가.
4. UI > Text - TextMeshPro로 텍스트 오브젝트 생성 (처음 추가 시 TMP Essentials 임포트 팝업이 뜨면 Import 클릭).
5. 해당 텍스트 오브젝트에 `GoldDisplay` 컴포넌트를 추가.
6. Play 버튼을 눌러 골드 숫자가 실시간으로 올라가는지 확인.

## 다음 단계 (확인 후 순서대로 추가 예정)

- [ ] 공유 노드 트리 데이터 구조 + 포인트 투자 UI
- [ ] 노드 해금 → 건물 도감 등록
- [ ] 건물 배치/업그레이드 (실시간 타이머)
- [ ] 마을회관 레벨 게이트 및 마일스톤 선택 이벤트
- [ ] 오프라인 자원 축적 계산
