# メタコードレビュー / Meta Code Review

> **方針**: 既存コードには一切変更を加えず、発見された問題点・改善案をここにまとめる。  
> **Policy**: No existing code is modified. All findings and recommendations are documented here only.

---

## 目次 / Table of Contents

1. [全体アーキテクチャ](#1-全体アーキテクチャ)
2. [Web版 (`web-version/`)](#2-web版-web-version)
3. [Rust ネイティブエンジン (`Takoyaki.Native`)](#3-rust-ネイティブエンジン-takoyakinative)
4. [C# コアロジック (`Takoyaki.Core`)](#4-c-コアロジック-takoyakicore)
5. [Unity C# スクリプト (`Assets/Scripts/`)](#5-unity-c-スクリプト-assetsscripts)
6. [Android ネイティブ層 (`Takoyaki.Android`)](#6-android-ネイティブ層-takoyakiandroid)
7. [テスト・CI/CD](#7-テストcicd)
8. [優先度まとめ](#8-優先度まとめ)

---

## 1. 全体アーキテクチャ

### 良い点 ✅
- **三層構造** (Rust 物理エンジン → C# コアロジック → Unity/Android プレゼンテーション) が明確に分離されており、各層の責務が明快。
- Rust エンジンが利用できない場合の **C# フォールバックパス** が `TakoyakiShapingLogic` に実装されており、堅牢性が高い。
- `web-version/` は依存ライブラリなしの純粋な HTML5/JS で、デプロイが容易。
- `TakoyakiStateMachine` と `ITakoyakiState` インターフェースにより、状態遷移ロジックがクリーンに分離されている。
- PID 制御理論を用いたゲームプレイ設計は独創的で教育的。

### 懸念点 ⚠️
- **スコア計算の不一致**: Rust パス (`calculate_score`)・Unity C# パス (`ScoreManager`)・Web 版 (`endGame()`) でスコア計算ロジックがそれぞれ独立して実装されており、プレイ結果が異なる可能性がある。単一の「スコア計算仕様」ドキュメントが存在しない。
- `IsPerfect` フラグは C# フォールバックパスでのみ更新されており、Rust パスが有効な場合は常に `false` のままになる（`TakoyakiShapingLogic.cs`）。
- **重複コメント**: `GameManager.cs` の `ChangeState()` 内に `// Calculate Score` が 2 行連続で存在する（L108-109）。

---

## 2. Web版 (`web-version/`)

### `game.js`

#### バグ・潜在的問題 🔴

| # | 場所 | 問題 | 説明 |
|---|------|------|------|
| 1 | `gameLoop()` (L387-395) | **`deltaTime` の精度** | `Date.now()` で差分を計算しているが、`requestAnimationFrame` のコールバック引数に渡される高精度タイムスタンプ (`DOMHighResTimeStamp`) を使う方がドリフトが少ない。 |
| 2 | `update()` (L243) | **毎フレームの DOM クエリ** | `document.getElementById('rotation-feedback')` がゲームループ内で毎フレーム実行されている。参照をコンストラクタでキャッシュすることでパフォーマンスが向上する。同様に `shaping-bar`, `cooking-bar`, `mastery-bar` も毎フレーム取得されている (L256-258)。 |
| 3 | `render()` (L291-294) | **RGB 値のクランプ不足** | `r + 20` / `g + 20` の計算が 255 を超える場合に `rgb(276, ...)` のような不正な値になる可能性がある。`Math.min(255, ...)` ガードを追加すること（L305 では実施済みだが L304 の `r+20, g+20` には未適用）。 |

#### 改善提案 🟡

| # | 場所 | 提案 |
|---|------|------|
| 4 | `setupEventListeners()` (L94, L102) | `touchstart`/`touchmove` に `{ passive: false }` オプションを明示すると、ブラウザへの意図が明確になる（`e.preventDefault()` を呼ぶのでパッシブにはできないが、警告抑制にもなる）。 |
| 5 | `initCanvas()` (L46) | `canvas.getContext('2d')` が `null` を返す可能性（古いブラウザや WebGL 専用コンテキストとの競合）を考慮し、null チェックを追加することが望ましい。 |
| 6 | `addToppings()` (L160-198) | トッピング追加のしきい値 (`shapeProgress < 80` など) がハードコードされている。定数として `this.TOPPING_THRESHOLDS` などに切り出すと可読性が向上する。 |
| 7 | 全体 | キーボード操作（矢印キーや Space キー）がサポートされていない。アクセシビリティ向上のため検討価値がある。 |

### `index.html`

- `<meta name="description">` は設定されており良好。
- ゲームの `<canvas>` に `aria-label` や `role="application"` 等のアクセシビリティ属性を付与すると、スクリーンリーダー対応が向上する。

---

## 3. Rust ネイティブエンジン (`Takoyaki.Native`)

### `src/lib.rs`

#### バグ・潜在的問題 🔴

| # | 場所 | 問題 | 説明 |
|---|------|------|------|
| 1 | `RhythmEngine::update()` (L112) | **ゼロ除算リスク** | `let der = (error - self.last_error) / dt;` において `dt == 0.0` の場合、`NaN` または `Inf` が生成され、以後の全計算が壊れる。`if dt <= 0.0 { return mag; }` などのガード処理が必要。 |
| 2 | `RhythmEngine::update()` | **PID 積分のワインドアップ** | `self.integral` に上限・下限のクランプがない。長時間の操作でインテグレーターが飽和し、制御が不安定になる可能性がある（アンチワインドアップ機構の追加を推奨）。 |
| 3 | `tako_step_physics()` (L278-292) | **負の `count` の UB** | 引数 `count: i32` が負の値の場合、`std::slice::from_raw_parts_mut(states, count as usize)` は非常に大きな `usize` となり未定義動作を引き起こす。冒頭で `if count <= 0 { return; }` を追加すること。 |
| 4 | 全 FFI 関数 | **ヌルポインターの非チェック** | `tako_update`, `tako_get_mastery` 等のほぼ全関数で `e: *mut RhythmEngine` のヌルチェックがない（`tako_free` は例外）。C# 側から `IntPtr.Zero` が渡された場合、即時クラッシュする。各関数の先頭に `if e.is_null() { return ...; }` を追加することを強く推奨。 |

#### 改善提案 🟡

| # | 提案 |
|---|------|
| 5 | `RhythmEngine` の各フィールドを公開するゲッター関数の命名が `tako_get_*` で統一されており良い。ただし `tako_get_pid_terms` のみ複数値を out ポインターで返す設計になっており、他のゲッターとインターフェースが不統一。 |
| 6 | `GamePhase` 列挙型の discriminant 値（`Raw=0, Cooking=1, Turned=2, Finished=3`）が C# の `rustPhase` 整数判定 (`rustPhase == 1`, `== 2`, `== 3`) に暗黙的に依存している。`#[repr(i32)]` で明示的に宣言することを推奨。 |

### `tests/integration_test.rs`

```rust
// 現状: コンパイル確認のみ
assert!(true, "Library compiled successfully");
```

- テストが事実上「何もテストしていない」。`tako_init` / `tako_update` / `tako_reset` / `tako_free` の基本的な動作確認テストを追加することを強く推奨。特にゼロ除算・NaN 伝播のシナリオはユニットテストで検証可能。

---

## 4. C# コアロジック (`Takoyaki.Core`)

### `TakoyakiStateMachine.cs`

#### バグ 🔴

| # | 場所 | 問題 |
|---|------|------|
| 1 | `StateRaw.Enter()` (L83-84) | `ball.CookLevel = 1.0f;` と `// Fully Cooked (Golden Brown!)` というコメントがある。**Raw（生）状態なのに完全に焼けた値** がセットされている。意図的な MVP 用暫定値であれば `// TODO: MVP placeholder - should be 0.0f` などのコメントで明示すること。現状では実際のゲームで Raw ペナルティが機能しない。 |

#### 改善提案 🟡

| # | 提案 |
|---|------|
| 2 | `TakoyakiStateMachine.Update(InputState, float, int rustPhase)` の `rustPhase` は `GamePhase` 列挙型で受け取るほうが型安全。現状は `int` の暗黙的な比較に依存している。 |

### `TakoyakiShapingLogic.cs`

#### バグ・潜在的問題 🔴

| # | 場所 | 問題 |
|---|------|------|
| 3 | `Dispose()` (L177-182) | ダブルディスポーズのガードがない。`Dispose()` を2回呼ぶと `tako_free` を2回実行し、ダングリングポインターを解放しようとする（UAF: Use-After-Free）。`_rustEngine = IntPtr.Zero` の代入は `tako_free` 呼び出し後に行う必要がある（現状は不要なく `if (_rustEngine != IntPtr.Zero)` でガードしているが、`Dispose()` 終了後に `_useRust` フラグも `false` に戻すべき）。 |

#### 改善提案 🟡

| # | 提案 |
|---|------|
| 4 | `IsPerfect` プロパティは C# フォールバックパスでのみ更新される。Rust パスが有効な場合は常に `false` のままであり、外部から参照した際に誤解を招く。Rust パスでの `harmony > 0.85f` 相当の判定を反映させるか、プロパティに使用条件をドキュメントコメントで明示すること。 |

### `SoftBodySolver.cs`

#### 改善提案 🟡

| # | 場所 | 提案 |
|---|------|------|
| 5 | `TriggerJiggle()` (L102-111) | `new Random()` を毎回生成している。`System.Random` はシード値として現在時刻を使用するため、短時間に複数回呼ばれると同じ乱数列が生成されてしまう。クラスフィールドとして `private readonly Random _rng = new Random();` を宣言し再利用すること。 |

### `ProceduralMesh.cs`

#### 改善提案 🟡

| # | 場所 | 提案 |
|---|------|------|
| 6 | `short[] Indices` (L12) | `short`（最大 32,767）を使用しているため、頂点数 32,768 以上のメッシュ（例: `GenerateSphere(256)` = 65,793 頂点）ではオーバーフローが発生する。`int[]` への変更、もしくは解像度の上限チェックを追加すること。 |
| 7 | `ToInterleavedArray()` (L17-35) | `Normals.Length` や `UVs.Length` が `Vertices.Length` と一致するかの検証がない。長さ不一致時に `IndexOutOfRangeException` が発生する。先頭にアサーションまたは例外スローを追加すること。 |

---

## 5. Unity C# スクリプト (`Assets/Scripts/`)

### `InputManager.cs`

#### バグ 🔴

| # | 場所 | 問題 |
|---|------|------|
| 1 | `UpdateInput()` (L96-111) | ジャイロ非対応デバイスの `else` ブロック内で加速度センサーの読み取りを **`SystemInfo.supportsGyroscope` で条件分岐している**。ジャイロと加速度計は独立したハードウェアであり、`SystemInfo.supportsAccelerometer` を使用すべき。ジャイロなしで加速度計ありのデバイスで加速度が読み取れない。 |

### `TakoyakiSoftBody.cs`

#### バグ 🔴

| # | 場所 | 問題 |
|---|------|------|
| 2 | `UpdatePhysics()` (L146) | `vel *= damping;` はフレームレートに依存した減衰計算になっている。60FPS なら `vel * 0.4` だが 30FPS では `vel * 0.4` の適用回数が半分になるため、物理挙動がフレームレートで変わる。`vel *= Mathf.Pow(damping, dt * 60f);` を使用して正規化すること。 |

#### 改善提案 🟡

| # | 提案 |
|---|------|
| 3 | `Update()` の catch-all で `this.enabled = false` にするのは良いフェイルセーフだが、本番では問題の原因を隠してしまう。開発ビルドと本番ビルドで挙動を変えることを検討（`#if UNITY_EDITOR` ブロックなど）。 |

### `States/PouringState.cs` / `CookingState.cs`

#### 改善提案 🟡

| # | 場所 | 提案 |
|---|------|------|
| 4 | `PouringState.Enter()` (L15) | `AudioManager.Instance.PlayPour()` に null チェックなし。`AudioManager.Instance?.PlayPour()` とすること。 |
| 5 | `CookingState.UpdateState()` (L43) | `AudioManager.Instance.StartSizzle(...)` に null チェックなし。毎フレーム呼ばれるため、Instance が null の場合 NullReferenceException が毎フレーム発生する。 |
| 6 | `CookingState.UpdateState()` (L46) | `Input.GetKeyDown(KeyCode.Space)` でターニングに遷移するロジックはモバイルゲームのプレースホルダーとして残っている。本番前に適切なタッチ操作への置き換えが必要。 |

### `Visuals/ToppingVisuals.cs`

#### 改善提案 🟡

| # | 提案 |
|---|------|
| 7 | `Start()` でオクトパス・ジンジャー・青のり・かつお節・マヨネーズの5つ全ての GameObject/ParticleSystem を作成している。実際に使用されないトッピングのオブジェクトが常に生成されるためメモリとパフォーマンスの無駄遣いになる。**遅延初期化（lazy initialization）** に変更することを推奨。 |
| 8 | `Shader.Find("Standard")` / `Shader.Find("Sprites/Default")` は、シェーダーがビルドに含まれない場合 `null` を返す。返り値のnullチェックと、null時のフォールバックシェーダーを用意すること。 |

### `Meta/ScoreManager.cs`

#### バグ 🔴

| # | 場所 | 問題 |
|---|------|------|
| 9 | `TotalScore` プロパティ (L14) / `CalculateScore()` (L21-69) | `TurnScore` フィールドはトッピングボーナス（0〜15 の整数）として流用されているが、`TotalScore = (ShapeScore + CookScore + TurnScore) / 3.0f * 100f` の式では `ShapeScore`/`CookScore` が 0〜1 の正規化値であるのに対し `TurnScore` は 0〜15 の非正規化値であるため、**計算結果が最大 566%** になりうる。スコアの上限チェック（100 への clamp）も存在しない。 |

### `Feedback/AudioManager.cs`

#### 改善提案 🟡

| # | 提案 |
|---|------|
| 10 | `GenerateProceduralSounds()` (L49) で `UnityEngine.Random.InitState(12345)` を呼び出している。これは Unity の**グローバル乱数状態**を固定シードで上書きするため、同フレームに `Random.value` を使用している他のシステムに影響を与える可能性がある。`System.Random` を使用した独立した乱数インスタンスで実装すること。 |

---

## 6. Android ネイティブ層 (`Takoyaki.Android`)

### `MainActivity.cs`

#### 改善提案 🟡

| # | 場所 | 提案 |
|---|------|------|
| 1 | `OnCreate()` (L104-125) | **デバッグボタン（流し込む・ひっくり返す・サーブ）が本番コードに残留している**。`BuildConfig.DEBUG` 相当のフラグで表示を制御するか、デバッグビルド専用のオーバーレイに分離すること。 |
| 2 | `OnCreate()` (L34) | `Log.Error("TakoyakiCrash", "STARTING ONCREATE")` というデバッグログが本番コードに存在する。ログレベルを `Log.Debug` にするか、デバッグビルド専用にすること。 |

### `TakoyakiRenderer.cs`

#### 改善提案 🟡

| # | 提案 |
|---|------|
| 3 | メッシュ生成で `ProceduralMesh.GenerateSphere(64)` を使用しており、頂点数は `65 * 33 = 2,145` 程度で `short` 範囲内に収まる。ただし `GenerateSphere(256)` に変更した場合にオーバーフローするため、`ProceduralMesh` 側の修正が推奨される（前述 #6 参照）。 |

---

## 7. テスト・CI/CD

### テストカバレッジ

| 層 | 現状 | 推奨 |
|----|------|------|
| Rust (`Takoyaki.Native`) | コンパイル確認のみ（1テスト） | `tako_init/update/reset/free` の基本動作・エッジケース（dt=0、負のcount）テストを追加 |
| C# Core (`Takoyaki.Core`) | テストなし | xUnit や NUnit で `PidController`・`TakoyakiShapingLogic`・`HeatSimulation`・`ProceduralMesh` の単体テストを追加 |
| Unity (`Assets/Scripts/`) | テストなし（Unity Test Framework 未使用） | `ScoreManager.CalculateScore()`・`CommentGenerator.GetComment()` などの純粋ロジックから始めると導入しやすい |
| Web (`web-version/`) | CI での構文チェックのみ | Jest 等を使い `TakoyakiGame.endGame()`・スコア計算のユニットテストを追加 |

### CI/CD 全般

- CI の基本構成（Rust CI・Unity CI・Web Deploy・統合 CI）は適切に整備されている。
- Web Deploy の JavaScript 構文チェック (`node --check game.js`) は有効。
- **推奨**: `web-version/game.js` に ESLint を追加して、コード品質（未使用変数、コールバック地獄等）の継続的チェックを実現する。

---

## 8. 優先度まとめ

### 🔴 高優先度（バグ・クラッシュリスク）

| # | ファイル | 問題 |
|---|---------|------|
| R-1 | `lib.rs` | `dt == 0` 時のゼロ除算 → NaN 伝播 |
| R-2 | `lib.rs` | FFI 関数のヌルポインター非チェック → セグフォルト |
| R-3 | `lib.rs` | 負の `count` による未定義動作 |
| R-4 | `TakoyakiStateMachine.cs` | `StateRaw.Enter()` で `CookLevel = 1.0f` → Raw ペナルティ無効 |
| R-5 | `ScoreManager.cs` | `TotalScore` の計算式が不正（最大 566%）・上限なし |
| R-6 | `InputManager.cs` | 加速度計の判定に `supportsGyroscope` を使用 |
| R-7 | `TakoyakiSoftBody.cs` | フレームレート依存の減衰計算 |

### 🟡 中優先度（品質・パフォーマンス）

| # | ファイル | 問題 |
|---|---------|------|
| Y-1 | `game.js` | 毎フレームの DOM クエリ（パフォーマンス） |
| Y-2 | `game.js` | `Date.now()` でのデルタタイム計算 |
| Y-3 | `lib.rs` | PID 積分ワインドアップ |
| Y-4 | `SoftBodySolver.cs` | `new Random()` を毎回生成 |
| Y-5 | `ProceduralMesh.cs` | `short` インデックスの上限オーバーフロー |
| Y-6 | `ToppingVisuals.cs` | `Start()` での全エフェクト即時生成 |
| Y-7 | `AudioManager.cs` | Unity グローバル乱数状態の上書き |
| Y-8 | `MainActivity.cs` | デバッグボタンの本番残留 |

### 🟢 低優先度（コード品質・ドキュメント）

| # | ファイル | 問題 |
|---|---------|------|
| G-1 | `TakoyakiShapingLogic.cs` | `IsPerfect` の Rust パスでの未更新 |
| G-2 | `TakoyakiShapingLogic.cs` | `Dispose()` のダブルディスポーズガード |
| G-3 | `GameManager.cs` | 重複コメント `// Calculate Score` |
| G-4 | `ProceduralMesh.cs` | `ToInterleavedArray()` の長さ検証なし |
| G-5 | `CookingState.cs` | キーボードによるプレースホルダー遷移 |
| G-6 | `lib.rs` | `GamePhase` の `repr(i32)` 明示 |
| G-7 | 全体 | スコア計算仕様の統一ドキュメント不在 |

---

*レビュー実施日: 2026-04-19*  
*対象ブランチ: `copilot/meta-code-review-documentation`*  
*レビュアー: GitHub Copilot*
