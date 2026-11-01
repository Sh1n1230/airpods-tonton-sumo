# AirPods トントン相撲 PoC

AirPodsを着けてうなずくと，その振動で紙相撲の土俵を叩く同室対戦ゲームの試作です．

## 構成

```
[東 AirPods] --BT--> [東 iPhone] --\
                                    >-- UDP (同一Wi-Fi, 既定ポート 9000) --> [Mac: Unity 土俵]
[西 AirPods] --BT--> [西 iPhone] --/
```

- iPhoneは入力専用です．うなずきを検出し，`{"p":プレイヤー,"seq":連番,"t":時刻,"s":強さ0..1}` のJSONを1回ごとに送ります．
- 土俵の物理と勝敗はMac（Unity）だけが持ちます．
- AirPodsのモーションは常に片方のバッドからしか届かないため（WWDC23 "What's new in Core Motion"），1組を二人で分けることはできません．1人につきAirPodsとiPhoneが1組ずつ要ります．

## ディレクトリ

| パス | 内容 |
|---|---|
| `ios/TontonController/TontonController.swift` | iPhoneアプリ（SwiftUI，1ファイル） |
| `unity/Assets/Tonton/Scripts/` | 受信・土俵・力士・取組進行 |
| `unity/Assets/Tonton/Editor/TontonSceneBuilder.cs` | PoCシーンを自動で組み立てるエディタ拡張 |
| `tools/mock_sender.py` | iPhoneなしで試すための疑似送信機 |

## 動かし方

### 1. Unity（Mac）

1. Unity Hub で新しい3Dプロジェクトを作ります（Built-in / URP どちらでも可）．
2. `unity/Assets/Tonton` をプロジェクトの `Assets/` にコピーします．
3. メニュー **Tonton > Build PoC Scene** を実行すると `Assets/Tonton/Scenes/TontonPoC.unity` ができます．
4. 再生し，**Space** で「はっけよい」，3秒後に取組開始です．
5. iPhoneなしでも **F**（東）と **J**（西）で叩けます．

macOSのファイアウォールが受信を許可するか尋ねてきたら許可してください．

### 2. 疑似送信機で通信を確認

```sh
python3 tools/mock_sender.py            # 東西がランダムに叩く
python3 tools/mock_sender.py --player 1  # 東だけ
```

画面左上の「受信中」表示とトン回数で受信を確認できます．

### 3. iPhone

1. Xcode で新しい iOS App（SwiftUI）を作り，テンプレートの `ContentView.swift` と `〜App.swift` を削除して `TontonController.swift` を追加します．
2. Info.plist に次を追加します．
   - `NSMotionUsageDescription`（ヘッドフォンのモーション取得）
   - `NSLocalNetworkUsageDescription`（Macへの送信）
3. 任意：トン検出時にAirPodsで鳴らす音として `ton.wav` をバンドルに追加します．
4. Macの IP（`ipconfig getifaddr en0`）とポート 9000 を入力して「接続」，プレイヤーを東か西に選び「開始」します．
5. 4秒以内に普段どおり小さく3回うなずくとキャリブレーションが終わり，検出が始まります．

Unityを開く前に Mac で `nc -ul 9000` を実行すれば，iPhoneからの送信だけを確認できます．

## 調整ポイント

| 場所 | 項目 | 意味 |
|---|---|---|
| iOS `NodController` | `threshold` / `fullScale` | キャリブレーションで決まる．小さいうなずきで強さ1に届くよう `fullScale` を低めにしている（酔い対策） |
| iOS `NodController` | `refractory` / `maxTonsPerSecond` | 二重検出の防止と連打の上限 |
| Unity `BoardController` | `heaveImpulse` / `pitchImpulse` / `rollNoise` | 1回のトンが板に与える動き．`rollNoise` は予測できなさ |
| Unity `BoardController` | `*Stiffness` / `*Damping` / `max*` | 板のバネ・ダンパと可動域 |
| Unity `MatchManager` | `ringRadius` / `boutSeconds` | 土俵の半径（1.2m）と1番の時間（30秒） |
| Unity `MatchManager` | `fixedTimestep` | 物理の刻み（0.005秒）．薄い紙を安定させるため細かくしている |

## 既知の制約

- このリポジトリ上ではビルド・実機確認をしていません．Xcode と Unity での確認が必要です．
- ヘッドフォンモーションのサンプリング周期は公開されていないため，実測して検出パラメータを詰める必要があります．
- 勝敗は「力士が55度以上傾いた（倒れ）」か「重心が土俵の円の外に出た（土俵外）」で判定しています．
- 端末間の時刻同期はしていません．トンはMacに届いた時点で反映します．
