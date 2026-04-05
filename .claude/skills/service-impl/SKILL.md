---
name: service-impl
description: "PLC Modbus TCP 통신, 시리얼 바코드 리더, Zebra ZPL 프린터, JSON 매핑 서비스 등 BarcodePrinter의 서비스 레이어 구현 스킬. 장치 통신 추가/수정, 프로토콜 변경, 데이터 모델 변경, PLC 레지스터 변경, 프린터 포맷 변경 요청 시 반드시 트리거할 것."
---

# 서비스/통신 구현 스킬

## 서비스 구조

### ModbusTcpService (Services/ModbusTcpService.cs)
- System.Net.Sockets 기반 자체 구현 (외부 패키지 없음)
- SemaphoreSlim으로 동시 요청 직렬화
- ReadExactAsync로 정확한 바이트 수 수신 보장
- FC 03: Read Holding Registers / FC 06: Write Single Register
- MBAP 헤더: TransactionId(2) + ProtocolId(2,0x0000) + Length(2) + UnitId(1)

### PlcService (Services/PlcService.cs)
- ModbusTcpService 래핑, 애플리케이션 레벨 추상화
- 레지스터: ResultRegister(PC→PLC, 0=Idle/1=OK/2=NG), PrintTriggerRegister(PLC→PC, 1=Print), PrintCompleteRegister(PC→PLC)
- 폴링 루프: Task.Run + CancellationToken, 자동 재연결
- 이벤트: PrintTriggered, ConnectionChanged, ErrorOccurred

### SerialBarcodeReader (Services/SerialBarcodeReader.cs)
- System.IO.Ports.SerialPort 사용
- DataReceived 이벤트 → 버퍼 누적 → CR/LF/CRLF 구분 → BarcodeReceived 이벤트
- 설정: PortName, BaudRate, DataBits, Parity, StopBits (SerialConfig)

### ZebraPrinterService (Services/ZebraPrinterService.cs)
- TCP 소켓으로 ZPL 명령 직접 전송 (포트 9100)
- QR 코드: ^BQN,2,{mag} + ^FDMA,{data}
- 라벨 텍스트: ^A0N,30,30 + ^FD{text}

### MappingService (Services/MappingService.cs)
- mappings.json 파일 기반 CRUD
- FindMatch(bc1, bc2) → 양방향 매칭
- GetByModel(name) / GetAllModelNames() / Add() / Remove()
- System.Text.Json, WriteIndented=true, PropertyNameCaseInsensitive=true

## 구현 원칙

1. **IDisposable 체인**: TcpClient, NetworkStream, SerialPort, SemaphoreSlim 모두 Dispose
2. **스레드 안전**: 이벤트 핸들러에서 UI 접근 시 반드시 Dispatcher 경유
3. **재연결**: PLC 폴링 루프에서 통신 실패 시 3초 대기 후 자동 재연결
4. **설정 외부화**: 모든 IP, 포트, 레지스터 주소는 AppConfig → appsettings.json
5. **async/await**: 네트워크 IO는 async, 시리얼은 이벤트 기반
