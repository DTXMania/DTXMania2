using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Animation;
using SharpDX.Direct2D1;
using SharpDX.Direct2D1.Effects;
using FDK;

namespace DTXMania2
{
    /// <summary>
    ///     ステージの背景として使う画像。
    /// </summary>
    /// <remarks>
    ///     ぼかし＆縮小アニメーションを適用したり、黒幕付き背景画像を表示したりすることもできる。
    /// </remarks>
    class 舞台画像 : IDisposable
    {

        // プロパティ


        public Size2F サイズ => this._背景画像.サイズ;

        public bool ぼかしと縮小を適用中 { get; protected set; } = false;



        // 生成と終了


        public 舞台画像( string? 背景画像ファイル名 = null, string? 背景黒幕付き画像ファイル名 = null )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._背景画像 = new 画像D2D( 背景画像ファイル名 ?? @"$(Images)\Background.jpg" );
            this._背景黒幕付き画像 = new 画像D2D( 背景黒幕付き画像ファイル名 ?? @"$(Images)\BackgroundWithDarkCurtain.jpg" );

            var dc = Global.GraphicResources.既定のD2D1DeviceContext;

            this._ガウスぼかしエフェクト = new GaussianBlur( dc );
            this._ガウスぼかしエフェクト黒幕付き用 = new GaussianBlur( dc );

            this._拡大エフェクト = new Scale( dc ) {
                CenterPoint = new Vector2(
                    Global.GraphicResources.設計画面サイズ.Width / 2.0f,
                    Global.GraphicResources.設計画面サイズ.Height / 2.0f ),
            };
            this._拡大エフェクト黒幕付き用 = new Scale( dc ) {
                CenterPoint = new Vector2( 
                    Global.GraphicResources.設計画面サイズ.Width / 2.0f, 
                    Global.GraphicResources.設計画面サイズ.Height / 2.0f ),
            };

            this._クリッピングエフェクト = new Crop( dc );
            this._クリッピングエフェクト黒幕付き用 = new Crop( dc );

            this._ぼかしと縮小割合 = new Variable( Global.Animation.Manager, initialValue: 0.0 );
            this.ぼかしと縮小を適用中 = false;

            this._ストーリーボード = null;

            this._初めての進行描画 = true;
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ストーリーボード?.Dispose();
            this._ぼかしと縮小割合?.Dispose();
            this._クリッピングエフェクト黒幕付き用.Dispose();
            this._クリッピングエフェクト.Dispose();
            this._拡大エフェクト黒幕付き用.Dispose();
            this._拡大エフェクト.Dispose();
            this._ガウスぼかしエフェクト黒幕付き用.Dispose();
            this._ガウスぼかしエフェクト.Dispose();
            this._背景黒幕付き画像.Dispose();
            this._背景画像.Dispose();
        }



        // 効果アニメーション


        public void ぼかしと縮小を適用する( double 完了までの最大時間sec = 1.0 )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // 既に適用中なら無視。
            if( this.ぼかしと縮小を適用中 )
                return;

            if( 0.0 == 完了までの最大時間sec )
            {
                // (A) アニメーションなしで即適用。
                this._ストーリーボード?.Dispose();
                this._ストーリーボード = null;
                this._ぼかしと縮小割合?.Dispose();
                this._ぼかしと縮小割合 = new Variable( Global.Animation.Manager, initialValue: 1.0 );
            }
            else
            {
                // (B) アニメーションを付けて徐々に適用。
                using( var 割合遷移 = Global.Animation.TrasitionLibrary.SmoothStop( 完了までの最大時間sec, finalValue: 1.0 ) )
                {
                    this._ストーリーボード?.Dispose();
                    this._ストーリーボード = new Storyboard( Global.Animation.Manager );
                    this._ストーリーボード.AddTransition( this._ぼかしと縮小割合, 割合遷移 );
                    this._ストーリーボード.Schedule( Global.Animation.Timer.Time ); // 今すぐアニメーション開始
                }
            }
            this.ぼかしと縮小を適用中 = true;
        }

        public void ぼかしと縮小を解除する( double 完了までの最大時間sec = 1.0 )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // 適用してないなら無視。
            if( !this.ぼかしと縮小を適用中 )
                return;

            if( 0.0 == 完了までの最大時間sec )
            {
                // (A) アニメーションなしで即適用。
                this._ストーリーボード?.Dispose();
                this._ストーリーボード = null;
                this._ぼかしと縮小割合?.Dispose();
                this._ぼかしと縮小割合 = new Variable( Global.Animation.Manager, initialValue: 0.0 );
            }
            else
            {
                // (B) アニメーションを付けて徐々に適用。
                using( var 割合遷移 = Global.Animation.TrasitionLibrary.SmoothStop( 完了までの最大時間sec, finalValue: 0.0 ) )
                {
                    this._ストーリーボード?.Dispose();
                    this._ストーリーボード = new Storyboard( Global.Animation.Manager );
                    this._ストーリーボード.AddTransition( this._ぼかしと縮小割合, 割合遷移 );
                    this._ストーリーボード.Schedule( Global.Animation.Timer.Time );    // 今すぐアニメーション開始
                }
            }
            this.ぼかしと縮小を適用中 = false;
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc, bool 黒幕付き = false, Vector4? 表示領域 = null, LayerParameters1? layerParameters1 = null )
        {
            if( this._初めての進行描画 )
            {
                #region " 背景画像と黒幕付き背景画像のそれぞれに対して、エフェクトチェーン（拡大 → ぼかし → クリッピング）を作成する。"
                //----------------
                this._拡大エフェクト.SetInput( 0, this._背景画像.Bitmap, true );
                this._ガウスぼかしエフェクト.SetInputEffect( 0, this._拡大エフェクト );
                this._クリッピングエフェクト.SetInputEffect( 0, this._ガウスぼかしエフェクト );

                this._拡大エフェクト黒幕付き用.SetInput( 0, this._背景黒幕付き画像.Bitmap, true );
                this._ガウスぼかしエフェクト黒幕付き用.SetInputEffect( 0, this._拡大エフェクト黒幕付き用 );
                this._クリッピングエフェクト黒幕付き用.SetInputEffect( 0, this._ガウスぼかしエフェクト黒幕付き用 );
                //----------------
                #endregion

                this._初めての進行描画 = false;
            }

            double 割合 = this._ぼかしと縮小割合?.Value ?? 0.0;

            var preBlend = dc.PrimitiveBlend;

            dc.PrimitiveBlend = PrimitiveBlend.SourceOver;

            if( 黒幕付き )
            {
                #region " (A) 黒幕付き背景画像を描画する。"
                //----------------
                this._拡大エフェクト黒幕付き用.ScaleAmount = new Vector2( (float) ( 1f + ( 1.0 - 割合 ) * 0.04 ) );    // 1.04 ～ 1
                this._ガウスぼかしエフェクト黒幕付き用.StandardDeviation = (float) ( 割合 * 10.0 );       // 0～10
                this._クリッピングエフェクト黒幕付き用.Rectangle = ( null != 表示領域 ) ? ( (Vector4) 表示領域 ) : new Vector4( 0f, 0f, this._背景黒幕付き画像.サイズ.Width, this._背景黒幕付き画像.サイズ.Height );

                if( layerParameters1.HasValue )
                {
                    // (A-a) レイヤーパラメータの指定あり
                    using( var layer = new Layer( dc ) )
                    {
                        dc.PushLayer( layerParameters1.Value, layer );
                        dc.DrawImage( this._クリッピングエフェクト黒幕付き用 );
                        dc.PopLayer();
                    }
                }
                else
                {
                    // (A-b) レイヤーパラメータの指定なし
                    dc.DrawImage( this._クリッピングエフェクト黒幕付き用 );
                }
                //----------------
                #endregion
            }
            else
            {
                #region " (B) 背景画像を描画する。"
                //----------------
                this._拡大エフェクト.ScaleAmount = new Vector2( (float) ( 1f + ( 1.0 - 割合 ) * 0.04 ) );    // 1.04 ～ 1
                this._ガウスぼかしエフェクト.StandardDeviation = (float) ( 割合 * 10.0 );       // 0～10
                this._クリッピングエフェクト.Rectangle = ( null != 表示領域 ) ? ( (Vector4) 表示領域 ) : new Vector4( 0f, 0f, this._背景画像.サイズ.Width, this._背景画像.サイズ.Height );

                if( layerParameters1.HasValue )
                {
                    // (B-a) レイヤーパラメータの指定あり
                    using( var layer = new Layer( dc ) )
                    {
                        dc.PushLayer( layerParameters1.Value, layer );
                        dc.DrawImage( this._クリッピングエフェクト );
                        dc.PopLayer();
                    }
                }
                else
                {
                    // (B-b) レイヤーパラメータの指定なし
                    dc.DrawImage( this._クリッピングエフェクト );
                }
                //----------------
                #endregion
            }

            dc.PrimitiveBlend = preBlend;
        }



        // ローカル


        private bool _初めての進行描画 = true;

        private readonly 画像D2D _背景画像;            // D2D Effect を使う

        private readonly 画像D2D _背景黒幕付き画像;    // D2D Effect を使う

        private readonly GaussianBlur _ガウスぼかしエフェクト;

        private readonly GaussianBlur _ガウスぼかしエフェクト黒幕付き用;

        private readonly Scale _拡大エフェクト;

        private readonly Scale _拡大エフェクト黒幕付き用;

        private readonly Crop _クリッピングエフェクト;

        private readonly Crop _クリッピングエフェクト黒幕付き用;

        /// <summary>
        ///		くっきり＆拡大: 0 ～ 1 :ぼかし＆縮小
        /// </summary>
        private Variable? _ぼかしと縮小割合;

        private Storyboard? _ストーリーボード = null;
    }
}
