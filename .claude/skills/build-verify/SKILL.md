---
name: build-verify
description: "BarcodePrinter 프로젝트 빌드 검증 및 코드 품질 확인 스킬. 코드 변경 후 빌드 확인, XAML-ViewModel 바인딩 정합성 검증, 서비스 Dispose 체인 확인 등을 수행. 구현 완료 후 검증 요청 시, 빌드 에러 발생 시, 코드 리뷰 요청 시 트리거할 것."
---

# 빌드 및 검증 스킬

## 빌드 명령

```bash
dotnet build
```

성공 기준: 경고 0, 오류 0

## 검증 항목

### 1. XAML-ViewModel 바인딩 교차 검증

XAML에서 `{Binding PropertyName}` 사용 시:
- ViewModel에 해당 속성이 [ObservableProperty]로 존재하는지 확인
- Command 바인딩은 [RelayCommand]로 생성된 `XxxCommand` 속성 확인
- Converter 키가 Resources/TouchStyles.xaml에 등록되어 있는지 확인

검증 방법:
```bash
# XAML에서 사용된 바인딩 추출
grep -r "Binding " Views/ --include="*.xaml" | grep -oP "Binding \K\w+"

# ViewModel 속성 확인
grep -E "\[ObservableProperty\]|RelayCommand" ViewModels/
```

### 2. Converter 등록 확인

Converters/Converters.cs에 정의된 클래스가 Resources/TouchStyles.xaml에 모두 등록되어 있는지 확인.

### 3. Dispose 체인

MainViewModel.Dispose()에서 모든 IDisposable 서비스가 Dispose되는지:
- PlcService
- SerialBarcodeReader
- ZebraPrinterService

### 4. JSON 설정 매칭

- appsettings.json 키 ↔ Models/AppConfig.cs 속성
- mappings.json 구조 ↔ Models/BarcodeMapping.cs
- csproj에 CopyToOutputDirectory 설정

### 5. async void 사용 검증

async void는 이벤트 핸들러에서만 허용. 그 외 사용 시 경고.

```bash
grep -rn "async void" --include="*.cs" | grep -v "EventHandler\|_Changed\|OnData\|OnPrint\|ProcessBarcode"
```

## 보고 형식

```
## 빌드 결과: PASS/FAIL
- 오류: N개
- 경고: N개

## 정합성 검증: PASS/FAIL
- [x] XAML 바인딩 일치
- [x] Converter 등록 완료
- [ ] XXX 누락 발견 → 수정 필요

## 수정 필요 사항
1. 파일:줄번호 - 설명
```
