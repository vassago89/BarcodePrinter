---
name: wpf-ui
description: "WPF XAML UI 구현 스킬. 1024x768 터치 모니터용 레이아웃, 버튼, 리스트, 다이얼로그, 스타일, 컨버터, 데이터 바인딩 작업 시 사용. UI 변경, 화면 추가, 스타일 수정, 해상도 조정, 터치 최적화 요청 시 반드시 이 스킬을 트리거할 것."
---

# WPF UI 구현 스킬

## 대상 환경
- 해상도: 1024x768, 터치 모니터
- 프레임워크: .NET 8.0 WPF
- MVVM: CommunityToolkit.Mvvm

## UI 규칙

### 터치 최적화
- 버튼 최소 높이: 42px (주요 액션 52px)
- 폰트: 타이틀 20pt, 본문 17pt, 라벨 14pt, 보조 12pt
- 여백: 카드 내부 Margin 22,16 / 카드 간 Gap 10px
- 모든 인터랙티브 요소에 `Cursor="Hand"`

### 색상 체계
| 용도 | 색상 |
|------|------|
| 배경 | #F0F2F5 |
| 카드 | White |
| 헤더/상태바 | #1E293B |
| Primary (파란) | #2563EB |
| Success (초록) | #16A34A |
| Danger (빨강) | #DC2626 |
| 보라 (설정) | #8B5CF6 |
| 연결됨 | #16A34A |
| 끊김 | #EF4444 |
| 보조 텍스트 | #64748B, #94A3B8 |
| 플레이스홀더 | #CBD5E1 |

### 레이아웃 구조
```
MainView:
├── Header (58px) - 타이틀 + 설정/연결/해제 버튼 + PLC/RDR/PRN 상태
├── Content (*) - 좌(240px 모델리스트) + 우(스캔+QR)
└── StatusBar (44px) - 상태 메시지 + 현재 상태
```

### 바인딩 패턴
- 빈 문자열 플레이스홀더: DataTrigger + Value="" 사용
- 색상 조건부 변경: MultiDataTrigger (IsScanned + IsMatch 조합)
- Visibility 토글: StringToVisibilityConverter
- 연결 상태 색상: BoolToColorConverter

### 스타일 정의 위치
- 공통 버튼 스타일: Resources/TouchStyles.xaml (TouchButton, TouchButtonPrimary 등)
- 컨버터: Converters/Converters.cs → TouchStyles.xaml에 인스턴스 등록
- DropShadowEffect: CardShadow 키

### 다이얼로그 창
- WindowStartupLocation="CenterOwner"
- ShowInTaskbar="False"
- ResizeMode="NoResize"
- SizeToContent="Height" 사용 시 잘림 방지
- Owner는 Application.Current.MainWindow로 설정
