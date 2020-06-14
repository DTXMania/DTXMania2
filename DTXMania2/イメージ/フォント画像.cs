using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using FDK;

namespace DTXMania2
{
    class フォント画像 : FDK.フォント画像
    {
        /// <summary>
        ///		コンストラクタ。
        ///		指定された画像ファイルと矩形リストyamlファイルを使って、フォント画像を生成する。
        /// </summary>
        /// <param name="文字幅補正dpx">文字と文字の間の（横方向の）間隔。拡大率の影響は受けない。負数もOK。</param>
        /// <param name="不透明度">透明: 0 ～ 1 :不透明</param>
        public フォント画像( VariablePath 文字盤の画像ファイルパス, VariablePath 文字盤設定ファイルパス, float 文字幅補正dpx = 0f, float 不透明度 = 1f )
            : base(
                Global.GraphicResources.D3D11Device1,
                文字盤の画像ファイルパス, 文字盤設定ファイルパス, 文字幅補正dpx, 不透明度 )
        {
        }

        /// <summary>
        ///     文字列を描画する。
        /// </summary>
        /// <param name="基点のX位置">左揃えなら左端位置、右揃えなら右端位置のX座標。</param>
        /// <param name="拡大率">文字列の拡大率。null なら等倍。</param>
        /// <param name="右揃え">trueなら右揃え、falseなら左揃え。</param>
        public void 描画する( float 基点のX位置, float 上位置, string 表示文字列, Size2F? 拡大率 = null, bool 右揃え = false )
        {
            base.描画する(
                Global.GraphicResources.既定のD3D11DeviceContext,
                Global.GraphicResources.設計画面サイズ,
                Global.GraphicResources.既定のD3D11ViewPort,
                Global.GraphicResources.既定のD3D11DepthStencilView,
                Global.GraphicResources.既定のD3D11RenderTargetView,
                Global.GraphicResources.既定のD3D11DepthStencilState,
                基点のX位置,
                上位置,
                表示文字列,
                拡大率,
                右揃え );
        }
    }
}
