using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania.選曲
{
    class 曲別スキルと達成率 : IDisposable
    {

        // 生成と終了


        public 曲別スキルと達成率()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._数字画像 = new 画像フォント( @"$(System)images\パラメータ文字_大太斜.png", @"$(System)images\パラメータ文字_大太斜.yaml", 文字幅補正dpx: 0f );
                this._スキルアイコン = new テクスチャ( @"$(System)images\曲別SKILLアイコン2.png" );
                this._達成率アイコン = new テクスチャ( @"$(System)images\達成率ロゴ.png" );
            }
        }

        public virtual void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._達成率アイコン?.Dispose();
                this._スキルアイコン?.Dispose();
                this._数字画像?.Dispose();
            }
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc )
        {
            var スキル描画領域 = new RectangleF( 10f, 340f, 275f, 98f );
            var 達成率描画領域 = new RectangleF( 10f, 240f, 275f, 98f );

            if( App進行描画.曲ツリー.フォーカス曲ノード != this._現在表示しているノード )
            {
                #region " フォーカスノードが変更されたので情報を更新する。"
                //----------------
                this._現在表示しているノード = App進行描画.曲ツリー.フォーカス曲ノード; // MusicNode 以外は null が返される
                this._スキル値文字列 = null;
                this._達成率文字列 = null;

                // 達成率は RecordDB から取得し、スキル値は達成率と難易度から算出する。

                if( null != this._現在表示しているノード )
                {
                    using( var recorddb = new RecordDB() )
                    {
                        var record = recorddb.Records.Where( ( r ) => ( r.UserId == App進行描画.ユーザ管理.ログオン中のユーザ.ユーザID && r.SongHashId == this._現在表示しているノード.曲ファイルのハッシュ ) ).SingleOrDefault();

                        if( null != record )
                        {
                            // 文字列はいずれも右詰め、余白は' '。
                            this._達成率文字列 = record.Achievement.ToString( "0.00" ).PadLeft( 6 ) + '%';

                            double skill = 成績.スキルを算出する( this._現在表示しているノード.難易度, record.Achievement );
                            this._スキル値文字列 = skill.ToString( "0.00" ).PadLeft( 6 );
                        }
                    }
                }
                //----------------
                #endregion
            }

            bool 表示可能ノードである = ( this._現在表示しているノード is MusicNode );

            if( 表示可能ノードである )
            {
                if( this._達成率文字列.Nullでも空でもない() )
                {
                    // 達成率アイコンを描画する。
                    this._達成率アイコン.描画する( 達成率描画領域.X, 達成率描画領域.Y - 50f, X方向拡大率: 0.8f, Y方向拡大率: 0.8f );

                    // 小数部と '%' を描画する。
                    var 拡大率 = new Size2F( 0.8f, 0.8f );
                    this._数字画像.描画する( dc, 達成率描画領域.X + 130f + 175f, 達成率描画領域.Y + ( 達成率描画領域.Height * ( 1.0f - 拡大率.Height ) ), _達成率文字列.Substring( 4 ), 拡大率 );

                    // 整数部と '.' を描画する。
                    拡大率 = new Size2F( 1.0f, 1.0f );
                    this._数字画像.描画する( dc, 達成率描画領域.X + 130f, 達成率描画領域.Y, _達成率文字列.Substring( 0, 4 ), 拡大率 );
                }

                if( this._スキル値文字列.Nullでも空でもない() )
                {
                    // スキルアイコンを描画する。
                    this._スキルアイコン.描画する( スキル描画領域.X, スキル描画領域.Y + 10f, X方向拡大率: 0.5f, Y方向拡大率: 0.4f );

                    // 小数部を描画する。
                    var 拡大率 = new Size2F( 0.8f, 0.8f );
                    this._数字画像.描画する( dc, スキル描画領域.X + 130f + 175f, スキル描画領域.Y + ( スキル描画領域.Height * ( 1.0f - 拡大率.Height ) ), _スキル値文字列.Substring( 4 ), 拡大率 );

                    // 整数部を描画する（'.'含む）。
                    拡大率 = new Size2F( 1.0f, 1.0f );
                    this._数字画像.描画する( dc, スキル描画領域.X + 130f, スキル描画領域.Y, _スキル値文字列.Substring( 0, 4 ), 拡大率 );
                }
            }
        }



        // private


        private 画像フォント _数字画像 = null;

        private テクスチャ _スキルアイコン = null;

        private テクスチャ _達成率アイコン = null;

        private MusicNode _現在表示しているノード = null;

        private string _スキル値文字列 = null;

        private string _達成率文字列 = null;
    }
}
