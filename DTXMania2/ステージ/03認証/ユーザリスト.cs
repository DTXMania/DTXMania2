﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.認証
{
    /// <summary>
    ///     ユーザリストパネルの表示とユーザの選択。
    /// </summary>
    /// <remarks>
    ///     ユーザリストは <see cref="App.ユーザリスト"/> が保持している。
    /// </remarks>
    class ユーザリスト : IDisposable
    {

        // プロパティ


        /// <summary>
        ///		現在選択中のユーザ。
        ///		<see cref="App.ユーザリスト"/> のインデックス番号。（0～）
        /// </summary>
        public int 選択中のユーザ { get; protected set; } = 0;



        // 生成と終了


        public ユーザリスト()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ユーザパネル = new 画像( @"$(Images)\AuthStage\UserPanel.png" );
            this._ユーザパネル光彩付き = new 画像( @"$(Images)\AuthStage\UserPanelWithFrame.png" );
            this._ユーザ肩書きパネル = new 画像( @"$(Images)\AuthStage\UserSubPanel.png" );
            this._ユーザ名 = new 文字列画像D2D( Global.DWriteFactory, Global.D2D1Factory1, Global.既定のD2D1DeviceContext, Global.設計画面サイズ ) {
                表示文字列 = "",
                フォントサイズpt = 46f,
                描画効果 = 文字列画像D2D.効果.縁取り,
                縁のサイズdpx = 6f,
                前景色 = Color4.Black,
                背景色 = Color4.White,
            };

            this._光彩アニメーションを開始する();
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._ユーザ名.Dispose();
            this._ユーザ肩書きパネル.Dispose();
            this._ユーザパネル光彩付き.Dispose();
            this._ユーザパネル.Dispose();
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc )
        {
            var 描画位置 = new Vector2( 569f, 188f );

            const float リストの改行幅 = 160f;


            // 選択中のパネルの光彩アニメーションの進行。

            float 不透明度 = 0f;

            var 割合 = this._光彩アニメカウンタ.現在値の割合;

            if( 0.5f > 割合 )
            {
                不透明度 = 1.0f - ( 割合 * 2.0f );     // 1→0
            }
            else
            {
                不透明度 = ( 割合 - 0.5f ) * 2.0f;     // 0→1
            }


            // ユーザリストを描画する。

            int 表示人数 = Math.Min( 5, Global.App.ユーザリスト.Count );   // HACK: 現状は最大５人までとする。

            for( int i = 0; i < 表示人数; i++ )
            {
                var user = Global.App.ユーザリスト[ i ];

                if( i == this.選択中のユーザ )
                    this._ユーザパネル光彩付き.描画する( 描画位置.X, 描画位置.Y + リストの改行幅 * i, 不透明度0to1: 不透明度 );

                this._ユーザパネル.描画する( 描画位置.X, 描画位置.Y + リストの改行幅 * i );

                this._ユーザ名.表示文字列 = user.名前;
                this._ユーザ名.描画する( dc, 描画位置.X + 32f, 描画位置.Y + 40f + リストの改行幅 * i );

                this._ユーザ肩書きパネル.描画する( 描画位置.X, 描画位置.Y + リストの改行幅 * i );
            }
        }



        // ユーザ選択


        /// <summary>
        ///     ユーザリスト上で、選択されているユーザのひとつ前のユーザを選択する。
        /// </summary>
        public void 前のユーザを選択する()
        {
            this.選択中のユーザ = ( this.選択中のユーザ - 1 + Global.App.ユーザリスト.Count ) % Global.App.ユーザリスト.Count;  // 前がないなら末尾へ

            this._光彩アニメーションを開始する();
        }

        /// <summary>
        ///     ユーザリスト上で、選択されているユーザのひとつ前のユーザを選択する。
        /// </summary>
        public void 次のユーザを選択する()
        {
            this.選択中のユーザ = ( this.選択中のユーザ + 1 ) % Global.App.ユーザリスト.Count;   // 次がないなら先頭へ

            this._光彩アニメーションを開始する();
        }



        // ローカル


        private readonly 画像 _ユーザパネル;

        private readonly 画像 _ユーザパネル光彩付き;

        private readonly 画像 _ユーザ肩書きパネル;

        private readonly 文字列画像D2D _ユーザ名;

        private LoopCounter _光彩アニメカウンタ = null!;


        private void _光彩アニメーションを開始する()
        {
            this._光彩アニメカウンタ = new LoopCounter( 0, 200, 5 );
        }
    }
}
