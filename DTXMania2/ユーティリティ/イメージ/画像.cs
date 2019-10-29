using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace DTXMania2
{
    /// <summary>
    ///     D3Dテクスチャを使った画像表示。
    /// </summary>
    class 画像 : IImage, IDisposable
    {

        // プロパティ


        /// <summary>
        ///		0:透明～1:不透明
        /// </summary>
        public float 不透明度 { get; set; } = 1f;

        public bool 加算合成する { get; set; } = false;

        public Size2F サイズ { get; protected set; }

        public Size2F 実サイズ { get; protected set; }



        // 生成と終了


        /// <summary>
        ///     指定した画像ファイルから画像を作成する。
        /// </summary>
        public 画像( VariablePath 画像ファイルパス, BindFlags bindFlags = BindFlags.ShaderResource )
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

            this._CreateShaderResourceViewFromFile( Global.D3D11Device1, bindFlags, 画像ファイルパス );
        }

        /// <summary>
        ///     指定したサイズの、空の画像を作成する。
        /// </summary>
        public 画像( Size2F サイズ, BindFlags bindFlags = BindFlags.ShaderResource )
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            #region " 条件チェック "
            //----------------
            if( 0f >= サイズ.Width || 0f >= サイズ.Height )
            {
                Log.ERROR( $"テクスチャサイズが不正です。[{サイズ}]" );
                return;
            }
            //----------------
            #endregion

            this._CreateShaderResourceView( Global.D3D11Device1, bindFlags, サイズ );
        }

        public virtual void Dispose()
        {
            //using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ShaderResourceView?.Dispose();
            this.Texture?.Dispose();
        }



        // 進行と描画


        /// <summary>
        ///		画像を描画する。
        ///	</summary>
        public void 描画する( float 左位置, float 上位置, float 不透明度0to1 = 1.0f, float X方向拡大率 = 1.0f, float Y方向拡大率 = 1.0f, RectangleF? 転送元矩形 = null )
        {
            RectangleF srcRect = 転送元矩形 ?? new RectangleF( 0, 0, this.サイズ.Width, this.サイズ.Height );

            var 変換行列 =
                Matrix.Scaling( X方向拡大率, Y方向拡大率, 0f ) *
                Matrix.Translation(
                    Global.画面左上dpx.X + ( 左位置 + X方向拡大率 * srcRect.Width / 2f ),
                    Global.画面左上dpx.Y - ( 上位置 + Y方向拡大率 * srcRect.Height / 2f ),
                    0f );

            this.描画する( 変換行列, 不透明度0to1, 転送元矩形 );
        }

        /// <summary>
        ///		画像を描画する。
        ///	</summary>
        /// <param name="ワールド行列変換">画像は原寸（<see cref="サイズ"/>）にスケーリングされており、その後にこのワールド行列が適用される。</param>
        /// <param name="転送元矩形">テクスチャ座標(値域0～1)で指定する。</param>
        public void 描画する( Matrix ワールド行列変換, float 不透明度0to1 = 1f, RectangleF? 転送元矩形 = null )
        {
            if( this.Texture is null )
                return;

            var d3ddc = Global.D3D11Device1.ImmediateContext;

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
                Global.等倍3D平面描画用の変換行列を取得する( out Matrix 転置済みビュー行列, out Matrix 転置済み射影行列 );
                this._定数バッファの転送元データ.View = 転置済みビュー行列;
                this._定数バッファの転送元データ.Projection = 転置済み射影行列;

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
                d3ddc.Rasterizer.SetViewports( Global.既定のD3D11ViewPort );
                d3ddc.Rasterizer.State = _RasterizerState;

                // ピクセルシェーダ
                d3ddc.PixelShader.Set( _PixelShader );
                d3ddc.PixelShader.SetConstantBuffers( 0, _ConstantBuffer );
                d3ddc.PixelShader.SetShaderResources( 0, 1, this._ShaderResourceView );
                d3ddc.PixelShader.SetSamplers( 0, 1, _SamplerState );

                // 出力マージャ
                d3ddc.OutputMerger.SetTargets( Global.既定のD3D11DepthStencilView, Global.既定のD3D11RenderTargetView );
                d3ddc.OutputMerger.SetBlendState(
                    ( this.加算合成する ) ? _BlendState加算合成 : _BlendState通常合成,
                    new Color4( 0f, 0f, 0f, 0f ),
                    -1 );
                d3ddc.OutputMerger.SetDepthStencilState( Global.既定のD3D11DepthStencilState, 0 );
            }
            //----------------
            #endregion

            // 頂点バッファとインデックスバッファを使わずに 4 つの頂点を描画する。
            d3ddc.Draw( vertexCount: 4, startVertexLocation: 0 );
        }



        // ローカル


        protected Texture2D Texture = null!;

        private ShaderResourceView _ShaderResourceView = null!;

        private ST定数バッファの転送元データ _定数バッファの転送元データ;

        /// <summary>
        ///		画像ファイルからシェーダリソースビューを作成して返す。
        /// </summary>
        /// <remarks>
        ///		（参考: http://qiita.com/oguna/items/c516e09ee57d931892b6 ）
        /// </remarks>
        private void _CreateShaderResourceViewFromFile( Device d3dDevice, BindFlags bindFlags, VariablePath 画像ファイルパス )
        {
            using var image = new System.Drawing.Bitmap( 画像ファイルパス.変数なしパス );
            var 画像の矩形 = new System.Drawing.Rectangle( 0, 0, image.Width, image.Height );
            using var bitmap = image.Clone( 画像の矩形, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

            var ロック領域 = bitmap.LockBits( 画像の矩形, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat );
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
            bitmap.UnlockBits( ロック領域 );

            this._ShaderResourceView = new ShaderResourceView( d3dDevice, this.Texture );
            this.サイズ = new Size2F( 画像の矩形.Width, 画像の矩形.Height );
            this.実サイズ = new Size2F( this.Texture.Description.Width, this.Texture.Description.Height );
        }

        /// <summary>
        ///		空のテクスチャとそのシェーダーリソースビューを作成し、返す。
        /// </summary>
        private void _CreateShaderResourceView( Device d3dDevice, BindFlags bindFlags, Size2F サイズ )
        {
            var textureDesc = new Texture2DDescription() {
                ArraySize = 1,
                BindFlags = bindFlags,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Height = (int) サイズ.Height,
                Width = (int) サイズ.Width,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription( 1, 0 ),
                Usage = ResourceUsage.Default
            };
            this.Texture = new Texture2D( d3dDevice, textureDesc );
            this._ShaderResourceView = new ShaderResourceView( d3dDevice, this.Texture );
            this.サイズ = サイズ;
            this.実サイズ = new Size2F( this.Texture.Description.Width, this.Texture.Description.Height );
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


        public static void 全インスタンスで共有するリソースを作成する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            var d3dDevice = Global.D3D11Device1;

            #region " 頂点シェーダを生成する。"
            //----------------
            {
                var byteCode = File.ReadAllBytes( new VariablePath( @"$(Images)\TextureVS.cso" ).変数なしパス );
                _VertexShader = new VertexShader( d3dDevice, byteCode );
            }
            //----------------
            #endregion

            #region " ピクセルシェーダを生成する。"
            //----------------
            {
                var byteCode = File.ReadAllBytes( new VariablePath( @"$(Images)\TexturePS.cso" ).変数なしパス );
                _PixelShader = new PixelShader( d3dDevice, byteCode );
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
                _BlendState通常合成 = new BlendState( d3dDevice, BlendStateNorm );
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
                _BlendState加算合成 = new BlendState( d3dDevice, BlendStateAdd );
            }
            //----------------
            #endregion

            #region " ラスタライザステートを生成する。"
            //----------------
            {
                var RSDesc = new RasterizerStateDescription() {
                    FillMode = FillMode.Solid,   // 普通に描画する
                    CullMode = CullMode.None,    // 両面を描画する
                    IsFrontCounterClockwise = false,    // 時計回りが表面
                    DepthBias = 0,
                    DepthBiasClamp = 0,
                    SlopeScaledDepthBias = 0,
                    IsDepthClipEnabled = true,
                    IsScissorEnabled = false,
                    IsMultisampleEnabled = false,
                    IsAntialiasedLineEnabled = false,
                };

                _RasterizerState = new RasterizerState( d3dDevice, RSDesc );
            }
            //----------------
            #endregion

            #region " サンプラーステートを生成する。"
            //----------------
            {
                var descSampler = new SamplerStateDescription() {
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
                };

                _SamplerState = new SamplerState( d3dDevice, descSampler );
            }
            //----------------
            #endregion

            #region " 定数バッファを作成する。"
            //----------------
            _ConstantBuffer = new SharpDX.Direct3D11.Buffer(
                d3dDevice,
                new BufferDescription() {
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
