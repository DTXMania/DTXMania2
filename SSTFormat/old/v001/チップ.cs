using System;
using System.Collections.Generic;

namespace SSTFormat.v001
{
    public class チップ : IComparable
    {
        public const int 最大音量 = 4;

        #region " プロパティ(1)（増減したら CopyFrom() を修正のこと）"
        //----------------
        public チップ種別 チップ種別 { get; set; } = チップ種別.Unknown;

        public int 小節番号 { get; set; } = -1;

        public int 小節内位置 { get; set; } = 0;

        public int 小節解像度 { get; set; } = 1;

        /// <summary>
        ///		チップの描画時刻[ms]。
        ///		譜面の先頭（小節番号 -1 の小節の先頭）からの時刻をミリ秒単位で表す。
        ///		未使用なら -1 。
        /// </summary>
        public long 描画時刻ms { get; set; } = -1;

        /// <summary>
        ///		チップの描画時刻[sec]。
        ///		譜面の先頭（小節番号 -1 の小節の先頭）からの時刻を秒単位で表す。
        /// </summary>
        public double 描画時刻sec => ( this.描画時刻ms / 1000.0 );

        /// <summary>
        ///		チップの発声時刻[ms]。
        ///		譜面の先頭（小節番号 -1 の小節の先頭）からの時刻をミリ秒単位で表す。
        ///		未使用なら -1 。
        /// </summary>
        /// <remarks>
        ///		サウンドの発声遅延を考慮して、描画時刻よりも遅く設定すること。
        /// </remarks>
        public long 発声時刻ms { get; set; } = -1;

        /// <summary>
        ///		チップの発声時刻[sec]。
        ///		譜面の先頭（小節番号 -1 の小節の先頭）からの時刻を秒単位で表す。
        /// </summary>
        /// <remarks>
        ///		サウンドの発声遅延を考慮して、描画時刻よりも遅く設定すること。
        /// </remarks>
        public double 発声時刻sec => ( this.発声時刻ms / 1000.0 );

        /// <summary>
        ///		チップの音量（小:1～4:大）。
        /// </summary>
        public int 音量
        {
            get
            {
                return this._音量;
            }
            set
            {
                if( ( 1 > value ) || ( チップ.最大音量 < value ) )
                    throw new ArgumentException( $"音量の値域(1～{チップ.最大音量})を超える値 '{value}' が指定されました。" );

                this._音量 = value;
            }
        }

        /// <summary>
        ///		チップが BPM チップである場合は、その BPM 値。
        ///		それ以外の場合は無効。
        /// </summary>
        public double BPM { get; set; } = 120.0;

        //----------------
        #endregion

        #region " プロパティ(2) 演奏用（増減したら CopyFrom() を修正のこと）"
        //----------------
        public bool 可視 { get; set; } = true;

        public bool 不可視
        {
            get { return !this.可視; }
            set { this.可視 = !value; }
        }

        public bool 可視の初期値
        {
            get
            {
                return (
                    // ↓これらは不可視。
                    ( this.チップ種別 == チップ種別.BPM ) ||
                    ( this.チップ種別 == チップ種別.背景動画 ) ||
                    ( this.チップ種別 == チップ種別.小節メモ ) ||
                    ( this.チップ種別 == チップ種別.Unknown )
                    ) ? false : true;
            }
        }

        public bool ヒット済みである { get; set; } = false;

        public bool ヒットされていない
        {
            get { return !this.ヒット済みである; }
            set { this.ヒット済みである = !value; }
        }

        public bool 発声済みである { get; set; } = false;

        public bool 発声されていない
        {
            get { return !this.発声済みである; }
            set { this.発声済みである = !value; }
        }
        //----------------
        #endregion

        #region " プロパティ(3) SSTFEditor用（増減したら CopyFrom() を修正のこと）"
        //----------------
        public int 譜面内絶対位置grid { get; set; } = 0;

        public bool ドラッグ操作により選択中である { get; set; } = false;

        public bool 選択が確定している { get; set; } = false;

        public bool 選択が確定していない
        {
            get { return !this.選択が確定している; }
            set { this.選択が確定している = !value; }
        }

        public bool 移動済みである { get; set; } = true;

        public bool 移動されていない
        {
            get { return !this.移動済みである; }
            set { this.移動済みである = !value; }
        }

        public string チップ内文字列 { get; set; } = null;

        public int 枠外レーン数 { get; set; } = 0;
        //----------------
        #endregion

        public チップ()
        {
        }

        public チップ( チップ コピー元チップ )
        {
            this.CopyFrom( コピー元チップ );
        }

        public void CopyFrom( チップ srcChip )
        {
            // プロパティ(1)
            this.チップ種別 = srcChip.チップ種別;
            this.小節番号 = srcChip.小節番号;
            this.小節内位置 = srcChip.小節内位置;
            this.小節解像度 = srcChip.小節解像度;
            this.描画時刻ms = srcChip.描画時刻ms;
            this.発声時刻ms = srcChip.発声時刻ms;
            this.音量 = srcChip.音量;
            this.BPM = srcChip.BPM;

            // プロパティ(2)
            this.可視 = srcChip.可視;
            this.ヒット済みである = srcChip.ヒット済みである;
            this.発声済みである = srcChip.発声済みである;

            // プロパティ(3)
            this.譜面内絶対位置grid = srcChip.譜面内絶対位置grid;
            this.ドラッグ操作により選択中である = srcChip.ドラッグ操作により選択中である;
            this.選択が確定している = srcChip.選択が確定している;
            this.移動済みである = srcChip.移動済みである;
            this.チップ内文字列 = srcChip.チップ内文字列;
            this.枠外レーン数 = srcChip.枠外レーン数;
        }

        public void ヒット前の状態にする()
        {
            // 演奏用プロパティについて設定する。

            this.可視 = this.可視の初期値;
            this.ヒット済みである = false;
            this.発声済みである = false;
        }

        public void ヒット済みの状態にする()
        {
            // 演奏用プロパティについて設定する。

            this.可視 = false;
            this.ヒット済みである = true;
            this.発声済みである = true;
        }

        #region " IComparable 実装 "
        //-----------------
        // 概要:
        //     現在のインスタンスを同じ型の別のオブジェクトと比較して、並べ替え順序において、現在のインスタンスの位置が同じ型の別のオブジェクトの前、後ろ、または同じのいずれであるかを示す整数を返します。
        //
        // パラメータ:
        //   obj:
        //     このインスタンスと比較するオブジェクト。
        //
        // 戻り値:
        //     比較対象オブジェクトの相対順序を示す 32 ビット符号付き整数。戻り値の意味は次のとおりです。 
        //
        //     値		説明
        //     --------------------
        //     負数		this ＜ obj
        //     0		this ＝ obj
        //     正数		this ＞ obj
        //
        // 例外:
        //   System.ArgumentException:
        //     obj の型がこのインスタンスの型と異なります。
        //
        public int CompareTo( object obj )
        {
            var other = obj as チップ;

            if( this.小節番号 < other.小節番号 ) { return -1; }
            if( this.小節番号 > other.小節番号 ) { return +1; }

            double dbThis = (double) this.小節内位置 / (double) this.小節解像度;
            double dbOther = (double) other.小節内位置 / (double) other.小節解像度;

            if( dbThis < dbOther ) { return -1; }
            if( dbThis > dbOther ) { return +1; }


            // グリッドが完全に等しいなら、チップの種類ごとに定義された深度で順序を決める。

            if( チップ.チップの深さ[ this.チップ種別 ] > チップ.チップの深さ[ other.チップ種別 ] ) { return -1; }
            if( チップ.チップの深さ[ this.チップ種別 ] < チップ.チップの深さ[ other.チップ種別 ] ) { return +1; }

            return 0;
        }
        //-----------------
        #endregion

        protected readonly static Dictionary<チップ種別, int> チップの深さ = new Dictionary<チップ種別, int>() {
            #region " *** "
            //-----------------
            { チップ種別.Ride_Cup, 50 },
            { チップ種別.HiHat_Open, 50 },
            { チップ種別.HiHat_HalfOpen, 50 },
            { チップ種別.HiHat_Close, 50 },
            { チップ種別.HiHat_Foot, 50 },
            { チップ種別.Snare, 50 },
            { チップ種別.Snare_OpenRim, 50 },
            { チップ種別.Snare_ClosedRim, 50 },
            { チップ種別.Snare_Ghost, 50 },
            { チップ種別.Tom1, 50 },
            { チップ種別.Tom1_Rim, 50 },
            { チップ種別.BPM, 50 },
            { チップ種別.Ride, 60 },
            { チップ種別.Splash, 60 },
            { チップ種別.Tom2, 60 },
            { チップ種別.Tom2_Rim, 60 },
            { チップ種別.LeftCrash, 70 },
            { チップ種別.China, 70 },
            { チップ種別.Tom3, 70 },
            { チップ種別.Tom3_Rim, 70 },
            { チップ種別.RightCrash, 70 },
            { チップ種別.Bass, 75 },
            { チップ種別.小節線, 80 },
            { チップ種別.拍線, 85 },
            { チップ種別.背景動画, 90 },
            { チップ種別.小節メモ, 99 },
            { チップ種別.Unknown, 99 },
            //-----------------
            #endregion
        };

        private int _音量 = チップ.最大音量;
    }
}
