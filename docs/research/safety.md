# AirPods頭振りトントン相撲 PoC：安全性・快適性・プレイ設計に関する先行研究調査

作成：2026-11-02（JST）．対象：同室の2人がAirPodsを装着し，うなずき動作でMac画面上の仮想トントン相撲の土俵を叩く．1取組20〜30秒．

## 凡例

- 各項目は「書誌情報／URL／主な知見（2〜4点）／本PoCへの設計含意」の順に書いた．
- 知見は実際に開いたページ（本文，抄録，PDF）に基づく．抄録しか読めていない文献はその旨を書いた．
- 【推論】は文献から筆者が導いた設計案で，文献自体が述べていることではない．【未検証】は今回の調査で一次資料を開いて確認できなかった事項である．
- 文献項目は15（§14は評価尺度2種をまとめた）．§12は文献ではなく公平性の論点整理である．

---

## 0. 要約（設計パラメータの暫定案）

| 項目 | 暫定案 | 根拠 |
|---|---|---|
| うなずきの可動域 | 判定に必要な振幅を小さく取り（目安として前後屈あわせて数十度未満），大振りを得点上有利にしない | §1（Patton & McIntosh 2008）．45°以下は同モデルでどの傷害閾値も下回った【数値の当てはめは推論】 |
| うなずきの頻度 | 上限を設け（例：2 Hz＝120回/分程度で頭打ち），それ以上の連打は得点に反映しない | §1．130 BPM付近から頸部傷害リスクが上がり始めるとのモデル結果【上限値は推論】 |
| 1セッションの総時間 | 取組20〜30秒×数本＋休憩．連続プレイの合計は10分程度までを目安にし，SSQとNASA-TLXで確認 | §3（Vinson et al. 2012：約12分で症状が顕在化）【数値の転用は推論】 |
| 画面演出 | 頭の動きに連動するカメラ移動・画面全体の回転や揺れを入れない．動くのは土俵上の力士だけ | §3，§4 |
| 休憩 | 取組ごとに休憩を挟み，取組間に頸部の違和感を口頭確認 | §2，§6 |
| キャリブレーション | 取組前に各自の「楽にできるうなずき」を数回記録し，閾値を個人ごとに設定 | §9，§10 |
| 公平性 | 生の強度ではなく個人の基準に対する相対値で力士の力を決める | §8，§9，§12 |
| スクリーニング | 乗り物酔いしやすさ，頸部の既往・痛み，当日の体調を事前質問で確認し，該当者は除外または観察のみ | §2，§3，§16 |
| 評価尺度 | SSQ（前後），NASA-TLX（Raw TLXも可），GEQ（または下位尺度の抜粋），IMI（興味・楽しさ，有能感，緊張），SUS | §5，§13，§14 |

---

## A. 頸部の安全性（反復的なうなずき・ヘッドバンギング）

### 1. Patton & McIntosh (2008) ヘッドバンギングの頭部・頸部傷害リスク

- 書誌：Patton, D., & McIntosh, A. (2008). Head and neck injury risks in heavy metal: head bangers stuck between rock and a hard bass. *BMJ*, 337, a2825. doi:10.1136/bmj.a2825
- URL：https://pmc.ncbi.nlm.nih.gov/articles/PMC2605616/
- 主な知見：
  1. 頭部の矢状面の振動を正弦波とみなし，C7-T1を回転軸とする理論モデルでHead Injury Criterion（HIC）とNeck Injury Criterion（NIC）を算出した．可動域は45°から120°まで15°刻みで変えた．
  2. 平均的なヘッドバンギング曲のテンポは約146 BPMで，可動域が75°を超えると軽度頭部傷害（頭痛・めまい相当）が予測された．頸部傷害リスクは130 BPM付近から，可動域に応じて上がり始める．
  3. 164 BPMでも可動域約45°ではどの傷害閾値も下回り，138 BPM・約45°でも傷害は予測されなかった．
  4. 著者自身が限界を明記している：HIC/NICは単発の加速度ピークを評価する指標であり，周期的な動きや1セッション内・複数セッションにわたる累積効果を評価する確立した基準はない．直接衝撃のない状況でのHIC適用にも議論がある．推奨は「可動域を減らす」「テンポを落とす」「2拍に1回だけ振る」など．
- 設計含意：
  - 得点や力士の力が可動域に比例する設計にすると，プレイヤーは大振りに誘導される．判定は小さな振幅で成立させ，一定以上の振幅は得点に上乗せしない（飽和させる）．
  - 頻度にも上限を設け，連打で得をしない設計にする．目安として2 Hz程度で頭打ちにする案が考えられる【推論．同論文のBPMは拍数であり，うなずき1往復の頻度と同一視できるかは未確定】．
  - 20〜30秒×数本という短い曝露はこのモデルの想定（曲単位）より短いが，累積効果は未知である．反復回数を記録し，研究報告では「安全が確認された」とは書かず「既知の閾値から距離をとった設計にした」と書く．
  - 注記：BMJのクリスマス号の論文で，理論モデルへの反論も寄せられている（BMJ rapid response「The Kids are Alright」）【反論の中身は抄録的な抜粋のみ確認．未検証】．

### 2. Kassirer & Manon (1993) 中学生のダンスマラソンにおける「ヘッドバンガー頸部痛」

- 書誌：Kassirer, M. R., & Manon, N. (1993). Head banger's whiplash. *Clinical Journal of Pain*, 9(2), 138–141. doi:10.1097/00002508-199306000-00010
- URL：https://pubmed.ncbi.nlm.nih.gov/8358138/（抄録のみ確認）
- 主な知見：
  1. 7時間のダンスマラソンに参加した13〜14歳の37名のうち，ヘッドバンギングをした17名と，しなかった20名を比べた．
  2. ヘッドバンギングをした女子の81.82%，男子の16.6%に1〜3日続く頸椎部痛が出た．しなかった群では女子26.2%，男子0%だった．
  3. 痛みは自然に治まる経過だった．ただし症例報告扱いの小規模観察研究で，自己選択による群分けである．
- 設計含意：
  - 急性の重篤な傷害でなくても，翌日以降に残る頸部痛は普通に起こりうる．事後アンケートだけでなく，翌日に簡単な追跡（頸部の痛みの有無）を入れると安全側に倒せる【推論】．
  - 性差が報告されているが，n が小さく交絡も多いので，参加者の性別で強度を変える根拠にはしない．全員について個人キャリブレーションで対応する．

※頭部操作ゲームでの頸部疲労は§9，§10を参照．

---

## B. 画面を見ながら頭を動かすことによる動揺病・サイバーシックネス（非HMD）

### 3. Vinson et al. (2012) デスクトップVRでもサイバーシックネスは起こる

- 書誌：Vinson, N. G., Lapointe, J.-F., Parush, A., & Roberts, S. (2012). Cybersickness induced by desktop virtual reality. *Proceedings of Graphics Interface 2012*, 69–75.
- URL：https://nrc-publications.canada.ca/eng/view/accepted/?id=aa9661c8-91d9-49d9-bb47-a27a194e454f
- 主な知見：
  1. 54 cmのデスクトップモニタ（視距離70 cm）で迷路を歩く課題を行った．1ブロック約4分，計約16分．SSQは3ブロック目（約12分）以降に有意に上昇し，最終ブロックの総合得点は約25だった．
  2. 参加を始めた34名のうち8名（23.5%）がサイバーシックネスで途中離脱した．
  3. 著者らは，急で頻繁な視点回転（1試行あたり90°旋回を左右15回ずつ）が主因と推測している．視点が環境の「外」にある操作メタファーでは結果を一般化しないよう注意を促している．
  4. 乗り物酔い・ゲーム酔いの自己申告（2項目）が離脱者を判別した．体調不良，睡眠不足，飲酒などの該当者は募集段階で除外していた．文献上の目安として，SSQが15を超えると懸念，20を超えると「悪いシミュレータ」とされる（Stanney et al. 1997の引用）．
- 設計含意：
  - トントン相撲の画面は，視点が環境の外にある静的な俯瞰に近く，この研究よりリスクは低いと考えられる．カメラの回転・移動，画面全体の揺れといった演出は入れない．
  - それでも，うなずき中は画面上の注視点が上下に動く．連続プレイの合計時間は約10分を目安に区切り，取組ごとに休憩を入れる【推論】．
  - 事前に乗り物酔い・ゲーム酔いのしやすさを質問し，該当者には観察だけの参加や中止の選択肢を明示する．
  - SSQは開始前と終了後に実施する．

### 4. Palmisano, Allison & Kim (2020) HMDとの比較で見た非HMD表示での頭部運動

- 書誌：Palmisano, S., Allison, R. S., & Kim, J. (2020). Cybersickness in head-mounted displays is caused by differences in the user's virtual and physical head pose. *Frontiers in Virtual Reality*, 1, 587698. doi:10.3389/frvir.2020.587698
- URL：https://www.frontiersin.org/journals/virtual-reality/articles/10.3389/frvir.2020.587698/full
- 主な知見：
  1. 仮想と物理の頭部姿勢の差（DVP）がHMDでのサイバーシックネスの原因だとする仮説を提示した論文である．
  2. 外部の固定ディスプレイでは，頭部回転に伴う視覚変化は物理的な頭の回転によってそのまま正しく生じるので，回転についてのDVPはほぼ生じない．これがHMDより非HMDのほうが酔いにくい主な理由だとしている．一方，頭部並進に伴う運動視差をコンピュータで生成する場合は，非HMDでも表示遅延によるDVPが生じる．
  3. 同論文が引く先行研究では，HMDで20名中11名が離脱した同じシミュレーションを，デスクトップで提示したときは離脱者がいなかった（Dennison et al. 2016）．
  4. 頭部を拘束しない場合や立位のほうが，酔いやすさは増すと予測している．
- 設計含意：
  - AirPodsの頭部姿勢を「画面の視点」に反映させると（頭部連動の擬似3D，パララックス演出など），遅延と姿勢差が生まれ，酔いの要因になりうる．頭部の動きは入力イベント（叩く）としてだけ使い，視点には結びつけない．
  - プレイは着座で行う．立位で全身を揺らすプレイを許すと，姿勢の不安定さも加わる【推論．論文の予測を転用】．
  - この論文は仮説提示とレビューが中心で，本PoCの条件（固定画面，入力専用の頭部運動）を直接実験したものではない．

### 5. Kennedy et al. (1993) Simulator Sickness Questionnaire（SSQ）

- 書誌：Kennedy, R. S., Lane, N. E., Berbaum, K. S., & Lilienthal, M. G. (1993). Simulator Sickness Questionnaire: An enhanced method for quantifying simulator sickness. *The International Journal of Aviation Psychology*, 3(3), 203–220. doi:10.1207/s15327108ijap0303_3
- URL：https://cgvr.cs.uni-bremen.de/teaching/vr_literatur/Simulator%20Sickness%20Questionnaire,%20An%20Enhanced%20Method%20for%20Quantifying%20Simulator%20Sickness%20-%20Kennedy,%20Lane,%20Berbaum,%20Lilienthal,%201993.pdf
- 主な知見：
  1. Pensacola Motion Sickness Questionnaireを因子分析で再構成し，16症状をOculomotor（眼精疲労，焦点の合いにくさ，かすみ目，頭痛），Disorientation（めまい，回転性めまい），Nausea（吐き気，胃の不快感など）の3下位尺度にまとめた．
  2. 各症状を0〜3の4段階で評価し，下位尺度ごとの重みづけと総合得点を算出する（換算係数の詳細は§3のVinson et al.も参照）．
  3. Vinson et al. (2012) によれば，サイバーシックネス研究の事実上の標準になっている．
- 設計含意：
  - 開始前と全取組の終了後に実施し，差分で評価する．20〜30秒の短い取組ごとに毎回SSQを取ると負担が大きいので，取組間は「気分の悪さ」1項目の口頭確認で代える【推論】．
  - 本PoCで最も懸念されるのは，うなずき中の注視点移動による眼精疲労，頭痛といったOculomotor系の症状と考えられる【推論】．下位尺度別に報告する．
  - 日本語版を使う場合は，出典と翻訳の手続きを明記する【日本語版の出典は今回未確認】．

---

## C. エクサーゲームと頭部操作ゲーム

### 6. Mueller et al. (2011) Exertion Framework

- 書誌：Mueller, F., Edge, D., Vetere, F., Gibbs, M. R., Agamanolis, S., Bongers, B., & Sheridan, J. G. (2011). Designing sports: A framework for exertion games. *Proceedings of CHI 2011*, 2651–2660. doi:10.1145/1978942.1979330
- URL：https://exertiongameslab.org/wp-content/uploads/2020/12/designing_sports_chi2011.pdf
- 主な知見：
  1. 身体の4つのレンズ（Responding Body，Moving Body，Sensing Body，Relating Body）と，ゲームの3つの視点（rules，play，context）から，エクサーゲームを設計・分析する枠組みを示した．
  2. 文脈の論点として「エクサーションのリスク」を挙げている．身体を危険にさらしながら成功することは魅力の一部だが，成功のスリルと失敗による負傷との釣り合いを取るのは容易ではない．
  3. 遠隔対戦（Remote Impact）では相手から怪我をさせられることはないが，自分自身を傷める可能性は残る，と述べている．
- 設計含意：
  - Relating Body：同室の2人が互いの動きを見られることはこのゲームの面白さの中心である．画面だけでなく相手の頭の動きが見える配置（並んで座るなど）にする．
  - リスクの論点：トントン相撲の「激しく叩けば勝てる」という感覚はスリルにつながるが，頭部では許容できる範囲が狭い．激しさの演出は音と画面上の力士で表現し，身体の動きそのものは小さく済むようにする【推論】．

### 7. Mueller & Isbister (2014) Movement-based Game Guidelines

- 書誌：Mueller, F., & Isbister, K. (2014). Movement-based game guidelines. *Proceedings of CHI 2014*, 2191–2200. doi:10.1145/2556288.2557163
- URL：https://exertiongameslab.org/wp-content/uploads/2011/07/movement_guidelines_chi2014.pdf
- 主な知見：
  1. 14名の専門家（学術，インディー，商業）による検討を経た10の指針を示した．たとえば Embrace ambiguity，Celebrate movement articulation，Consider movement's cognitive load，Focus on the body，Intend fatigue，Exploit risk，Facilitate social fun，Support self-expression．
  2. Intend fatigue：疲労をゲームの挑戦として使うなら意図的に設計し，ゲームの一部でないなら避ける．設計者向けの方策として「短いゲームサイクルで疲労の機会を減らす」を挙げている．
  3. Exploit risk：身体的リスクは「分別をもって（sensibly）」利用する．
  4. Facilitate social fun：動きは他者に見られて自然にパフォーマンスになるので，複数プレイヤーと観客を想定して設計する．
- 設計含意：
  - 20〜30秒の取組は「短いゲームサイクル」の方策と一致する．取組の長さを延ばして持久戦にする変更は，疲労を意図的な挑戦として扱う別設計になり，安全面の再評価が必要になる．
  - センサ値はばらつく（ambiguity）．厳密な判定にこだわるより，多少のばらつきを「紙相撲らしい揺れ」として演出に吸収するほうが，指針に沿う【推論】．
  - 観客がいる状況での実施も想定する．ただし観客の前では無理に大きく振る可能性があるので，§1の振幅の飽和がいっそう重要になる【推論】．

### 8. Mueller et al. (2012) Balancing Exertion Experiences

- 書誌：Mueller, F., Vetere, F., Gibbs, M., Edge, D., Agamanolis, S., Sheridan, J., & Heer, J. (2012). Balancing exertion experiences. *Proceedings of CHI 2012*, 1853–1862. doi:10.1145/2207676.2208322
- URL：https://idl.cs.washington.edu/files/2012-BalancingExertionExperiences-CHI.pdf
- 主な知見：
  1. 体力の異なる2人が遠隔で一緒に走るJogging over a Distance（心拍数を空間音響の位置に反映）の利用者32名を分析し，バランス調整の設計空間を Measurement，Adjustment，Presentation，Control の4次元で整理した．
  2. 6つの設計戦術：Facilitate Empathy（相手の消耗を知覚させる），Represent Exertion in Low Resolution（細かい差を見えにくくする），Support Varying Levels of Exertion，Exploit a Shared Space，Negotiate Duration，Exploit Physical Risk．
  3. 低解像度の表示によって，参加者は細かい成績差にこだわらず，活動を社会的な体験として扱った．
  4. Exploit Physical Risk では，システムが不健康な消耗を検知して調整し，努力を減らすよう促す可能性を挙げている．
- 設計含意：
  - Measurement：生の加速度・角速度の大きさではなく，キャリブレーションで得た個人の基準に対する相対値を使う．首の可動域や筋力の差が勝敗に直結しなくなる．
  - Presentation：叩く強さを細かい数値で出さず，力士の揺れ方などの粗い表現にする．
  - Control：一定時間内のうなずき回数や振幅が上限を超えたら，入力をそれ以上増幅しない（または画面上で「力みすぎ」と表示する）安全装置を入れる【推論】．

### 9. Taheri, Weissman & Sra (2021) 運動障害者向けハンズフリーゲームコントローラ

- 書誌：Taheri, A., Weissman, Z., & Sra, M. (2021). Design and evaluation of a hands-free video game controller for individuals with motor impairments. *Frontiers in Computer Science*, 3, 751455. doi:10.3389/fcomp.2021.751455
- URL：https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2021.751455/full
- 主な知見：
  1. 表情認識とうなずき（頭部ピッチの回転角）をキー入力に変換するシステム．首を横に振る動作は誤検出が多いため，うなずきを採用した．うなずきの縦方向の可動範囲はユーザが設定でき，各自が試して決めるよう促した．
  2. フレームごとの判定では1秒に何度も入力が入り，Midas touch問題が起きた．連続5フレーム持続した場合だけ入力とみなす閾値で対処し，この閾値も個人ごとに変えられるべきだと結論した．
  3. 疲労を考慮して手順を2日に分けた．参加者からは「長時間だと疲れそう」「同じ表情の繰り返しで頬が疲れた」との声があった．
  4. GEQから各因子2項目ずつ計14項目を抜粋して使い，Flow と Challenge では2項目間の内的一貫性が低かった．
- 設計含意：
  - うなずき判定の振幅閾値，持続時間（またはデバウンス時間）を個人設定可能にし，取組前のキャリブレーションで決める．
  - 頻繁に使う操作ほど楽な動作に割り当てるという同論文の指摘から，本PoCは「叩く」の1動作に絞るのが妥当である．
  - GEQを抜粋して使う場合，1因子2項目では信頼性が下がりうる．抜粋するなら項目数を確保し，αを報告する．

### 10. Fouché (2017) ヘッドマウスの研究結果は健常者に一般化できるか

- 書誌：Fouché, R. C. (2017). Head mouse: Generalisability of research focused on the disabled to able bodied users. *Proceedings of SAICSIT '17*, Article 14, 1–10. doi:10.1145/3129416.3129442
- URL：https://doi.org/10.1145/3129416.3129442（抄録のみ確認）
- 主な知見：
  1. 頭部操作のカーソルで遊ぶシューティングゲームを作り，健常者を対象に，最適な感度設定，頸部疲労，学習性，満足度を2段階のユーザテストで調べた．
  2. 健常者では低感度設定が最も速いプレイにつながり，障害者を対象とした先行研究とは逆の結果だった．
  3. 頸部疲労は数セッションを重ねると減少し，効率と満足度は時間とともに向上した（学習効果）．
- 設計含意：
  - 初回は慣れていないので疲労が出やすい可能性がある．練習取組を1〜2本入れてから本番の取組を測定し，練習と本番を分けて記録する【推論】．
  - 感度の適正値は対象者によって変わりうるので，固定値を使わずキャリブレーションで決める．
  - 抄録のみの確認で，疲労の測り方や数値は確認できていない．

---

## D. 同室での対戦・社会的プレイと公平性

### 11. Gajadhar, de Kort & IJsselsteijn (2008) 共在する対戦相手は楽しさを増やす

- 書誌：Gajadhar, B. J., de Kort, Y. A. W., & IJsselsteijn, W. A. (2008). Shared fun is doubled fun: Player enjoyment as a function of social setting. In *Fun and Games 2008*, LNCS 5294, 106–117. doi:10.1007/978-3-540-88322-7_11
- URL：https://research.tue.nl/en/publications/shared-fun-is-doubled-fun-player-enjoyment-as-a-function-of-socia/（抄録のみ確認）
- 主な知見：
  1. 対戦相手がコンピュータ（virtual），遠隔の人（mediated），同室の人（co-located）の3条件で，GEQと攻撃性の状態尺度を比較した．
  2. 同室の相手がいると，楽しさ，挑戦，有能感が有意に高まった．
  3. 社会的文脈の効果は社会的存在感（social presence）の程度に媒介された．攻撃性は親しさと勝敗に影響された．
- 設計含意：
  - 2人を同室・並びで座らせ，相手の様子が見える状態で遊ばせることは，体験の質を上げる要因として理にかなう．
  - 評価ではGEQのSocial Presence Moduleの併用を検討する．
  - 勝敗と親しさが感情に影響するので，ペアの関係（友人同士か初対面か）と勝敗を記録し，分析の共変量にする．
  - 勝ちたい気持ちが強くなるほど動きが大きくなる可能性がある．§1の振幅の飽和はこの点でも必要になる【推論】．

### 12. 対称的な身体入力ゲームの公平性（§8の補足）

- 本PoCは左右対称の1対1対戦なので，ルール上は対称である．身体能力（首の可動域，筋力，反応速度）とセンサ個体差（AirPodsの機種差，装着状態）が非対称性の源になる．
- 文献上の対処：Mueller et al. (2012) の Measurement／Adjustment による個人差の吸収（§8）と，Taheri et al. (2021) の個人閾値の設定（§9）．
- 心拍数と成績に基づく競技型エクサーゲームのバランス調整（OzCHI 2017, doi:10.1145/3152771.3152794），身体能力のバランス調整が成績・体験・自尊感情に与える影響（Gerling et al., CHI 2014）は検索で存在を確認したが，本文を開いていない【未検証】．
- 設計含意【推論】：
  - 取組ごとに左右（座席）を入れ替える．
  - 入力値は「個人の基準振幅に対する比」に正規化する．正規化前後の値をログに残し，公平性の事後検証に使う．
  - 勝敗の偏り（同じペアで一方が勝ち続けるか）を記録する．

---

## E. 評価尺度

### 13. Law, Brühlmann & Mekler (2018) GEQの系統的レビューと妥当性検証

- 書誌：Law, E. L.-C., Brühlmann, F., & Mekler, E. D. (2018). Systematic review and validation of the Game Experience Questionnaire (GEQ) – Implications for citation and reporting practice. *Proceedings of CHI PLAY 2018*, 271–283. doi:10.1145/3242671.3242683
- URL：https://research.aalto.fi/en/publications/systematic-review-and-validation-of-the-game-experience-questionn/
- 主な知見：
  1. GEQは正式な査読付き論文として公刊されていない（"manuscript in preparation"）まま広く使われている．
  2. 73本の論文を調べると，心理測定的特性の報告が一貫せず，出典，使用理由，項目数の引用に誤解を招く慣行が多かった．
  3. N = 633の検証研究で，当初想定された7因子構造は支持されなかった．
- 設計含意：
  - GEQを使う場合は，版（core／social presence／post-game），使った項目，出典を正確に書き，下位尺度のαを自前データで報告する．
  - PoCの主要評価は，構造が比較的確かな尺度（IMIの興味・楽しさ，SSQ，NASA-TLX，SUS）に置き，GEQは補助的に使う案がある．
  - GEQ原版はTU/eのPDFで公開されている（https://pure.tue.nl/ws/files/21666907/Game_Experience_Questionnaire_English.pdf，検索結果の抜粋のみ確認）．

### 14. NASA-TLX と SUS（作業負荷とユーザビリティ）

- 書誌（NASA-TLX）：Hart, S. G., & Staveland, L. E. (1988). Development of NASA-TLX (Task Load Index): Results of empirical and theoretical research. In *Human Mental Workload* (Advances in Psychology, 52). doi:10.1016/S0166-4115(08)62386-9．手順書：NASA Ames Research Center, *NASA Task Load Index (NASA-TLX) v1.0 Paper and Pencil Package*.
- URL（手順書）：https://www.nasa.gov/wp-content/uploads/2026/03/nasa-tlx-v1-0-searchable-text-and-forms.pdf
- 書誌（SUS）：Bangor, A., Kortum, P., & Miller, J. (2009). Determining what individual SUS scores mean: Adding an adjective rating scale. *Journal of Usability Studies*, 4(3), 114–123．原典は Brooke, J. (1996). SUS: A "quick and dirty" usability scale【原典本文は未確認】．
- URL（SUS）：https://dl.acm.org/doi/10.5555/2835587.2835589（抄録）
- 主な知見：
  1. NASA-TLXは Mental，Physical，Temporal Demand，Performance，Effort，Frustration の6下位尺度で，0〜100（5刻み，20区間）で評定する．15組の一対比較で重みを求め，重みつき平均で総合負荷を出す．
  2. 手順書は，課題の後に評定を取り，同じ構造の課題群には1組の重みを使い回せると述べている．
  3. SUSは10項目で0〜100の得点を出す．Bangor et al. は約1,000件のSUS調査に7段階の形容詞尺度を加え，SUS得点との相関 r = 0.822 を得た．得点の解釈に使える．
- 設計含意：
  - 本PoCでは Physical Demand と Frustration の下位尺度が重要である．下位尺度ごとに報告する．
  - 取組が20〜30秒と短いので，取組ごとではなく条件（またはセッション）ごとに評定する．一対比較の重みづけは参加者の負担が大きいので，重みなしの平均（Raw TLX）を使う手もある【Raw TLXの妥当性の根拠文献は今回未確認】．
  - SUSはゲームの面白さではなくシステムの使いやすさ（装着，キャリブレーション，判定の分かりやすさ）を測るのに使う．

### 15. Intrinsic Motivation Inventory（IMI）

- 書誌：Center for Self-Determination Theory. *Intrinsic Motivation Inventory (IMI)*．原典は Ryan (1982)，Ryan, Mims & Koestner (1983) など．競技スポーツでの妥当性は McAuley, Duncan & Tammen (1989)．
- URL：https://selfdeterminationtheory.org/intrinsic-motivation-inventory
- 主な知見：
  1. 下位尺度は interest/enjoyment，perceived competence，effort，value/usefulness，pressure/tension，perceived choice の6つ，さらに妥当性が確立途上の relatedness．内発的動機づけそのものを測るのは interest/enjoyment 下位尺度である．
  2. 研究目的に合わせて下位尺度を選んで使うのが一般的で，下位尺度の取捨は他の下位尺度に影響しないとされる．研究者が自分のデータで因子分析をすることを推奨している．
  3. 自己報告と行動指標の相関は控えめ（約0.4）なので，解釈に注意が必要である．項目の文言は活動に合わせて言い換えてよい．
  4. 学術利用は許可されている．商用利用には許可が必要である．
- 設計含意：
  - interest/enjoyment，perceived competence，pressure/tension（必要なら effort）を選ぶ．relatedness は同室プレイと関係が深いが，妥当性が未確立であることを明記する．
  - 行動指標として「自由時間にもう1本やりたいと言ったか」などを併せて記録すると，IMIの解釈の助けになる【推論．IMIの説明にある自由選択行動の考え方に基づく】．

---

## F. 倫理（小規模な大学生対象テスト）

### 16. 電気通信大学における人を対象とする研究に関する倫理規程（令和7年3月31日最終改正）

- 書誌：国立大学法人電気通信大学「電気通信大学における人を対象とする研究に関する倫理規程」（平成18年9月6日規程第118号，最終改正 令和7年3月31日規程第47号）．あわせて「人を対象とする生命科学・医学系研究に関する倫理指針」（令和3年文部科学省・厚生労働省・経済産業省告示第1号）．
- URL：https://www.uec.ac.jp/about/basicinfo/rule/pdf/99998B013.pdf
- 主な知見：
  1. 研究責任者は研究計画書を作り，倫理委員会の意見を聴いたうえで学長の承認を受ける（第8条，第8条の2，第9条）．判定には「承認」「条件付承認」「変更の勧告」「不承認」と並んで「非該当」がある．
  2. 「侵襲を伴わない研究であって介入を行わないもの」は迅速審査の対象になりうる（第11条第1項第3号）．
  3. 教育の一環として行う学生実験では，実験計画届出書を学長に提出する（第8条第4項）．
  4. インフォームド・コンセントで説明すべき事項に，研究対象者のコンディションに関する条件とその理由，安全上の注意，中止の要求が可能であることとその方法，データの取扱いと公表方法，苦情の窓口などが列挙されている（第13条第2項）．研究への参加の強要は禁じられ，中止を求められたら安全を確保したうえで速やかに中止する（第14条第7項）．
- 設計含意：
  - 本PoCが規程上の「人を対象とする研究」に当たるか，授業の学生実験として届け出るか，「非該当」になりうるかは，筆者には判断できない．実施前に指導教員と人事労務課（問い合わせ先 human-ethics@office.uec.ac.jp は承認計画リストPDFの検索抜粋で見たもの【未検証】）に確認する．本調査ではこの判断は保留した．
  - 頭部の反復運動を求めるので，説明文書に「頸部に痛み・既往がある人，乗り物酔いしやすい人，当日体調の悪い人は参加を控える，または途中でいつでもやめられる」を明記する（第13条第2項第5号・第6号・第7号に対応）．
  - 同級生や同じ研究室の学生を募集すると断りにくさが生じる．募集と同意取得は直接の上下関係のない人が担当する，不参加でも成績・評価に影響しないことを書くなどの配慮をする【推論．第14条第7項の趣旨から】．
  - 記録するデータは頭部の姿勢・加速度ログ，アンケート，（撮るなら）映像である．映像を撮る場合は顔が映るので，撮影の有無，保管期間，廃棄方法を説明に含める（第13条第2項第11〜13号）．映像は必須でなければ撮らない．
  - 未成年（18歳未満）の参加者がいる場合は別表の手続きに従う．
  - 中止基準を事前に決める：SSQ相当の気分の悪さ，頸部痛，めまいを申告したら即中止し，回復まで見守る．
  - 機器の衛生：AirPodsを複数人で共用するならイヤーチップの交換・清拭を手順に入れる【推論．文献根拠なし】．

---

## G. 確認できなかったこと・限界

- 反復うなずきの累積的な頸部負荷について，短時間・低振幅（数十度未満）の条件で直接測った生体力学研究は見つけられなかった．§1の閾値はヘッドバンギングの理論モデルからの外挿である．
- 固定画面を見ながら入力としてうなずく条件（視点が頭に連動しない）での酔いを直接調べた研究は見つけられなかった．§3と§4からの推論である．
- AirPods（CMHeadphoneMotionManager）のセンサ精度，更新レート，遅延を検証した査読研究は今回確認できなかった．GitHub上の実装（約25 Hzとの記述）は検索結果の抜粋で見ただけである【未検証】．
- イヤラブル（eSense等）によるうなずき認識の研究（Min et al. 2018 WearSys，ICMI 2021など）は検索で存在を確認したが，本文を精読していない【未検証】．
- SSQ・GEQ・IMI・NASA-TLX・SUSの日本語版の出典と妥当性は今回確認していない．
- Gerling et al. (CHI 2014) の身体能力バランス調整研究は本文未確認【未検証】．

## 参照URL一覧（開いて確認したもの）

1. https://pmc.ncbi.nlm.nih.gov/articles/PMC2605616/
2. https://pubmed.ncbi.nlm.nih.gov/8358138/
3. https://nrc-publications.canada.ca/eng/view/accepted/?id=aa9661c8-91d9-49d9-bb47-a27a194e454f
4. https://www.frontiersin.org/journals/virtual-reality/articles/10.3389/frvir.2020.587698/full
5. https://cgvr.cs.uni-bremen.de/teaching/vr_literatur/ （Kennedy et al. 1993 SSQ PDF）
6. https://exertiongameslab.org/wp-content/uploads/2020/12/designing_sports_chi2011.pdf
7. https://exertiongameslab.org/wp-content/uploads/2011/07/movement_guidelines_chi2014.pdf
8. https://idl.cs.washington.edu/files/2012-BalancingExertionExperiences-CHI.pdf
9. https://www.frontiersin.org/journals/computer-science/articles/10.3389/fcomp.2021.751455/full
10. https://doi.org/10.1145/3129416.3129442
11. https://research.tue.nl/en/publications/shared-fun-is-doubled-fun-player-enjoyment-as-a-function-of-socia/
12. https://research.aalto.fi/en/publications/systematic-review-and-validation-of-the-game-experience-questionn/
13. https://www.nasa.gov/wp-content/uploads/2026/03/nasa-tlx-v1-0-searchable-text-and-forms.pdf
14. https://dl.acm.org/doi/10.5555/2835587.2835589
15. https://selfdeterminationtheory.org/intrinsic-motivation-inventory
16. https://www.uec.ac.jp/about/basicinfo/rule/pdf/99998B013.pdf
