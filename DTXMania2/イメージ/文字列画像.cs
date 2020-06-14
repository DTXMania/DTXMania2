using System;
using System.Collections.Generic;
using System.Text;
using SharpDX;

namespace DTXMania2
{
    class 文字列画像 : FDK.文字列画像
    {
        public 文字列画像()
            : base( Global.GraphicResources.D3D11Device1, Global.GraphicResources.DWriteFactory, Global.GraphicResources.D2D1Factory1, Global.GraphicResources.既定のD2D1DeviceContext, Global.GraphicResources.設計画面サイズ )
        {
        }

        public void ビットマップを生成または更新する()
        {
            base.ビットマップを生成または更新する(
                Global.GraphicResources.DWriteFactory,
                Global.GraphicResources.D2D1Factory1,
                Global.GraphicResources.既定のD2D1DeviceContext,
                Global.GraphicResources.D3D11Device1 );
        }

        public void 描画する( float 左位置, float 上位置, float 不透明度0to1 = 1.0f, float X方向拡大率 = 1.0f, float Y方向拡大率 = 1.0f )
        {
            base.描画する(
                Global.GraphicResources.DWriteFactory,
                Global.GraphicResources.D2D1Factory1,
                Global.GraphicResources.既定のD2D1DeviceContext,
                Global.GraphicResources.D3D11Device1,
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
                Y方向拡大率 );
        }

        public void 描画する( Matrix? 変換行列3D = null, float 不透明度0to1 = 1.0f )
        {
            base.描画する(
                Global.GraphicResources.DWriteFactory,
                Global.GraphicResources.D2D1Factory1,
                Global.GraphicResources.既定のD2D1DeviceContext,
                Global.GraphicResources.D3D11Device1,
                Global.GraphicResources.既定のD3D11DeviceContext,
                Global.GraphicResources.設計画面サイズ,
                Global.GraphicResources.既定のD3D11ViewPort,
                Global.GraphicResources.既定のD3D11DepthStencilView,
                Global.GraphicResources.既定のD3D11RenderTargetView,
                Global.GraphicResources.既定のD3D11DepthStencilState,
                変換行列3D,
                不透明度0to1 );
        }
    }
}
