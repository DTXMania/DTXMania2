using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;

namespace DTXMania2.結果
{
    partial class 曲別SKILL : IDisposable
    {

        // プロパティ


        public bool アニメ完了 => this._アイコン.アニメ完了 && this._下線.アニメ完了 && this._数値.アニメ完了;



        // 生成と終了


        public 曲別SKILL()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._アイコン = new アイコン();
            this._下線 = new 下線();
            this._数値 = new 数値();

            this._初めての進行描画 = true;
        }

        public virtual void Dispose()
        {
            using var _ = new LogBlock( Log.現在のメソッド名 );

            this._数値.Dispose();
            this._下線.Dispose();
            this._アイコン.Dispose();
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc, float left, float top, double スキル値 )
        {
            if( this._初めての進行描画 )
            {
                // アニメーション開始
                this._アイコン.開始する();
                this._下線.開始する();
                this._数値.開始する( スキル値 );

                this._初めての進行描画 = false;
            }

            this._アイコン.進行描画する( left, top );
            this._数値.進行描画する( left + 180f, top + 3f );
            this._下線.進行描画する( dc, left + 33f, top + 113f );
        }

        public void アニメを完了する()
        {
            this._アイコン.アニメを完了する();
            this._下線.アニメを完了する();
            this._数値.アニメを完了する();
        }



        // ローカル


        private const double 最初の待機時間sec = 0.75;

        private const double アニメ時間sec = 0.5;

        private bool _初めての進行描画;

        private readonly 曲別SKILL.アイコン _アイコン;

        private readonly 曲別SKILL.下線 _下線;

        private readonly 曲別SKILL.数値 _数値;
    }
}
