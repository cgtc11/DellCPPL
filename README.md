AfterEffectsの CPPL～CPPL 間にあるゴミのようなデータを削除するツール</BR>
<img width="839" height="507" alt="DellCPPL" src="https://github.com/user-attachments/assets/9ff85ad4-b4f7-4ad7-bb6a-d9930086c873" /></BR>
</BR>
<h1>DellCPPL – AEP/AEPX CPPl Remover</h1>

<p>After Effects プロジェクトから不要な CPPl 情報を除去するユーティリティ。<br>
対象拡張子は <code>.aep</code> と <code>.aepx</code>。出力は元ファイルと同じ場所に
<code>&lt;元名&gt;_DellCPPl.&lt;拡張子&gt;</code> を保存。元ファイルは変更しません。</p>

<h2>対応フォーマット</h2>
<ul>
  <li><strong>.aep</strong>（RIFF/RIFX バイナリ）
    <ul>
      <li><code>LIST(type=CPPl)</code> チャンクを検出して除去。</li>
      <li>見つからない場合は <em>パターン法</em>（<code>"CPPl"</code> の直前 <code>"LIST"</code> と後方 <code>"cpid"</code> を基準に短縮）を試行。</li>
    </ul>
  </li>
  <li><strong>.aepx</strong>（XML）
    <ul>
      <li><code>&lt;CPPl&gt;</code> と <code>&lt;CPPI&gt;</code> を大文字小文字無視で除去。</li>
      <li>UTF-8 / UTF-16LE/BE を自動判定し、保存時も維持。</li>
    </ul>
  </li>
</ul>

<h2>動作環境</h2>
<ul>
  <li>Windows 10/11</li>
  <li>.NET Framework（同梱ターゲット）</li>
</ul>

<h2>使い方</h2>

<h3>方法 A：EXE に直接ドラッグ＆ドロップ（推奨）</h3>
<ol>
  <li><code>DellCPPL.exe</code> に <code>.aep</code> / <code>.aepx</code> を 1 個以上ドラッグ。</li>
  <li>処理結果をダイアログで表示。出力は同フォルダに保存。</li>
</ol>

<pre><code>入力＝Sample.aep : 2,415,616 B
出力＝Sample_DellCPPl.aep : 2,104,320 B
</code></pre>

<p>除去対象が無い場合:</p>
<pre><code>入力＝Sample.aep : 2,415,616 B
出力＝処理不要（CPPl/CPPI なし）
</code></pre>

<h3>方法 B：アプリを起動してウィンドウにドロップ</h3>
<ol>
  <li><code>DellCPPL.exe</code> を起動。</li>
  <li>ウィンドウへファイルをドラッグ＆ドロップ（複数可）または「ファイル選択…」。</li>
  <li>画面下部ログに処理内容を表示。保存場所は方法 A と同じ。</li>
</ol>

<h2>出力仕様</h2>
<ul>
  <li>保存名：<code>&lt;元名&gt;_DellCPPl.&lt;拡張子&gt;</code></li>
  <li>保存場所：元ファイルと同じフォルダ</li>
  <li>サイズ表示：バイト（B）</li>
  <li>元ファイル：変更しない</li>
</ul>

<h2>内部処理の要点</h2>
<ul>
  <li><strong>.aep</strong>：RIFF/RIFX を再帰パースして <code>LIST(type=CPPl)</code> を削除。見つからない場合は
    <em>パターン法</em>（<code>CPPl</code> 直前の <code>LIST</code> と後方 <code>cpid</code> までを短縮し RIFF サイズ再計算）。</li>
  <li><strong>.aepx</strong>：正規表現で <code>&lt;CPPl&gt;</code> / <code>&lt;CPPI&gt;</code> を除去。BOM とエンコーディングを保持。</li>
</ul>

<h2>推奨フロー</h2>
<ol>
  <li>入力ファイルを用意（自動上書きはしない）。</li>
  <li>方法 A でドラッグ。</li>
  <li>結果ダイアログを確認し、生成された <code>*_DellCPPl.*</code> を使用。</li>
</ol>

<h2>制約</h2>
<ul>
  <li>破損したプロジェクトは非対応。</li>
  <li><code>CPPl</code> 文字列が存在しない独自レイアウトではパターン法が適用されない場合がある。</li>
  <li>.aepx 内でコメントや CDATA に埋め込まれた要素は対象外。</li>
</ul>

<h2>トラブルシュート</h2>
<ul>
  <li><strong>「処理不要」</strong>：対象に CPPl/CPPI が無い。保存形式や AE バージョンを確認。</li>
  <li><strong>出力が無い</strong>：書き込み不可の場所ではないか確認。</li>
  <li><strong>サイズが増えた</strong>：想定外。入力と出力の両方を検証すること。</li>
</ul>

<h2>FAQ</h2>
<ul>
  <li>複数同時処理：可。複数ファイルをまとめてドラッグ。</li>
  <li>フォルダドラッグ：不可。中のファイルを選んで渡す。</li>
  <li>上書き保存：現仕様は別名保存。要望で切替オプション追加は可能。</li>
</ul>

<h2>変更履歴</h2>
<ul>
  <li>v1.0 初版</li>
</ul>

<h2>免責</h2>
<p>本ツールの利用による不具合・損失については責任を負いません。実運用前に出力の検証を推奨します。</p>
