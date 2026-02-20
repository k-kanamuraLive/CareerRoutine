# CareerRoutine

Gmail 受信トレイから求人メールを自動取得し、自分のスキルにマッチした案件を毎日1件ピックアップする、キャリア管理支援 Windows デスクトップアプリです。

---

## 機能

- **今日の1件取得** — Gmail API で受信トレイを検索し、スキルキーワードにマッチする最新の求人メールを1件表示
- **スキルマッチング** — `C#` / `C++` / `VisualStudio` / `Windows` をキーワードに自動フィルタリング
- **カーソル管理** — 確認済みメールに `careerroutine.selected` ラベルを付与し、次回起動時に未確認件数を表示
- **Gmail 連携** — リンククリックでブラウザの Gmail を直接開く
- **差分取得** — 前回確認メール（カーソル）以降のメールのみを取得対象にすることで効率化

---

## 画面イメージ

```
┌─────────────────────────────────────────────┐
│ カーソル以降の新着: 12 件（最終確認: 2025/06/01 10:30） │
│                                             │
│ [今日の1件]  ████████████████░░  80%        │
│                                             │
│ ┌─────────────────────────────────────────┐ │
│ │ 件名: C# エンジニア募集 / 東京          │ │
│ │ ...本文...                              │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ 🔗 C# エンジニア募集 / 東京                  │
└─────────────────────────────────────────────┘
```

---

## 必要要件

- Windows 10 / 11
- .NET 8.0 以上
- Google アカウント（Gmail）
- Google Cloud Project（Gmail API 有効化済み）

---

## セットアップ

### 1. Google Cloud で認証情報を作成

1. [Google Cloud Console](https://console.cloud.google.com/) でプロジェクトを作成
2. **API とサービス** → **ライブラリ** から `Gmail API` を有効化
3. **API とサービス** → **認証情報** → **OAuth 2.0 クライアント ID** を作成
   - アプリケーションの種類: `デスクトップアプリ`
4. ダウンロードした JSON ファイルを `credentials.json` にリネーム

### 2. 配置

```
CareerRoutine.exe
credentials.json   ← ここに配置
```

### 3. 初回起動

アプリを起動するとブラウザで Google 認証画面が開きます。認証が完了すると `token.json` が生成され、以降は自動ログインになります。

---

## 使い方

1. アプリを起動 → 前回カーソル以降の新着件数が上部に表示される
2. **「今日の1件」** ボタンをクリック → スキルマッチした求人が本文エリアに表示される
3. リンクをクリック → ブラウザで Gmail が開き、そのメールに `careerroutine.selected` ラベルが自動付与される
4. 翌日また手順 2 から繰り返す

---

## プロジェクト構成

```
CareerRoutine/
├── Models/
│   └── Job.cs                  # メール1件を表すモデル
├── Services/
│   ├── GmailServiceFactory.cs  # Gmail API 認証・サービス生成
│   ├── GmailFetcher.cs         # メール取得・本文パース
│   ├── GmailLabelService.cs    # ラベル付与・カーソル管理
│   └── SkillMatcher.cs         # スキルキーワードによるフィルタリング
├── MainForm.cs                 # メイン画面
├── MainForm.Designer.cs
└── credentials.json            # ※ Git 管理外
```

---

## カスタマイズ

### スキルキーワードの変更

`Services/SkillMatcher.cs` の `IsSkillMatch` メソッドを編集します。

```csharp
private bool IsSkillMatch(Job job)
{
    string fullText = job.GetFullText();
    return Contains(fullText, "C#") ||
           Contains(fullText, "C++") ||
           Contains(fullText, "VisualStudio") ||
           Contains(fullText, "Windows");
           // ↑ ここにキーワードを追加・変更
}
```

---

## .gitignore 推奨設定

```gitignore
# Google 認証情報（絶対にコミットしない）
credentials.json
token.json/

# ビルド成果物
bin/
obj/
```

---

## 使用ライブラリ

| ライブラリ | 用途 |
|---|---|
| [Google.Apis.Gmail.v1](https://www.nuget.org/packages/Google.Apis.Gmail.v1) | Gmail API クライアント |
| [HtmlAgilityPack](https://www.nuget.org/packages/HtmlAgilityPack) | HTML メール本文のテキスト変換 |

---

## ライセンス

MIT
