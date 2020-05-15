using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.結果
{
    partial class 達成率 : 達成率Base
    {

        // プロパティ


        public override bool アニメ完了 => this._アイコン.アニメ完了 && this._下線.アニメ完了 && this._数値.アニメ完了;



        // 生成と終了


        public 達成率()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._アイコン = new アイコン();
            this._下線 = new 下線();
            this._数値 = new 数値();

            this._初めての進行描画 = true;
        }

        public override void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._数値.Dispose();
            this._下線.Dispose();
            this._アイコン.Dispose();
        }



        // 進行と描画


        public override void 進行描画する( DeviceContext dc, float left, float top, double 達成率0to100 )
        {
            if( this._初めての進行描画 )
            {
                // アニメーション開始
                this._アイコン.開始する();
                this._下線.開始する();
                this._数値.開始する( 達成率0to100 );

                this._初めての進行描画 = false;
            }

            this._アイコン.進行描画する( dc, left, top );
            this._数値.進行描画する( dc, left + 150f, top + 48f );
            this._下線.進行描画する( dc, left + 33f, top + 198f );
        }

        public override void アニメを完了する()
        {
            this._アイコン.アニメを完了する();
            this._下線.アニメを完了する();
            this._数値.アニメを完了する();
        }



        // ローカル


        private bool _初めての進行描画;

        private const double 最初の待機時間sec = 1.0;

        private const double アニメ時間sec = 0.25;

        private readonly 達成率.アイコン _アイコン;

        private readonly 達成率.下線 _下線;

        private readonly 達成率.数値 _数値;
    }
}
