# CI/CD セットアップガイド

## 概要

このドキュメントでは、Takoyaki Soul プロジェクトの CI/CD パイプラインについて説明します。

## ワークフロー一覧

### 1. 統合 CI (`ci.yml`)

**トリガー:**
- すべてのブランチへの push
- すべての pull request

**実行内容:**
- リポジトリ構造の検証
- センシティブファイルのチェック
- コーディング規約の確認
- プロジェクトメトリクスの収集

**目的:**
リポジトリ全体の健全性を維持し、基本的な品質基準を満たしていることを確認します。

### 2. Rust CI (`rust-ci.yml`)

**トリガー:**
- `TakoyakiNative/Takoyaki.Native/**` の変更時
- main/master ブランチへの push
- pull request

**実行内容:**
1. **テストジョブ:**
   - コードフォーマット検証 (`cargo fmt`)
   - Clippy によるリンティング
   - ビルド
   - ユニットテスト実行

2. **Android ビルドジョブ:**
   - aarch64-linux-android (64-bit ARM)
   - armv7-linux-androideabi (32-bit ARM)
   - NDK を使用したクロスコンパイル
   - ビルド成果物 (`libtakores.so`) のアップロード

**目的:**
Rust ネイティブライブラリの品質を保証し、Android デバイス向けのビルドが正常に行えることを確認します。

### 3. Unity CI (`unity-ci.yml`)

**トリガー:**
- `Assets/**`, `ProjectSettings/**`, `Packages/**` の変更時
- main/master ブランチへの push
- pull request

**実行内容:**
- Unity プロジェクトバージョンの確認
- プロジェクト構造の検証
- C# スクリプトの存在確認
- ビルド要件の表示

**制限事項:**
- フル Unity ビルドには Unity ライセンスが必要
- 現在は検証のみを実施
- 完全なビルドには Unity Cloud Build または GameCI の使用を推奨

**目的:**
Unity プロジェクトの構造的な整合性を維持します。

### 4. Web デプロイ (`deploy-web.yml`)

**トリガー:**
- `web-version/**` の変更時
- main/master ブランチへの push
- 手動トリガー (workflow_dispatch)

**実行内容:**
1. **テストジョブ:**
   - HTML/JavaScript/CSS ファイルの存在確認
   - JavaScript 構文チェック
   - ファイルサイズの確認

2. **デプロイジョブ:**
   - GitHub Pages への自動デプロイ
   - デプロイ URL: https://furukawa1020.github.io/takoyakiapp/

**目的:**
Web 版の品質を保証し、GitHub Pages への自動デプロイを実現します。

## ステータスバッジ

README のトップにある各バッジで、ワークフローの状態を確認できます：

- [![CI](https://github.com/furukawa1020/takoyakiapp/actions/workflows/ci.yml/badge.svg)](https://github.com/furukawa1020/takoyakiapp/actions/workflows/ci.yml) - 統合 CI
- [![Rust CI](https://github.com/furukawa1020/takoyakiapp/actions/workflows/rust-ci.yml/badge.svg)](https://github.com/furukawa1020/takoyakiapp/actions/workflows/rust-ci.yml) - Rust ライブラリ
- [![Unity CI](https://github.com/furukawa1020/takoyakiapp/actions/workflows/unity-ci.yml/badge.svg)](https://github.com/furukawa1020/takoyakiapp/actions/workflows/unity-ci.yml) - Unity プロジェクト
- [![Deploy Web](https://github.com/furukawa1020/takoyakiapp/actions/workflows/deploy-web.yml/badge.svg)](https://github.com/furukawa1020/takoyakiapp/actions/workflows/deploy-web.yml) - Web デプロイ

## ベストプラクティス

### Pull Request ワークフロー

1. **新しいブランチを作成:**
   ```bash
   git checkout -b feature/new-feature
   ```

2. **変更を加えて commit:**
   ```bash
   git add .
   git commit -m "Add new feature"
   ```

3. **Push して PR を作成:**
   ```bash
   git push origin feature/new-feature
   ```

4. **CI が自動実行:**
   - すべてのチェックが緑色になることを確認
   - 必要に応じて修正

5. **レビュー後にマージ:**
   - main/master へマージ
   - Web 版の変更があれば自動デプロイ

### Rust 開発

**ローカルでの確認:**
```bash
cd TakoyakiNative/Takoyaki.Native

# フォーマット
cargo fmt

# リンティング
cargo clippy --all-targets --all-features -- -D warnings -A clippy::not_unsafe_ptr_arg_deref

# テスト
cargo test

# ビルド
cargo build --release
```

### Web 版開発

**ローカルテスト:**
```bash
cd web-version
python3 -m http.server 8000
# または
npx serve .
```

ブラウザで http://localhost:8000 にアクセス

## トラブルシューティング

### CI が失敗する場合

1. **Rust CI の失敗:**
   - フォーマットエラー: `cargo fmt` を実行
   - Clippy 警告: コードを修正またはワークフローで許可を追加
   - ビルドエラー: ローカルで `cargo build` を実行して確認

2. **Web デプロイの失敗:**
   - ファイルの存在確認
   - JavaScript 構文エラーをローカルで修正
   - `node --check game.js` で構文チェック

3. **Unity CI の失敗:**
   - プロジェクト構造を確認
   - 必須ディレクトリ (Assets, ProjectSettings, Packages) の存在確認

## 今後の拡張

### 検討事項

1. **Unity フルビルド:**
   - Unity Cloud Build の導入
   - GameCI の利用
   - セルフホストランナーでの Unity ライセンス管理

2. **Android APK 自動ビルド:**
   - Unity と Rust の統合ビルド
   - APK への署名
   - リリースへの自動アップロード

3. **自動リリース:**
   - タグベースのリリース作成
   - リリースノート自動生成
   - バージョン管理の自動化

4. **パフォーマンステスト:**
   - ベンチマークの追加
   - パフォーマンス回帰の検出

## 参考資料

- [GitHub Actions Documentation](https://docs.github.com/actions)
- [Rust CI Best Practices](https://doc.rust-lang.org/cargo/guide/continuous-integration.html)
- [GameCI - Unity CI/CD](https://game.ci/)
- [GitHub Pages Documentation](https://docs.github.com/pages)
