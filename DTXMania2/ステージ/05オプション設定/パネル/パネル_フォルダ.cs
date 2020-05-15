using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX.Direct2D1;
using FDK;

namespace DTXMania2.オプション設定
{
    /// <summary>
    ///		子パネルリストを持つフォルダ。
    ///		子パネルリストの活性化・非活性化はこのクラスで行う。
    /// </summary>
    class パネル_フォルダ : パネル
    {

        // プロパティ


        /// <summary>
        ///		null ならルート階層。
        /// </summary>
        public パネル_フォルダ? 親パネル { get; protected set; } = null;

        public SelectableList<パネル> 子パネルリスト
        {
            get
                => this._子パネルリスト;
            set
            {
                this._子パネルリスト = value;
                this._子パネルリスト.SelectFirst();
            }
        }



        // 生成と終了


        public パネル_フォルダ( string パネル名, パネル_フォルダ? 親パネル, IEnumerable<パネル>? 初期子パネルリスト = null, Action<パネル>? 値の変更処理 = null )
            : base( パネル名, 値の変更処理, ヘッダ色種別.赤 )
        {
            this.親パネル = 親パネル;
            this.子パネルリスト = new SelectableList<パネル>();

            if( null != 初期子パネルリスト )
            {
                foreach( var panel in 初期子パネルリスト )
                    this.子パネルリスト.Add( panel );

                this._子パネルリスト.SelectFirst();
            }
        }

        public override void Dispose()
        {
            base.Dispose(); // 忘れずに
        }



        // 進行と描画


        public override void 進行描画する( DeviceContext dc, float left, float top, bool 選択中 )
        {
            // パネルの下地と名前を描画。
            base.進行描画する( dc, left, top, 選択中 );

            // その他の描画があれば、ここに記述する。
        }



        // ローカル


        protected SelectableList<パネル> _子パネルリスト = null!;
    }
}
