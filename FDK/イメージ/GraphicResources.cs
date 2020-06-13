﻿using SharpDX.DirectComposition;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
    public class GraphicResources : IDisposable
    {

        // プロパティ


        /// <summary>
        ///     メインフォームインスタンスのウィンドウハンドル。
        /// </summary>
        /// <remarks>
        ///     GUIスレッド 以 外 のスレッドから参照する場合は、Form.Handle ではなくこのメンバを参照すること。
        ///     （Form のメンバはすべて、必ずGUIスレッドから参照されなれければならないため。）
        /// </remarks>
        public IntPtr Handle { get; set; } = IntPtr.Zero;

        /// <summary>
        ///     設計（プログラム側）で想定する固定画面サイズ[dpx]。
        /// </summary>
        /// <remarks>
        ///     物理画面サイズはユーザが自由に変更できるが、プログラム側では常に設計画面サイズを使うことで、
        ///     物理画面サイズに依存しない座標をハードコーディングできるようにする。
        ///     プログラム内では設計画面におけるピクセルの単位として「dpx (designed pixel)」と称することがある。
        ///     なお、int より float での利用が多いので、Size や Size2 ではなく Size2F を使う。
        ///     （int 同士ということを忘れて、割り算しておかしくなるケースも多発したので。）
        /// </remarks>
        public SharpDX.Size2F 設計画面サイズ { get; private set; }

        public SharpDX.Size2F 物理画面サイズ { get; private set; }

        /// <summary>
        ///     等倍3D平面での画面左上の3D座標。
        /// </summary>
        /// <remarks>
        ///     等倍3D平面については <see cref="等倍3D平面描画用の変換行列を取得する"/> を参照。
        /// </remarks>
        public SharpDX.Vector3 画面左上dpx => new SharpDX.Vector3( -設計画面サイズ.Width / 2f, +設計画面サイズ.Height / 2f, 0f );

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
                var fs = DCompDevice2.FrameStatistics;
                return ( fs.NextEstimatedFrameTime - fs.CurrentTime ) / fs.TimeFrequency;
            }
        }



        // イベント


        public event EventHandler スワップチェーンに依存しないグラフィックリソースの作成;
        public event EventHandler スワップチェーンに依存しないグラフィックリソースの解放;
        
        public event EventHandler スワップチェーンに依存するグラフィックリソースの作成;
        public event EventHandler スワップチェーンに依存するグラフィックリソースの解放;



        // プロパティ／スワップチェーンに依存しないグラフィックリソース


        public SharpDX.Direct3D11.Device1 D3D11Device1 { get; private set; } = null!;
        /// <summary>
        ///     アプリは、D3D11Device1.ImmediateContext ではなくこちらを使用すること。
        /// </summary>
        /// <remarks>
        ///     D3D11Device1.ImmediateContext を参照すると、内部のCOM参照カウンタを１つカウントアップする。
        ///     これを Release するには Dispose() するしかないが、Dispose() すると、COM参照のカウントダウンだけでなく
        ///     D3D11Device1.ImmediateContext 自体を破棄してしまう。
        ///     従って、D3D11Device1.ImmediateContext は、最初に一度だけ参照し、最後に一度だけ Dispose() する運用にする。
        /// </remarks>
        public SharpDX.Direct3D11.DeviceContext 既定のD3D11DeviceContext { get; private set; } = null!;
        public SharpDX.DXGI.Output1 DXGIOutput1 { get; private set; } = null!;
        public SharpDX.MediaFoundation.DXGIDeviceManager MFDXGIDeviceManager { get; private set; } = null!;
        public SharpDX.Direct2D1.Factory1 D2D1Factory1 { get; private set; } = null!;
        public SharpDX.Direct2D1.Device D2D1Device { get; private set; } = null!;
        public SharpDX.Direct2D1.DeviceContext 既定のD2D1DeviceContext { get; private set; } = null!;
        public SharpDX.DirectComposition.DesktopDevice DCompDevice2 { get; private set; } = null!;    // IDCompositionDevice2 から派生
        public SharpDX.DirectComposition.Visual2 DCompVisual2ForSwapChain { get; private set; } = null!;
        public SharpDX.DirectComposition.Target DCompTarget { get; private set; } = null!;
        public SharpDX.WIC.ImagingFactory2 WicImagingFactory2 { get; private set; } = null!;
        public SharpDX.DirectWrite.Factory DWriteFactory { get; private set; } = null!;

        private void _スワップチェーンに依存しないグラフィックリソースを作成する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            #region " MediaFoundation をセットアップする。"
            //----------------
            SharpDX.MediaFoundation.MediaManager.Startup();
            //----------------
            #endregion

            #region " D3Dデバイスを作成する。"
            //----------------
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
                D3D11Device1 = d3dDevice.QueryInterface<SharpDX.Direct3D11.Device1>();
            }

            // D3D11デバイスから ID3D11VideoDevice が取得できることを確認する。
            // （DXVAを使った動画の再生で必須。Windows8 以降のPCで実装されている。）
            using( var videoDevice = D3D11Device1.QueryInterfaceOrNull<SharpDX.Direct3D11.VideoDevice>() )
            {
                // ↓以下のコメントを外すと、グラフィックデバッグでは例外が発生する。
                //if( videoDevice is null )
                //    throw new Exception( "Direct3D11デバイスが、ID3D11VideoDevice をサポートしていません。" );
            }
            //----------------
            #endregion

            #region " 既定のD3Dデバイスコンテキストを作成する。"
            //----------------
            既定のD3D11DeviceContext = D3D11Device1.ImmediateContext;
            //----------------
            #endregion

            using var dxgiDevice1 = D3D11Device1.QueryInterface<SharpDX.DXGI.Device1>();

            #region " DXGIデバイスのレイテンシを設定する。"
            //----------------
            dxgiDevice1.MaximumFrameLatency = 1;
            //----------------
            #endregion

            #region " 既定のDXGI出力を取得する。"
            //----------------
            using( var dxgiAdapter = dxgiDevice1.Adapter )
            using( var output = dxgiAdapter.Outputs[ 0 ] ) // 「現在のDXGI出力」を取得することはできないので[0]で固定。
                DXGIOutput1 = output.QueryInterface<SharpDX.DXGI.Output1>();
            //----------------
            #endregion

            #region " DXGIデバイスマネージャを生成し、D3Dデバイスを登録する。MediaFoundationで必須。"
            //----------------
            MFDXGIDeviceManager = new SharpDX.MediaFoundation.DXGIDeviceManager();
            MFDXGIDeviceManager.ResetDevice( D3D11Device1 );
            //----------------
            #endregion

            #region " D3D10 マルチスレッドモードを ON に設定する。D3D11 はスレッドセーフだが、MediaFoundation でDXVAを使う場合は必須。"
            //----------------
            using( var multithread = D3D11Device1.QueryInterfaceOrNull<SharpDX.Direct3D.DeviceMultithread>() )
            {
                if( multithread is null )
                    throw new Exception( "Direct3D11デバイスが、ID3D10Multithread をサポートしていません。" );

                multithread.SetMultithreadProtected( true );
            }
            //----------------
            #endregion


            #region " D2Dファクトリを作成する。"
            //----------------
            D2D1Factory1 = new SharpDX.Direct2D1.Factory1(

                SharpDX.Direct2D1.FactoryType.MultiThreaded,
#if DEBUG
                // D2D Debugメッセージは、Visual Studio のプロジェクトプロパティで「ネイティブコードのデバッグを有効にする」を ON にしないと表示されない。
                // なお、「ネイティブコードのデバッグを有効にする」を有効にしてアプリケーションを実行すると、速度が恐ろしく低下する。
                SharpDX.Direct2D1.DebugLevel.Information
#else
                SharpDX.Direct2D1.DebugLevel.None
#endif
            );
            //----------------
            #endregion

            #region " D2Dデバイスを作成する。"
            //----------------
            D2D1Device = new SharpDX.Direct2D1.Device( D2D1Factory1, dxgiDevice1 );
            //----------------
            #endregion

            #region " 既定のD2Dデバイスコンテキストを作成する。"
            //----------------
            既定のD2D1DeviceContext = new SharpDX.Direct2D1.DeviceContext(
                D2D1Device,
                SharpDX.Direct2D1.DeviceContextOptions.EnableMultithreadedOptimizations ) {
                TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale,  // Grayscale がすべての Windows ストアアプリで推奨される。らしい。
            };
            //----------------
            #endregion


            #region " DirectCompositionデバイスを作成する。"
            //----------------
            DCompDevice2 = new SharpDX.DirectComposition.DesktopDevice( D2D1Device );
            //----------------
            #endregion

            #region " スワップチェーン用のVisualを作成する。"
            //----------------
            DCompVisual2ForSwapChain = new SharpDX.DirectComposition.Visual2( DCompDevice2 );
            //----------------
            #endregion

            #region " DirectCompositionターゲットを作成し、Visualツリーのルートにスワップチェーン用Visualを設定する。"
            //----------------
            DCompTarget = SharpDX.DirectComposition.Target.FromHwnd( DCompDevice2, Handle, topmost: true );
            DCompTarget.Root = DCompVisual2ForSwapChain;
            //----------------
            #endregion


            #region " WICイメージングファクトリを作成する。"
            //----------------
            WicImagingFactory2 = new SharpDX.WIC.ImagingFactory2();
            //----------------
            #endregion

            #region " DirectWriteファクトリを作成する。"
            //----------------
            DWriteFactory = new SharpDX.DirectWrite.Factory( SharpDX.DirectWrite.FactoryType.Shared );
            //----------------
            #endregion
        }
        private void _スワップチェーンに依存しないグラフィックリソースを解放する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            #region " DirectWrite ファクトリを解放する。"
            //----------------
            DWriteFactory.Dispose();
            //----------------
            #endregion

            #region " WICイメージングファクトリを解放する。"
            //----------------
            WicImagingFactory2.Dispose();
            //----------------
            #endregion


            #region " DirectCompositionターゲットを解放する。"
            //----------------
            DCompTarget.Dispose();
            //----------------
            #endregion

            #region " スワップチェーン用のVisualを解放する。"
            //----------------
            DCompVisual2ForSwapChain.Dispose();
            //----------------
            #endregion

            #region " DirectCompositionデバイスを解放する。"
            //----------------
            DCompDevice2.Dispose();
            //----------------
            #endregion


            #region " 既定のD2Dデバイスコンテキストを解放する。"
            //----------------
            既定のD2D1DeviceContext.Dispose();
            //----------------
            #endregion

            #region " D2Dデバイスを解放する。"
            //----------------
            D2D1Device.Dispose();
            //----------------
            #endregion

            #region " D2Dファクトリを解放する。"
            //----------------
            D2D1Factory1.Dispose();
            //----------------
            #endregion


            #region " DXGIデバイスマネージャを解放する。"
            //----------------
            MFDXGIDeviceManager.Dispose();
            //----------------
            #endregion

            #region " DXGI出力を解放する。"
            //----------------
            DXGIOutput1.Dispose();
            //----------------
            #endregion

            #region " 既定のD3Dデバイスコンテキストを解放する。"
            //----------------
            既定のD3D11DeviceContext.ClearState();
            既定のD3D11DeviceContext.Flush();
            既定のD3D11DeviceContext.Dispose();
            //----------------
            #endregion

            #region " D3Dデバイスを解放する。"
            //----------------
#if DEBUG
            // ReportLiveDeviceObjects。
            // デバッガの「ネイティブデバッグを有効にする」をオンにすれば表示される。
            using( var debug = new SharpDX.Direct3D11.DeviceDebug( D3D11Device1 ) )
            {
                D3D11Device1.Dispose();
                debug.ReportLiveDeviceObjects( SharpDX.Direct3D11.ReportingLevel.Detail | SharpDX.Direct3D11.ReportingLevel.IgnoreInternal );
            }
#endif
            //----------------
            #endregion

            #region " MediaFoundation をシャットダウンする。"
            //----------------
            SharpDX.MediaFoundation.MediaManager.Shutdown();
            //----------------
            #endregion
        }



        // プロパティ／スワップチェーン


        public SharpDX.DXGI.SwapChain1 DXGISwapChain1 { get; private set; } = null!;

        private void _スワップチェーンを作成する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // DirectComposition用スワップチェーンを作成する。
            var swapChainDesc = new SharpDX.DXGI.SwapChainDescription1() {
                BufferCount = 2,
                Width = (int) 設計画面サイズ.Width,
                Height = (int) 設計画面サイズ.Height,
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

            using var dxgiDevice1 = D3D11Device1.QueryInterface<SharpDX.DXGI.Device1>();
            using var dxgiAdapter = dxgiDevice1!.Adapter;
            using var dxgiFactory2 = dxgiAdapter!.GetParent<SharpDX.DXGI.Factory2>();

            DXGISwapChain1 = new SharpDX.DXGI.SwapChain1( dxgiFactory2, D3D11Device1, ref swapChainDesc );  // CreateSwapChainForComposition()
            //DXGISwapChain1 = new SharpDX.DXGI.SwapChain1( dxgiFactory2, D3D11Device1, this.Handle, ref swapChainDesc );  // CreateSwapChainForHWnd()

            // 標準機能である PrintScreen と Alt+Enter は使わない。
            dxgiFactory2!.MakeWindowAssociation(
                Handle,
                SharpDX.DXGI.WindowAssociationFlags.IgnoreAll
                //SharpDX.DXGI.WindowAssociationFlags.IgnorePrintScreen |
                //SharpDX.DXGI.WindowAssociationFlags.IgnoreAltEnter
                );

            // Visual のコンテンツに指定してコミット。
            DCompVisual2ForSwapChain.Content = DXGISwapChain1;
            using var trans = new SharpDX.DirectComposition.ScaleTransform( DCompDevice2 );
            trans.SetScaleX( this.物理画面サイズ.Width / 設計画面サイズ.Width );
            trans.SetScaleY( this.物理画面サイズ.Height / 設計画面サイズ.Height );
            DCompVisual2ForSwapChain.SetTransform( trans );
            DCompDevice2.Commit();
        }
        private void _スワップチェーンを解放する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            // Visual のコンテンツから解除してコミット。
            DCompVisual2ForSwapChain.Content = null;
            DCompDevice2.Commit();

            DXGISwapChain1.Dispose();
        }



        // プロパティ／スワップチェーンに依存するグラフィックリソース


        /// <summary>
        ///     スワップチェーンのバックバッファに対する既定のレンダーターゲットビュー。
        /// </summary>
        public SharpDX.Direct3D11.RenderTargetView 既定のD3D11RenderTargetView { get; private set; } = null!;

        /// <summary>
        ///     スワップチェーンのバックバッファに対する既定の深度ステンシル。
        /// </summary>
        public SharpDX.Direct3D11.Texture2D 既定のD3D11DepthStencil { get; private set; } = null!;

        /// <summary>
        ///     スワップチェーンのバックバッファに対する既定の深度ステンシルビュー。
        /// </summary>
        public SharpDX.Direct3D11.DepthStencilView 既定のD3D11DepthStencilView { get; private set; } = null!;

        /// <summary>
        ///     スワップチェーンのバックバッファに対する既定の深度ステンシルステート。
        /// </summary>
        public SharpDX.Direct3D11.DepthStencilState 既定のD3D11DepthStencilState { get; private set; } = null!;

        /// <summary>
        ///     スワップチェーンのバックバッファに対する既定のビューポートの配列。
        /// </summary>
        public SharpDX.Mathematics.Interop.RawViewportF[] 既定のD3D11ViewPort { get; private set; } = null!;

        /// <summary>
        ///     スワップチェーンのバックバッファとメモリを共有するD2Dレンダービットマップ。
        /// </summary>
        /// <remarks>
        ///     これにD2Dで描画を行うことは、すなわちD3Dスワップチェーンのバックバッファに描画することを意味する。
        /// </remarks>
        public SharpDX.Direct2D1.Bitmap1 既定のD2D1RenderBitmap1 { get; private set; } = null!;

        private void _スワップチェーンに依存するグラフィックリソースを作成する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );


            // D3D 関連

            using( var backbufferTexture2D = DXGISwapChain1.GetBackBuffer<SharpDX.Direct3D11.Texture2D>( 0 ) )
            {
                #region " バックバッファに対する既定のD3D11レンダーターゲットビューを作成する。"
                //----------------
                既定のD3D11RenderTargetView = new SharpDX.Direct3D11.RenderTargetView( D3D11Device1, backbufferTexture2D );
                //----------------
                #endregion

                #region " バックバッファに対する既定の深度ステンシル、既定の深度ステンシルビュー、既定の深度ステンシルステートを作成する。"
                //----------------
                // 既定の深度ステンシル
                既定のD3D11DepthStencil = new SharpDX.Direct3D11.Texture2D(
                    D3D11Device1,
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
                既定のD3D11DepthStencilView = new SharpDX.Direct3D11.DepthStencilView(
                    D3D11Device1,
                    既定のD3D11DepthStencil,
                    new SharpDX.Direct3D11.DepthStencilViewDescription {
                        Format = 既定のD3D11DepthStencil.Description.Format,
                        Dimension = SharpDX.Direct3D11.DepthStencilViewDimension.Texture2D,
                        Flags = SharpDX.Direct3D11.DepthStencilViewFlags.None,
                        Texture2D = new SharpDX.Direct3D11.DepthStencilViewDescription.Texture2DResource() {
                            MipSlice = 0,
                        },
                    } );

                // 既定の深度ステンシルステート
                既定のD3D11DepthStencilState = new SharpDX.Direct3D11.DepthStencilState(
                    D3D11Device1,
                    new SharpDX.Direct3D11.DepthStencilStateDescription {
                        IsDepthEnabled = false,                                         // 深度無効
                        IsStencilEnabled = false,                                       // ステンシルテスト無効
                        DepthWriteMask = SharpDX.Direct3D11.DepthWriteMask.All,         // 書き込む
                        DepthComparison = SharpDX.Direct3D11.Comparison.Less,           // 手前の物体を描画
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
                既定のD3D11ViewPort = new SharpDX.Mathematics.Interop.RawViewportF[] {
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
            }


            // D2D 関連

            using( var backbufferSurface = DXGISwapChain1.GetBackBuffer<SharpDX.DXGI.Surface>( 0 ) )
            {
                #region " バックバッファとメモリを共有する、既定のD2Dレンダーターゲットビットマップを作成する。"
                //----------------
                既定のD2D1RenderBitmap1 = new SharpDX.Direct2D1.Bitmap1(   // このビットマップは、
                    既定のD2D1DeviceContext,
                    backbufferSurface,                                     // このDXGIサーフェス（スワップチェーンのバックバッファ）とメモリを共有する。
                    new SharpDX.Direct2D1.BitmapProperties1() {
                        PixelFormat = new SharpDX.Direct2D1.PixelFormat( backbufferSurface.Description.Format, SharpDX.Direct2D1.AlphaMode.Premultiplied ),
                        BitmapOptions = SharpDX.Direct2D1.BitmapOptions.Target | SharpDX.Direct2D1.BitmapOptions.CannotDraw,
                    } );

                既定のD2D1DeviceContext.Target = 既定のD2D1RenderBitmap1;
                既定のD2D1DeviceContext.Transform = SharpDX.Matrix3x2.Identity;
                既定のD2D1DeviceContext.PrimitiveBlend = SharpDX.Direct2D1.PrimitiveBlend.SourceOver;
                //----------------
                #endregion
            }
        }
        private void _スワップチェーンに依存するグラフィックリソースを解放する()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            #region " 既定の深度ステンシルステート、既定の深度ステンシルビュー、深度ステンシルを解放する。"
            //----------------
            既定のD3D11DepthStencilState.Dispose();
            既定のD3D11DepthStencilView.Dispose();
            既定のD3D11DepthStencil.Dispose();
            //----------------
            #endregion

            #region " 既定のD3D11レンダーターゲットビューを解放する。"
            //----------------
            既定のD3D11RenderTargetView.Dispose();
            //----------------
            #endregion

            #region " 既定のD2Dレンダーターゲットビットマップを解放する。"
            //----------------
            既定のD2D1DeviceContext.Target = null;
            既定のD2D1RenderBitmap1.Dispose();
            //----------------
            #endregion
        }



        // 生成と終了


        public GraphicResources()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.スワップチェーンに依存しないグラフィックリソースの作成 += ( s, e ) => this._スワップチェーンに依存しないグラフィックリソースを作成する();
            this.スワップチェーンに依存しないグラフィックリソースの解放 += ( s, e ) => this._スワップチェーンに依存しないグラフィックリソースを解放する();
            this.スワップチェーンに依存するグラフィックリソースの作成 += ( s, e ) => this._スワップチェーンに依存するグラフィックリソースを作成する();
            this.スワップチェーンに依存するグラフィックリソースの解放 += ( s, e ) => this._スワップチェーンに依存するグラフィックリソースを解放する();
        }

        public void 生成する( IntPtr hWindow, SharpDX.Size2F 設計画面サイズ, SharpDX.Size2F 物理画面サイズ )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.Handle = hWindow;
            this.設計画面サイズ = 設計画面サイズ;
            this.物理画面サイズ = 物理画面サイズ;

            this.スワップチェーンに依存しないグラフィックリソースの作成( this, new EventArgs() );
            this._スワップチェーンを作成する();
            this.スワップチェーンに依存するグラフィックリソースの作成( this, new EventArgs() );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.スワップチェーンに依存するグラフィックリソースの解放( this, new EventArgs() );
            this._スワップチェーンを解放する();
            this.スワップチェーンに依存しないグラフィックリソースの解放( this, new EventArgs() );
        }



        // その他


        /// <summary>
        ///		物理画面サイズ（スワップチェーンのバックバッファのサイズ）を変更する。
        /// </summary>
        /// <param name="newSize">新しいサイズ。</param>
        public void 物理画面サイズを変更する( SharpDX.Size2F newSize )
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.物理画面サイズ = newSize;

            using var trans = new SharpDX.DirectComposition.ScaleTransform( DCompDevice2 );
            trans.SetScaleX( this.物理画面サイズ.Width / 設計画面サイズ.Width );
            trans.SetScaleY( this.物理画面サイズ.Height / 設計画面サイズ.Height );
            DCompVisual2ForSwapChain.SetTransform( trans );
            DCompDevice2.Commit();
        }

        /// <summary>
        ///     等倍3D平面描画用のビュー行列と射影行列を生成して返す。
        /// </summary>
        /// <remarks>
        ///     「等倍3D平面」とは、Z = 0 におけるビューポートサイズが <see cref="設計画面サイズ"/> に一致する平面である。
        ///     例えば、設計画面サイズが 1024x720 の場合、等倍3D平面の表示可能な x, y の値域は (-512, -360)～(+512, +360) となる。
        ///     この平面を使うと、3Dモデルの配置やサイズ設定を設計画面サイズを基準に行うことができるようになる。
        ///     本メソッドは、等倍3D平面を実現するためのビュー行列と射影行列を返す。
        /// </remarks>
        public void 等倍3D平面描画用の変換行列を取得する( out SharpDX.Matrix ビュー行列, out SharpDX.Matrix 射影行列, float 視野角deg = 10f )
        {
            var dz = (float) ( this.設計画面サイズ.Height / ( 4.0 * System.Math.Tan( SharpDX.MathUtil.DegreesToRadians( 視野角deg / 2.0f ) ) ) );

            var カメラの位置 = new SharpDX.Vector3( 0f, 0f, -2f * dz );
            var カメラの注視点 = new SharpDX.Vector3( 0f, 0f, 0f );
            var カメラの上方向 = new SharpDX.Vector3( 0f, 1f, 0f );

            ビュー行列 = SharpDX.Matrix.LookAtLH( カメラの位置, カメラの注視点, カメラの上方向 );

            射影行列 = SharpDX.Matrix.PerspectiveFovLH(
                SharpDX.MathUtil.DegreesToRadians( 視野角deg ),
                設計画面サイズ.Width / 設計画面サイズ.Height,   // アスペクト比
                -dz,                                            // 前方投影面までの距離
                +dz );                                          // 後方投影面までの距離
        }
    }
}
