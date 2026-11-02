# IdleBlade — 상용 방치형 RPG 개발 시작점

## 결정
Android 우선 / 가로 화면 / 2D 자동사냥 / 향후 iOS.
Unity 6000.3.25f1 + C#, Nakama 3.41.0 + ES5 JavaScript 런타임, PostgreSQL 17.11.
버전은 versions.json 및 실행 설정에 고정했다. Unity 패키지 잠금 파일은 최초 실제 에디터 import 후 생성·검토한다.
2026-09-27 실제 Unity 에디터 Play에서 서버 연결을 확인했다. Nakama/PostgreSQL 컨테이너에서 게스트 로그인, 시간 기반 보상, 강화, 중복 강화 거부 및 저장 확인 테스트를 통과했다. Android 빌드는 아직 검증하지 않았다.

Unity는 모바일 상용 SDK와 제작 생태계를 우선한 선택이다. Godot도 2D 구현에는 충분하고 가볍다.
Unreal은 이 프로젝트의 2D 규모에 비해 렌더링·배포·제작 복잡도가 커서 선택하지 않는다.
Nakama로 계정·저장·랭킹 기반을 활용하고 게임 규칙만 구현한다. 실시간 전용 Unity 서버는 사용하지 않는다.
운영 인프라는 서울 리전 Linux 컨테이너 + 관리형 PostgreSQL을 기본안으로 한다. 현재 클라우드 자원과 비용은 생성하지 않았다.

## 실행
1. Unity Hub에서 Unity **6000.3.25f1**과 Android Build Support (SDK/NDK/OpenJDK 포함)를 설치하고 라이선스를 활성화한다.
2. Docker Desktop의 Linux 컨테이너 환경을 준비한다.
3. 이 폴더의 PowerShell에서 `docker compose -p idleblade -f server/compose.yaml up -d`.
4. Unity Hub에 `client` 폴더를 추가하고 고정 버전으로 연다.
5. 메뉴 `IdleBlade > Create Boot Scene`을 실행하고 Play를 누른다.
6. 자동사냥으로 골드가 쌓이면 UPGRADE 버튼으로 공격력을 강화한다.

현재 REST 연결은 PC 에디터의 localhost 전용이다. 실제 Android 단말은 개발 서버 주소와 HTTPS 설정이 필요하다.
클라이언트 IMGUI·사각형 캐릭터는 기능 검증용이며 출시 UI/아트가 아니다. 게스트 ID 저장도 개발용이다.
최초 실행에서 서버는 0골드·1레벨을 생성한다. 3초마다 첫 몬스터 처치, 12초 후 첫 강화 비용을 확보한다.
플레이어가 접속하지 않아도 재접속 시 서버 시간으로 최대 8시간을 정산한다.

## 검증
`node --test server/tests/economy.test.cjs`
이 테스트는 실제 배포할 서버 로직을 Node VM에서 실행하고 Nakama 저장 API를 모의한다.
실제 Nakama JS 런타임, PostgreSQL 연결, Unity 컴파일 및 화면 검증을 대신하지 않는다.

실제 서버 통합 테스트: 서버 실행 후 `node server/tests/live-smoke.cjs`. 별도 테스트 계정을 생성하고 약 16초간 실제 사냥 시간을 기다린다.
컨테이너는 Docker 엔진 재시작 시 자동으로 재시작하도록 설정했다. Docker Desktop 자체는 실행 중이어야 한다.

## 현재 범위
- 서버 권한의 자동사냥·골드·강화·스테이지 1~100, 강화 1~100.
- 서버 시간 정산, 반복 요청 보상 중복 방지, 버전 비교로 저장 충돌 방지.
- 강화 요청에 현재 revision 필요. 응답 유실 시 sync 후 상태를 보고 새 요청한다.
- 클라이언트는 2초마다 상태를 받아 표시하며 연결 실패 중 보상을 임의 확정하지 않는다.
- 성장 데이터는 클라이언트 직접 읽기·쓰기를 막은 단일 저장 문서로 유지한다.

## 다음 구현 순서
1. 실제 에디터/서버 실행 및 Android 기기 테스트.
2. 이동·타격·피격·처치 연출, 보스 제한시간과 실패 후 반복 파밍, 액티브 스킬.
3. 장비 드롭·장착·승급·퀘스트와 데이터 테이블, 30분 성장 페이스 검증.
4. 계정 연동·복구, 구매 영수증 검증과 지급 원장, 광고 보상 검증, 운영 도구.
5. 재시도/동시접속 통합 테스트, 요청 제한, 부하 테스트, 백업 복구, 모니터링.
6. 출시 심사·개인정보·결제 정책 확인 후 스토어 배포.

개발용 compose에는 공개된 로컬 비밀번호가 있으며 loopback에만 포트를 연다.
운영에는 HTTPS, 별도 비밀정보, 관리 콘솔 접근 제한, DB 백업이 필요하다.
수치 상한은 첫 100스테이지 검증용이다. 장기적인 큰 수 성장 시스템은 별도 수치 표현을 설계한다.

## 생성 아트 방향
이미지는 분위기 참고용으로만 사용하며 기존 캐릭터·UI·배경을 복제하지 않는다.
가제 IdleBlade, 세계관·출시 제목은 미정. PixelLab은 작은 캐릭터 동작 시트,
GPT 이미지 생성은 배경·아이콘·컨셉에 우선 사용한다.
캐릭터 기준 64×64, 발 기준점 통일, idle 4 / run 6 / attack 6 / hit 2 / death 6 프레임.
배경은 16:9 다층 구성, 텍스트 없는 원본으로 제작한다. UI 글자는 엔진에서 표시한다.
캐릭터는 point 필터, 배경은 원본 스타일에 따라 필터를 결정한다.
생성 도구·모델·생성일·프롬프트·사용 조건 확인 내역을 에셋별로 기록한다.

## 버전 근거
- https://unity.com/releases/editor/whats-new/6000.3.25f1
- https://github.com/heroiclabs/nakama/releases/tag/v3.41.0
- https://www.postgresql.org/docs/release/
- https://heroiclabs.com/docs/nakama/getting-started/install/docker/
