==============================================================
  こはねEX ベストタイム・スコア ビューア
  (KohaneTimeViewer)  バージョン 1.0
==============================================================

■ これは何?
  Steam版『ホッピングガールこはねEX』のセーブデータから、
  各ステージのランク・ベストタイム・ベストスコアを読み取って、
  一覧で表示するツールです。

  ・全ステージ(01-01 〜 10-10、CS-01 〜 CS-10)の記録を一覧表示
  ・ランク・ベストタイム・ベストスコアで並べ替え
  ・合計タイムの表示

  セーブデータは読み取るだけで、変更することはありません。


■ 動作環境
  ・Windows 10 / 11
  ・.NET Framework 4.8(Windows 10 / 11 には最初から入っています)
  ・Steam版『ホッピングガールこはねEX』


■ 使い方
  1. KohaneTimeViewer.exe をダブルクリックして起動します。
  2. パソコンに保存されているセーブデータを自動で読み込み、
     記録が表示されます。

  ・ゲームを遊んだあとは「再読み込み」ボタン(または F5 キー)で
    最新の記録に更新できます。
  ・列の見出し(ステージ・ランクなど)をクリックすると並べ替えできます。
    もう一度クリックすると逆順になります。
  ・別の場所にあるセーブデータ(save.sav)を見たいときは、
    「セーブを開く」ボタンを使うか、save.sav を画面にドラッグしてください。

  インストールは不要です。好きなフォルダに置いて使えます。
  使わなくなったら、フォルダごと削除してください。


■ よくある質問
  Q. 起動しても何も表示されません。
  A. このパソコンでゲームを遊んだことがない場合は、セーブデータが
     見つかりません。「セーブを開く」から save.sav を選んでください。

  Q. セーブデータの場所がわかりません。
  A. セーブデータのフォルダは、次の手順で開けます。
       1. エクスプローラー(フォルダの画面)を開きます。
       2. 上のアドレス欄をクリックし、次の1行をそのまま貼り付けて
          Enter キーを押します。

            %USERPROFILE%\AppData\LocalLow\D-O\Hopping Girl Kohane EX\Save

       3. 開いたフォルダの中にある「17桁の数字のフォルダ」に、
          save.sav が入っています。

     ※ エクスプローラーでは「Users」が「ユーザー」と表示されたり、
        「AppData」が隠しフォルダで表示されなかったりするため、
        フォルダを順番にたどるより、上の方法が確実です。

  Q. 「SteamIDの入力」という画面が出ました。
  A. 別のパソコンからコピーしたセーブデータなどを開くと表示されます。
     セーブデータが入っていた「17桁の数字のフォルダ名」を入力してください。
     一度入力すれば、次回からは自動で使われます。
     (入力した番号は、このツールと同じフォルダの steamid.txt に保存されます)

  Q. 表示されるタイムが、ゲーム内の表示と 0.01 秒ずれることがあります。
  A. 1/100 秒未満を切り捨てて表示しているため、まれに差が出る場合があります。


■ 注意事項
  ・本ツールは非公式のファンメイドツールです。
    開発元・販売元とは一切関係ありません。
    本ツールについて、開発元・販売元へのお問い合わせはご遠慮ください。
  ・本ツールの使用は自己責任でお願いします。
    本ツールの使用によって生じたいかなる損害についても、作者は責任を負いません。
  ・ゲームのアップデートにより、正しく表示できなくなる場合があります。


■ 権利表記
  『ホッピングガールこはねEX』および関連する名称・画像の著作権・商標権は、
  各権利者に帰属します。

  権利者様からご指摘があった場合は、速やかに公開を停止いたします。


■ 再配布について
  本ツールは MIT ライセンスで公開しています。
  下の「ライセンス」の文章(著作権表示を含む)を残せば、
  自由に再配布・改変できます。
  (ライセンスの対象は本ツールだけです。ゲーム本体には適用されません)


■ 作者・連絡先
  作者  : Nore139
  連絡先: https://x.com/Nore139


■ 更新履歴
  2026/10/17  バージョン 1.0 公開


■ ライセンス
MIT License

Copyright (c) 2026 Nore139

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
