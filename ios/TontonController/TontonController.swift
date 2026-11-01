// TontonController.swift
// iPhone側：AirPodsのうなずきを「トン」イベントとして検出し，UDPでMac（Unity）へ送る．
// 必要なInfo.plistキー：NSMotionUsageDescription，NSLocalNetworkUsageDescription
// 任意：バンドルに ton.wav を入れるとトン検出時にAirPodsで鳴る．

import SwiftUI
import CoreMotion
import Network
import AVFoundation

// MARK: - App

@main
struct TontonControllerApp: App {
    var body: some Scene {
        WindowGroup { ContentView() }
    }
}

// MARK: - 送信イベント

struct TonEvent: Codable {
    let p: Int      // プレイヤーID（1 or 2）
    let seq: Int    // 連番（UDPの欠落・重複検出用）
    let t: Double   // 送信側の時刻（秒）．端末間の同期には使わず，間隔の参考用
    let s: Double   // 強さ 0...1（上限でクリップ済み）
}

// MARK: - UDP送信

final class UDPSender {
    private var connection: NWConnection?
    private let queue = DispatchQueue(label: "tonton.udp")

    func connect(host: String, port: UInt16) {
        connection?.cancel()
        guard let nwPort = NWEndpoint.Port(rawValue: port) else { return }
        let conn = NWConnection(host: NWEndpoint.Host(host), port: nwPort, using: .udp)
        conn.start(queue: queue)
        connection = conn
    }

    func send(_ data: Data) {
        connection?.send(content: data, completion: .contentProcessed { _ in })
    }

    func disconnect() {
        connection?.cancel()
        connection = nil
    }
}

// MARK: - うなずき検出

final class NodController: NSObject, ObservableObject, CMHeadphoneMotionManagerDelegate {
    enum Phase: String { case idle = "待機", calibrating = "キャリブレーション中", ready = "検出中" }

    @Published var phase: Phase = .idle
    @Published var headphonesConnected = false
    @Published var sensor = "-"
    @Published var lastStrength: Double = 0
    @Published var tonCount = 0
    @Published var status = ""

    var playerID = 1
    let sender = UDPSender()

    private let manager = CMHeadphoneMotionManager()
    private var player: AVAudioPlayer?

    // キャリブレーション
    private let calibDuration: TimeInterval = 4.0
    private var calibSamples: [(x: Double, y: Double, z: Double)] = []
    private var calibStart: TimeInterval?

    // 検出パラメータ（キャリブレーションで上書き）
    private var axis = 0               // 0:x 1:y 2:z
    private var sign: Double = 1
    private var threshold: Double = 1.5   // rad/s
    private var fullScale: Double = 3.0   // この角速度で強さ1．小さめのうなずきで届くようにする
    private let refractory: TimeInterval = 0.15
    private let maxTonsPerSecond = 6

    // 検出状態
    private var inPeak = false
    private var peak: Double = 0
    private var lastTonTime: TimeInterval = 0
    private var recentTons: [TimeInterval] = []
    private var seq = 0
    private var lastLocation: CMDeviceMotion.SensorLocation?

    override init() {
        super.init()
        manager.delegate = self
        setupSound()
    }

    // MARK: 開始・停止

    func start() {
        guard manager.isDeviceMotionAvailable else {
            status = "この端末はヘッドフォンモーションに対応していません"
            return
        }
        beginCalibration()
        manager.startDeviceMotionUpdates(to: .main) { [weak self] motion, error in
            guard let self else { return }
            if let error { self.status = "エラー: \(error.localizedDescription)"; return }
            guard let motion else { return }
            self.handle(motion)
        }
    }

    func stop() {
        manager.stopDeviceMotionUpdates()
        phase = .idle
    }

    func beginCalibration() {
        calibSamples.removeAll()
        calibStart = nil
        phase = .calibrating
        status = "\(Int(calibDuration))秒以内に，普段どおり小さく3回うなずいてください"
    }

    // MARK: モーション処理

    private func handle(_ m: CMDeviceMotion) {
        sensor = locationName(m.sensorLocation)

        // データ元のバッドが左右で切り替わったら取り直す
        if let last = lastLocation, last != m.sensorLocation, phase == .ready {
            beginCalibration()
            status = "センサーが切り替わりました．もう一度3回うなずいてください"
        }
        lastLocation = m.sensorLocation

        let r = m.rotationRate
        switch phase {
        case .calibrating:
            calibrate(r: r, now: m.timestamp)
        case .ready:
            detect(r: r, now: m.timestamp)
        case .idle:
            break
        }
    }

    private func calibrate(r: CMRotationRate, now: TimeInterval) {
        if calibStart == nil { calibStart = now }
        calibSamples.append((r.x, r.y, r.z))
        guard let start = calibStart, now - start >= calibDuration else { return }

        // うなずきが最も強く出た軸と向きを選ぶ
        var bestAxis = 0, bestAbs = 0.0, bestSign = 1.0
        for s in calibSamples {
            let v = [s.x, s.y, s.z]
            for i in 0..<3 where abs(v[i]) > bestAbs {
                bestAbs = abs(v[i]); bestAxis = i; bestSign = v[i] >= 0 ? 1 : -1
            }
        }
        guard bestAbs > 0.8 else {
            beginCalibration()
            status = "うなずきが検出できませんでした．もう一度お願いします"
            return
        }
        axis = bestAxis
        sign = bestSign
        threshold = max(0.8, bestAbs * 0.35)
        fullScale = max(threshold * 1.5, bestAbs * 0.7)
        phase = .ready
        status = "準備完了（軸\(["x", "y", "z"][axis])，閾値\(String(format: "%.2f", threshold)) rad/s）"
    }

    private func detect(r: CMRotationRate, now: TimeInterval) {
        let v = sign * [r.x, r.y, r.z][axis]
        if !inPeak {
            if v > threshold && now - lastTonTime > refractory {
                inPeak = true
                peak = v
            }
        } else {
            peak = max(peak, v)
            // 閾値の半分まで戻ったら1回のトンとして確定
            if v < threshold * 0.5 {
                inPeak = false
                emitTon(peak: peak, now: now)
            }
        }
    }

    private func emitTon(peak: Double, now: TimeInterval) {
        lastTonTime = now
        recentTons.removeAll { now - $0 > 1.0 }
        guard recentTons.count < maxTonsPerSecond else { return }   // 連打の上限
        recentTons.append(now)

        let strength = min(peak / fullScale, 1.0)
        lastStrength = strength
        tonCount += 1
        seq += 1
        playTon()

        let event = TonEvent(p: playerID, seq: seq, t: now, s: strength)
        if let data = try? JSONEncoder().encode(event) {
            sender.send(data)
        }
    }

    // MARK: 音

    private func setupSound() {
        try? AVAudioSession.sharedInstance().setCategory(.playback, options: [.mixWithOthers])
        try? AVAudioSession.sharedInstance().setActive(true)
        if let url = Bundle.main.url(forResource: "ton", withExtension: "wav") {
            player = try? AVAudioPlayer(contentsOf: url)
            player?.prepareToPlay()
        }
    }

    private func playTon() {
        guard let player else { return }
        player.currentTime = 0
        player.play()
    }

    // MARK: 接続状態

    func headphoneMotionManagerDidConnect(_ manager: CMHeadphoneMotionManager) {
        DispatchQueue.main.async { self.headphonesConnected = true }
    }

    func headphoneMotionManagerDidDisconnect(_ manager: CMHeadphoneMotionManager) {
        DispatchQueue.main.async {
            self.headphonesConnected = false
            self.status = "AirPodsが切断されました"
        }
    }

    private func locationName(_ l: CMDeviceMotion.SensorLocation) -> String {
        switch l {
        case .headphoneLeft: return "左"
        case .headphoneRight: return "右"
        default: return "既定"
        }
    }
}

// MARK: - 画面

struct ContentView: View {
    @StateObject private var nod = NodController()
    @State private var host = "192.168.0.10"
    @State private var port = "9000"
    @State private var playerID = 1

    var body: some View {
        Form {
            Section("送信先（Mac）") {
                TextField("IPアドレス", text: $host)
                    .keyboardType(.numbersAndPunctuation)
                TextField("ポート", text: $port)
                    .keyboardType(.numberPad)
                Picker("プレイヤー", selection: $playerID) {
                    Text("東（1）").tag(1)
                    Text("西（2）").tag(2)
                }
                .pickerStyle(.segmented)
                Button("接続") {
                    nod.playerID = playerID
                    nod.sender.connect(host: host, port: UInt16(port) ?? 9000)
                }
            }
            Section("うなずき") {
                Button(nod.phase == .idle ? "開始" : "キャリブレーションし直す") {
                    nod.playerID = playerID
                    if nod.phase == .idle { nod.start() } else { nod.beginCalibration() }
                }
                Button("停止", role: .destructive) { nod.stop() }
            }
            Section("状態") {
                LabeledContent("AirPods", value: nod.headphonesConnected ? "接続中" : "未接続")
                LabeledContent("センサー", value: nod.sensor)
                LabeledContent("フェーズ", value: nod.phase.rawValue)
                LabeledContent("トン回数", value: "\(nod.tonCount)")
                ProgressView(value: nod.lastStrength) { Text("直近の強さ") }
                Text(nod.status).font(.footnote)
            }
        }
    }
}
