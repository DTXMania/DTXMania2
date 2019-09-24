using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Animation;
using FDK;

namespace DTXMania.選曲
{
    class 表示方法選択パネル : IDisposable
    {
        public enum 表示方法
        {
            全曲,
            評価順,
        }

        public 表示方法 現在の表示方法 { get; protected set; }



        // 生成と終了


        public 表示方法選択パネル()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                this.現在の表示方法 = 表示方法.全曲;

                this._表示開始位置 = this._指定した表示方法が選択位置に来る場合の表示開始位置を返す( this.現在の表示方法 );

                foreach( var p in this._パネルs )
                    p.画像 = new テクスチャ( p.vpath );

                var animation = DXResources.Instance.Animation;
                this._横方向差分割合 = new Variable( animation.Manager, initialValue: 0.0 );
                this._横方向差分移動ストーリーボード = new Storyboard( animation.Manager );
                using( var 維持 = animation.TrasitionLibrary.Constant( 0.0 ) )
                {
                    this._横方向差分移動ストーリーボード.AddTransition( this._横方向差分割合, 維持 );
                }
                this._横方向差分移動ストーリーボード.Schedule( animation.Timer.Time );
            }
        }

        public virtual void Dispose()
        {
            using( Log.Block( FDKUtilities.現在のメソッド名 ) )
            {
                foreach( var p in this._パネルs )
                    p.画像?.Dispose();

                this._横方向差分移動ストーリーボード?.Abandon();
                this._横方向差分移動ストーリーボード?.Dispose();
                this._横方向差分割合?.Dispose();
            }
        }



        // 進行と描画


        public void 進行描画する( DeviceContext dc )
        {
            // パネルを合計８枚表示する。（左隠れ１枚 ＋ 表示６枚 ＋ 右隠れ１枚）

            int 表示元の位置 = this._表示開始位置;

            for( int i = 0; i < 8; i++ )
            {
                var 画像 = this._パネルs[ 表示元の位置 ].画像;

                画像.描画する(
                    (float) ( ( 768f + this._横方向差分割合.Value * 144f ) + 144f * i ),
                    ( 3 == i ) ? 100f : 54f ); // i==3 が現在の選択パネル

                表示元の位置 = ( 表示元の位置 + 1 ) % this._パネルs.Count;
            }
        }



        // パネルの選択


        public void 次のパネルを選択する()
        {
            // todo: 実装する
        }

        public void 前のパネルを選択する()
        {
            // todo: 実装する
        }


        private class Panel
        {
            public 表示方法 表示方法;
            public VariablePath vpath;
            public テクスチャ 画像;
        };
        private List<Panel> _パネルs = new List<Panel>() {
            new Panel { 表示方法 = 表示方法.全曲, vpath = @"$(System)images\選曲\表示方法・全曲.png", 画像 = null },
            new Panel { 表示方法 = 表示方法.評価順, vpath = @"$(System)images\選曲\表示方法・評価順.png", 画像 = null },
        };

        /// <summary>
        ///     表示パネルの横方向差分の割合。
        ///     左:-1.0 ～ 中央:0.0 ～ +1.0:右。
        /// </summary>
        private Variable _横方向差分割合 = null;

        private Storyboard _横方向差分移動ストーリーボード = null;

        /// <summary>
        ///     左隠れパネルの <see cref="_パネルs"/>[] インデックス番号。
        ///     0 ～ <see cref="_パネルs"/>.Count-1。
        /// </summary>
        private int _表示開始位置 = 0;


        private int _指定した表示方法が選択位置に来る場合の表示開始位置を返す( 表示方法 表示方法 )
        {
            int n = this._パネルs.FindIndex( ( p ) => ( p.表示方法 == this.現在の表示方法 ) );
            return ( ( n - 3 ) % this._パネルs.Count + this._パネルs.Count );
        }
    }
}
