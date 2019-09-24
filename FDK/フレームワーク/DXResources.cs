using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SharpDX;

namespace FDK
{
    /// <summary>
    ///     グラフィック関連のリソース。
    /// </summary>
    public class DXResources : IDisposable
    {

        // static


        public static DXResources Instance { get; protected set; } = null;

        public static void CreateInstance( IntPtr hWindow, Size 設計画面サイズ, Size 物理画面サイズ )
        {
            if( null != Instance )
                throw new Exception( "インスタンスはすでに生成済みです。" );

            Instance = new DXResources( 設計画面サイズ, 物理画面サイズ, hWindow, hWindow );
        }

        public static void ReleaseInstance()
        {
            Instance?.Dispose();
            Instance = null;
        }



        // プロパティ


        /// <summary>
        ///     設計時に想定した画面サイズ[dpx]。
        /// </summary>
        /// <remarks>
        ///     物理画面サイズはユーザが自由に変更できるが、プログラム側では常に設計画面サイズを使うことで、
        ///     物理画面サイズに依存しないデザインがコーディングできる。
        ///     プログラム内では設計画面におけるピクセルの単位として「dpx」と称することがある。
        ///     なお、int より float での利用が多いので、Size や Size2 ではなく Size2F を使う。
        ///     （int 同士ということを忘れて、割り算しておかしくなるケースも多発したので。）
        /// </remarks>
        public Size2F 設計画面サイズ { get; protected set; } = new Size2F( 1920f, 1080f );

        /// <summary>
        ///     画面に実際に表示される画面のサイズ[px]。
        /// </summary>
        /// <remarks>
        ///     物理画面サイズは、表示先コントロールのクライアントサイズ（＝スワップチェーンのサイズ）を表す。
        ///     物理画面サイズは、ユーザが自由に変更することができるという点に留意すること。
        ///     プログラム内では物理画面におけるピクセルの単位として「px」と称することがある。
        ///     なお、int より float での利用が多いので、Size や Size2 ではなく Size2F を使う。
        ///     （int 同士ということを忘れて、割り算しておかしくなるケースも多発したので。）
        /// </remarks>
        public Size2F 物理画面サイズ { get; protected set; } = new Size2F( 1024f, 576f );

        // 設計-物理サイズ間の変換

        public float 拡大率DPXtoPX横 => ( this.物理画面サイズ.Width / this.設計画面サイズ.Width );
        public float 拡大率DPXtoPX縦 => ( this.物理画面サイズ.Height / this.設計画面サイズ.Height );
        public float 拡大率PXtoDPX横 => ( this.設計画面サイズ.Width / this.物理画面サイズ.Width );
        public float 拡大率PXtoDPX縦 => ( this.設計画面サイズ.Height / this.物理画面サイズ.Height );
        public Matrix3x2 拡大行列DPXtoPX => Matrix3x2.Scaling( this.拡大率DPXtoPX横, this.拡大率DPXtoPX縦 );
        public Matrix3x2 拡大行列PXtoDPX => Matrix3x2.Scaling( this.拡大率PXtoDPX横, this.拡大率PXtoDPX縦 );

        /// <summary>
        ///     等倍3D平面での画面左上の3D座標。
        /// </summary>
        /// <remarks>
        ///     等倍3D平面については <see cref="等倍3D平面描画用の変換行列を取得する(out Matrix, out Matrix)"/> を参照。
        /// </remarks>
        public Vector3 画面左上dpx => new Vector3( -this.設計画面サイズ.Width / 2f, +this.設計画面サイズ.Height / 2f, 0f );

        /// <summary>
        ///		現在時刻から、DirectComposition Engine による次のフレーム表示時刻までの間隔[秒]を返す。
        /// </summary>
        /// <remarks>
        ///		この時刻の仕様と使い方については、以下を参照。
        ///		Architecture and components - MSDN
        ///		https://msdn.microsoft.com/en-us/library/windows/desktop/hh437350.aspx
        /// </remarks>
        public double 次のDComp表示までの残り時間sec
        {
            get
            {
                var fs = this.DCompDevice2.FrameStatistics;
                return ( fs.NextEstimatedFrameTime - fs.CurrentTime ) / fs.TimeFrequency;
            }
        }

        
        
        // プロパティ； スワップチェーンに依存しないグラフィックリソース


        public SharpDX.Direct3D11.Device1 D3D11Device1 { get; protected set; }

        public SharpDX.DXGI.SwapChain1 DXGISwapChain1 { get; protected set; }

        public SharpDX.DXGI.Output1 DXGIOutput1 { get; protected set; }

        public SharpDX.MediaFoundation.DXGIDeviceManager MFDXGIDeviceManager { get; protected set; }

        public SharpDX.Direct2D1.Factory1 D2D1Factory1 { get; protected set; }

        public SharpDX.Direct2D1.Device D2D1Device { get; protected set; }

        public SharpDX.Direct2D1.DeviceContext 既定のD2D1DeviceContext { get; protected set; }

        public SharpDX.DirectComposition.DesktopDevice DCompDevice2 { get; private set; }    // IDCompositionDevice2 から派生

        public SharpDX.DirectComposition.Visual2 DCompVisual2ForSwapChain { get; private set; }

        public SharpDX.DirectComposition.Target DCompTarget { get; private set; } = null;

        public SharpDX.WIC.ImagingFactory2 WicImagingFactory2 { get; private set; } = null;

        public SharpDX.DirectWrite.Factory DWriteFactory { get; private set; } = null;

        public Animation Animation { get; private set; } = null;



        // プロパティ； スワップチェーンに依存するグラフィックリソース


        /// <summary>
        ///     スワップチェーンのバックバッファとメモリを共有するレンダービットマップ。
        /// </summary>
        public SharpDX.Direct2D1.Bitmap1 既定のD2D1RenderBitmap1 { get; private set; }

        /// <summary>
        ///     スワップチェーンのバックバッファに対する既定のレンダーターゲットビュー。
        /// </summary>
        public SharpDX.Direct3D11.RenderTargetView 既定のD3D11RenderTargetView { get; private set; }

        /// <summary>
        ///     スワップチェーンのバックバッファに対する既定の深度ステンシル。
        /// </summary>
        public SharpDX.Direct3D11.Texture2D 既定のD3D11DepthStencil { get; private set; } = null;

        /// <summary>
        ///     スワップチェーンのバックバッファに対する既定の深度ステンシルビュー。
        /// </summary>
        public SharpDX.Direct3D11.DepthStencilView 既定のD3D11DepthStencilView { get; private set; }

        /// <summary>
        ///     スワップチェーンのバックバッファに対する既定の深度ステンシルステート。
        /// </summary>
        public SharpDX.Direct3D11.DepthStencilState 既定のD3D11DepthStencilState { get; private set; }

        /// <summary>
        ///     スワップチェーンのバックバッファに対する既定のビューポートの配列。
        /// </summary>
        public SharpDX.Mathematics.Interop.RawViewportF[] 既定のD3D11ViewPort { get; private set; }


        
        // 生成と終了


        protected DXResources( Size 物理画面サイズ, Size 設計画面サイズ, IntPtr hWindow, IntPtr hControl )
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this.物理画面サイズ = new Size2F( 物理画面サイズ.Width, 物理画面サイズ.Height );
                this.設計画面サイズ = new Size2F( 設計画面サイズ.Width, 設計画面サイズ.Height );

                this.hWindow = hWindow;
                this.hControl = hControl;

                this._スワップチェーンに依存しないグラフィックリソースを作成する();
                this._スワップチェーンを作成する();
                this._スワップチェーンに依存するグラフィックリソースを作成する();
            }
        }

        public void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._スワップチェーンに依存するグラフィックリソースを解放する();
                this._スワップチェーンを解放する();
                this._スワップチェーンに依存しないグラフィックリソースを解放する();

                this.hControl = IntPtr.Zero;
                this.hWindow = IntPtr.Zero;
            }
        }

        /// <summary>
        ///     DirectComposition のターゲットとなるウィンドウハンドル。
        /// </summary>
        protected IntPtr hWindow;

        /// <summary>
        ///     スワップチェーンが作成される描画先コントロールのハンドル。
        /// </summary>
        protected IntPtr hControl;



        // サイズ変更


        /// <summary>
        ///		物理画面サイズ（スワップチェーンのバックバッファのサイズ）を変更する。
        /// </summary>
        /// <param name="newSize">新しいサイズ。</param>
        /// <remarks>
        ///     スワップチェーンを ResizeBuffer() する前に、バックバッファに依存するリソースをすべて解放しなければならない。
        ///     また、ResizeBuffer() の後には、バックバッファまたはそのサイズに依存するリソースをすべて再構築しなければならない。
        /// </remarks>
        public void 物理画面サイズを変更する( Size newSize )
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                // (1) 依存リソースを解放。
                this._スワップチェーンに依存するグラフィックリソースを解放する();

                // (2) バックバッファのサイズを変更。
                this.DXGISwapChain1.ResizeBuffers(
                    0,                                  // 現在のバッファ数を維持
                    newSize.Width,                      // 新しいサイズ
                    newSize.Height,                     //
                    SharpDX.DXGI.Format.Unknown,        // 現在のフォーマットを維持
                    SharpDX.DXGI.SwapChainFlags.None );

                this.物理画面サイズ = new Size2F( newSize.Width, newSize.Height );

                // (3) 依存リソースを作成。
                this._スワップチェーンに依存するグラフィックリソースを作成する();
            }
        }

        private void _スワップチェーンに依存しないグラフィックリソースを作成する()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                // MediaFoundation をセットアップする。
                SharpDX.MediaFoundation.MediaManager.Startup();

                // D3D11デバイスを生成する。
                using( var d3dDevice = new SharpDX.Direct3D11.Device(
                    SharpDX.Direct3D.DriverType.Hardware,
#if DEBUG
                    // D3D11 Debugメッセージは、Visual Studio のプロジェクトプロパティで「ネイティブコードのデバッグを有効にする」を ON にしないと表示されない。
                    // なお、「ネイティブコードのデバッグを有効にする」を有効にしてアプリケーションを実行すると、速度が恐ろしく低下する。
                    SharpDX.Direct3D11.DeviceCreationFlags.Debug |
#endif
                    SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport, // D2Dで必須
                    new SharpDX.Direct3D.FeatureLevel[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 } ) )    // D3D11.1 を使うが機能レベルは 11_0 でいい
                {
                    // ID3D11Device1 を取得する。（Windows8.1 以降のPCで実装されている。）
                    this.D3D11Device1 = d3dDevice.QueryInterface<SharpDX.Direct3D11.Device1>();
                }

                // D3D11デバイスから ID3D11VideoDevice が取得できることを確認する。
                // （DXVAを使った動画の再生で必須。Windows8 以降のPCで実装されている。）
                using( var videoDevice = this.D3D11Device1.QueryInterfaceOrNull<SharpDX.Direct3D11.VideoDevice>() )
                {
                    if( null == videoDevice )
                        throw new Exception( "Direct3D11デバイスが、ID3D11VideoDevice をサポートしていません。" );
                }

                // IDXGIDevice1 を取得し、Direct2D の生成とDXGIの初期化を行う。
                using( var dxgiDevice1 = this.D3D11Device1.QueryInterface<SharpDX.DXGI.Device1>() )
                {
                    // DXGIDevice のレイテンシを設定する。
                    dxgiDevice1.MaximumFrameLatency = 1;

                    // 既定のDXGI出力を取得する。
                    using( var dxgiAdapter = dxgiDevice1.Adapter )
                    using( var output = dxgiAdapter.Outputs[ 0 ] )
                        this.DXGIOutput1 = output.QueryInterface<SharpDX.DXGI.Output1>(); // 「現在のDXGI出力」を取得することはできないので[0]で固定。

                    // DXGIデバイスマネージャを生成し、D3Dデバイスを登録する。MediaFoundationで必須。
                    this.MFDXGIDeviceManager = new SharpDX.MediaFoundation.DXGIDeviceManager();
                    this.MFDXGIDeviceManager.ResetDevice( this.D3D11Device1 );

                    // マルチスレッドモードを ON に設定する。基本的に Direct3D11 では設定不要だが、MediaFoundation でDXVAを使う場合は必須。
                    using( var multithread = this.D3D11Device1.QueryInterfaceOrNull<SharpDX.Direct3D.DeviceMultithread>() )
                    {
                        if( null == multithread )
                            throw new Exception( "Direct3D11デバイスが、ID3D10Multithread をサポートしていません。" );

                        multithread.SetMultithreadProtected( true );
                    }

                    // D2Dファクトリを作成する。
                    this.D2D1Factory1 = new SharpDX.Direct2D1.Factory1(

                        SharpDX.Direct2D1.FactoryType.MultiThreaded,
#if DEBUG
                        SharpDX.Direct2D1.DebugLevel.Information
#else
                        SharpDX.Direct2D1.DebugLevel.None
#endif
                    );

                    // D2Dデバイスを作成する。
                    this.D2D1Device = new SharpDX.Direct2D1.Device( this.D2D1Factory1, dxgiDevice1 );

                    // 既定のD2Dデバイスコンテキストを作成する。
                    this.既定のD2D1DeviceContext = new SharpDX.Direct2D1.DeviceContext(
                        this.D2D1Device,
                        SharpDX.Direct2D1.DeviceContextOptions.EnableMultithreadedOptimizations ) {
                        TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale,  // Grayscale がすべての Windows ストアアプリで推奨される。らしい。
                    };
                }

                // DirectCompositionデバイスを作成する。
                this.DCompDevice2 = new SharpDX.DirectComposition.DesktopDevice( this.D2D1Device );

                // スワップチェーン用のVisualを作成する。
                this.DCompVisual2ForSwapChain = new SharpDX.DirectComposition.Visual2( this.DCompDevice2 );

                // DirectCompositionターゲットを作成し、Visualツリーのルートにスワップチェーン用Visualを設定する。
                this.DCompTarget = SharpDX.DirectComposition.Target.FromHwnd( this.DCompDevice2, this.hWindow, topmost: true );
                this.DCompTarget.Root = this.DCompVisual2ForSwapChain;

                // IWICImagingFactory2 を生成する。
                this.WicImagingFactory2 = new SharpDX.WIC.ImagingFactory2();

                // IDWriteFactory を生成する。
                this.DWriteFactory = new SharpDX.DirectWrite.Factory( SharpDX.DirectWrite.FactoryType.Shared );

                // Windows Animation を生成する。
                this.Animation = new Animation();
            }
        }
        private void _スワップチェーンに依存しないグラフィックリソースを解放する()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this.Animation?.Dispose();
                this.DWriteFactory?.Dispose();
                this.WicImagingFactory2?.Dispose();
                this.DCompTarget?.Dispose();
                this.DCompVisual2ForSwapChain?.Dispose();
                this.DCompDevice2?.Dispose();
                this.既定のD2D1DeviceContext?.Dispose();
                this.D2D1Device?.Dispose();
                this.D2D1Factory1?.Dispose();
                this.MFDXGIDeviceManager?.Dispose();
                this.DXGIOutput1?.Dispose();
#if DEBUG
                // ReportLiveDeviceObjects。
                // デバッガの「ネイティブデバッグを有効にする」をオンにすれば表示される。
                using( var d3ddc = this.D3D11Device1.ImmediateContext )
                {
                    d3ddc.Flush();
                    d3ddc.ClearState();
                }
                using( var debug = new SharpDX.Direct3D11.DeviceDebug( this.D3D11Device1 ) )
                {
                    this.D3D11Device1?.Dispose();
                    debug.ReportLiveDeviceObjects( SharpDX.Direct3D11.ReportingLevel.Detail | SharpDX.Direct3D11.ReportingLevel.IgnoreInternal );
                }
#endif
                // MediaFoundation をシャットダウンする。
                SharpDX.MediaFoundation.MediaManager.Shutdown();
            }
        }

        private void _スワップチェーンを作成する()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                // DirectComposition用スワップチェーンを作成する。
                var swapChainDesc = new SharpDX.DXGI.SwapChainDescription1() {
                    BufferCount = 2,
                    Width = (int) this.物理画面サイズ.Width,
                    Height = (int) this.物理画面サイズ.Height,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,    // D2D をサポートするなら B8G8R8A8 を指定する必要がある。
                    AlphaMode = SharpDX.DXGI.AlphaMode.Ignore,      // Premultiplied にすると、ウィンドウの背景（デスクトップ画像）と加算合成される。（意味ない）
                    Stereo = false,
                    SampleDescription = new SharpDX.DXGI.SampleDescription( 1, 0 ), // マルチサンプリングは使わない。
                    SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,    // SwapChainForComposition での必須条件。
                    Scaling = SharpDX.DXGI.Scaling.Stretch,                 // SwapChainForComposition での必須条件。
                    Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                    Flags = SharpDX.DXGI.SwapChainFlags.None,

                    // https://msdn.microsoft.com/en-us/library/windows/desktop/bb174579.aspx
                    // > You cannot call SetFullscreenState on a swap chain that you created with IDXGIFactory2::CreateSwapChainForComposition.
                    // よって、以下のフラグは使用禁止。
                    //Flags = SharpDX.DXGI.SwapChainFlags.AllowModeSwitch,
                };
                using( var dxgiDevice1 = this.D3D11Device1.QueryInterface<SharpDX.DXGI.Device1>() )
                using( var dxgiAdapter = dxgiDevice1.Adapter )
                using( var dxgiFactory2 = dxgiAdapter.GetParent<SharpDX.DXGI.Factory2>() )
                {
                    this.DXGISwapChain1 = new SharpDX.DXGI.SwapChain1( dxgiFactory2, this.D3D11Device1, this.hControl, ref swapChainDesc );

                    // 標準機能である PrintScreen と Alt+Enter は使わない。
                    dxgiFactory2.MakeWindowAssociation(
                        this.hWindow,
                        SharpDX.DXGI.WindowAssociationFlags.IgnoreAll
                        //SharpDX.DXGI.WindowAssociationFlags.IgnorePrintScreen |
                        //SharpDX.DXGI.WindowAssociationFlags.IgnoreAltEnter
                        );
                }

                // Visual のコンテンツに指定してコミット。
                this.DCompVisual2ForSwapChain.Content = this.DXGISwapChain1;
                this.DCompDevice2.Commit();
            }
        }
        private void _スワップチェーンを解放する()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                // Visual のコンテンツから解除してコミット。
                this.DCompVisual2ForSwapChain.Content = null;
                this.DCompDevice2.Commit();

                this.DXGISwapChain1?.Dispose();
            }
        }

        private void _スワップチェーンに依存するグラフィックリソースを作成する()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                // ※正確には、「スワップチェーン」というより、「スワップチェーンが持つバックバッファ」に依存するリソース。

                using( var backbufferTexture2D = this.DXGISwapChain1.GetBackBuffer<SharpDX.Direct3D11.Texture2D>( 0 ) ) // D3D 用
                using( var backbufferSurface = this.DXGISwapChain1.GetBackBuffer<SharpDX.DXGI.Surface>( 0 ) )           // D2D 用
                {
                    // D3D 関連

                    #region " バックバッファに対する既定のD3D11レンダーターゲットビューを作成する。"
                    //----------------
                    this.既定のD3D11RenderTargetView = new SharpDX.Direct3D11.RenderTargetView( this.D3D11Device1, backbufferTexture2D );
                    //----------------
                    #endregion

                    #region " バックバッファに対する既定の深度ステンシル、既定の深度ステンシルビュー、既定の深度ステンシルステートを作成する。"
                    //----------------
                    // 既定の深度ステンシル
                    this.既定のD3D11DepthStencil = new SharpDX.Direct3D11.Texture2D(
                        this.D3D11Device1,
                        new SharpDX.Direct3D11.Texture2DDescription {
                            Width = backbufferTexture2D.Description.Width,              // バックバッファと同じサイズ
                            Height = backbufferTexture2D.Description.Height,            // 
                            MipLevels = 1,
                            ArraySize = 1,
                            Format = SharpDX.DXGI.Format.D32_Float,                     // 32bit Depth
                            SampleDescription = backbufferTexture2D.Description.SampleDescription,  // バックバッファと同じサンプル記述
                            Usage = SharpDX.Direct3D11.ResourceUsage.Default,
                            BindFlags = SharpDX.Direct3D11.BindFlags.DepthStencil,
                            CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,    // CPUからはアクセスしない
                            OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
                        } );

                    // 既定の深度ステンシルビュー
                    this.既定のD3D11DepthStencilView = new SharpDX.Direct3D11.DepthStencilView(
                        this.D3D11Device1,
                        this.既定のD3D11DepthStencil,
                        new SharpDX.Direct3D11.DepthStencilViewDescription {
                            Format = this.既定のD3D11DepthStencil.Description.Format,
                            Dimension = SharpDX.Direct3D11.DepthStencilViewDimension.Texture2D,
                            Flags = SharpDX.Direct3D11.DepthStencilViewFlags.None,
                            Texture2D = new SharpDX.Direct3D11.DepthStencilViewDescription.Texture2DResource() {
                                MipSlice = 0,
                            },
                        } );

                    // 既定の深度ステンシルステート
                    this.既定のD3D11DepthStencilState = new SharpDX.Direct3D11.DepthStencilState(
                        this.D3D11Device1,
                        new SharpDX.Direct3D11.DepthStencilStateDescription {
                            IsDepthEnabled = false,                                     // 深度無効
                            IsStencilEnabled = false,                                   // ステンシルテスト無効
                            DepthWriteMask = SharpDX.Direct3D11.DepthWriteMask.All,     // 書き込む
                            DepthComparison = SharpDX.Direct3D11.Comparison.Less,       // 手前の物体を描画
                            StencilReadMask = 0,
                            StencilWriteMask = 0,
                            // 面が表を向いている場合のステンシル・テストの設定
                            FrontFace = new SharpDX.Direct3D11.DepthStencilOperationDescription() {
                                FailOperation = SharpDX.Direct3D11.StencilOperation.Keep,       // 維持
                                DepthFailOperation = SharpDX.Direct3D11.StencilOperation.Keep,  // 維持
                                PassOperation = SharpDX.Direct3D11.StencilOperation.Keep,       // 維持
                                Comparison = SharpDX.Direct3D11.Comparison.Never,               // 常に失敗
                            },
                            // 面が裏を向いている場合のステンシル・テストの設定
                            BackFace = new SharpDX.Direct3D11.DepthStencilOperationDescription() {
                                FailOperation = SharpDX.Direct3D11.StencilOperation.Keep,       // 維持
                                DepthFailOperation = SharpDX.Direct3D11.StencilOperation.Keep,  // 維持
                                PassOperation = SharpDX.Direct3D11.StencilOperation.Keep,       // 維持
                                Comparison = SharpDX.Direct3D11.Comparison.Always,              // 常に成功
                            },
                        } );
                    //----------------
                    #endregion

                    #region " バックバッファに対する既定のビューポートを作成する。"
                    //----------------
                    this.既定のD3D11ViewPort = new SharpDX.Mathematics.Interop.RawViewportF[] {
                        new SharpDX.Mathematics.Interop.RawViewportF() {
                            X = 0.0f,                                                   // バックバッファと同じサイズ
                            Y = 0.0f,                                                   //
                            Width = (float) backbufferTexture2D.Description.Width,      //
                            Height = (float) backbufferTexture2D.Description.Height,    //
                            MinDepth = 0.0f,                                            // 近面Z: 0.0（最も近い）
                            MaxDepth = 1.0f,                                            // 遠面Z: 1.0（最も遠い）
                        },
                    };
                    //----------------
                    #endregion

                    // D2D 関連

                    #region " バックバッファとメモリを共有する、既定のD2Dレンダーターゲットビットマップを作成する。"
                    //----------------
                    this.既定のD2D1RenderBitmap1 = new SharpDX.Direct2D1.Bitmap1(   // このビットマップは、
                        this.既定のD2D1DeviceContext,
                        backbufferSurface,                                          // このDXGIサーフェス（スワップチェーンのバックバッファ）とメモリを共有する。
                        new SharpDX.Direct2D1.BitmapProperties1() {
                            PixelFormat = new SharpDX.Direct2D1.PixelFormat( backbufferSurface.Description.Format, SharpDX.Direct2D1.AlphaMode.Premultiplied ),
                            BitmapOptions = SharpDX.Direct2D1.BitmapOptions.Target | SharpDX.Direct2D1.BitmapOptions.CannotDraw,
                        } );

                    this.既定のD2D1DeviceContext.Target = this.既定のD2D1RenderBitmap1;
                    this.既定のD2D1DeviceContext.Transform = Matrix3x2.Identity;
                    //----------------
                    #endregion
                }
            }
        }
        private void _スワップチェーンに依存するグラフィックリソースを解放する()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this.既定のD3D11DepthStencilState?.Dispose();
                this.既定のD3D11DepthStencilView?.Dispose();
                this.既定のD3D11DepthStencil?.Dispose();
                this.既定のD3D11RenderTargetView?.Dispose();

                if( null != this.既定のD2D1DeviceContext )
                    this.既定のD2D1DeviceContext.Target = null;

                this.既定のD2D1RenderBitmap1?.Dispose();
            }
        }



        // ユーティリティ


        /// <summary>
        ///     等倍3D平面描画用のビュー行列と射影行列を生成して返す。
        /// </summary>
        /// <remarks>
        ///     Z = 0 におけるビューポートサイズが <see cref="設計画面サイズ"/> に一致する平面を、「等倍3D平面」と称する。
        ///     例えば、設計画面サイズが 1024x720 の場合、等倍3D平面の表示可能な x, y の値域は (-512, -360)～(+512, +360) となる。
        ///     本メソッドは、等倍3D平面を実現するためのビュー行列と射影行列を返す。
        /// </remarks>
        public void 等倍3D平面描画用の変換行列を取得する( out Matrix 転置済みビュー行列, out Matrix 転置済み射影行列 )
        {
            const float 視野角deg = 45.0f;

            var dz = (float) ( this.設計画面サイズ.Height / ( 4.0 * Math.Tan( MathUtil.DegreesToRadians( 視野角deg / 2.0f ) ) ) );

            var カメラの位置 = new Vector3( 0f, 0f, -2f * dz );
            var カメラの注視点 = new Vector3( 0f, 0f, 0f );
            var カメラの上方向 = new Vector3( 0f, 1f, 0f );

            転置済みビュー行列 = Matrix.LookAtLH( カメラの位置, カメラの注視点, カメラの上方向 );
            転置済みビュー行列.Transpose();  // 転置

            転置済み射影行列 = Matrix.PerspectiveFovLH(
                MathUtil.DegreesToRadians( 視野角deg ),
                設計画面サイズ.Width / 設計画面サイズ.Height,   // アスペクト比
                -dz,                                            // 前方投影面までの距離
                +dz );                                          // 後方投影面までの距離
            転置済み射影行列.Transpose();  // 転置
        }

        /// <summary>
        ///		指定したレンダーターゲットに対して、D2D描画処理をバッチ実行する。
        /// </summary>
        /// <remarks>
        ///		D2D描画処理は、レンダーターゲットの BeginDraw() と EndDraw() の間で行われることが保証される。
        ///		D2D描画処理中に例外が発生しても EndDraw() の呼び出しが確実に保証される。
        /// </remarks>
        /// <param name="rt">レンダリングターゲット。</param>
        /// <param name="D2D描画処理">BeginDraw() と EndDraw() の間で行う処理。</param>
        public void D2DBatchDraw( SharpDX.Direct2D1.RenderTarget rt, Action D2D描画処理 )
        {
            // リストになかったらこの RenderTarget を使うのは初回なので、BeginDraw/EndDraw() の呼び出しを行う。
            // もしリストに登録されていたら、この RenderTarget は他の誰かが BeginDraw して EndDraw してない状態
            // （D2DBatcDraw() の最中に D2DBatchDraw() が呼び出されている状態）なので、これらを呼び出してはならない。
            bool BeginとEndを行う = !( this._BatchDraw中のレンダーターゲットリスト.Contains( rt ) );

            var pretrans = rt.Transform;
            var preblend = ( rt is SharpDX.Direct2D1.DeviceContext dc ) ? dc.PrimitiveBlend : SharpDX.Direct2D1.PrimitiveBlend.SourceOver;

            try
            {
                if( BeginとEndを行う )
                {
                    this._BatchDraw中のレンダーターゲットリスト.Add( rt );     // Begin したらリストに追加。
                    rt.BeginDraw();
                }

                D2D描画処理();
            }
            finally
            {
                rt.Transform = pretrans;
                if( rt is SharpDX.Direct2D1.DeviceContext dc2 )
                    dc2.PrimitiveBlend = preblend;

                if( BeginとEndを行う )
                {
                    rt.EndDraw();
                    this._BatchDraw中のレンダーターゲットリスト.Remove( rt );  // End したらリストから削除。
                }
            }
        }

        private List<SharpDX.Direct2D1.RenderTarget> _BatchDraw中のレンダーターゲットリスト = new List<SharpDX.Direct2D1.RenderTarget>();
    }
}
