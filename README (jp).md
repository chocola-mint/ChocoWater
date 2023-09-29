# ChocoWater


https://github.com/chocola-mint/ChocoWater/assets/56677134/4f22b11e-9a71-483a-82d0-2b869f2847a8

**[WebGL デモアプリ](https://chocola-mint.itch.io/chocowater-demo)**

ChocoWaterはUnityのUniversal Rendering Pipeline (URP)向けの2.5D水面シミュレーションシステムです。**URP 2D Renderer**と**Universal Forward Renderer**二つのURPレンダラーで動作確認済みです。このパッケージはCompute Shader一切使わないので、ほぼどこでも動けると言えるでしょう。WebGLでも動けます。

パッケージ内のコードやシェーダーは全部英語でコメント付き、そしてツールチップも付いています。URPの機能の実装について興味がある方にも勉強になれるかなと思っています。

このパッケージはrucchoさんのビルトインレンダリングパイプライン向けの[WaterRW](https://github.com/ruccho/WaterRW/)パッケージと、Illham Effendiさんの[記事](https://ilhamhe.medium.com/dynamic-2d-water-in-unity-8d897852ee01)に触発されて、作られたものです。

## 要件

* Unity 2021.3.30f1で開発したものです。多分2021.3以上の全てのバージョンで動けます。
* [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.1/manual/index.html)パッケージも必要です。
    * URPのテンプレートからプロジェクトを作成した方が一番便利だと思います。
* 現時点ではターゲットプラットフォームは`R32F`フォーマットのテクスチャーをサポートしないとビルドは正しく動けません。

## インストール手順

ChocoWaterはgitパッケージとして提供されています。Unityの[パッケージマネージャー](https://docs.unity3d.com/Manual/upm-ui-giturl.html)を使って、`https://github.com/chocola-mint/ChocoWater.git`からインストールしてください。

インストールし終わったら、`ChocoWaterRenderFeature`というレンダーフィーチャーのスクリプトを`Renderer 2D`アセットに付けてください。それから`Post Transparent Layer Mask`のレイヤーを`Water`に変えてください。（このレイヤーマスクは「水オブジェクトのレイヤー」を含まないと正しく表示できません）
![image](https://github.com/chocola-mint/ChocoWater/assets/56677134/98c45379-2b7a-43e6-9716-320ba3926829)

最後に、`Prefab`フォルダーに置いてある`ChocoWater`のプレハブをシーンに配置してください。プレイモードに入ったら水の状態が確認できます。

## マテリアル設定

ChocoWaterは`Shader Graphs/Water Surface`シェーダーを使用します。*イタリック体*のプロパティーはスクリプトから自動的に設定されてるので、無視してください。

内蔵の`ChocoWater`プレハブはこのシェーダーを使ってるマテリアルを持っていますが、自分のマテリアルを分けて作るのがおすすめです。

| Property                        | Type        | Description                                                                   |
| ------------------------------- | ----------- | ----------------------------------------------------------------------------- |
| *DisplacementMap*               | `Texture2D `| 高さが1ピクセルの1Dテクスチャー。ワールドスペースの水面の起伏が入っています。`WaterVolume`コンポーネントで自動的に作られて、アップデートされます      
| *ObjectSize*                    | `Vector2`   | 水面のオブジェクトスペースでのサイズ。`WaterVolume`コンポーネントから自動的に設定されます
| WaterColor                      | `Color`     | 水のメインの色
| DepthColor                      | `Color`     | 乗算で深い部分の水を暗くする色
| SurfaceViewDepth                | `Float`     | ワールドスペースでの、水の上面の最大サイズ（奥行き）
| Wave Depth Propagation Ratio    | `Float`     | 水面の起伏がカメラに近い方の上面エッジに影響与える量。1にしたらどんな波でもリボンみたいな波に見えます
| Surface Foam Depth              | `Float`     | 水面の白い泡の深さ
| Surface Foam Wave Scale         | `Float`     | 水面の泡の変位を制御するパターンのスケール
| Surface Foam Wave Frequency     | `Float`     | 水面の泡の変位を制御するパターンの頻度（時間による）
| Surface Foam Wave Depth         | `Float`     | 水面の泡の変位を制御するパターンの最大値
| Ripple Distortion Intensity     | `Float`     | 水面の波紋の歪みの強さ
| Ripple Depth Propagation Ratio  | `Float`     | 1にすると波紋は水面の奥行きを全部カバーできるようになります。0にすると水面の奥の方のエッジでしか波紋が見えなくなります。推奨設定：0.75
| Underwater Flow Frequency        | `Float`     | 水面下のピクセルの歪みの頻度（時間による）
| Underwater Flow Intensity        | `Float`     | 水面下のピクセルの歪みの強さ（スクリーンスペース）
| Surface Flow Frequency           | `Float`     | 水面のピクセルの歪みの頻度（時間による）
| Surface Flow Intensity           | `Float`     | 水面のピクセルの歪みの強さ（スクリーンスペース）

## 技術情報

### レンダリング

まず、`WaterVolume`コンポーネントで、水のメッシュが作成されて、`renderResolution`プロパティーによって細分化されます。`WaterVolume`内部では水面のバネの情報が記録されて、FixedUpdateごとシミュレーションを行って、バネを移動させて、それらの起伏をフロート（`RFloat`）テクスチャーとしてGPUにアップロードします。このテクスチャーはシェーダーでバイリニアフィルタリングを通してサンプルされます。注意しておきたいところは、バネの数は水面の細分化の数と同じじゃなくてもいいです。

水を正確に表示されるため、`ChocoWaterRenderFeature`というレンダーフィーチャーを使用しています。このレンダーフィーチャーはURPのレンダラーのアウトプットをグローバルテクスチャー（`_CWScreenColor`）にコピーして、`Water Surface`シェーダーに提供します。コピーしてから`Post Transparent Layer Mask`のレイヤーに入っているオブジェクトを描画します。
* スプライトは透明マテリアルで描画されるため、URPカメラの内蔵の`Camera Opaque Texture`（不透明マテリアルで描画した画面）は使えません。

バーテックスシェーダーでは、上記の起伏が記録されてるテクスチャーを読み込んで、水面のバーテックスを正しい位置に移動します。

フラグメントシェーダーでは:
* **スクリーンスペース反射（Screen-Space Reflection）**は上記の`_CWScreenColor`で実装されて、水面を基準に反射を計算します。反射のピクセルが画面外にある場合はアーティファクトが発生するので、反射のリミットの近い部分は段々とフェードアウトします。リミットを超える部分は反射を映すじゃなくて、水の側面を表示します。リミットを操作して、水面の奥行きの長さを制御することも可能です。
* 水の流れによる**屈折**はノイズでUVに歪みを入れて、`_CWScreenColor`をサンプリングして実装されます。
* **波の泡**は水面との距離によって計算されます。水面より高い起伏に対して波紋を追加して、屈折を入れます。


### 物理シミュレーション

最初に書いた通り、`WaterVolume`は水面のバネのシミュレーションを制御しています。このシミュレーションは`FixedUpdate`ごとアップデートされて、そしてシミュレーションの速さは`Time.timeScale`でコントロールできます。バネは「起伏」と「速度」二つのプロパティーでシミュレートされるので、`WaterVolume`では二つのフロートのバッファーをアロケートします。

物理シミュレーションのステップごと(`WaterVolume.Step()`)：
* `WaterVolume`はまずバネの速度によって、バネを縦軸に移動させます。
* それから、バネの起伏と速度によって、速度をアップデートします。これでバネは「バネっぽく」感じられます。
* それで、全てのバネはとなりのバネの高低差によって、自分の速度を移します。これで水面の波は移動できるようになります。

`WaterVolume`では`WaterVolume.SurfaceImpact()`というメソッドが提供されています。これを使えば水面に力を与えることができます。もし水のシミュレーションは完全に予測できる場合、例えば「いつ、どこでオブジェクトが水面に落ちる」のが知っている場合、このメソッドを直接に呼び出せばいいです。シミュレーションのパラメーターを調整すれば水の性質を変えることもできますが、極端な値の組み合わせはシミュレーションを崩壊させる可能性もありますので、ツールチップをちゃんと読みながらパラメーターを調整してください。

`WaterTrigger`コンポーネントを使えば、自動的にUnityの2D物理システムに影響されて、Rigidbody2Dとの衝突で水面に波を発生させます。

`WaterTrigger`は`WaterVolume`の情報を読み込んで、浮力をシミュレートすることもできます。この機能はUnity内蔵のBuoyancyEffector2Dを使って実装されて、BuoyancyEffector2Dのフィールドを調整すれば水の流れの強さや水面下の速度の減衰なども変えられます。もう一つの機能として、`WaterTrigger`は波の近くに上向きのフォースを与えて、オブジェクトを波と共に上下と移動させます。

`WaterTrigger`は決して物理的に正確とは言えません。少なくともRigidbodyの水面下の体積を計算するのはリアルタイムではコスパがあまりにも悪すぎるので、折衷案として作られました。

## ライセンス

MITライセンスを使用しています。`ChocoWaterRenderFeature`は[DMeville's RefractedTransparentRenderPass](https://github.com/DMeville/RefractedTransparentRenderPass)を元に書かれていたので、ファイル内では原作者のライセンスをコメントで付いています。
