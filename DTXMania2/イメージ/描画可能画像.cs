﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D11;
using FDK;

namespace DTXMania2
{
    /// <summary>
    ///		D3Dテクスチャとメモリを共有するD2Dビットマップを持つテクスチャ。
    ///		D2Dビットマップに対して描画を行えば、それをD3Dテクスチャとして表示することができる。
    /// </summary>
    class 描画可能画像 : FDK.描画可能画像
    {
        /// <summary>
        ///     指定した画像ファイルから描画可能画像を作成する。
        /// </summary>
        public 描画可能画像( VariablePath 画像ファイルパス, BindFlags bindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget )
            : base( 
                  Global.GraphicResources.D3D11Device1,
                  Global.GraphicResources.既定のD2D1DeviceContext,
                  Folder.カルチャを考慮した絶対パスを返す( 画像ファイルパス.変数なしパス ),
                  bindFlags )
        {
        }

        /// <summary>
        ///     指定したサイズの、空の描画可能画像を作成する。
        /// </summary>
        public 描画可能画像( Size2F サイズ, BindFlags bindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget)
            : base( Global.GraphicResources.D3D11Device1, Global.GraphicResources.既定のD2D1DeviceContext, サイズ, bindFlags )
        {
        }

        /// <summary>
        ///		画像を描画する。
        ///	</summary>
        public void 描画する( float 左位置, float 上位置, float 不透明度0to1 = 1.0f, float X方向拡大率 = 1.0f, float Y方向拡大率 = 1.0f, RectangleF? 転送元矩形 = null )
        {
            base.描画する(
                Global.GraphicResources.既定のD3D11DeviceContext,
                Global.GraphicResources.設計画面サイズ,
                Global.GraphicResources.既定のD3D11ViewPort,
                Global.GraphicResources.既定のD3D11DepthStencilView,
                Global.GraphicResources.既定のD3D11RenderTargetView,
                Global.GraphicResources.既定のD3D11DepthStencilState,
                左位置,
                上位置,
                不透明度0to1,
                X方向拡大率,
                Y方向拡大率,
                転送元矩形 );
        }

        /// <summary>
        ///		画像を描画する。
        ///	</summary>
        /// <param name="ワールド行列変換">画像は原寸（<see cref="サイズ"/>）にスケーリングされており、その後にこのワールド行列が適用される。</param>
        /// <param name="転送元矩形">テクスチャ座標(値域0～1)で指定する。</param>
        public void 描画する( Matrix ワールド行列変換, float 不透明度0to1 = 1f, RectangleF? 転送元矩形 = null )
        {
            base.描画する(
                Global.GraphicResources.既定のD3D11DeviceContext,
                Global.GraphicResources.設計画面サイズ,
                Global.GraphicResources.既定のD3D11ViewPort,
                Global.GraphicResources.既定のD3D11DepthStencilView,
                Global.GraphicResources.既定のD3D11RenderTargetView,
                Global.GraphicResources.既定のD3D11DepthStencilState,
                ワールド行列変換,
                不透明度0to1,
                転送元矩形 );
        }
    }
}
