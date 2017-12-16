﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpDX;
using FDK;
using FDK.メディア;

namespace DTXmatixx.ステージ.設定
{
	/// <summary>
	///		任意個の文字列から１つを選択できるパネル項目。
	///		コンストラクタから活性化までの間に、<see cref="選択肢リスト"/> を設定すること。
	/// </summary>
	class パネル_文字列リスト : パネル
	{
		public int 現在選択されている選択肢の番号
		{
			get;
			protected set;
		} = 0;
		public List<string> 選択肢リスト
		{
			get;
			protected set;
		} = new List<string>();

		public パネル_文字列リスト( string パネル名, int 初期選択肢番号 = 0, IEnumerable<string> 選択肢初期値s = null, Action<パネル> 値の変更処理 = null )
			: base( パネル名, 値の変更処理 )
		{
			this.現在選択されている選択肢の番号 = 初期選択肢番号;

			// 初期値があるなら設定する。
			if( null != 選択肢初期値s )
			{
				foreach( var item in 選択肢初期値s )
					this.選択肢リスト.Add( item );
			}
		}

		public override void 左移動キーが入力された()
		{
			this.現在選択されている選択肢の番号 = ( this.現在選択されている選択肢の番号 - 1 + this.選択肢リスト.Count ) % this.選択肢リスト.Count;
			this._値の変更処理?.Invoke( this );
		}
		public override void 右移動キーが入力された()
		{
			this.現在選択されている選択肢の番号 = ( this.現在選択されている選択肢の番号 + 1 ) % this.選択肢リスト.Count;
			this._値の変更処理?.Invoke( this );
		}
		public override void 確定キーが入力された()
			=> this.右移動キーが入力された();

		protected override void On活性化( グラフィックデバイス gd )
		{
			Trace.Assert( 0 < this.選択肢リスト.Count, "リストが空です。活性化するより先に設定してください。" );

			this._選択肢画像リスト = new Dictionary<string, 文字列画像>();

			for( int i = 0; i < this.選択肢リスト.Count; i++ )
			{
				var image = new 文字列画像() {
					表示文字列 = this.選択肢リスト[ i ],
					フォントサイズpt = 34f,
					前景色 = Color4.White,
				};

				this._選択肢画像リスト.Add( this.選択肢リスト[ i ], image );

				this.子リスト.Add( image );
			}
		}
		protected override void On非活性化( グラフィックデバイス gd )
		{
			foreach( var kvp in this._選択肢画像リスト )
				this.子リスト.Remove( kvp.Value );

			this._選択肢画像リスト = null;
		}

		public override void 進行描画する( グラフィックデバイス gd, float left, float top, bool 選択中 )
		{
			// パネルの共通部分を描画。
			base.進行描画する( gd, left, top, 選択中 );

			// 以下、項目部分の描画。

			var 項目矩形 = new RectangleF(
				x: this.項目領域.X + left,
				y: this.項目領域.Y + top,
				width: this.項目領域.Width,
				height: this.項目領域.Height );

			var 項目画像 = this._選択肢画像リスト[ this.選択肢リスト[ this.現在選択されている選択肢の番号 ] ];

			float 拡大X = Math.Min( 1f, ( 項目矩形.Width - 20f ) / 項目画像.サイズ.Width );    // -20 は左右マージンの最低値[dpx]

			項目画像.描画する(
				gd,
				項目矩形.Left + ( 項目矩形.Width - 項目画像.サイズ.Width * 拡大X ) / 2f,
				項目矩形.Top + ( 項目矩形.Height - 項目画像.サイズ.Height ) / 2f,
				X方向拡大率: 拡大X );
		}

		private Dictionary<string, 文字列画像> _選択肢画像リスト = null;
	}
}
