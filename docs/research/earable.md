# イヤラブルIMUによる頭部ジェスチャ認識とAirPodsモーション：調査レポート

対象PoC：2人のプレイヤーがそれぞれAirPodsを装着し，iPhone上で `CMHeadphoneMotionManager` の `rotationRate` から小さな頷き（トン）を検出する（4秒キャリブレーション，閾値，150 ms不応期，最大6回/秒）．検出イベントはUDP（JSON）でMacのUnityホストへ送る．

調査日：2026-11-02（JST）．本文で引用した情報はすべて実際に開いたページ・PDFから取った．開けたのが要旨だけの文献はその旨を書いた．【未検証】は一次情報で確認できていない値，【推論】は文献から筆者が導いた設計上の判断を示す．

---

## 0. 要点（先に結論）

1. **AirPodsの更新レートは約25 Hzとする報告が多い**（EarDA，IMUPoser，Stack Overflowの実測報告）．ただし「約100 Hz」と書くOSSのREADMEもあり，Appleは公式に数値を出していない．PoCではまず `CMDeviceMotion.timestamp` の差分から実効レートとジッタを実測すべきである．25 Hzなら1サンプル40 msなので，150 ms不応期は約3.75サンプルに当たる．
2. **データは「片耳ずつ」届く**．Appleの公式説明（WWDC23）では，各サンプルは左右どちらか一方のバッドから来て，`SensorLocation` で区別する．自動耳検出が有効だと，ストリーム中のバッドを外せばもう一方が引き継ぐ．一方で「左右のIMUは融合済み」と書く論文が2本ある（EarDA，IMUPoser）．公式説明と食い違うので，PoCでは `sensorLocation` の切り替わりをログに残すこと．
3. **閾値方式で十分な可能性が高い**．ただし誤検出対策が要る．イヤピースでの頭部ジェスチャ研究（HeadText）では，ジャイロのエネルギー閾値を10〜50 deg/sで振った結果，30 deg/sが精度と使い心地の両方で最良だった．低い閾値では雑音動作が拾われて被験者の集中が削がれたと報告されている．雑音の大半は**歩行**由来だった（雑音サンプル81件中72件）．
4. **会話・咀嚼・笑い**も誤検出源になる．会話・咀嚼で頭部追跡誤差が増える（Ferlini 2019），笑いによるリズミカルな上下動が頷きと誤認される（Kapoor & Picard 2001，映像ベース）．
5. **ML/DTWは精度は高いが遅い**．セグメント全体を見てから判定するDTW方式は，ジェスチャ終了後およそ0.8秒（60 fps換算で約50フレーム）で結果を出していた（Sepanosian 2024）．「トントン」の即応性が要るゲームでは，オンセット閾値方式を主にして，ML/DTWは誤検出の後段フィルタに回すのが妥当である【推論】．
6. **個人差が大きい**．eSenseでのCNNは，既知ユーザの新データで平衡精度80%だったのが，未知ユーザでは55%まで落ちた（Laporte 2021）．ユーザごとの4秒キャリブレーションは，文献の傾向と合う設計である．
7. **ローカルWi-Fiは中央値は小さくても尾が重い**．802.11acで99.96%のパケットは10 ms以内に往復したが，最大227.8 msのRTTがあった（Zhang 2018）．Android端末では省電力モード（PSM）のビーコン周期（102.4 ms）により，RTTが100 ms超上乗せされうる（Li 2016）．iOSで同じことが起きるかは【未検証】．p95/p99と最大値の計測は必須である．

---

## 1. Apple公式：CMHeadphoneMotionManager／Delegate／接続イベント／認可

### [S1] Apple Developer Documentation: CMHeadphoneMotionManager，CMHeadphoneMotionManagerDelegate，startConnectionStatusUpdates()

- URL：
  - https://docs.developer.apple.com/tutorials/data/documentation/coremotion/cmheadphonemotionmanager.md
  - https://docs.developer.apple.com/tutorials/data/documentation/coremotion/cmheadphonemotionmanagerdelegate.md
  - https://docs.developer.apple.com/tutorials/data/documentation/coremotion/cmheadphonemotionmanager/startconnectionstatusupdates().md
- 主な知見：
  - 対応OSはiOS／iPadOS 14.0以降，macOS 14.0以降，Mac Catalyst 14.0以降，watchOS 7.0以降．使う前に `isDeviceMotionAvailable` を確認するよう書かれている．
  - iOSとmacOSでは `Info.plist` に `NSMotionUsageDescription` が必須．無い状態でdevice-motion更新を開始すると，システムがアプリをクラッシュさせる．
  - API構成：`authorizationStatus()`，`startDeviceMotionUpdates(to:withHandler:)`（push型），`deviceMotion`（pull型），`startConnectionStatusUpdates()`／`stopConnectionStatusUpdates()`，`isConnectionStatusActive`．Delegateは `headphoneMotionManagerDidConnect(_:)` と `headphoneMotionManagerDidDisconnect(_:)` の2つ．
  - `startConnectionStatusUpdates()` には説明文が無く，宣言だけが載っている．挙動の詳細は【未検証】．
  - 更新レートを設定するAPIは載っていない（`CMMotionManager` の `deviceMotionUpdateInterval` に当たるものが無い）．
- 設計への示唆：
  - 起動時に `isDeviceMotionAvailable` と `authorizationStatus()` を確認し，未許可・非対応・切断の各状態をUnity側にも状態メッセージとして送る．試合中に片方のプレイヤーが「無入力」になったとき，原因を区別できるようにするためである．
  - レートは指定できない前提で，検出器のパラメータ（不応期など）はサンプル数ではなく**時間（timestamp差）**で書く．

### [S2] Apple WWDC23 "What's new in Core Motion"（Session 10179）

- URL：https://developer.apple.com/videos/play/wwdc2023/10179/
- 主な知見（トランスクリプトより）：
  - `CMHeadphoneMotionManager` は，空間オーディオのダイナミックヘッドトラッキングを支えるのと同じdevice-motionデータ（attitude，user acceleration，rotation rate）をアプリに出す．macOS 14からはMacでも使える．
  - 「motion data is delivered to you from one bud at a time」．各サンプルに付く `SensorLocation` で，左右どちらのバッドから来たかが分かる．自動耳検出が有効なら，ストリーム中のバッドを耳から外すともう一方が引き継ぐ．
  - 自動耳検出が有効な場合，バッドを外すとdisconnect，戻すとconnectイベントが来る．
  - 認可は `Info.plist` のMotion Usage Descriptionでユーザに求められ，`authorizationStatus` で確認できる．attitudeは基準姿勢を保持し，`multiply` で相対変化を得る例が示されている．
  - 比較として，`CMMotionManager`（iPhone内蔵）は1サンプルずつ最大100 Hzで配信され，低遅延用途に向く，と説明されている．ヘッドホン側のレートは明言されていない．
- 設計への示唆：
  - **片耳ストリーム**なので，プレイ中に耳の位置を直す動作でバッドの切り替わりや接続断が起こりうる．`sensorLocation` が切り替わった直後は，軸の符号・オフセットが変わるおそれがある【推論】．切り替え直後の数百msは検出を止める（あるいは再キャリブレーションする）処理を入れ，切り替え回数をログで数える．
  - 自動耳検出による切断・再接続をイベントとしてUnityに送り，試合を一時停止する仕様にできる．
  - 頷きは主にピッチ軸の回転である．ただしバッドの装着角度で軸が傾くので，キャリブレーション中に重力方向から「頭の左右軸」を推定し，その軸への射影でrotationRateを評価する方法が考えられる【推論】．

---

## 2. AirPods（CMHeadphoneMotionManager）を使った研究・計測：サンプリングレート，融合，遅延

### [S3] Lyu, S., Chen, Y., Duan, D., Jia, R., Xu, W. "EarDA: Towards Accurate and Data-Efficient Earable Activity Sensing." arXiv:2406.16943（2024）．ACMの被引用情報では2024 IEEE CSCAIoTに掲載．

- URL：https://arxiv.org/html/2406.16943v1
- 主な知見：
  - AirPods Pro 2を使い，サンプリングレートを「APIが許す最大値である25 Hz」に設定したと明記している．
  - 「各バッドのIMU信号はAPI内で自動的に融合され，頭部の動きを表す」と書いている．これは[S2]の公式説明（片耳ずつ配信）と食い違う．
  - 頭部動作はおおむね5 Hz以下の帯域にあり（先行研究では頭部信号は5.8 Hz未満），5 Hzローパスで頭部動作の外乱を抑えたところ，HAR精度が約4ポイント上がった．
  - 4秒窓（25 Hz × 100サンプル）で処理した．
- 設計への示唆：
  - 25 Hzならナイキスト周波数は12.5 Hzなので，頷き（数Hz以下）の検出には帯域が足りる【推論】．逆に5 Hz程度のローパスを掛けると，速い「トントン」のピークがなまって遅延が増える．フィルタは軽い（例：2〜3タップの移動平均）にとどめ，遅延とのトレードオフを実測する．
  - 「融合済み」という記述はAppleの説明と食い違うので採用しない．

### [S4] Mollyn, V., Arakawa, R., Goel, M., Harrison, C., Ahuja, K. "IMUPoser: Full-Body Pose Estimation using IMUs in Phones, Watches, and Earbuds." CHI '23. https://doi.org/10.1145/3544548.3581392

- URL：https://dl.acm.org/doi/fullHtml/10.1145/3544548.3581392
- 主な知見：
  - AirPods Proを含む構成で，システムのレートを「AirPodsの最大サンプリングレートである25 Hz」に設定した．リアルタイム実装でも，AirPodsが約25 FPSで最も遅いデバイスだったと書いている．
  - AirPodsには磁気センサが無く，向きは「頭の初期フレームに対する相対値」しか得られない．そのため `XArbitraryCorrectedZVertical` 参照フレームを使い，3秒間の静止でキャリブレーションした．
  - AirPodsは「左右のIMUを融合して単一の頭部6DoF推定を出す」と書いている（[S2]と食い違う）．
  - iPhoneからMacBook Airへストリームしてモデルを回し，推論時間は平均26.8 msだった．ただしAirPods→iPhone→Mac間の伝送遅延は報告されていない．
- 設計への示唆：
  - 3秒の静止キャリブレーションという先例があるので，4秒キャリブレーションは妥当な範囲である．
  - 磁気センサが無いのでヨー（左右の向き）はドリフトする．頷き検出はピッチ方向の角速度（rotationRate）で行い，姿勢の絶対値には頼らないほうが安定する【推論】．

### [S5] コミュニティによる実測報告（2件．レートの食い違い）

- (a) Stack Overflow "What frequency standard does CMHeadphoneMotionManager's startDeviceMotionUpdates() has?"（2022）．
  URL：https://stackoverflow.com/questions/71612270/what-frequency-standard-does-cmheadphonemotionmanagers-startdevicemotionupdates
  - 回答者2名が独立に「実験上約25 Hz」と報告している（AirPods Pro第1世代）．設定するAPIは無い，という点も一致している．
- (b) AWARE Framework "com.awareframework.ios.sensor.airpods" README（東京大学 西山勇毅氏）．
  URL：https://github.com/awareframework/com.awareframework.ios.sensor.airpods
  - 「更新レートはAirPodsのファームウェアが決め，プログラムからは設定できない．ハードウェア本来のレート（約100 Hz）で届く」と書いている．25 Hz説とは食い違う．`sensorLocation` を `headphonePlacement`（0=default，1=left，2=right）として保存している．
- 設計への示唆：
  - 実効レートは**機種（AirPods Pro 1／2，AirPods 3／4，Max）とOSバージョン**で変わる可能性がある【未検証】．PoC初日の計測項目として，(i) timestamp差分の分布（中央値・p95・最大），(ii) 1秒あたりのサンプル数，(iii) `sensorLocation` の切り替え回数を，プレイヤー2人分・各機種で記録する．
  - 「最大6回/秒」は25 Hzなら1回あたり約4.2サンプルに当たる．頷き1回のピークを2〜3サンプルで捉える前提になるので，検出の取りこぼし率を必ず測る【推論】．

---

## 3. イヤラブルIMUによる頷き・首振り検出（eSenseほか）

### [S6] Laporte, M., Baglat, P., Gashi, S., Gjoreski, M., Santini, S., Langheinrich, M. "Detecting Verbal and Non-Verbal Gestures Using Earables." UbiComp/ISWC '21 Adjunct（EarComp 2021），pp.165–170. https://doi.org/10.1145/3460418.3479322

- URL：https://www.esense.io/earcomp2021/EarComp-PreACM.pdf
- 主な知見：
  - eSense左耳のIMUを25 Hzで記録した．被験者10名で，頷き・発話・食事・静止・首振りの5動作．4秒窓（75%オーバーラップ）を3層1D-CNNに入力し，非言語（頷き・首振り）／言語（発話）／その他の3クラスに分類した．
  - 平衡精度は，既知ユーザの未見データ（LOPO）で80%，未知ユーザ（LOSO）で55%，未知の動作（LOAO）で41%だった．
  - 加速度のみ75%，ジャイロのみ69%，両方で80%．被験者ごとの平衡精度は約48〜85%と開きがある．
- 設計への示唆：
  - **ユーザ間の汎化が弱い**．ユーザごとのキャリブレーション（閾値の個人化）は必須と考えてよい．
  - 25 Hz・4秒窓という条件はPoCとほぼ同じである．ただし4秒窓の分類はゲーム入力には遅すぎるので，イベント検出（オンセット）とは分けて考える．
  - 評価指標は，クラス不均衡に強い平衡精度とF1を併記するのがこの分野の慣行である．

### [S7] Gashi, S., Saeed, A., Vicini, A., Di Lascio, E., Santini, S. "Hierarchical Classification and Transfer Learning to Recognize Head Gestures and Facial Expressions Using Earbuds." ICMI '21, pp.168–176. https://doi.org/10.1145/3462244.3479921

- URL：https://dl.acm.org/doi/10.1145/3462244.3479921（**要旨のみ確認**．本文は未読）
- 主な知見（要旨より）：
  - イヤーバッドの慣性信号で，首振り・頷き・笑顔・発話・あくびを階層分類した．
  - 頭部動作と口周り動作の識別はF1=84.79，笑顔・発話・あくびの識別はF1=45.42，頷きと首振りの識別はF1=88.24だった．浅い分類器を2〜9ポイント上回った．
- 設計への示唆：
  - **頷き対首振りの区別は比較的やさしく，発話・笑顔など口周りの動作の区別は難しい**．PoCでは「頷き」と「それ以外の頭の揺れ（会話・笑い）」を区別することが主な課題になる．
  - 第1段で「頭部の動き／口周りの動き」を分ける階層構造は，誤検出フィルタの設計に参考になる【推論】．

### [S8] Ferlini, A., Montanari, A., Mascolo, C., Harle, R. "Head Motion Tracking Through in-Ear Wearables." EarComp '19, pp.8–13. https://doi.org/10.1145/3345615.3361131

- URL：https://mobile-systems.cl.cam.ac.uk/papers/earcomp19.pdf
- 主な知見：
  - eSense（左バッド2個）を100 Hzで使い，相補フィルタで頭部回転を推定した．平均誤差は，黙っている被験者の短い動作で5.4°，咀嚼しながらの長い動作で18.7°だった．
  - **咀嚼と発話は，どちらも同程度に推定誤差を増やす**（振動と微小な動きを拾うため）．
  - イヤラブルは磁石（ケース・スピーカー）があるため磁気センサを積めず，AirPodsにも磁気センサが無い，と述べている．ドリフト補正には重力しか使えない．
  - 左右2つのIMUの200 ms窓平均で精度を上げた．
- 設計への示唆：
  - 発話中・咀嚼中の誤差増は，PoCでの「喋りながらプレイ」による誤検出リスクに直結する．**発話中の誤検出率（FP/分）**を条件別に測る．
  - 姿勢の絶対値ではなく角速度のピークで判定する設計は，ドリフトの影響を受けにくい点で合理的である【推論】．

### [S9] Sepanosian, T. "Real-Time Recognition of Boxing Head Gestures with IMU-Earables: Machine Learning and Dynamic Time Warping." TScIT 41，University of Twente（2024）．（関連する短報：UbiComp Companion '24, https://doi.org/10.1145/3675094.3680524 ．こちらは未読）

- URL：https://essay.utwente.nl/fileshare/file/100840/Sepanosian_BA_EEMCS.pdf
- 主な知見：
  - OpenEarable，50 Hz，被験者1名，5種類の頭部回避動作で約460サンプル．50サンプル窓・25オーバーラップの特徴量＋ランダムフォレストは，テストでは92%だったが，**実運用では右スリップを一度も認識できないなど破綻した**．
  - DBA（DTW重心平均）テンプレート＋DTWは，テストで99%，実運用（各50回）で96%だった．速さ・強さの違いにも頑健だった．
  - DTWは区間の始点・終点を検出してから比較するので，**判定はジェスチャ終了後になる**．録画では異常区間の終了から約50フレーム（60 fps）後に予測が出ており，体感では「1秒以内」だった．
  - 閾値が敏感すぎると，静止中でもアノマリ検出が不要に発火するのが主な失敗例だった．
  - 関連研究表には，頭部装着DTWによる頷き・首振り認識（Mavus & Sezer 2017，約80%），スマートグラスでのDTW（Li & Hu 2024，100%）が挙がっている（いずれも二次引用で未読）．
- 設計への示唆：
  - **テスト精度と実運用精度は大きくずれうる**．PoCの評価はオフラインの分割検証だけでなく，実際にプレイしたセッションで行う．
  - DTWは「確定判定」には強いが遅い．即応性が要る「トン」は閾値オンセットで出し，DTWなどは（遅れて）取り消し・確定に使う二段構えが考えられる【推論】．
  - 閾値の感度と誤発火はトレードオフである．閾値の掃引実験を計画に入れる．

### [S10] Xu, S., Wang, G., Fang, Z., Zhang, G., Shang, G., Lu, R., He, L. "HeadText: Exploring Hands-free Text Entry using Head Gestures by Motion Sensing on a Smart Earpiece." arXiv:2205.09978（2022）．

- URL：https://arxiv.org/abs/2205.09978（PDF v2本文を確認）
- 主な知見：
  - 耳の下に固定したIMU（ITG3200／ADXL345／HMC5883L）で7種類の頭部ジェスチャを扱い，KNN＋DTWで94.29%（10名）．文字入力速度は平均9.84 WPMだった．
  - **セグメンテーション**：3軸ジャイロの「エネルギー」をPeak Thresholdと比べ，Left Buffer／Tolerance Range／Right Bufferの4パラメータで区間を切り出す．一瞬閾値を下回っただけでは区間を切らない．
  - **閾値の掃引**：Peak Thresholdを10〜50 deg/sで5条件比較した．**30 deg/sで全ジェスチャ90%超，平均94%**，NASA-TLXも最良だった．10〜20 deg/sでは雑音動作が検出されやすく集中を妨げ，40〜50 deg/sでは動作に余計な力が要った．閾値10→30 deg/sでジェスチャの平均所要時間は0.16秒，30→50 deg/sでは0.08秒短くなった．
  - **雑音動作**：歩行・食事・発話で切り出された雑音81件のうち，歩行が72件，食事8件，発話1件．雑音と本来のジェスチャを分ける二値分類器を別に学習させた．ただし文字入力の評価は着座のみで行っている．
- 設計への示唆：
  - PoCの `rotationRate` 閾値の初期値の目安として，**約30 deg/s ≈ 0.52 rad/s**が使える．ただしHeadTextの動作は「タップ／スライド」でトントンより大きい可能性がある．キャリブレーション値（静止時のノイズ）からの相対閾値と，絶対下限の両方を設けるのが安全である【推論】．
  - 閾値ごとの「体感の楽さ（NASA-TLXや主観評価）」も評価軸に入れる．
  - 歩行中にプレイさせないルールにするか，歩行検出（周期的な加速度）でゲート処理を入れる．

### [S11] Yang, X., Wang, X., Dong, G., Yan, Z., Srivastava, M., Hayashi, E., Zhang, Y. "Headar: Sensing Head Gestures for Confirmation Dialogs on Smartwatches with Wearable Millimeter-Wave Radar." Proc. ACM IMWUT（2023．巻号は未確認）．https://doi.org/10.1145/3610900

- URL：https://yangzhang.dev/research/Headar/Headar.pdf
- 主な知見（センサはレーダーだが，ゲートと閾値の設計が参考になる）：
  - 事前調査で，人が頭部ジェスチャ（頷き・首振りなど）を終えるのに**平均約2秒**かかった．
  - 計算コストの高いスライディング窓をやめ，信号強度に対するオンセット／オフセット閾値によるイベント駆動パイプラインにした．閾値は**IMUで測った動きの大きさに応じて自動で引き上げる**（動きが大きいほど閾値を高くする）ことで誤検出を減らした．
  - 手首を上げた直後のレーダー信号による誤検出を消すため，**0.7秒のクーリング期間**を設けた．
  - 歩行時は胴体の動きが主な誤差源で，歩行・発話中の無意識の頭の動きは，小さなジェスチャと似た信号になると述べている．ユーザ調査では平均84.0%，ライブテストでは72.6%だった．
- 設計への示唆：
  - **動的閾値**：直近の「背景の揺れ」（例：直近1〜2秒のrotationRateのRMS）に応じて閾値を上げる方式は，会話・笑い対策としてそのまま移植できる【推論】．
  - 状態遷移（キャリブレーション直後，バッド切り替え直後，再接続直後）にクーリング期間を設ける．150 ms不応期とは別物で，「状態変化後のマスク」と位置づける【推論】．
  - 頷き全体は約2秒かかるのに対し，トントンはその一部（1ストローク）を入力にしている．速く小さい動作ほどユーザ差・疲労が出やすいと考えられるので，1分程度の連打での取りこぼし率の推移を測る【推論】．

---

## 4. 発話・笑い・歩行中の誤検出

### [S12] Kapoor, A., Picard, R. W. "A Real-Time Head Nod and Shake Detector." Proc. Workshop on Perceptive User Interfaces（PUI 2001）．MIT Media Lab Affective Computing TR No.544．

- URL：https://hd.media.mit.edu/tech-reports/TR-544.pdf
- 主な知見（IMUではなく，赤外線瞳孔追跡＋HMMによる映像ベースの研究）：
  - 30 fpsのリアルタイム処理で，頷き・首振りの認識率は78.46%だった．
  - **誤検出の例として，被験者が笑い出して頭がリズミカルに上下したときに頷きと検出された**ことを挙げている．
  - 頷き・首振りは「小さく，速く，ぎくしゃくした」動きなので，追跡器が失敗しやすいと述べている．
- 設計への示唆：
  - 笑いは頷きと同じピッチ方向の周期的な揺れなので，IMUでも同じ誤検出が起こると考えられる【推論】．ゲームの性質上，対戦中に笑いは頻繁に起こるはずである．評価プロトコルに「笑いながら（冗談・動画視聴）」の条件を明示的に入れ，FP/分を測る．
  - 対策候補【推論】：(i) 上下両方向のピークが短時間に続く「往復」パターンを笑いの特徴として抑制する，(ii) [S11]の動的閾値，(iii) 最大6回/秒の上限を，ユーザが実際に出せる連打速度に合わせて下げる．

（補足）発話・咀嚼による誤差増は[S8]，歩行が雑音の大半を占めることは[S10]，歩行中は胴体の動きが主な誤差源であることは[S11]，頭部外乱の帯域（5 Hz以下）は[S3]を参照．

---

## 5. ローカルWi-Fi上のUDP遅延（ゲーム用途）

### [S13] Li, W., Wu, D., Chang, R. K. C., Mok, R. K. P. "Demystifying and Puncturing the Inflated Delay in Smartphone-based WiFi Network Measurement." CoNEXT '16, Irvine, CA, USA.

- URL：https://daoyuan14.github.io/papers/CoNEXT16_WiFiMeasure.pdf
- 主な知見（**対象はAndroid端末**）：
  - スマートフォン内部のSDIOバス休止と802.11の省電力モード（PSM）が，計測される遅延を膨らませ不安定にする主な原因である．
  - Nexus 5では，SDIOバス休止が有効だと，送信間隔1秒のとき片方向の内部遅延が平均約10〜14 ms増えた．休止を無効にすると1 ms未満に下がった．
  - 適応型PSMのタイムアウトは端末ごとに違う（Nexus 4は約40 ms，Nexus 5は約205 ms）．実際のlisten intervalは1ビーコン周期の102.4 msで，**PSMによりRTTが100 ms超膨らみうる**．
  - 軽い背景トラフィック（間隔20 ms程度）で端末を起こしておくと，遅延の上乗せの中央値を3 ms以内に抑えられた．
- 設計への示唆：
  - iPhoneにも同様の省電力動作があるかは【未検証】である．もしあるなら，**トントンの間が空いた直後の最初のパケットだけが遅れる**ことになる．対策【推論】：iPhoneから一定間隔（例：20〜50 ms）でハートビート（またはIMU要約）を送り続け，無線を起こしたままにする．イベントは即時送信する．
  - 計測は「間を空けた後の初回イベント」と「連打中のイベント」を分けて集計する．

### [S14] Zhang, H., Elmokashfi, A., Mohapatra, P. "WiFi and Multiple Interfaces: Adequate for Virtual Reality?" IEEE ICPADS 2018（会場名はPDFのファイル名からの推定）．

- URL：https://pmlab.cse.usf.edu/assets/pubs/zhang-icpads18.pdf
- 主な知見：
  - 屋内の802.11acで2台のWi-Fi機器間のRTTを測ったところ，99.96%のパケットは10 ms以内に戻ったが，**最大227.8 ms**のものがあり，24.2%超のパケットで1 msを超えるジッタがあった．
  - 遅延の主因はチャネル伝送時間（ドライバに入ってからACKまで）だった．2つのインタフェースを併用すると，中央値RTTが28.6%，1 ms超のジッタが11.5%減った．
- 設計への示唆：
  - ローカルWi-Fiでも**中央値ではなくテール（p99，最大）**がゲーム体験を壊す．UDP JSONに `seq`（連番）と送信側の単調増加時刻を入れ，Unity側で損失・順序入れ替わり・遅延分布を記録する．
  - 2人のプレイヤーの到着遅延に差があると，相撲の「同時押し」判定が不公平になる．Unity側で到着時刻ではなく**送信側タイムスタンプ（時計を同期したもの）で順序を決める**か，一定の受付窓（例：p95遅延に相当する時間）を設ける【推論】．
  - 試合中は同じAPに大きなトラフィック（動画など）を流さない．可能ならMacを有線にするか，専用AP・5 GHz帯を使う【推論】．

---

## 6. PoCの検出器・キャリブレーション・評価の推奨（文献からの統合）

【推論】の印を付けた項目は，上の文献から筆者が組み立てた提案である．

### 6.1 検出器アルゴリズム（第1段：低遅延オンセット）

1. 入力：`rotationRate` を，キャリブレーションで推定した頭の左右軸（ピッチ軸）へ射影した値 ω_p(t)．補助として3軸ノルム |ω| も使う（[S2][S4]【推論】）．
2. 前処理：2〜3サンプルの軽い平滑化に限る．5 Hzローパス（[S3]）は遅延が増えるので，比較条件として扱う．
3. 判定：ω_p が `max(μ + k·σ, 絶対下限)` を下向き（頷き方向）に超えた最初のサンプルでイベントを出す．絶対下限の初期値は約0.5 rad/s（≈30 deg/s，[S10]）を目安に掃引する．
4. 動的閾値：直近1〜2秒の |ω| のRMSが高い（会話・笑い・歩行）ときは閾値を引き上げる（[S11]）．
5. 不応期150 ms＋上限6回/秒．状態変化（キャリブレーション直後，`sensorLocation` 切り替え，再接続）の後は，別に0.5〜0.7秒程度のマスクを入れる（[S11]【推論】）．
6. 往復パターンの抑制：上下両方向のピークが短い間隔で続く場合は，笑いの可能性として重み付けする（[S12]【推論】）．

### 6.2 第2段（任意）：取り消し・確定フィルタ

- 第1段のイベントの前後0.5〜1秒をDTW（DBAテンプレート）や小さな分類器で再判定し，雑音らしければ後から取り消す（[S9][S10]）．ゲーム側で「取り消し可能な入力」を扱えるかは仕様判断になる．

### 6.3 キャリブレーション（4秒）

- 静止4秒で μ・σ（ピッチ軸のrotationRate），重力方向（＝頭の上下軸），`sensorLocation` を記録する．3秒の先例（[S4]）がある．
- 4秒の中で「頷き3回」など本番の動作を数回させ，個人のピーク振幅を測って閾値を個人化する．ユーザ間の汎化が弱いことへの対策である（[S6]【推論】）．
- バッドの切り替え・再接続時は，再キャリブレーションするか，少なくともμの再推定を行う（[S2]【推論】）．

### 6.4 PoCで測る評価指標

| 区分 | 指標 | 根拠 |
|---|---|---|
| センサ | 実効サンプルレート，timestamp差分の中央値・p95・最大，`sensorLocation` 切り替え回数，切断回数 | [S2][S3][S4][S5] |
| 検出精度 | イベント単位の適合率・再現率・F1（正解とのマッチング許容窓を明記），ユーザ別とLOSO的な横断比較，平衡精度 | [S6][S7] |
| 誤検出 | 条件別のFP/分（静止・会話・笑い・咀嚼・歩行），閾値掃引ごとのROC/PR曲線 | [S8][S10][S11][S12] |
| 連打性能 | 目標リズム（2・4・6回/秒）での取りこぼし率，1分連打での低下 | [S5]【推論】 |
| 遅延 | 動作オンセット→iPhone検出（ハイスピード動画で外部計測），iPhone送信→Unity受信（p50/p95/p99/最大），間を空けた後の初回と連打中の比較 | [S9][S13][S14] |
| ネットワーク | UDPの損失率，順序入れ替わり，2台間の到着時刻差 | [S14] |
| 主観 | 閾値ごとの楽さ・誤反応のストレス（NASA-TLXなど） | [S10] |

遅延の外部計測は，Sepanosianが画面録画（60 fps）でフレームを数えた方法（[S9]）が手軽である．240 fpsのスロー動画で頭とUnity画面を同時に撮れば，ずっと細かく測れる【推論】．

---

## 7. 未確認事項・限界

- **AirPodsの実効レート**：25 Hz（[S3][S4][S5a]）と約100 Hz（[S5b]）で報告が食い違う．機種・OSによる差は【未検証】．
- **左右融合か片耳か**：Apple公式（[S2]）は片耳ずつと説明し，2本の論文（[S3][S4]）は融合済みと記述している．本レポートは公式説明を優先した．
- **AirPods→iPhoneのBluetooth伝送遅延**：一次情報で数値を見つけられなかった【未検証】．PoCで外部計測するしかない．
- **iOSのWi-Fi省電力が遅延に与える影響**：[S13]はAndroid端末での計測であり，iPhoneへの当てはまりは【未検証】．
- **Gashi et al.（[S7]）は要旨のみ**を確認した．窓長・サンプリングレートなどの詳細は未確認．
- 二次引用だけで本文を開いていない文献（Mavus & Sezer 2017，Li & Hu 2024，Morency et al.など）は，事実の根拠としては使っていない．
- 頷き「トントン」のような**短い1ストローク入力**を直接扱った研究は見つからなかった．文献の多くは頷き・首振りのジェスチャ全体（約2秒，[S11]）を対象にしている．閾値やタイミングの数値はそのまま移植せず，PoCで掃引すること．

---

## 参照ソース一覧（すべて実際に開いたもの）

1. [S1] Apple Developer Documentation — CMHeadphoneMotionManager／CMHeadphoneMotionManagerDelegate／startConnectionStatusUpdates()：https://docs.developer.apple.com/tutorials/data/documentation/coremotion/cmheadphonemotionmanager.md ，https://docs.developer.apple.com/tutorials/data/documentation/coremotion/cmheadphonemotionmanagerdelegate.md ，https://docs.developer.apple.com/tutorials/data/documentation/coremotion/cmheadphonemotionmanager/startconnectionstatusupdates().md
2. [S2] Apple WWDC23 "What's new in Core Motion"：https://developer.apple.com/videos/play/wwdc2023/10179/
3. [S3] Lyu et al., EarDA（2024）：https://arxiv.org/html/2406.16943v1
4. [S4] Mollyn et al., IMUPoser（CHI '23）：https://dl.acm.org/doi/fullHtml/10.1145/3544548.3581392
5. [S5a] Stack Overflow（AirPodsのレート報告）：https://stackoverflow.com/questions/71612270/what-frequency-standard-does-cmheadphonemotionmanagers-startdevicemotionupdates
   [S5b] AWARE AirPods Motion README：https://github.com/awareframework/com.awareframework.ios.sensor.airpods
6. [S6] Laporte et al.（EarComp 2021）：https://www.esense.io/earcomp2021/EarComp-PreACM.pdf
7. [S7] Gashi et al.（ICMI '21，要旨のみ）：https://dl.acm.org/doi/10.1145/3462244.3479921
8. [S8] Ferlini et al.（EarComp '19）：https://mobile-systems.cl.cam.ac.uk/papers/earcomp19.pdf
9. [S9] Sepanosian（Univ. Twente, 2024）：https://essay.utwente.nl/fileshare/file/100840/Sepanosian_BA_EEMCS.pdf
10. [S10] Xu et al., HeadText（arXiv 2022）：https://arxiv.org/abs/2205.09978
11. [S11] Yang et al., Headar（IMWUT 2023）：https://yangzhang.dev/research/Headar/Headar.pdf
12. [S12] Kapoor & Picard（PUI 2001）：https://hd.media.mit.edu/tech-reports/TR-544.pdf
13. [S13] Li et al.（CoNEXT '16）：https://daoyuan14.github.io/papers/CoNEXT16_WiFiMeasure.pdf
14. [S14] Zhang et al.（ICPADS 2018）：https://pmlab.cse.usf.edu/assets/pubs/zhang-icpads18.pdf
