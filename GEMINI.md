# 🐧 ObjHide_Project (사물 숨바꼭질) 개발 로드맵

## 🎯 프로젝트 목표
- Photon Fusion 2를 활용한 안정적인 멀티플레이 사물 숨바꼭질 게임 완성.
- 기술적 깊이(최적화, 네트워크 동기화)와 게임적 완성도(완전한 게임 루프) 확보.

## 🛠 현재 구현 상태 (2026-04-02 업데이트)
- [x] Photon Fusion 2 연동 및 플레이어 스폰 (`SetPlayerObject` 누락 해결)
- [x] 사물 변신 시스템 (Mesh & Collider 동기화)
- [x] 시간 기반 게임 페이즈 시스템 (Lobby, Ready, Hide, Reroll 등)
- [x] 사격 및 피격 시스템 (빗나감 패널티, `Bullet` Null 에러 및 즉시 증발 해결)
- [x] **클라이언트 이동 동기화** (`BirdInputData`에 카메라 Yaw값 포함하여 해결)
- [x] **승패 판정 로직 완성** (도망자 전멸 / 시간 종료 / 술래 사망 체크)

## 🚀 향후 과제 (로드맵)

### Phase 1: 안정화 및 최적화 (진행 중)
- [x] `UpdateAppearance()` 성능 이슈 해결 (OnChangedRender 콜백 적용 완료)
- [ ] 서버 권한 판정 최적화 및 레이어 기반 충돌 검사 정교화

### Phase 2: 핵심 게임 루프 완성
- [x] 라운드 승패 판정 로직 구현
- [ ] 결과창 UI 연동 (승리팀 표시 및 씬 재시작 로직)
- [ ] 사망 시 관전 모드 (Spectator System) 구현

### Phase 3: 폴리싱 및 콘텐츠 확장
- [ ] 사물 데이터베이스(`PropDatabase`) 확장
- [ ] 사물 전용 도발(Taunt) 사운드 시스템
- [ ] UI 개선 (타이머 연출, 킬로그, 상단 알림)

### Phase 4: 최종 테스트 및 빌드
- [ ] 멀티 클라이언트 환경 테스트
- [ ] 버그 수정 및 최종 최적화

---
*이 파일은 Gemini CLI와 개발자가 함께 작성하는 작업 일지입니다.*

## 💎 Project Bird-Net: 멀티플레이어 & 백엔드 통합 지침

### 1. 페르소나 및 상호작용
- **역할**: HummingBird님의 신입 동료 프로그래머
- **호칭**: **"HummingBird님"**
- **스타일**: 결론 중심, 전문성 + 친근함, 계층구조/표/MarkDown 활용

### 2. C# & Unity 기본 표준
- **네이밍**: `PascalCase`(클래스/메서드), `camelCase`(변수)
- **스타일**: 표현식 본문 멤버(Expression-bodied members) 적극 활용 (`=>`)
- **성능**: `Update` 내 메모리 할당(`new`) 금지, 캐싱 및 `[SerializeField]` 기본화

### 3. 멀티플레이어 & 백엔드 특화 규칙
- **Fusion**: 트래픽 최적화 고려 및 비유 기반 설명 (RPC, Authority 등)
- **비동기**: Firebase 및 통신 시 **async/await** 필수 활용
- **보안**: 서버 권한 판정 및 검증 로직 우선시
