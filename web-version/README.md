# Takoyaki Soul: Web Version 🐙

このディレクトリには、Takoyaki Soul の Web 版が含まれています。

## 概要

Unity/Android 版の機能を簡略化した、ブラウザで遊べる Web 版です。

### 主な機能

- **マウス/タッチ操作**: ジャイロの代わりにマウスドラッグで回転
- **シェイピング**: 一定の速度で回転して球体を成型
- **マスタリー**: 完璧な回転を維持すると「Zen Mastery」が上昇
- **トッピング**: ボタンでトッピングを追加
- **スコア計算**: 形状、焼き加減、マスタリーに基づいたスコア

## 技術スタック

- **HTML5 Canvas**: レンダリング
- **Vanilla JavaScript**: ゲームロジック (ライブラリ依存なし)
- **CSS3**: UI とアニメーション

## 遊び方

1. ブラウザで `index.html` を開く
2. "Start Cooking" ボタンをクリック
3. マウスを左右にドラッグして回転
   - 適切な速度 (青いフィードバック) を維持
   - Shaping Progress を 0% まで減らす
4. "Add Toppings" ボタンでトッピングを追加
5. 完成後、スコアを確認

## ローカル開発

```bash
# シンプルな HTTP サーバーで起動
python3 -m http.server 8000
# または
npx serve .
```

ブラウザで http://localhost:8000 にアクセス

## デプロイ

GitHub Pages で自動デプロイされます:
- リポジトリの Settings > Pages
- Source: GitHub Actions
- URL: https://furukawa1020.github.io/takoyakiapp/

## Unity 版との違い

| 機能 | Unity 版 | Web 版 |
|------|----------|--------|
| 物理演算 | Rust ネイティブ | JavaScript 簡易版 |
| 入力 | ジャイロ/加速度センサー | マウス/タッチ |
| グラフィック | 3D/シェーダー | 2D Canvas |
| プラットフォーム | Android | ブラウザ (全プラットフォーム) |
| ファイルサイズ | 大きい | 小さい (20KB 未満) |

## ライセンス

Unity 版と同じライセンスに従います。
