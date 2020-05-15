using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.演奏
{
    class 判定パラメータ表示 : IDisposable
    {

        // 生成と終了


        public 判定パラメータ表示()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this.パラメータ文字 = new フォント画像D2D( @"$(Images)\ParameterFont_Small.png", @"$(Images)\ParameterFont_Small.yaml" );
            this._判定種別文字 = new 画像D2D( @"$(Images)\PlayStage\JudgeLabelForParameter.png" );
            this.判定種別文字の矩形リスト = new 矩形リスト( @"$(Images)\PlayStage\JudgeLabelForParameter.yaml" );
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._判定種別文字.Dispose();
            this.パラメータ文字.Dispose();
        }



        // 進行と描画


        public virtual void 描画する( DeviceContext dc, float x, float y, 成績 現在の成績 )
        {
            var scaling = new Size2F( 1.0f, 1.4f );

            var 判定別ヒット数 = 現在の成績.判定別ヒット数;
            var 割合表 = 現在の成績.判定別ヒット割合;
            int MaxCombo = 現在の成績.MaxCombo;
            int 合計 = 0;

            this.パラメータを一行描画する( dc, x, y, scaling, 判定種別.PERFECT, 判定別ヒット数[ 判定種別.PERFECT ], 割合表[ 判定種別.PERFECT ] );

            合計 += 判定別ヒット数[ 判定種別.PERFECT ];
            y += _改行幅dpx;

            this.パラメータを一行描画する( dc, x, y, scaling, 判定種別.GREAT, 判定別ヒット数[ 判定種別.GREAT ], 割合表[ 判定種別.GREAT ] );

            合計 += 判定別ヒット数[ 判定種別.GREAT ];
            y += _改行幅dpx;

            this.パラメータを一行描画する( dc, x, y, scaling, 判定種別.GOOD, 判定別ヒット数[ 判定種別.GOOD ], 割合表[ 判定種別.GOOD ] );

            合計 += 判定別ヒット数[ 判定種別.GOOD ];
            y += _改行幅dpx;

            this.パラメータを一行描画する( dc, x, y, scaling, 判定種別.OK, 判定別ヒット数[ 判定種別.OK ], 割合表[ 判定種別.OK ] );

            合計 += 判定別ヒット数[ 判定種別.OK ];
            y += _改行幅dpx;

            this.パラメータを一行描画する( dc, x, y, scaling, 判定種別.MISS, 判定別ヒット数[ 判定種別.MISS ], 割合表[ 判定種別.MISS ] );

            合計 += 判定別ヒット数[ 判定種別.MISS ];
            y += _改行幅dpx;


            y += 3f;    // ちょっと間を開けて


            var 矩形 = this.判定種別文字の矩形リスト[ "MaxCombo" ]!;

            this._判定種別文字.描画する( dc, x, y, 転送元矩形: 矩形, X方向拡大率: scaling.Width, Y方向拡大率: scaling.Height );

            x += 矩形.Value.Width + 16f;

            this.数値を描画する( dc, x, y, scaling, MaxCombo, 桁数: 4 );
            this.数値を描画する( dc, x + _dr, y, scaling, (int) Math.Floor( 100.0 * MaxCombo / 合計 ), 桁数: 3 );    // 切り捨てでいいやもう
            this.パラメータ文字.描画する( dc, x + _dp, y, "%", scaling );
        }



        // ローカル


        protected const float _dr = 78f;       // 割合(%)までのXオフセット[dpx]

        protected const float _dp = 131f;      // "%" 文字までのXオフセット[dpx]

        protected const float _改行幅dpx = 40f;

        protected readonly フォント画像D2D パラメータ文字;

        protected readonly 画像D2D _判定種別文字;

        protected readonly 矩形リスト 判定種別文字の矩形リスト;

        protected void パラメータを一行描画する( DeviceContext dc, float x, float y, Size2F 拡大率, 判定種別 judge, int ヒット数, int ヒット割合, float 不透明度 = 1.0f )
        {
            var 矩形 = this.判定種別文字の矩形リスト[ judge.ToString() ]!;

            this._判定種別文字.描画する( dc, x, y - 4f, 不透明度, 転送元矩形: 矩形, X方向拡大率: 拡大率.Width, Y方向拡大率: 拡大率.Height );

            x += 矩形.Value.Width + 16f;

            this.数値を描画する( dc, x, y, 拡大率, ヒット数, 4, 不透明度 );
            this.数値を描画する( dc, x + _dr * 拡大率.Width, y, 拡大率, ヒット割合, 3, 不透明度 );
            this.パラメータ文字.不透明度 = 不透明度;
            this.パラメータ文字.描画する( dc, x + _dp * 拡大率.Width, y, "%", 拡大率 );
        }

        protected void 数値を描画する( DeviceContext dc, float x, float y, Size2F 拡大率, int 描画する数値, int 桁数, float 不透明度 = 1.0f )
        {
            Debug.Assert( 1 <= 桁数 && 10 >= 桁数 );    // 最大10桁まで

            int 最大値 = (int) Math.Pow( 10, 桁数 ) - 1;     // 1桁なら9, 2桁なら99, 3桁なら999, ... でカンスト。
            int 判定数 = Math.Clamp( 描画する数値, min: 0, max: 最大値 );   // 丸める。
            var 判定数文字列 = 判定数.ToString().PadLeft( 桁数 ).Replace( ' ', 'o' );  // グレーの '0' は 'o' で描画できる（矩形リスト参照）。

            this.パラメータ文字.不透明度 = 不透明度;
            this.パラメータ文字.描画する( dc, x, y, 判定数文字列, 拡大率 );
        }
    }
}
