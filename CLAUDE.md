# BarcodePrinter

산업용 WPF 바코드 검증 및 QR 출력 시스템. 1024x768 터치 모니터에서 동작.

## 기능

1. 모델 선택 → 바코드 리더(시리얼 COM)로 부품 2개 스캔 → 매핑 테이블 비교
2. 일치 시 PLC(Modbus TCP)에 OK, 불일치 시 NG 기록
3. PLC IO 신호 수신 시 해당 모델의 QR 코드를 Zebra 라벨 프린터(ZPL/TCP 9100)로 출력
4. 모델 추가/삭제 (리더기로 바코드 스캔 입력 지원)
5. 장치 설정 다이얼로그 (PLC/리더/프린터)

## 기술 스택

- .NET 8.0 WPF (net8.0-windows)
- CommunityToolkit.Mvvm 8.4
- System.IO.Ports (시리얼 통신)
- QRCoder (QR 코드 생성)
- Modbus TCP 자체 구현 (외부 패키지 없음)
- Zebra ZPL 직접 TCP 전송

## 빌드 및 실행

```bash
dotnet build
dotnet run
```

## 프로젝트 구조

```
Models/           AppConfig.cs, BarcodeMapping.cs
Services/         ModbusTcpService, PlcService, SerialBarcodeReader, ZebraPrinterService, MappingService
ViewModels/       MainViewModel (상태 머신: Idle→Scan1→Scan2→OK/NG→Print)
Views/            MainView, ModelEditDialog, SettingsDialog
Converters/       BoolToColor, ResultToColor, StringToVisibility, InverseBool
Resources/        TouchStyles.xaml (버튼 스타일, 컨버터 등록, CardShadow)
appsettings.json  장치 연결 설정 (PLC IP/포트, COM 포트, 프린터 IP)
mappings.json     모델별 바코드 매핑 데이터
```

## 설정 파일

- `appsettings.json` — PLC(Modbus TCP), 시리얼(COM), 프린터(Zebra) 연결 설정
- `mappings.json` — 모델명 ↔ 부품1/부품2 바코드 ↔ QR 데이터 매핑

## 하네스 구성

### 에이전트

| 에이전트 | 파일 | 역할 |
|---------|------|------|
| wpf-builder | `.claude/agents/wpf-builder.md` | UI, ViewModel, Service 코드 구현 |
| qa-verifier | `.claude/agents/qa-verifier.md` | 빌드 및 코드 품질 검증 |

### 스킬

| 스킬 | 경로 | 용도 |
|------|------|------|
| wpf-ui | `.claude/skills/wpf-ui/` | XAML UI, 스타일, 바인딩 구현 |
| service-impl | `.claude/skills/service-impl/` | 서비스/통신 레이어 구현 |
| build-verify | `.claude/skills/build-verify/` | 빌드 및 정합성 검증 |
| barcode-printer-orchestrator | `.claude/skills/barcode-printer-orchestrator/` | 구현+검증 워크플로우 조율 |

### 실행 모드: 서브 에이전트

오케스트레이터가 요청을 분석 → wpf-builder로 구현 → qa-verifier로 검증 → 결과 종합.

## 주의사항

- 모든 UI 요소는 터치 최적화 (최소 42px 높이)
- 백그라운드 스레드 이벤트는 Dispatcher로 UI 스레드 전환 필수
- JSON 파일은 csproj에서 CopyToOutputDirectory=PreserveNewest
