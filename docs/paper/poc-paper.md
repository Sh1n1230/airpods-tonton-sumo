# AirPods頭振りトントン相撲：うなずき入力による同室対戦型バーチャル紙相撲のPoC設計

- 版：v0.1（2026-11-02）
- 対象コード：PR #1（`feat/poc-prototype`，コミット `3a0c4b8`）時点のPoC
- 調査メモ：`docs/research/` 以下の4本（紙相撲HCI，イヤラブル頭部ジェスチャ，安全性・評価，競合事例）．本文の主張はこれらに書いた出典に基づく．

## 概要

本稿は，2人のプレイヤーがそれぞれAirPodsを装着し，小さなうなずきで共有の土俵を叩いて対戦する「AirPods頭振りトントン相撲」のPoCについて，先行研究と競合事例を整理し，設計の根拠と検証計画をまとめたものである．入力はiPhone上の `CMHeadphoneMotionManager` から得る角速度で，うなずき1回を1回の「トン」として検出し，UDPでMac上のUnityへ送る．Unity側は土俵を弾性支持された板として揺らし，紙力士はその揺れで受動的に動く．

先行研究からは次の4点が得られた．第1に，2005年のバーチャル紙相撲研究［1］は，力士を直接動かす方式では紙相撲らしい予測不能な動きが失われることを示し，土俵（場）を揺らす間接操作に移行した．本PoCが入力を土俵への衝撃にしたのはこの知見と一致する．同研究では体験者が「相手の力のフィードバック」を求めており，本PoCには未実装である．第2に，AirPodsのモーション更新は約25 Hzとする報告が多いが［16–18］，約100 Hzとする記述もあり［19］，実測が必要である．データは片方のバッドから届く［15］．第3に，イヤラブルの頭部ジェスチャ検出では，閾値約30 deg/sが精度と負担のバランスで最良だった報告があり［24］，誤検出の主な原因は歩行，発話，笑いである［22, 24, 26］．第4に，頸部の安全性について小振幅・短時間のうなずきを直接測った研究は見つからなかった．ヘッドバンギングの理論モデル［29］からは，振幅と頻度の両方を飽和させる設計が妥当と考えられる．

競合調査では，共有画面と各自のスマホで遊ぶWeb版トントン相撲［C4］や，声で土俵を揺らす玩具［C7］はあったが，AirPodsのうなずきで2人が同時に対戦するものは調べた範囲では見つからなかった．

最後に，実装・計測・評価の作業を16件に分け，GitHub issueとして登録した（§9）．

---

## 1. はじめに

### 1.1 背景

トントン相撲（紙相撲）は，紙で作った力士を箱の土俵に立て，2人が土俵の端を指で叩いて，その振動で力士を戦わせる遊びである．プレイヤーは力士を直接操作できない．叩く強さとリズムで土俵の揺れ方が変わり，力士は揺れに応じて予測しにくい動きをする．この「間接性」が遊びの中心にある．

AirPodsはiOS 14以降，`CMHeadphoneMotionManager` によって頭部の姿勢と角速度をアプリに提供している［14］．頭の小さなうなずきを「土俵を叩く」入力にすれば，手を使わずにトントン相撲ができる．手がふさがっていても遊べること，相手の顔を見ながら遊べることが，指で叩く元の遊びとの違いになる．

### 1.2 PoCの範囲

本PoCで決めた範囲は次の通りである．

- 2人が同じ部屋で，1台のMacの画面を共有して対戦する．オンライン対戦は対象外とする．
- 各プレイヤーは自分のiPhoneと自分のAirPodsを使う．AirPodsのモーションは一度に片方のバッドからしか届かないので［15］，1組を2人で分けることはできない．
- 入力は「トン」だけとし，頭の傾きで力士を操縦しない．
- Macは物理演算，勝敗判定，表示を担当し，iPhoneはうなずき検出とイベント送信だけを担当する．

### 1.3 リサーチクエスチョン

PoCで確かめたいことを，次の5つの問いにまとめる．

- RQ1：AirPodsの角速度から，小さなうなずきを低遅延で検出できるか．実効サンプルレート，取りこぼし率，誤検出率はどの程度か．
- RQ2：うなずきから土俵の揺れが見えるまでのエンドツーエンド遅延は，対戦ゲームとして許容できる範囲に収まるか．
- RQ3：うなずき入力の土俵で，紙相撲らしい予測不能さと，押し合いとしての駆け引きが両立するか．
- RQ4：首の負担と画面酔いを，遊びとして成り立つ強度のまま十分に抑えられるか．
- RQ5：同室で相手の顔を見ながら頭で対戦する体験は，手で叩く既存のデジタル紙相撲と比べて何が違うか．

---

## 2. 関連研究

### 2.1 デジタル紙相撲

薮らのバーチャル紙相撲「トントン」［1］は本PoCに最も近い先行研究である．Ver.1では，ジャンピングパッドの傾きと加速度で力士を直接動かした．IVRC 2004予選のアンケート（82件）では楽しさ「満足」が78.0%だったが，力士の操作に「満足」と答えたのは45.1%にとどまり，「力のフィードバックがほしい（台が震えるなど）」という要望が出た．著者らは，力士を思い通りに動かせることは紙相撲の「予測不可能なカオティックな動き」と異なると分析した．Ver.2では水槽の水面を揺らしてスクリーン上の力士を間接的に動かす方式に変え，楽しさ「満足」は86.0%（93件）になった．相手の力の伝わりは波紋で見せた．Ver.2で多かった不満は応答性の悪さだった．同研究は，初期案として机を叩く音を拾う方式を検討したが，熱中して机を強打し負傷するおそれがあるとして見送っている．Ver.2はSIGGRAPH 2005 Emerging Technologiesでも展示された［2］．

Dosukoi-Tap［3, 4］は感圧タッチパッドを土俵に見立て，タップの強さで前進，タップの位置で力士の左右バランスが変わる設計にした．実物の紙相撲には「すぐ倒れる」「押し合わない」という問題があるとして，それを補う方向に設計している．相手方向へのスワイプ（突っ張り）と手前へのスワイプ（払い）を加え，強い技ほど自分も転びやすくした．［1］が予測不能さを重視したのに対し，［3］は押し合いの成立を重視しており，両者の間で調整点を選ぶことが本PoCの設計課題の1つになる（RQ3）．

手以外の入力で土俵を揺らす先例として，声で土俵を振動させるSUMMO SONIC［5］と，世界ゆるスポーツ協会のTON-TON VOICE SUMO［6］がある．後者は高齢者の喉の機能訓練として作られ，年齢や障害の有無を問わない「ダイバーシティスポーツ」として展開された．入力のモダリティを変えても，土俵の振動を介せば遊びの構造は保てることを示している．

### 2.2 紙相撲の物理

紙相撲そのものの力学を直接測った論文は見つからなかった．日本紙相撲協会の徳川式紙相撲［7］では，一辺14 cmの土俵を，約5 cm間隔で接着した消しゴム4個で土台から浮かせている．これは4点で弾性支持された板と見なせる．

力士の前進については，振動台の上の受動的なブリッスルボットの研究が参考になる．Cicconofriら［8］は，鉛直に加振される基板上の受動ロボットが，内部加振型のブリッスルボットと同じ運動方程式に従うことを示した．前進は脚の傾きと，法線力が変動するときの摩擦から生じ，加振周波数によって進行方向が反転する（試作機で約14 Hz）．Vibrot［9］は，鉛直加振された斜め脚の剛体が，加速度の閾値以下では動かず，閾値を超えると速度が上がり，やがて飽和することを示した．1周期の歩幅は浮上時間でほぼ決まり，非周期的な加振でも動く．

UnityのPhysXの摩擦は等方的なので，土俵を揺らすだけでは力士の進行方向が決まらない．本PoCでは［8, 9］の性質を簡易モデルとして自前で加える必要がある（§5.3）．

### 2.3 イヤラブルによる頭部ジェスチャ検出

AppleはWWDC23で，ヘッドホンのモーションは一度に片方のバッドから届き，`SensorLocation` でどちらかを区別できると説明している［15］．自動耳検出が有効なら，ストリーム中のバッドを外すともう一方が引き継ぎ，切断と接続のイベントが発生する．更新レートを設定するAPIはない［14］．実効レートは，論文2本［16, 17］とStack Overflowの実測報告［18］が約25 Hz，AWAREライブラリのREADME［19］が約100 Hzとしており，一致していない．なお，［16, 17］は左右のIMUが融合済みと書いており，これもAppleの説明と食い違う．本稿はAppleの説明を採る．

検出手法では，イヤピースのIMUで頭部ジェスチャを入力に使うHeadText［24］が，ジャイロのエネルギー閾値を10〜50 deg/sで比較し，30 deg/s（約0.52 rad/s）で精度とNASA-TLXの両方が最良だったと報告している．低い閾値では雑音動作が拾われ，雑音81件のうち72件は歩行由来だった．発話と咀嚼は頭部追跡の誤差を増やし［22］，笑いのリズミカルな上下動は頷きと誤認される［26］．Headar［25］は，背景の動きが大きいときに閾値を上げる動的閾値と，状態変化後の0.7秒のクーリング期間を使った．

DTWや機械学習は精度が高いが遅い．Sepanosian［23］のDTW方式は実運用で96%の精度だったが，判定はジェスチャ終了の約0.8秒後だった．同じ研究で，特徴量とランダムフォレストの方式はテストで92%だったのに実運用で破綻した．eSenseのCNNは，既知ユーザで平衡精度80%，未知ユーザで55%だった［20］．ユーザ間の汎化が弱いことは，個人ごとのキャリブレーションの必要性を示している．

一方，本PoCのような1ストロークの短いうなずきを入力として扱った研究は見つからなかった．文献の多くは約2秒かかるうなずき全体を対象にしている［25］．閾値などの数値はそのまま移植せず，実測で調整する必要がある．

### 2.4 ローカルWi-FiのUDP遅延

802.11acの屋内計測では，99.96%のパケットが10 ms以内に往復した一方で，最大227.8 msの往復時間があった［28］．Android端末では，省電力モードのビーコン周期（102.4 ms）によって往復時間が100 ms以上増えうる［27］．iPhoneで同じことが起きるかは確認できていない．いずれにせよ，中央値ではなくp99と最大値が体験を左右する．

### 2.5 安全性と快適性

頸部について最も近い資料は，ヘッドバンギングの頭部・頸部傷害リスクを理論モデルで見積もったPatton & McIntosh［29］である．平均的なテンポ（約146 BPM）では可動域75°超で軽度の頭部傷害が予測され，頸部の傷害リスクは130 BPM付近から上がり始める．可動域約45°ではどのテンポでも閾値を下回った．ただし同論文自身が，周期的な動きの累積効果を評価する確立した基準はないと述べている．中学生のダンスマラソンでは，ヘッドバンギングをした群で1〜3日続く頸部痛が多く報告された［30］．数十度未満の小さなうなずきを20〜30秒繰り返す条件を直接調べた研究は見つからなかった．

画面酔いについて，Palmisanoら［32］は，固定ディスプレイでは頭部の回転に伴う視覚の変化が現実と一致するため，回転による酔いの要因はほぼ生じないと論じている．頭の動きを画面の視点に結びつけると，遅延による姿勢差が生じ，固定画面でも酔いの要因になる．デスクトップVRでも約12分で症状が顕在化した例がある［31］．

### 2.6 エクサーゲームと同室プレイ

Muellerらは，身体を使う遠隔ゲームで遊んだペアのほうが，キーボードで遊んだペアより相手をよく知り楽しかったと答えたことを報告した［10］．同じ部屋の相手と遊ぶと，コンピュータや遠隔の相手より楽しさ・挑戦・有能感が高くなる［39］．設計指針としては，短いゲームサイクルで疲労を避けること［35］，身体能力の差を測定・調整・提示の段階で吸収すること［36］，身体的リスクは分別をもって扱うこと［34, 35］が挙げられている．

触覚や振動による演出は，既に他の手段で伝えている情報を補強するだけでも体験を上げうる［12］．ただし好みは分かれ，Bubble Jumper［13］では振動フィードバックを標準構成から外して選択肢にした．

---

## 3. 競合・参考事例

調べた範囲を表1にまとめる．詳細と出典URLは `docs/research/competitors.md` にある．並びはカテゴリ順で，優劣を表すものではない．

**表1　主な競合・参考事例**

| ID | 名称 | 入力 | 人数・表示 | 本PoCとの主な違い |
|---|---|---|---|---|
| C1 | どすこい！バトル（iOS） | タップ連打 | 1〜2人，1台を共有 | 手の入力，1台を挟んで遊ぶ |
| C2 | トントン紙相撲（took.jp） | タップ／キー，着地タイミング判定 | 1人（CPU） | 対人戦なし |
| C3 | トントン相撲（ふりーむ！） | 叩く・連打・払う | 1〜2人，同じ画面 | 3種類の手入力，文脈で技が変化 |
| C4 | Tonton Sumo（JOIN//PLAY） | スマホ画面の連打 | 多人数，共有画面＋各自のスマホ | 構成はほぼ同じで入力がタップ |
| C5 | アルティメットントン相撲（OECU） | タブレットのタップ・長押し・連打 | 2人 | 手の入力 |
| C6 | SUMOU BATTLE ALENA（Steam） | Z／Xキー | 1人 | 物理エンジンで形・質量が効く |
| C7 | さけべ！トントンボイス相撲（玩具） | 声 | 1〜4人，実物の土俵 | 手以外の入力の先例 |
| C8 | ポケモン トントンバトル！（玩具） | 指で叩く | 実物の土俵 | デジタル要素なし |
| C9 | RidePods（iOS） | 頭の左右の傾き（連続値） | 1人 | 単独，連続値の操舵 |
| C10 | Head Games（iOS ほか） | 傾き・うなずきの保持 | 1人 | 単独，UI操作中心 |
| C11 | TurtleUp（macOS, OSS） | 頭部姿勢 | 1人 | 姿勢改善が主目的，急な頭振りを弾く |
| C12 | AirTracker（macOS, OSS） | 頭の向き→UDP | 1人 | Macで直接読む |
| C13 | 1-2-Switch／エブリバディ 1-2-Switch!（Hip Bump） | Joy-Con | 1〜8人，TV | 手に持つ．相手を見る遊びの先例 |
| C14 | Just Dance＋Controllerアプリ | スマホのモーション | 最大6人，TV | スマホを中継に使う大規模な先例 |
| C15 | Heads Up! | 額のスマホを上下に傾ける | グループ，交代制 | うなずき入力の受容例 |
| C16 | Kishimen（OSS） | AirPods→UDP | 1台 | iPhone→UDP→Unityという同じデータの流れ |
| C17 | HeadphoneMotion Unity Plugin（OSS） | AirPodsのQuaternion | 1台 | 描画がiOS側 |
| C18 | Zennのうなずき検出記事 | pitch／yawの差分＋閾値 | 1人 | 閾値0.04 rad／フレーム，クールダウン0.5秒 |

表1から言えることは3点ある．第1に，「AirPodsのうなずきで2人が同時に対戦する」ゲームは調べた範囲では見つからなかった．これは存在しないことを意味しない．第2に，構成が最も近いのはC4とC16で，前者は共有画面と個人デバイスの構成，後者はiPhoneからUnityへのデータの流れが同じである．したがって本PoCの新しさは，構成ではなく入力（頭のうなずき）と，それに伴う体験（相手の顔を見ながら頭で叩き合う）にある．第3に，運用面の課題も見えた．C9のレビューでは中立姿勢のキャリブレーションの要望が多く，C14のレビューではスマホとホストの接続不良が評価を大きく下げている．C12は，AirPodsがMacのアクティブな音声出力である間しかモーションを出さないと注意している．これは1台のMacで2組を読むのが難しいことを示唆しており，各プレイヤーにiPhoneを1台ずつ割り当てた本PoCの構成の理由になる．

C13のレビューでは，現実の遊びを置き換えただけに見えるミニゲームが批判されていた．本PoCも「指で叩けば済むことを頭でやる意味」を問われうる．RQ5はこの問いに答えるためのものである．

---

## 4. システム構成（現状）

### 4.1 全体

```text
[P1 AirPods] --Bluetooth--> [P1 iPhone] --\
                                             UDP / 同一Wi-Fi --> [Mac Unity]
[P2 AirPods] --Bluetooth--> [P2 iPhone] --/
```

UDPのペイロードは `{"p":1,"seq":1,"t":123.45,"s":0.8}` で，`p` はプレイヤー番号，`seq` は連番，`t` は送信側の時刻，`s` は0〜1の強さである．既定のポートは9000とした．

### 4.2 iPhone側の検出器

PR #1時点の検出器は次の通りである．

1. 4秒のキャリブレーション中に3回うなずいてもらい，`rotationRate` の3軸のうち絶対値が最大になった軸と符号を反応軸に選ぶ．ピークが0.8 rad/sに届かなければやり直す．
2. 閾値を `max(0.8, ピーク×0.35)` rad/s，強さ1に相当する角速度を `max(閾値×1.5, ピーク×0.7)` とする．閾値と強さの尺度は個人ごとに決まる．
3. 反応軸の値が閾値を超えたらピークの追跡を始め，閾値の半分まで戻ったところで1回の「トン」として確定する．
4. 前回のトンから150 ms以内は検出しない．1秒間に6回を超えるトンは送らない．
5. `sensorLocation` が変わったらキャリブレーションをやり直す．

### 4.3 Mac側の土俵と力士

土俵はキネマティックなRigidbodyで，上下動（heave），pitch，rollの3自由度を，それぞれ独立したばね・ダンパとして半陰的オイラー法で積分する．ばね定数900，減衰18なので，固有角振動数は約30 rad/s（約4.8 Hz），減衰比は約0.3になる．トンが入ると，叩いた側が下がる向きにpitchの角速度を，下向きにheaveの速度を加え，rollには乱数の角速度を加える．変位にはheave 0.05 m，傾き4°の上限がある．

力士は複数のBoxColliderで作った紙の形の剛体で，摩擦係数は0.7である．傾きが55°を超えるか，半径1.2 mの土俵の外に出ると負ける．取組は30秒で，物理の固定タイムステップは5 msとした．

### 4.4 現状と先行研究の差

先行研究と照らして，現状の実装について次のことが言える．

- 入力を土俵への衝撃にしたことは［1, 2］と一致する．
- 土俵のばね・ダンパ支持は［7］の弾性支持と同じ考え方である．ただし4点支持ではなく3自由度の独立したばねで近似している．
- 力士の前進はPhysXの等方的な摩擦に任せており，［8, 9］の異方的な摩擦と浮上の効果を含まない．
- 相手のトンを知覚する手段は，土俵の揺れが画面に見えることだけで，［1］の要望や［12］の知見に照らすと弱い．
- 閾値の下限0.8 rad/sは，［24］の最良値0.52 rad/sより高い．ただし［24］の動作は本PoCのトンより大きい可能性がある．
- 連打上限の6回/秒は，［29］が頸部リスクの上昇を示した約130 BPM（約2.2回/秒）を上回る．ただし［29］のBPMは曲の拍で，うなずき1往復の頻度と同一とは限らない．
- 実効サンプルレート，遅延，取りこぼし率，誤検出率は，いずれも未計測である．
- Swift／C#のコードは，sandboxにXcodeとUnityがないため未コンパイルで，実機でも確認していない．`mock_sender.py` によるUDP送信だけをローカルで確認した．

---

## 5. 設計方針と改訂案

### 5.1 検出器

検出器は，即応性を優先した閾値方式を主とする．DTWや分類器は遅く［23］，トンの入力そのものには使わない．改訂案は次の通りである．

1. パラメータを時間で書く．更新レートは指定できず［14］，機種で変わる可能性があるため，不応期や窓長をサンプル数ではなく `timestamp` の差で扱う．現状のコードは既にそうなっている．
2. 閾値の下限を掃引する．0.5〜1.0 rad/sの範囲で，取りこぼし率，誤検出率，主観的な楽さを比べる［24］．閾値は個人ごとに決める現状の方式を保つ［20, 37］．
3. 動的閾値を入れる．直近1〜2秒の角速度のRMSが高いとき（会話，笑い，歩行）は閾値を上げる［25］．
4. 状態変化の後にマスクを入れる．キャリブレーション直後，`sensorLocation` の切り替え，再接続の後0.5〜0.7秒は検出しない［25］．これは150 msの不応期とは別のものである．
5. 笑いを抑える．上下両方向のピークが短い間隔で続く往復パターンは，笑いの可能性として扱う［26］．
6. 後段フィルタは将来の選択肢とする．検出の前後0.5〜1秒をDTWで再判定し，雑音らしければ取り消す方式［23, 24］は，ゲーム側で入力の取り消しを扱う必要があるため，PoCの後半で検討する．

### 5.2 入力から衝撃への写像

うなずきの強さから土俵への衝撃までは，「不感帯→線形→飽和」の形にする［9］．弱い動きでは何も起きず，強すぎる動きも一定以上は効かない．これは公平性（首を大きく振れる人が有利にならない）と安全性（大振りを誘わない）の両方に効く［29, 36］．現状の検出器の閾値が不感帯，強さの上限1が飽和に当たるので，形はすでにできている．PoCではこの写像をUnity側でも調整できるようにし，強さを数段階に量子化する案［10, 36］とも比べる．

### 5.3 土俵と力士の物理

土俵は［7］に合わせて4隅を弾性支持された板として扱う案を，現状の3自由度モデルと比べる．力士の前進には，土俵の鉛直加速度に応じた異方的な水平力を自前で加える簡易モデルを入れる［8］．加速度が閾値以下なら動かず，上がると速くなり，やがて飽和するようにする［9］．浮上は消さずに残す．

［1］の予測不能さと［3］の押し合いは，力士の重心の高さ，土俵中央へのわずかな傾き，力士の質量などのパラメータで調整する．重い力士は動き出しやすく遅い，という［9］の性質は，力士ごとの違いとして使える．

### 5.4 相手の力の知覚

［1］の体験者がいちばん求めたのは，相手の力の手応えだった．本PoCでは次の3段階で応える．

1. 画面：トンの打点に波紋を出す［1, 11］．
2. 耳：相手のトンを自分のAirPodsでも鳴らす．Macから各iPhoneへ相手のトンを送り返し，相手側から聞こえるように空間的に配置する．AirPodsには触覚を出す機構はないと考えられるので，耳で伝えるのが第一候補になる（AirPodsに触覚アクチュエータがないことは公式資料で未確認）．
3. 手：iPhoneの触覚（Core Haptics）は任意のおまけとし，ON／OFFを選べるようにする［12, 13］．

### 5.5 遅延と公平性

［1］のVer.2で最大の不満は応答性だった．本PoCは，AirPods→iPhone→Wi-Fi→Macの各段で遅延が積み重なるので，段ごとに測る．iPhoneの無線が省電力で眠ると，間を空けた後の最初のトンだけが遅れる可能性がある［27］．20〜50 ms間隔のハートビートを送り，無線を起こしておく案を試す．2人の到着遅延に差があると，ほぼ同時のトンの順序が不公平になる．送信側の時刻を時計合わせしたうえで使うか，一定の受付窓を設けることを検討する［28］．

### 5.6 安全と快適性

先行研究から，次の設計上の制約を置く．数値は確立した基準ではなく，文献からの外挿である．

- 判定は小さな振幅で成立させ，大振りを得点上有利にしない［29］．
- 連打の上限を設ける．現状の6回/秒と，2回/秒，4回/秒を比べ，首の負担の主観評価とあわせて決める［29］．
- 頭の動きを画面の視点やカメラに結びつけず，画面全体を揺らす演出も入れない［31, 32］．
- 取組は20〜30秒とし，連続プレイの合計は10分程度までを目安に，取組の間に休憩を挟む［31, 35］．
- 着座でプレイする［32］．
- 練習取組を1〜2本入れてから本番を測る［38］．
- 取組ごとに座席（東西）を入れ替え，正規化前後の強さと勝敗をログに残す［36］．

---

## 6. 評価計画

### 6.1 段階

評価は3段階で行う．

1. 技術計測（開発者本人）：センサ，検出器，遅延を測る（§6.2）．
2. パラメータ調整（開発者と数名）：閾値，連打上限，物理パラメータを掃引する．
3. 小規模ユーザテスト（ペア数組）：体験，負担，酔いを測る（§6.3）．実施前に§7の倫理確認を済ませる．

### 6.2 技術計測の指標

**表2　技術計測の指標**

| 区分 | 指標 | 対応RQ | 根拠 |
|---|---|---|---|
| センサ | 実効サンプルレート，`timestamp` 差の中央値・p95・最大，`sensorLocation` の切り替え回数，切断回数（機種別） | RQ1 | ［15–19］ |
| 検出精度 | イベント単位の適合率・再現率・F1（正解との許容窓を明記），ユーザ別の比較 | RQ1 | ［20, 21］ |
| 誤検出 | 条件別の誤検出数/分（静止，会話，笑い，咀嚼，歩行），閾値ごとの曲線 | RQ1 | ［22, 24–26］ |
| 連打性能 | 2・4・6回/秒の目標リズムでの取りこぼし率，1分連打での変化 | RQ1, RQ4 | ［24］ |
| 遅延 | うなずき開始→iPhone検出（高速度撮影で外部計測），iPhone送信→Unity受信（p50／p95／p99／最大），間を空けた後の初回と連打中の比較 | RQ2 | ［23, 27, 28］ |
| ネットワーク | UDPの損失率，順序の入れ替わり，2台の到着時刻差 | RQ2 | ［28］ |

遅延の外部計測は，240 fpsのスロー動画で頭とUnityの画面を同時に撮り，フレームを数える方法が手軽である．

### 6.3 ユーザテストの尺度

- 酔い：SSQ［33］をプレイの前後に取る．取組の間は「気分の悪さ」1項目の口頭確認で代える．
- 負担：NASA-TLX［41］を条件ごとに取る．身体的要求とフラストレーションを下位尺度として報告する．一対比較の負担が大きければ重みなし（Raw TLX）を使う．
- 動機づけ：IMI［43］から興味・楽しさ，有能感，緊張を選ぶ．
- 使いやすさ：SUS［42］で，装着，キャリブレーション，判定の分かりやすさを測る．
- ゲーム体験：GEQは正式に公刊されておらず因子構造も支持されなかったので［40］，補助的に使い，使った版と項目を明記する．1因子2項目まで削ると信頼性が下がりうるので［37］，抜粋するなら項目数を確保してαを報告する．
- 社会的指標：相手を知った感じなど［10, 39］．
- 行動指標：自由時間にもう1本やりたいと言ったか．

RQ5のために，同じペアに「指で叩く」条件（F/Jキーまたは画面タップ）と「うなずき」条件の両方を遊んでもらう案がある．比較の設計は，ユーザテストの前に別途決める．

---

## 7. 倫理

本PoCのユーザテストが，電気通信大学の「人を対象とする研究に関する倫理規程」［44］で審査対象になるのか，学生実験としての届出で済むのか，「非該当」になるのかは，本稿では判断していない．実施前に指導教員と担当部署へ確認する．

規程の説明事項に沿って，説明文書には少なくとも次を書く．頸部に痛みや既往がある人，乗り物酔いしやすい人，当日体調が悪い人は参加を控えること．いつでも中止できること．頸部の痛み，めまい，気分の悪さの申告があれば即中止すること．記録するデータ（頭部モーションのログ，アンケート，撮るなら映像）と，その保管と廃棄の方法．同級生を募集する場合は，断っても不利益がないことを明記する．AirPodsを共用するなら，イヤーチップの交換と清拭を手順に入れる．翌日に頸部の痛みの有無を簡単に確認すると，安全側に倒せる［30］．

---

## 8. 制約と未確認事項

- 本稿の多くの数値は，本PoCとは異なる条件の研究からの外挿である．特に頸部の安全性の閾値［29］と閾値の初期値［24］は，そのまま使わず実測で確かめる．
- AirPodsの実効サンプルレート，左右のバッドの扱い，Bluetooth区間の遅延，iPhoneのWi-Fi省電力による遅延は，一次情報で確認できていない．
- AirPodsに触覚アクチュエータがないことは，公式資料で確認していない．
- 抄録だけを読んだ文献［2, 11, 12, 21, 30, 38, 39］がある．
- 競合調査は検索で見つかった範囲に限られる．検索の抜粋しか見ていない事例は `docs/research/competitors.md` の「未検証」にまとめた．
- コードは未コンパイルで，実機での確認はしていない．

---

## 9. 作業計画（issue）

§4〜§7から作業を16件に分け，GitHub issue #3〜#18 として登録した．

**表3　issue一覧**

| # | 区分 | 内容 | 本稿の節 |
|---|---|---|---|
| #3 | 計測 | センサ計測ログ（実効レート，バッド切り替え，切断） | §4.4, §6.2 |
| #4 | iOS | 接続・認可の状態をUnityへ送り，試合を一時停止 | §5.5 |
| #5 | iOS | 状態変化後の検出マスク | §5.1 |
| #6 | iOS | 動的閾値と閾値下限の掃引 | §5.1 |
| #7 | iOS | 笑い・会話による誤検出の抑制 | §5.1 |
| #8 | 通信 | ハートビートと遅延計測 | §5.5, §6.2 |
| #9 | 計測 | エンドツーエンド遅延の外部計測手順 | §6.2 |
| #10 | 通信 | 同時入力の公平性（送信時刻と受付窓） | §5.5 |
| #11 | Unity | 強さ→衝撃の写像（不感帯・線形・飽和）の調整 | §5.2 |
| #12 | Unity | 土俵の4点弾性支持モデルの比較 | §5.3 |
| #13 | Unity | 力士の前進モデル（異方的摩擦と浮上） | §5.3 |
| #14 | 体験 | 相手のトンを画面の波紋と自分のAirPodsで知覚させる | §5.4 |
| #15 | 安全 | 連打上限・連続プレイ時間・休憩の安全装置 | §5.6 |
| #16 | 公平 | 座席入れ替えと公平性ログ | §5.6 |
| #17 | 評価 | ユーザテスト計画書と倫理確認 | §6.3, §7 |
| #18 | iOS | DTWによる後段フィルタの検討（後半） | §5.1 |

---

## 参考文献

### デジタル紙相撲・紙相撲の物理・卓上の遊び

1. 薮 博史，鎌田 洋輔，高橋 誠史，河原塚 有希彦，宮田 一乘．変位情報を用いたVRアプリケーションの実装 -バーチャル紙相撲"トントン"-．芸術科学会論文誌，Vol.4，No.2，pp.36–46，2005．doi:10.3756/artsci.4.36．https://www.jstage.jst.go.jp/article/artsci/4/2/4_2_36/_pdf/-char/ja
2. H. Yabu, Y. Kamada, M. Takahashi, Y. Kawarazuka, K. Miyata. Ton²: a VR application with novel interaction method using displacement data. ACM SIGGRAPH 2005 Emerging Technologies, p.24, 2005. doi:10.1145/1187297.1187322（抄録のみ確認）
3. Y. Nakagawa, K. Tsukamoto, Y. Kono. Dosukoi-Tap: The Virtual Paper Sumo Game. ACE 2013, LNCS, Springer, 2013. doi:10.1007/978-3-319-03161-3_62．http://www.hcilab.jp/~kono/Paper/2013/ACE2013/Dosukoi-Tap.pdf
4. Y. Nakagawa, K. Tsukamoto, S. Tanaka, M. Suzuki. Virtual Paper Sumo Game. UIST 2012 ポスター（会議名はファイルパスからの推定）．http://www.hcilab.jp/~kono/Paper/2012/UIST2012/UIST2012_Poster.pdf
5. 鈴木 浩．SUMMO SONIC：音声による振動とAR演出を利用した紙相撲システムの提案．情報処理学会研究報告，Vol.2017-GN-100，No.20，pp.1–5，2017．http://altmetrics.ceek.jp/article/id.nii.ac.jp/1001/00176834/
6. TON-TON VOICE SUMO Production Team. TON-TON VOICE SUMO. 第23回文化庁メディア芸術祭 エンターテインメント部門 新人賞，2020．https://www.bunka.go.jp/j-mediaarts-festival/en/award/single/ton-ton-voice-sumo/index-2.html
7. 朝西 知徳．卓上スポーツ・徳川式「紙相撲」の実践と普及．羽衣国際大学現代社会学部研究紀要，第11号，pp.71–80，2022．doi:10.51025/00000018．https://hagoromo.repo.nii.ac.jp/record/26/files/kiyou_g11-71.pdf
8. G. Cicconofri, F. Becker, G. Noselli, A. DeSimone, K. Zimmermann. The inversion of motion of bristle bots: analytical and experimental analysis. ROMANSY 21, Springer, pp.225–232, 2016. doi:10.1007/978-3-319-33714-2_25．https://arxiv.org/abs/1702.00343
9. E. Altshuler, J. M. Pastor, A. Garcimartín, I. Zuriguel, D. Maza. Vibrot, a Simple Device for the Conversion of Vibration into Rotation Mediated by Friction: Preliminary Evaluation. PLoS ONE 8(8): e67838, 2013. doi:10.1371/journal.pone.0067838
10. F. Mueller, S. Agamanolis, R. Picard. Exertion Interfaces: Sports over a Distance for Social Bonding and Fun. CHI 2003. doi:10.1145/642611.642709
11. H. Ishii, C. Wisneski, J. Orbanes, B. Chun, J. Paradiso. PingPongPlus: design of an athletic-tangible interface for computer-supported cooperative play. CHI '99, pp.394–401, 1999. doi:10.1145/302979.303115（抄録のみ確認）
12. T. Singhal, O. Schneider. Juicy Haptic Design: Vibrotactile Embellishments Can Improve Player Experience in Games. CHI 2021. doi:10.1145/3411764.3445463（抄録のみ確認）
13. R. Ando, A. Ando, K. Kunze, K. Minamizawa. Bubble Jumper: Enhancing the Traditional Japanese Sport Sumo with Physical Augmentation. SHS '18, 2018. doi:10.1145/3210299.3210301

### イヤラブル・AirPods・ネットワーク

14. Apple. CMHeadphoneMotionManager／CMHeadphoneMotionManagerDelegate. Apple Developer Documentation. https://developer.apple.com/documentation/coremotion/cmheadphonemotionmanager
15. Apple. What's new in Core Motion. WWDC23 Session 10179. https://developer.apple.com/videos/play/wwdc2023/10179/
16. S. Lyu, Y. Chen, D. Duan, R. Jia, W. Xu. EarDA: Towards Accurate and Data-Efficient Earable Activity Sensing. arXiv:2406.16943, 2024.
17. V. Mollyn, R. Arakawa, M. Goel, C. Harrison, K. Ahuja. IMUPoser: Full-Body Pose Estimation using IMUs in Phones, Watches, and Earbuds. CHI '23. doi:10.1145/3544548.3581392
18. Stack Overflow. What frequency standard does CMHeadphoneMotionManager's startDeviceMotionUpdates() has? 2022. https://stackoverflow.com/questions/71612270
19. AWARE Framework. com.awareframework.ios.sensor.airpods README. https://github.com/awareframework/com.awareframework.ios.sensor.airpods
20. M. Laporte, P. Baglat, S. Gashi, M. Gjoreski, S. Santini, M. Langheinrich. Detecting Verbal and Non-Verbal Gestures Using Earables. UbiComp/ISWC '21 Adjunct, pp.165–170. doi:10.1145/3460418.3479322
21. S. Gashi, A. Saeed, A. Vicini, E. Di Lascio, S. Santini. Hierarchical Classification and Transfer Learning to Recognize Head Gestures and Facial Expressions Using Earbuds. ICMI '21, pp.168–176. doi:10.1145/3462244.3479921（抄録のみ確認）
22. A. Ferlini, A. Montanari, C. Mascolo, R. Harle. Head Motion Tracking Through in-Ear Wearables. EarComp '19, pp.8–13. doi:10.1145/3345615.3361131
23. T. Sepanosian. Real-Time Recognition of Boxing Head Gestures with IMU-Earables: Machine Learning and Dynamic Time Warping. TScIT 41, University of Twente, 2024. https://essay.utwente.nl/fileshare/file/100840/Sepanosian_BA_EEMCS.pdf
24. S. Xu, G. Wang, Z. Fang, G. Zhang, G. Shang, R. Lu, L. He. HeadText: Exploring Hands-free Text Entry using Head Gestures by Motion Sensing on a Smart Earpiece. arXiv:2205.09978, 2022.
25. X. Yang, X. Wang, G. Dong, Z. Yan, M. Srivastava, E. Hayashi, Y. Zhang. Headar: Sensing Head Gestures for Confirmation Dialogs on Smartwatches with Wearable Millimeter-Wave Radar. Proc. ACM IMWUT, 2023. doi:10.1145/3610900
26. A. Kapoor, R. W. Picard. A Real-Time Head Nod and Shake Detector. PUI 2001. MIT Media Lab TR No.544. https://hd.media.mit.edu/tech-reports/TR-544.pdf
27. W. Li, D. Wu, R. K. C. Chang, R. K. P. Mok. Demystifying and Puncturing the Inflated Delay in Smartphone-based WiFi Network Measurement. CoNEXT '16. https://daoyuan14.github.io/papers/CoNEXT16_WiFiMeasure.pdf
28. H. Zhang, A. Elmokashfi, P. Mohapatra. WiFi and Multiple Interfaces: Adequate for Virtual Reality? IEEE ICPADS 2018（会議名はPDFのファイル名からの推定）．https://pmlab.cse.usf.edu/assets/pubs/zhang-icpads18.pdf

### 安全性・設計指針・評価・倫理

29. D. Patton, A. McIntosh. Head and neck injury risks in heavy metal: head bangers stuck between rock and a hard bass. BMJ 337:a2825, 2008. doi:10.1136/bmj.a2825
30. M. R. Kassirer, N. Manon. Head banger's whiplash. Clinical Journal of Pain 9(2):138–141, 1993. doi:10.1097/00002508-199306000-00010（抄録のみ確認）
31. N. G. Vinson, J.-F. Lapointe, A. Parush, S. Roberts. Cybersickness induced by desktop virtual reality. Graphics Interface 2012, pp.69–75.
32. S. Palmisano, R. S. Allison, J. Kim. Cybersickness in head-mounted displays is caused by differences in the user's virtual and physical head pose. Frontiers in Virtual Reality 1:587698, 2020. doi:10.3389/frvir.2020.587698
33. R. S. Kennedy, N. E. Lane, K. S. Berbaum, M. G. Lilienthal. Simulator Sickness Questionnaire: An enhanced method for quantifying simulator sickness. The International Journal of Aviation Psychology 3(3):203–220, 1993. doi:10.1207/s15327108ijap0303_3
34. F. Mueller, D. Edge, F. Vetere, M. R. Gibbs, S. Agamanolis, B. Bongers, J. G. Sheridan. Designing sports: A framework for exertion games. CHI 2011, pp.2651–2660. doi:10.1145/1978942.1979330
35. F. Mueller, K. Isbister. Movement-based game guidelines. CHI 2014, pp.2191–2200. doi:10.1145/2556288.2557163
36. F. Mueller, F. Vetere, M. Gibbs, D. Edge, S. Agamanolis, J. Sheridan, J. Heer. Balancing exertion experiences. CHI 2012, pp.1853–1862. doi:10.1145/2207676.2208322
37. A. Taheri, Z. Weissman, M. Sra. Design and evaluation of a hands-free video game controller for individuals with motor impairments. Frontiers in Computer Science 3:751455, 2021. doi:10.3389/fcomp.2021.751455
38. R. C. Fouché. Head mouse: Generalisability of research focused on the disabled to able bodied users. SAICSIT '17, Article 14, 2017. doi:10.1145/3129416.3129442（抄録のみ確認）
39. B. J. Gajadhar, Y. A. W. de Kort, W. A. IJsselsteijn. Shared fun is doubled fun: Player enjoyment as a function of social setting. Fun and Games 2008, LNCS 5294, pp.106–117. doi:10.1007/978-3-540-88322-7_11（抄録のみ確認）
40. E. L.-C. Law, F. Brühlmann, E. D. Mekler. Systematic review and validation of the Game Experience Questionnaire (GEQ). CHI PLAY 2018, pp.271–283. doi:10.1145/3242671.3242683
41. S. G. Hart, L. E. Staveland. Development of NASA-TLX (Task Load Index): Results of empirical and theoretical research. Human Mental Workload, 1988. doi:10.1016/S0166-4115(08)62386-9．手順書：https://www.nasa.gov/wp-content/uploads/2026/03/nasa-tlx-v1-0-searchable-text-and-forms.pdf
42. A. Bangor, P. Kortum, J. Miller. Determining what individual SUS scores mean: Adding an adjective rating scale. Journal of Usability Studies 4(3):114–123, 2009.
43. Center for Self-Determination Theory. Intrinsic Motivation Inventory (IMI). https://selfdeterminationtheory.org/intrinsic-motivation-inventory
44. 国立大学法人電気通信大学．電気通信大学における人を対象とする研究に関する倫理規程（最終改正 令和7年3月31日）．https://www.uec.ac.jp/about/basicinfo/rule/pdf/99998B013.pdf
