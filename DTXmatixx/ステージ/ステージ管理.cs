﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FDK;
using FDK.メディア;

namespace DTXmatixx.ステージ
{
	class ステージ管理 : Activity, IDisposable
	{
		public string 最初のステージ名
		{
			get
				=> this.ステージリスト.ElementAt( 0 ).Value.GetType().Name;
		}

		public ステージ 現在のステージ
		{
			get
				=> this._現在のステージ;
		}

		/// <summary>
		///		全ステージのリスト。
		///		新しいステージができたら、ここに追加すること。
		/// </summary>
		public Dictionary<string, ステージ> ステージリスト = new Dictionary<string, ステージ>() {
			{ nameof( 曲ツリー構築.曲ツリー構築ステージ ), new 曲ツリー構築.曲ツリー構築ステージ() },
			{ nameof( タイトル.タイトルステージ ), new タイトル.タイトルステージ() },
			{ nameof( 認証.認証ステージ ), new 認証.認証ステージ() },
			{ nameof( 選曲.選曲ステージ ), new 選曲.選曲ステージ() },
			{ nameof( 曲読み込み.曲読み込みステージ ), new 曲読み込み.曲読み込みステージ() },
		};

		// 全ステージで共通のアイキャッチインスタンス。ステージ間をまたいで描画することができる。
		public アイキャッチ.シャッター シャッター
		{
			get;
			protected set;
		} = null;
		public アイキャッチ.回転幕 回転幕
		{
			get;
			protected set;
		} = null;
		public アイキャッチ.GO GO
		{
			get;
			protected set;
		} = null;

		public ステージ管理()
		{
			this.子リスト.Add( this.シャッター = new アイキャッチ.シャッター() );
			this.子リスト.Add( this.回転幕 = new アイキャッチ.回転幕() );
			this.子リスト.Add( this.GO = new アイキャッチ.GO() );
		}
		protected override void On活性化( グラフィックデバイス gd )
		{
			using( Log.Block( FDKUtilities.現在のメソッド名 ) )
			{
				if( this.現在のステージ?.活性化していない ?? false )
				{
					this.現在のステージ?.活性化する( gd );
				}
			}
		}
		protected override void On非活性化( グラフィックデバイス gd )
		{
			using( Log.Block( FDKUtilities.現在のメソッド名 ) )
			{
				if( this.現在のステージ?.活性化している ?? false )
				{
					this.現在のステージ?.非活性化する( gd );
				}
			}
		}
		public void Dispose()
		{
			throw new InvalidOperationException( "このメソッドは使用できません。別のオーバーロードメソッドを使用してください。" );
		}
		public void Dispose( グラフィックデバイス gd )
		{
			Debug.Assert( null != gd );

			// 現在活性化しているステージがあれば、すべて非活性化する。
			foreach( var kvp in this.ステージリスト )
			{
				if( kvp.Value.活性化している )
				{
					kvp.Value.非活性化する( gd );
				}
			}
		}

		/// <summary>
		///		現在のステージを非活性化し、指定されたステージに遷移して、活性化する。
		/// </summary>
		/// <param name="遷移先ステージ名">Nullまたは空文字列なら、非活性化のみ行う。</param>
		public void ステージを遷移する( グラフィックデバイス gd, string 遷移先ステージ名 )
		{
			Log.Header( $"{遷移先ステージ名} へ遷移します。" );

			using( Log.Block( FDKUtilities.現在のメソッド名 ) )
			{
				if( null != this._現在のステージ &&
					this._現在のステージ.活性化している )
				{
					this._現在のステージ.非活性化する( gd );
				}

				if( 遷移先ステージ名.Nullでも空でもない() )
				{
					this._現在のステージ = this.ステージリスト[ 遷移先ステージ名 ];
					this._現在のステージ.活性化する( gd );

					//App.入力管理.すべての入力デバイスをポーリングする();
				}
				else
				{
					Log.Header( "ステージの遷移を終了します。" );
					this._現在のステージ = null;
				}
			}
		}


		/// <summary>
		///		現在実行中のステージ。<see cref="ステージリスト"/> の中のひとつを参照している（ので、うかつにDisposeとかしたりしないこと）。
		/// </summary>
		private ステージ _現在のステージ;
	}
}
