﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace FDK
{
    /// <summary>
    ///     D3Dテクスチャを使った画像表示。
    /// </summary>
    public class 画像 : IImage, IDisposable
    {

        // プロパティ


        /// <summary>
        ///		0:透明～1:不透明
        /// </summary>
        public float 不透明度 { get; set; } = 1f;

        public bool 加算合成する { get; set; } = false;

        /// <summary>
        ///     画像の有効領域のサイズ。
        ///     有効領域は画像の左上を原点とし、大きさは<see cref="画像.実サイズ"/>と同じかそれより小さい。
        /// </summary>
        public Size2F サイズ { get; protected set; }

        /// <summary>
        ///     Direct3D デバイス内で実際に生成された画像のサイズ。
        ///     このサイズは、（あるなら）Direct3D デバイスの制約を受ける。
        /// </summary>
        public Size2F 実サイズ { get; protected set; }

        /// <summary>
        ///     テクスチャ。
        /// </summary>
        public Texture2D Texture { get; protected set; } = null!;

        /// <summary>
        ///     テクスチャのシェーダーリソースビュー。
        /// </summary>
        public ShaderResourceView ShaderResourceView { get; protected set; } = null!;



        // 生成と終了


        /// <summary>
        ///     指定した画像ファイルから画像を作成する。
        /// </summary>
        public 画像( Device1 d3dDevice1, VariablePath 画像ファイルパス, BindFlags bindFlags = BindFlags.ShaderResource )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            #region " 条件チェック "
            //----------------
            if( string.IsNullOrEmpty( 画像ファイルパス.変数なしパス ) )
            {
                Log.ERROR( $"画像ファイルパスの指定がありません。" );
                return;
            }
            if( !File.Exists( 画像ファイルパス.変数なしパス ) )
            {
                Log.ERROR( $"画像ファイルが存在しません。[{画像ファイルパス.変数付きパス}]" );
                return;
            }
            //----------------
            #endregion

            this._画像ファイルからシェーダリソースビューを作成して返す( d3dDevice1, bindFlags, 画像ファイルパス );
        }

        /// <summary>
        ///     指定したサイズの、空の画像を作成する。
        /// </summary>
        public 画像( Device1 d3dDevice1, Size2F サイズ, BindFlags bindFlags = BindFlags.ShaderResource )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            #region " 条件チェック "
            //----------------
            if( 0f >= サイズ.Width || 0f >= サイズ.Height )
            {
                Log.ERROR( $"テクスチャサイズが不正です。0 より大きい正の値を指定してください。[{サイズ}]" );
                return;
            }
            //----------------
            #endregion

            this._空のテクスチャとそのシェーダーリソースビューを作成して返す( d3dDevice1, bindFlags, サイズ );
        }

        public virtual void Dispose()
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this.ShaderResourceView?.Dispose();    // 画像の生成に失敗した場合、null のままである。
            this.Texture?.Dispose();                // 
        }



        // 進行と描画


        /// <summary>
        ///		画像を描画する。
        ///	</summary>
        public void 描画する(
            DeviceContext d3dDeviceContext,
            Size2F 設計画面サイズdpx,
            RawViewportF[] viewports,
            DepthStencilView depthStencilView,
            RenderTargetView renderTargetView,
            DepthStencilState depthStencilState,
            float 左位置,
            float 上位置,
            float 不透明度0to1 = 1.0f,
            float X方向拡大率 = 1.0f,
            float Y方向拡大率 = 1.0f,
            RectangleF? 転送元矩形 = null )
        {
            RectangleF srcRect = 転送元矩形 ?? new RectangleF( 0, 0, this.サイズ.Width, this.サイズ.Height );
            var 画面左上dpx = new Vector3( -設計画面サイズdpx.Width / 2f, +設計画面サイズdpx.Height / 2f, 0f );

            var 変換行列 =
                Matrix.Scaling( X方向拡大率, Y方向拡大率, 0f ) *
                Matrix.Translation(
                    画面左上dpx.X + ( 左位置 + X方向拡大率 * srcRect.Width / 2f ),
                    画面左上dpx.Y - ( 上位置 + Y方向拡大率 * srcRect.Height / 2f ),
                    0f );

            this.描画する( d3dDeviceContext, 設計画面サイズdpx, viewports, depthStencilView, renderTargetView, depthStencilState, 変換行列, 不透明度0to1, 転送元矩形 );
        }

        /// <summary>
        ///		画像を描画する。
        ///	</summary>
        /// <param name="ワールド行列変換">画像は原寸（<see cref="サイズ"/>）にスケーリングされており、その後にこのワールド行列が適用される。</param>
        /// <param name="転送元矩形">テクスチャ座標(値域0～1)で指定する。</param>
        public void 描画する(
            DeviceContext d3dDeviceContext,
            Size2F 設計画面サイズdpx,
            RawViewportF[] viewports,
            DepthStencilView depthStencilView,
            RenderTargetView renderTargetView,
            DepthStencilState depthStencilState,
            Matrix ワールド行列変換,
            float 不透明度0to1 = 1f,
            RectangleF? 転送元矩形 = null )
        {
            if( this.Texture is null )
                return;

            var d3ddc = d3dDeviceContext;

            this.不透明度 = MathUtil.Clamp( 不透明度0to1, 0f, 1f );
            var srcRect = 転送元矩形 ?? new RectangleF( 0f, 0f, this.サイズ.Width, this.サイズ.Height );

            #region " 定数バッファを更新する。"
            //----------------
            {
                // 1x1のモデルサイズをテクスチャの描画矩形サイズへスケーリングする行列を前方に乗じる。
                ワールド行列変換 =
                    Matrix.Scaling( srcRect.Width, srcRect.Height, 0f ) *
                    ワールド行列変換;

                // ワールド変換行列
                ワールド行列変換.Transpose();    // 転置
                this._定数バッファの転送元データ.World = ワールド行列変換;

                // ビュー変換行列と射影変換行列
                this._等倍3D平面描画用の変換行列を取得する( 設計画面サイズdpx, out Matrix ビュー行列, out Matrix 射影行列 );
                ビュー行列.Transpose();  // 転置（シェーダーにあわせる）
                射影行列.Transpose();    // 転置（シェーダーにあわせる）
                this._定数バッファの転送元データ.View = ビュー行列;
                this._定数バッファの転送元データ.Projection = 射影行列;

                // 描画元矩形（x,y,zは0～1で指定する（UV座標））
                this._定数バッファの転送元データ.TexLeft = srcRect.Left / this.サイズ.Width;
                this._定数バッファの転送元データ.TexTop = srcRect.Top / this.サイズ.Height;
                this._定数バッファの転送元データ.TexRight = srcRect.Right / this.サイズ.Width;
                this._定数バッファの転送元データ.TexBottom = srcRect.Bottom / this.サイズ.Height;

                // アルファ
                this._定数バッファの転送元データ.TexAlpha = this.不透明度;
                this._定数バッファの転送元データ.dummy1 = 0f;
                this._定数バッファの転送元データ.dummy2 = 0f;
                this._定数バッファの転送元データ.dummy3 = 0f;

                // 定数バッファへ書き込む。
                var dataBox = d3ddc.MapSubresource(
                    resourceRef: _ConstantBuffer,
                    subresource: 0,
                    mapType: MapMode.WriteDiscard,
                    mapFlags: MapFlags.None );
                SharpDX.Utilities.Write( dataBox.DataPointer, ref this._定数バッファの転送元データ );
                d3ddc.UnmapSubresource( _ConstantBuffer, 0 );
            }
            //----------------
            #endregion

            #region " 3Dパイプラインを設定する。"
            //----------------
            {
                // 入力アセンブラ
                d3ddc.InputAssembler.InputLayout = null;
                d3ddc.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

                // 頂点シェーダ
                d3ddc.VertexShader.Set( _VertexShader );
                d3ddc.VertexShader.SetConstantBuffers( 0, _ConstantBuffer );

                // ハルシェーダ
                d3ddc.HullShader.Set( null );

                // ドメインシェーダ
                d3ddc.DomainShader.Set( null );

                // ジオメトリシェーダ
                d3ddc.GeometryShader.Set( null );

                // ラスタライザ
                d3ddc.Rasterizer.SetViewports( viewports );
                d3ddc.Rasterizer.State = _RasterizerState;

                // ピクセルシェーダ
                d3ddc.PixelShader.Set( _PixelShader );
                d3ddc.PixelShader.SetConstantBuffers( 0, _ConstantBuffer );
                d3ddc.PixelShader.SetShaderResources( 0, 1, this.ShaderResourceView );
                d3ddc.PixelShader.SetSamplers( 0, 1, _SamplerState );

                // 出力マージャ
                d3ddc.OutputMerger.SetTargets( depthStencilView, renderTargetView );
                d3ddc.OutputMerger.SetBlendState(
                    ( this.加算合成する ) ? _BlendState加算合成 : _BlendState通常合成,
                    new Color4( 0f, 0f, 0f, 0f ),
                    -1 );
                d3ddc.OutputMerger.SetDepthStencilState( depthStencilState, 0 );
            }
            //----------------
            #endregion

            // 頂点バッファとインデックスバッファを使わずに 4 つの頂点を描画する。
            d3ddc.Draw( vertexCount: 4, startVertexLocation: 0 );
        }



        // ローカル


        private ST定数バッファの転送元データ _定数バッファの転送元データ;

        /// <seealso cref="http://qiita.com/oguna/items/c516e09ee57d931892b6"/>
        private void _画像ファイルからシェーダリソースビューを作成して返す( Device d3dDevice, BindFlags bindFlags, VariablePath 画像ファイルパス )
        {
            // ファイルからビットマップを生成し、Clone() でフォーマットを A8R8G8B8 に変換する。
            using var originalBitmap = new System.Drawing.Bitmap( 画像ファイルパス.変数なしパス );
            var 画像の矩形 = new System.Drawing.Rectangle( 0, 0, originalBitmap.Width, originalBitmap.Height );
            using var bitmap = originalBitmap.Clone( 画像の矩形, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

            // ビットマップからテクスチャを生成する。
            var ロック領域 = bitmap.LockBits( 画像の矩形, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat );
            try
            {
                var dataBox = new[] { new DataBox( ロック領域.Scan0, bitmap.Width * 4, bitmap.Height ) };
                var textureDesc = new Texture2DDescription() {
                    ArraySize = 1,
                    BindFlags = bindFlags,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    Height = bitmap.Height,
                    Width = bitmap.Width,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SharpDX.DXGI.SampleDescription( 1, 0 ),
                    Usage = ResourceUsage.Default,
                };
                this.Texture = new Texture2D( d3dDevice, textureDesc, dataBox );
            }
            finally
            {
                bitmap.UnlockBits( ロック領域 );
            }

            // テクスチャのシェーダーリソースビューを生成する。
            this.ShaderResourceView = new ShaderResourceView( d3dDevice, this.Texture );

            this.サイズ = new Size2F( 画像の矩形.Width, 画像の矩形.Height );
            this.実サイズ = new Size2F( this.Texture.Description.Width, this.Texture.Description.Height );
        }

        private void _空のテクスチャとそのシェーダーリソースビューを作成して返す( Device d3dDevice, BindFlags bindFlags, Size2F サイズ )
        {
            // 空のテクスチャを生成する。
            var textureDesc = new Texture2DDescription() {
                ArraySize = 1,
                BindFlags = bindFlags,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Height = (int)サイズ.Height,
                Width = (int)サイズ.Width,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription( 1, 0 ),
                Usage = ResourceUsage.Default
            };
            this.Texture = new Texture2D( d3dDevice, textureDesc );

            // テクスチャのシェーダーリソースビューを生成する。
            this.ShaderResourceView = new ShaderResourceView( d3dDevice, this.Texture );

            this.サイズ = サイズ;
            this.実サイズ = new Size2F( this.Texture.Description.Width, this.Texture.Description.Height );
        }

        /// <summary>
        ///     等倍3D平面描画用のビュー行列と射影行列を生成して返す。
        /// </summary>
        /// <remarks>
        ///     「等倍3D平面」とは、Z = 0 におけるビューポートサイズが <paramref name="設計画面サイズdpx"/> に一致する平面である。
        ///     例えば、設計画面サイズが 1024x720 の場合、等倍3D平面の表示可能な x, y の値域は (-512, -360)～(+512, +360) となる。
        ///     この平面を使うと、3Dモデルの配置やサイズ設定を設計画面サイズを基準に行うことができるようになる。
        ///     本メソッドは、等倍3D平面を実現するためのビュー行列と射影行列を返す。
        /// </remarks>
        public void _等倍3D平面描画用の変換行列を取得する( Size2F 設計画面サイズdpx, out Matrix ビュー行列, out Matrix 射影行列 )
        {
            const float 視野角deg = 45.0f;

            var dz = (float)( 設計画面サイズdpx.Height / ( 4.0 * Math.Tan( MathUtil.DegreesToRadians( 視野角deg / 2.0f ) ) ) );

            var カメラの位置 = new Vector3( 0f, 0f, -2f * dz );
            var カメラの注視点 = new Vector3( 0f, 0f, 0f );
            var カメラの上方向 = new Vector3( 0f, 1f, 0f );

            ビュー行列 = Matrix.LookAtLH( カメラの位置, カメラの注視点, カメラの上方向 );

            射影行列 = Matrix.PerspectiveFovLH(
                MathUtil.DegreesToRadians( 視野角deg ),
                設計画面サイズdpx.Width / 設計画面サイズdpx.Height, // アスペクト比
                -dz,                                                // 前方投影面までの距離
                +dz );                                              // 後方投影面までの距離
        }



        // 全インスタンス共通項目(static) 


#pragma warning disable 0649    // 「警告: ～は割り当てられません」
        private struct ST定数バッファの転送元データ
        {
            public Matrix World;      // ワールド変換行列
            public Matrix View;       // ビュー変換行列
            public Matrix Projection; // 透視変換行列

            public float TexLeft;   // 描画元矩形の左u座標(0～1)
            public float TexTop;    // 描画元矩形の上v座標(0～1)
            public float TexRight;  // 描画元矩形の右u座標(0～1)
            public float TexBottom; // 描画元矩形の下v座標(0～1)

            public float TexAlpha;  // テクスチャに乗じるアルファ値(0～1)
            public float dummy1;    // float4境界に合わせるためのダミー
            public float dummy2;    // float4境界に合わせるためのダミー
            public float dummy3;    // float4境界に合わせるためのダミー
        };
#pragma warning restore 0649

        private static VertexShader _VertexShader = null!;

        private static PixelShader _PixelShader = null!;

        private static BlendState _BlendState通常合成 = null!;

        private static BlendState _BlendState加算合成 = null!;

        private static RasterizerState _RasterizerState = null!;

        private static SamplerState _SamplerState = null!;

        private static SharpDX.Direct3D11.Buffer _ConstantBuffer = null!;


        public static void 全インスタンスで共有するリソースを作成する( Device1 d3dDevice1, VariablePath 頂点シェーダCSOパス, VariablePath ピクセルシェーダCSOパス )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            #region " 頂点シェーダを生成する。"
            //----------------
            {
                var byteCode = File.ReadAllBytes( 頂点シェーダCSOパス.変数なしパス );
                _VertexShader = new VertexShader( d3dDevice1, byteCode );
            }
            //----------------
            #endregion

            #region " ピクセルシェーダを生成する。"
            //----------------
            {
                var byteCode = File.ReadAllBytes( ピクセルシェーダCSOパス.変数なしパス );
                _PixelShader = new PixelShader( d3dDevice1, byteCode );
            }
            //----------------
            #endregion

            #region " ブレンドステート通常版を生成する。"
            //----------------
            {
                var BlendStateNorm = new BlendStateDescription() {
                    AlphaToCoverageEnable = false,  // アルファマスクで透過する（するならZバッファ必須）
                    IndependentBlendEnable = false, // 個別設定。false なら BendStateDescription.RenderTarget[0] だけが有効で、[1～7] は無視される。
                };
                BlendStateNorm.RenderTarget[ 0 ].IsBlendEnabled = true; // true ならブレンディングが有効。
                BlendStateNorm.RenderTarget[ 0 ].RenderTargetWriteMask = ColorWriteMaskFlags.All;        // RGBA の書き込みマスク。

                // アルファ値のブレンディング設定 ... 特になし
                BlendStateNorm.RenderTarget[ 0 ].SourceAlphaBlend = BlendOption.One;
                BlendStateNorm.RenderTarget[ 0 ].DestinationAlphaBlend = BlendOption.Zero;
                BlendStateNorm.RenderTarget[ 0 ].AlphaBlendOperation = BlendOperation.Add;

                // 色値のブレンディング設定 ... アルファ強度に応じた透明合成（テクスチャのアルファ値は、テクスチャのアルファ×ピクセルシェーダでの全体アルファとする（HLSL参照））
                BlendStateNorm.RenderTarget[ 0 ].SourceBlend = BlendOption.SourceAlpha;
                BlendStateNorm.RenderTarget[ 0 ].DestinationBlend = BlendOption.InverseSourceAlpha;
                BlendStateNorm.RenderTarget[ 0 ].BlendOperation = BlendOperation.Add;

                // ブレンドステートを作成する。
                _BlendState通常合成 = new BlendState( d3dDevice1, BlendStateNorm );
            }
            //----------------
            #endregion

            #region " ブレンドステート加算合成版を生成する。"
            //----------------
            {
                var BlendStateAdd = new BlendStateDescription() {
                    AlphaToCoverageEnable = false,  // アルファマスクで透過する（するならZバッファ必須）
                    IndependentBlendEnable = false, // 個別設定。false なら BendStateDescription.RenderTarget[0] だけが有効で、[1～7] は無視される。
                };
                BlendStateAdd.RenderTarget[ 0 ].IsBlendEnabled = true; // true ならブレンディングが有効。
                BlendStateAdd.RenderTarget[ 0 ].RenderTargetWriteMask = ColorWriteMaskFlags.All;        // RGBA の書き込みマスク。

                // アルファ値のブレンディング設定 ... 特になし
                BlendStateAdd.RenderTarget[ 0 ].SourceAlphaBlend = BlendOption.One;
                BlendStateAdd.RenderTarget[ 0 ].DestinationAlphaBlend = BlendOption.Zero;
                BlendStateAdd.RenderTarget[ 0 ].AlphaBlendOperation = BlendOperation.Add;

                // 色値のブレンディング設定 ... 加算合成
                BlendStateAdd.RenderTarget[ 0 ].SourceBlend = BlendOption.SourceAlpha;
                BlendStateAdd.RenderTarget[ 0 ].DestinationBlend = BlendOption.One;
                BlendStateAdd.RenderTarget[ 0 ].BlendOperation = BlendOperation.Add;

                // ブレンドステートを作成する。
                _BlendState加算合成 = new BlendState( d3dDevice1, BlendStateAdd );
            }
            //----------------
            #endregion

            #region " ラスタライザステートを生成する。"
            //----------------
            _RasterizerState = new RasterizerState( d3dDevice1, new RasterizerStateDescription() {
                FillMode = FillMode.Solid,      // 普通に描画する
                CullMode = CullMode.None,       // 両面を描画する
                IsFrontCounterClockwise = false,// 時計回りが表面
                DepthBias = 0,
                DepthBiasClamp = 0,
                SlopeScaledDepthBias = 0,
                IsDepthClipEnabled = true,
                IsScissorEnabled = false,
                IsMultisampleEnabled = false,
                IsAntialiasedLineEnabled = false,
            } );
            //----------------
            #endregion

            #region " サンプラーステートを生成する。"
            //----------------
            _SamplerState = new SamplerState( d3dDevice1, new SamplerStateDescription() {
                Filter = Filter.Anisotropic,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                MipLodBias = 0.0f,
                MaximumAnisotropy = 2,
                ComparisonFunction = Comparison.Never,
                BorderColor = new RawColor4( 0f, 0f, 0f, 0f ),
                MinimumLod = float.MinValue,
                MaximumLod = float.MaxValue,
            } );
            //----------------
            #endregion

            #region " 定数バッファを作成する。"
            //----------------
            _ConstantBuffer = new SharpDX.Direct3D11.Buffer( d3dDevice1, new BufferDescription() {
                Usage = ResourceUsage.Dynamic,              // 動的使用法
                BindFlags = BindFlags.ConstantBuffer,       // 定数バッファ
                CpuAccessFlags = CpuAccessFlags.Write,      // CPUから書き込む
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = SharpDX.Utilities.SizeOf<ST定数バッファの転送元データ>(),   // バッファサイズ
                StructureByteStride = 0,
            } );
            //----------------
            #endregion
        }

        public static void 全インスタンスで共有するリソースを解放する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            _ConstantBuffer.Dispose();
            _SamplerState.Dispose();
            _RasterizerState.Dispose();
            _BlendState加算合成.Dispose();
            _BlendState通常合成.Dispose();
            _PixelShader.Dispose();
            _VertexShader.Dispose();
        }
    }
}
