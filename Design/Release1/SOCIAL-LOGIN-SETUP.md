# 타이틀·로그인과 소셜 인증 연결 상태

## 완료
- 앱 시작 → 타이틀 → 로그인 방법 선택 → 서버 저장 수신 성공 → 로비.
- 게스트 시작/기존 기기 계정 이어하기, 기존 이메일 로그인. 실패 시 로그인 화면 유지 및 재시도.
- 비밀번호 마스킹, 요청 후 입력값 제거. 비밀번호·인증 토큰을 디스크에 저장하지 않음.
- Google/네이버 진입 버튼과 설정 필요 안내.
- Google 토큰을 Nakama의 authenticate/google 또는 link/google에 전달하는 어댑터 구현. 플랫폼 로그인 SDK가 성공한 뒤 CompleteGoogleSignIn을 호출하도록 연결해야 함. link=true는 기존 계정 보존, false는 해당 Google 계정으로 로그인/신규 생성.

## 실제 소셜 로그인 활성화 전 필요한 정보
1. Android 패키지 ID 및 iOS 번들 ID. 기존 임시 식별자를 출시용으로 임의 확정하지 않음.
2. Google 개발자 프로젝트·OAuth 클라이언트, Android 서명 지문, 대상별 로그인 SDK 선택/설정. Nakama 3.41.0의 선택한 Google 로그인 방식에 맞는 서버 설정.
3. 네이버 개발자 앱 Client ID/Secret, 등록된 콜백 주소, Android/iOS 앱 설정.
4. HTTPS 인증 서버 주소. 네이버는 서버가 state 검증·일회성 코드 교환·프로필 고유 ID 조회를 수행한 뒤 Nakama 계정을 연결해야 함. 클라이언트가 보낸 네이버 ID만으로 custom 인증하지 않음.

비밀키는 Unity Resources/PlayerPrefs/소스에 넣지 않고 서버 비밀 설정으로 보관한다. 채팅에 붙여넣지 않는다. SDK 설치/외부 계정 등록/네이버 브리지/딥링크 및 취소·만료 검증은 아직 완료되지 않았다. 따라서 소셜 버튼은 설정 안내만 제공하며 실제 OAuth 브라우저를 열지 않는다.

## 출처
- https://heroiclabs.com/docs/authentication/index.html
- https://heroiclabs.com/docs/nakama/getting-started/configuration/
- https://developers.naver.com/docs/login/devguide/devguide.md

## 검수 경계
C# 컴파일과 기존 서버 계정 연동 검사는 수행 가능. 외부 앱 등록 정보가 없어 Google/네이버 실제 계정 로그인은 검증 불가. 별도 테스트 계정 없이 사용자의 소셜 계정으로 자동 로그인하지 않는다.
