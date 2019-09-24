using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;  
using SharpDX.Direct2D1;
using SharpDX.Animation;
using FDK;

namespace DTXMania.結果
{
    partial class 難易度 : IDisposable
    {

        // 生成と終了


        public 難易度()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._アイコン = new 難易度.アイコン( DXResources.Instance.Animation );
                this._下線 = new 難易度.下線( DXResources.Instance.Animation );
                this._数値 = new 難易度.数値( DXResources.Instance.Animation );

                this._初めての進行描画 = true;
            }
        }

        public virtual void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this._アイコン?.Dispose();
                this._下線?.Dispose();
                this._数値?.Dispose();
            }
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc, float left, float top, double 難易度 )
        {
            if( this._初めての進行描画 )
            {
                // アニメーション開始
                this._アイコン.開始する();
                this._下線.開始する();
                this._数値.開始する( 難易度 );

                this._初めての進行描画 = false;
            }

            // アイコン
            this._アイコン.進行描画する( dc, left, top );

            // 数値
            this._数値.進行描画する( dc, left + 221f, top + 3f ); 

            // 下線
            this._下線.進行描画する( dc, left + 33f, top + 113f );
        }



        // ローカル


        private const double 最初の待機時間sec = 0.5;

        private const double アニメ時間sec = 0.5;

        private bool _初めての進行描画 = false;

        private 難易度.アイコン _アイコン;

        private 難易度.下線 _下線;

        private 難易度.数値 _数値;
    }
}
