---
name: barcode-printer-orchestrator
description: "BarcodePrinter 프로젝트의 기능 구현 요청을 분석하고, wpf-builder와 qa-verifier 에이전트를 조율하여 구현+검증을 수행하는 오케스트레이터. UI 추가/수정, 서비스 변경, 새 기능 구현 등 복합 작업 요청 시 사용. '하네스 실행', '에이전트로 구현해줘', '팀으로 작업해줘' 요청 시 트리거할 것."
---

# BarcodePrinter 오케스트레이터

## 실행 모드: 서브 에이전트

사용자 요청을 분석하여 적절한 에이전트에 작업을 위임하고, 결과를 검증한 뒤 종합 보고한다.

## 워크플로우

### Phase 1: 요청 분석
1. 사용자 요청에서 변경 범위 파악 (UI / Service / ViewModel / Config)
2. 영향받는 파일 목록 식별
3. 작업 분해

### Phase 2: 구현 (wpf-builder)
```
Agent(
  subagent_type: "general-purpose",
  model: "opus",
  prompt: "wpf-builder 에이전트로서 다음 작업을 수행하라: {작업 내용}. 
           에이전트 정의: .claude/agents/wpf-builder.md 참조.
           관련 스킬: .claude/skills/wpf-ui/SKILL.md, .claude/skills/service-impl/SKILL.md 참조."
)
```

작업 유형별 스킬 매핑:
- UI/XAML 변경 → wpf-ui 스킬
- 서비스/통신 변경 → service-impl 스킬
- 둘 다 필요 → 두 스킬 모두 참조

### Phase 3: 검증 (qa-verifier)
```
Agent(
  subagent_type: "general-purpose",
  model: "opus",
  prompt: "qa-verifier 에이전트로서 변경 사항을 검증하라.
           에이전트 정의: .claude/agents/qa-verifier.md 참조.
           스킬: .claude/skills/build-verify/SKILL.md 참조.
           변경된 파일: {파일 목록}"
)
```

### Phase 4: 결과 종합
- 빌드 성공 여부
- 검증 결과 요약
- 수정 필요 사항 있으면 Phase 2로 복귀 (최대 1회 재시도)

## 데이터 전달

- Phase 2 → Phase 3: 파일 기반 (변경된 소스 파일 자체가 전달물)
- 결과 보고: 사용자에게 직접 텍스트 출력

## 에러 핸들링

| 상황 | 대응 |
|------|------|
| 빌드 실패 | qa-verifier가 원인 분석 → wpf-builder에 수정 재요청 (1회) |
| 정합성 불일치 | 구체적 위치와 수정 방안 보고 후 수정 |
| 재시도 후에도 실패 | 사용자에게 문제 보고, 수동 개입 요청 |

## 테스트 시나리오

### 정상 흐름
1. 사용자: "메인 화면에 로그 영역 추가해줘"
2. 오케스트레이터: 요청 분석 (UI 변경 → wpf-ui 스킬)
3. wpf-builder: MainView.xaml 수정 + MainViewModel에 로그 속성 추가
4. qa-verifier: dotnet build 성공 + 바인딩 정합성 확인
5. 결과: "로그 영역 추가 완료, 빌드 성공"

### 에러 흐름
1. 사용자: "프린터 설정에 용지 크기 옵션 추가해줘"
2. wpf-builder: SettingsDialog + PrinterConfig + AppConfig 수정
3. qa-verifier: 빌드 실패 (PrinterConfig에 새 속성 추가했으나 SettingsDialog에서 미사용)
4. wpf-builder: 누락된 바인딩 추가
5. qa-verifier: 빌드 성공
6. 결과: "용지 크기 옵션 추가 완료 (1회 수정 후 빌드 성공)"
