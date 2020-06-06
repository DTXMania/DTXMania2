using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;
using DTXMania2.曲;

namespace DTXMania2.選曲
{
    class 曲別スキルと達成率 : IDisposable
    {

        // 生成と終了


        public 曲別スキルと達成率()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._数字画像 = new フォント画像( @"$(Images)\ParameterFont_LargeBoldItalic.png", @"$(Images)\ParameterFont_LargeBoldItalic.yaml", 文字幅補正dpx: 0f );
            this._スキルアイコン = new 画像( @"$(Images)\SkillIcon2.png" );
            this._達成率アイコン = new 画像( @"$(Images)\AchivementLogo.png" );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._達成率アイコン.Dispose();
            this._スキルアイコン.Dispose();
            this._数字画像.Dispose();
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc, Node フォーカスノード )
        {
            if( !( フォーカスノード is SongNode snode ) || snode.曲.フォーカス譜面 is null )
                return; // 現状、表示できるノードは SongNode のみ。

            #region " フォーカスノードが変更されていれば情報を更新する。"
            //----------------
            if( フォーカスノード != this._現在表示しているノード )
            {
                this._現在表示しているノード = フォーカスノード;

                this._スキル値文字列 = null;
                this._達成率文字列 = null;

                if( snode.曲.フォーカス譜面.最高記録を現行化済み )
                {
                    var 最高記録 = snode.曲.フォーカス譜面.最高記録;

                    if( null != 最高記録 )
                    {
                        // 達成率。右詰め、余白は' '。
                        this._達成率文字列 = 最高記録.Achievement.ToString( "0.00" ).PadLeft( 6 ) + '%';

                        // スキル値。右詰め、余白は' '。
                        double skill = 成績.スキルを算出する( snode.曲.フォーカス譜面.譜面.Level, 最高記録.Achievement );
                        this._スキル値文字列 = skill.ToString( "0.00" ).PadLeft( 6 );
                    }
                }
            }
            //----------------
            #endregion

            var スキル描画領域 = new RectangleF( 10f, 340f, 275f, 98f );
            var 達成率描画領域 = new RectangleF( 10f, 240f, 275f, 98f );

            #region " 達成率を描画する。"
            //----------------
            if( null != this._達成率文字列 )
            {
                // 達成率アイコンを描画する。
                this._達成率アイコン.進行描画する( 
                    達成率描画領域.X, 
                    達成率描画領域.Y - 50f,
                    X方向拡大率: 0.8f,
                    Y方向拡大率: 0.8f );

                // 小数部と '%' を描画する。
                var 拡大率 = new Size2F( 0.8f, 0.8f );
                this._数字画像.進行描画する(
                    達成率描画領域.X + 130f + 175f,
                    達成率描画領域.Y + ( 達成率描画領域.Height * ( 1.0f - 拡大率.Height ) ),
                    this._達成率文字列[ 4.. ],
                    拡大率 );

                // 整数部と '.' を描画する。
                拡大率 = new Size2F( 1.0f, 1.0f );
                this._数字画像.進行描画する(
                    達成率描画領域.X + 130f,
                    達成率描画領域.Y, 
                    this._達成率文字列[ 0..4 ],
                    拡大率 );
            }
            //----------------
            #endregion

            #region " スキル値を描画する。"
            //----------------
            if( null != this._スキル値文字列 )
            {
                // スキルアイコンを描画する。
                this._スキルアイコン.進行描画する( 
                    スキル描画領域.X,
                    スキル描画領域.Y + 10f,
                    X方向拡大率: 0.5f,
                    Y方向拡大率: 0.4f );

                var 小数部 = this._スキル値文字列[ 4.. ];
                var 整数部 = this._スキル値文字列[ ..4 ];

                // 小数部を描画する。
                var 拡大率 = new Size2F( 0.8f, 0.8f );
                this._数字画像.進行描画する(
                    スキル描画領域.X + 130f + 175f,
                    スキル描画領域.Y + ( スキル描画領域.Height * ( 1.0f - 拡大率.Height ) ),
                    小数部,
                    拡大率 );

                // 整数部を描画する（'.'含む）。
                拡大率 = new Size2F( 1.0f, 1.0f );
                this._数字画像.進行描画する(
                    スキル描画領域.X + 130f,
                    スキル描画領域.Y,
                    整数部,
                    拡大率 );
            }
            //----------------
            #endregion
        }



        // ローカル


        private readonly フォント画像 _数字画像;

        private readonly 画像 _スキルアイコン;

        private readonly 画像 _達成率アイコン;

        private Node? _現在表示しているノード = null;

        private string? _スキル値文字列 = null;

        private string? _達成率文字列 = null;
    }
}
